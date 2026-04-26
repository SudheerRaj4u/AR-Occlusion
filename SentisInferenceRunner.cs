using System;
using System.Collections;
using Unity.InferenceEngine;
using UnityEngine;

/// <summary>
/// SentisInferenceRunner — Full Two-Stage MobileSAM Pipeline
/// ──────────────────────────────────────────────────────────
/// Compatible with Unity Sentis 2.0+ (package: com.unity.ai.inference v2.4+)
///
/// Key API changes from Sentis 1.x → 2.x applied here:
///   • TensorFloat  → Tensor<float>
///   • TensorInt    → Tensor<int>
///   • TensorFloat.AllocZeros(shape) → new Tensor<float>(shape, new float[n])
///   • PeekOutput() returns Tensor base class — cast to Tensor<float>
///
/// Pipeline:
///   Stage 1 — Encoder:  camera frame  → image_embeddings [1, 256, 64, 64]
///   Stage 2 — Decoder:  embeddings + full-frame box prompt → masks [1, 1, 256, 256]
///
/// Models (assign in Inspector):
///   • mobile_sam_image_encoder.onnx  (~28 MB)
///   • sam_mask_decoder_single.onnx   (~16.5 MB)
///   Both from: https://huggingface.co/Acly/MobileSAM
/// </summary>
public class SentisInferenceRunner : MonoBehaviour
{
    // ── Inspector — Models ─────────────────────────────────────────────────
    [Header("Models — assign BOTH from Assets/Models/")]
    [Tooltip("mobile_sam_image_encoder.onnx")]
    [SerializeField] private ModelAsset encoderModel;

    [Tooltip("sam_mask_decoder_single.onnx")]
    [SerializeField] private ModelAsset decoderModel;

    // ── Inspector — Dimensions ─────────────────────────────────────────────
    [Header("Encoder Input — must be 1024×1024 to match ONNX export")]
    [SerializeField] private int encoderInputWidth = 1024;
    [SerializeField] private int encoderInputHeight = 1024;

    [Header("Mask Output")]
    [SerializeField] private int maskWidth = 256;
    [SerializeField] private int maskHeight = 256;

    [Tooltip("Foreground threshold. Start at 0, raise if mask is noisy.")]
    [SerializeField][Range(0f, 1f)] public float maskThreshold = 0.0f;

    // ── Inspector — Tensor Names ───────────────────────────────────────────
    [Header("Tensor Names — defaults match Acly/MobileSAM ONNX files")]
    [SerializeField] private string encoderInputName = "image";
    [SerializeField] private string encoderOutputName = "image_embeddings";
    [SerializeField] private string decoderMaskOutput = "masks";

    // ── Private State ──────────────────────────────────────────────────────
    private Worker _encoderWorker;
    private Worker _decoderWorker;
    private Model _encoderRuntimeModel;
    private Model _decoderRuntimeModel;

    // Sentis 2.x: Tensor<float> replaces TensorFloat
    private Tensor<float> _encoderInputTensor;
    private Tensor<float> _pointCoords;
    private Tensor<float> _pointLabels;
    private Tensor<float> _maskInput;
    private Tensor<float> _hasMaskInput;
    private Tensor<float> _origImSize;

    private RenderTexture _maskRenderTexture;
    private bool _isRunning;
    private bool _ready;

