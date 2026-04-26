using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;   // HumanSegmentationStencilMode lives here

/// <summary>
/// OcclusionManager (ARCore Native Version)
/// ─────────────────────────────────────────
/// Uses ARCore's built-in human segmentation stencil via AROcclusionManager
/// instead of running MobileSAM. This is the reliable, production-ready approach.
///
/// How it works:
///   ARCore continuously produces a humanStencilTexture — a per-pixel mask
///   where white = person, black = background. We pass this directly to
///   OcclusionShader which uses clip() to punch holes through the video plane,
///   revealing the real camera feed and making people appear IN FRONT of the video.
///
/// Setup in Unity Inspector:
///   1. Attach this script to the AR Camera GameObject.
///   2. Also add AROcclusionManager component to the same GameObject.
///   3. On AROcclusionManager: set Human Segmentation Stencil Mode = "Best"
///   4. Wire ARTrackedImageHandler.occlusionManager to this component.
/// </summary>
[RequireComponent(typeof(ARCameraManager))]
[RequireComponent(typeof(AROcclusionManager))]
public class OcclusionManager : MonoBehaviour
{
    // ── Inspector ──────────────────────────────────────────────────────────
    [Tooltip("Renderer on the AR video plane. Set automatically by ARTrackedImageHandler.")]
    [SerializeField] private Renderer videoPlaneRenderer;

    [Tooltip("Shader property name for the occlusion mask.")]
    [SerializeField] private string maskPropertyName = "_OcclusionMask";

    [Tooltip("Enable / disable occlusion at runtime (for the demo toggle button).")]
    public bool OcclusionEnabled = true;

    // ── Private ────────────────────────────────────────────────────────────
    private AROcclusionManager _arOcclusionManager;
    private int _maskShaderID;
    private bool _videoPlaneReady;

    // ── Lifecycle ──────────────────────────────────────────────────────────
    private void Awake()
    {
        _arOcclusionManager = GetComponent<AROcclusionManager>();
        _maskShaderID = Shader.PropertyToID(maskPropertyName);
    }

    private void Start()
    {
        if (_arOcclusionManager == null)
        {
            Debug.LogError("[OcclusionManager] AROcclusionManager component missing! " +
                           "Add it to the AR Camera GameObject.");
            return;
        }

        // ARCore requires BOTH depth and stencil modes to be enabled for
        // human occlusion. Stencil alone causes the Inspector warning.
        _arOcclusionManager.requestedHumanStencilMode =
            HumanSegmentationStencilMode.Best;
        _arOcclusionManager.requestedHumanDepthMode =
            HumanSegmentationDepthMode.Best;

        Debug.Log("[OcclusionManager] ARCore human stencil occlusion ready.");
    }

    private void Update()
    {
        if (!_videoPlaneReady || videoPlaneRenderer == null) return;

        if (!OcclusionEnabled)
        {
            // Toggle OFF: clear the mask so video renders fully opaque.
            videoPlaneRenderer.material.SetTexture(_maskShaderID, null);
            return;
        }

        // Get ARCore's human stencil texture.
        // White pixels = person/hand, black pixels = background.
        Texture humanStencil = _arOcclusionManager.humanStencilTexture;

        if (humanStencil != null)
        {
            videoPlaneRenderer.material.SetTexture(_maskShaderID, humanStencil);
            // No per-frame log here — logging every frame causes significant delay.
        }
        else
        {
            // Stencil not yet available — device may not support it or
            // ARCore hasn't initialised yet. Log once per second.
            if (Time.frameCount % 60 == 0)
                Debug.LogWarning("[OcclusionManager] humanStencilTexture is null. " +
                                 "Check: AROcclusionManager is added, device supports " +
                                 "ARCore human segmentation, and Depth API is enabled.");
        }
    }

    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>Called by ARTrackedImageHandler when the video plane prefab is spawned or re-activated.</summary>
    public void RegisterVideoPlane(Renderer planeRenderer)
    {
        videoPlaneRenderer = planeRenderer;  // Always update — handles re-detection after looking away.
        _videoPlaneReady = true;
        Debug.Log("[OcclusionManager] Video plane registered.");
    }
}