    // ── Unity Lifecycle ────────────────────────────────────────────────────
    private void Start()
    {
        if (encoderModel == null || decoderModel == null)
        {
            Debug.LogError("[SentisInferenceRunner] Both encoder AND decoder ModelAssets must be assigned in the Inspector!");
            enabled = false;
            return;
        }

        // Load runtime models
        _encoderRuntimeModel = ModelLoader.Load(encoderModel);
        _decoderRuntimeModel = ModelLoader.Load(decoderModel);

        // GPU compute backend — best performance on Android.
        // Falls back to CPU if GPU is unavailable (CPU will be slow but won't crash).
        try
        {
            _encoderWorker = new Worker(_encoderRuntimeModel, BackendType.GPUCompute);
            _decoderWorker = new Worker(_decoderRuntimeModel, BackendType.GPUCompute);
            Debug.Log("[SentisInferenceRunner] Workers created on GPUCompute backend.");
        }
        catch (Exception)
        {
            Debug.LogWarning("[SentisInferenceRunner] GPUCompute unavailable — falling back to CPU.");
            _encoderWorker = new Worker(_encoderRuntimeModel, BackendType.CPU);
            _decoderWorker = new Worker(_decoderRuntimeModel, BackendType.CPU);
        }

        // Pre-allocate encoder input tensor [1, 3, H, W] — overwritten each frame
        int encoderSize = 1 * 3 * encoderInputHeight * encoderInputWidth;
        _encoderInputTensor = new Tensor<float>(
            new TensorShape(1, 3, encoderInputHeight, encoderInputWidth),
            new float[encoderSize]);

        // Pre-allocate constant decoder prompt tensors (allocated once, reused every frame)
        AllocateDecoderPrompts();

        // Binary mask output RenderTexture — single-channel R8
        _maskRenderTexture = new RenderTexture(maskWidth, maskHeight, 0, RenderTextureFormat.R8);
        _maskRenderTexture.enableRandomWrite = true;
        _maskRenderTexture.Create();

        _ready = true;
        Debug.Log("[SentisInferenceRunner] MobileSAM two-stage pipeline ready.");
    }

    private void OnDestroy()
    {
        _encoderWorker?.Dispose();
        _decoderWorker?.Dispose();
        _encoderInputTensor?.Dispose();
        _pointCoords?.Dispose();
        _pointLabels?.Dispose();
        _maskInput?.Dispose();
        _hasMaskInput?.Dispose();
        _origImSize?.Dispose();
        if (_maskRenderTexture != null) _maskRenderTexture.Release();
    }

    // ── Public API ─────────────────────────────────────────────────────────

    public void RunInference(Texture2D cameraFrame, Action<RenderTexture> onComplete)
    {
        if (!_ready || _isRunning) return;
        StartCoroutine(TwoStagePipeline(cameraFrame, onComplete));
    }

    // ── Pipeline ───────────────────────────────────────────────────────────

    private IEnumerator TwoStagePipeline(Texture2D cameraFrame,
                                          Action<RenderTexture> onComplete)
    {
        _isRunning = true;

        // ── STAGE 1: Image Encoder ─────────────────────────────────────────
        try
        {
            // Write camera frame pixels into the pre-allocated input tensor
            TextureConverter.ToTensor(cameraFrame, _encoderInputTensor,
                new TextureTransform()
                    .SetTensorLayout(TensorLayout.NCHW));
        }
        catch (Exception e)
        {
            Debug.LogError($"[SentisInferenceRunner] Encoder — TextureConverter.ToTensor failed: {e.Message}");
            _isRunning = false;
            yield break;
        }

        _encoderWorker.SetInput(encoderInputName, _encoderInputTensor);
        _encoderWorker.Schedule();
        yield return null; // one frame — GPU executes encoder

        Tensor<float> embeddings = null;
        try
        {
            // PeekOutput returns base Tensor — cast to Tensor<float>
            embeddings = _encoderWorker.PeekOutput(encoderOutputName) as Tensor<float>;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SentisInferenceRunner] Encoder PeekOutput('{encoderOutputName}') failed: {e.Message}");
            _isRunning = false;
            yield break;
        }

        if (embeddings == null)
        {
            Debug.LogWarning("[SentisInferenceRunner] Encoder output is null — check encoderOutputName in Inspector.");
            _isRunning = false;
            yield break;
        }

        // ── STAGE 2: Mask Decoder ──────────────────────────────────────────
        try
        {
            _decoderWorker.SetInput("image_embeddings", embeddings);
            _decoderWorker.SetInput("point_coords", _pointCoords);
            _decoderWorker.SetInput("point_labels", _pointLabels);
            _decoderWorker.SetInput("mask_input", _maskInput);
            _decoderWorker.SetInput("has_mask_input", _hasMaskInput);
            _decoderWorker.SetInput("orig_im_size", _origImSize);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SentisInferenceRunner] Decoder SetInput failed: {e.Message}\n" +
                           "Ensure both ONNX files are from https://huggingface.co/Acly/MobileSAM");
            _isRunning = false;
            yield break;
        }

        _decoderWorker.Schedule();
        yield return null; // one frame — GPU executes decoder

        Tensor<float> maskTensor = null;
        try
        {
            maskTensor = _decoderWorker.PeekOutput(decoderMaskOutput) as Tensor<float>;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SentisInferenceRunner] Decoder PeekOutput('{decoderMaskOutput}') failed: {e.Message}");
            _isRunning = false;
            yield break;
        }

        if (maskTensor == null)
        {
            Debug.LogWarning("[SentisInferenceRunner] Decoder mask output is null — check decoderMaskOutput name.");
            _isRunning = false;
            yield break;
        }

        // ── OUTPUT ─────────────────────────────────────────────────────────
        // DEBUG: sample the mask tensor to confirm inference is producing non-zero values.
        // ReadbackAndClone() downloads the GPU tensor to a CPU-readable copy (Sentis 2.x API).
        // MakeReadable() was removed in Unity Inference Engine 2.x — use ReadbackAndClone() instead.
        try
        {
            using var cpuMask = maskTensor.ReadbackAndClone();
            float maxSample = 0f;
            // Sample a 5x5 grid across the mask to find max activation
            for (int r = 0; r < 5; r++)
                for (int c = 0; c < 5; c++)
                    maxSample = Mathf.Max(maxSample, cpuMask[0, 0,
                        (int)(r * (maskHeight - 1) / 4f),
                        (int)(c * (maskWidth - 1) / 4f)]);
            Debug.Log($"[SentisInferenceRunner] Mask max5x5grid={maxSample:F3} " +
                      "(if ~0, inference is producing no segmentation — check tensor names)");
        }
        catch (Exception dbgEx)
        {
            Debug.LogWarning($"[SentisInferenceRunner] Debug sampling failed: {dbgEx.Message}");
        }

        BlitMaskToRenderTexture(maskTensor);
        onComplete?.Invoke(_maskRenderTexture);
        _isRunning = false;
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    /// <summary>
    /// Allocates all constant decoder input tensors once at startup.
    /// Uses a full-frame bounding-box prompt so the decoder segments the
    /// most prominent foreground object (i.e. the person) in the whole frame.
    /// </summary>
    private void AllocateDecoderPrompts()
    {
        // Full-frame bounding box prompt (top-left corner + bottom-right corner).
        // SAM label 2 = bounding box corner point.
        // Using a full-frame box tells the decoder to segment the most prominent
        // foreground object ANYWHERE in the frame — hand, arm, body, etc.
        // This is far more robust than a single center point.
        float W = encoderInputWidth;   // 1024
        float H = encoderInputHeight;  // 1024

        // Two points: top-left (0,0) and bottom-right (W,H), both label 2 (box corners)
        _pointCoords = new Tensor<float>(
            new TensorShape(1, 2, 2),
            new float[]
            {
                0f, 0f,     // top-left corner of bounding box
                W,  H       // bottom-right corner of bounding box
            });

        // Label 2 = box corner (SAM convention for bounding box prompts)
        _pointLabels = new Tensor<float>(
            new TensorShape(1, 2),
            new float[] { 2f, 3f }); // 2 = box top-left, 3 = box bottom-right

        // No prior mask — all zeros [1, 1, 256, 256]
        _maskInput = new Tensor<float>(
            new TensorShape(1, 1, 256, 256),
            new float[1 * 1 * 256 * 256]);

        // has_mask_input = 0 → decoder ignores mask_input
        _hasMaskInput = new Tensor<float>(
            new TensorShape(1),
            new float[] { 0f });

        // Original image size [H, W] that was fed to the encoder
        _origImSize = new Tensor<float>(
            new TensorShape(2),
            new float[] { encoderInputHeight, encoderInputWidth });
    }

    /// <summary>
    /// Writes the decoder output mask tensor into the RenderTexture.
    /// Tries Sentis 2.x RenderToTexture first; falls back to ToTexture + Blit.
    /// </summary>
    private void BlitMaskToRenderTexture(Tensor<float> maskTensor)
    {
        try
        {
            // Unity.InferenceEngine 2.4.1: RenderToTexture is the correct API
            TextureConverter.RenderToTexture(maskTensor, _maskRenderTexture,
                new TextureTransform().SetTensorLayout(TensorLayout.NCHW));
        }
        catch (Exception e)
        {
            Debug.LogError($"[SentisInferenceRunner] BlitMask failed: {e.Message}");
        }
    }
}
