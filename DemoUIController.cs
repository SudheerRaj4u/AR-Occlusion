using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// DemoUIController
/// ─────────────────
/// Provides the on-screen toggle button for the occlusion ON/OFF demo,
/// plus an FPS counter overlay.
///
/// Attach to a World Space or Screen Space — Overlay Canvas.
/// Assign the OcclusionManager reference and the two UI Text/Image
/// elements via the Inspector.
/// </summary>
public class DemoUIController : MonoBehaviour
{
    // ── Inspector References ───────────────────────────────────────────────
    [Header("Dependencies")]
    [Tooltip("The OcclusionManager on the AR Camera.")]
    [SerializeField] private OcclusionManager occlusionManager;

    [Header("Toggle Button")]
    [Tooltip("The UI Button that toggles occlusion ON/OFF.")]
    [SerializeField] private Button toggleButton;
    [Tooltip("Text label on the toggle button.")]
    [SerializeField] private TextMeshProUGUI toggleButtonLabel;

    [Header("FPS Counter")]
    [Tooltip("TextMeshPro label showing current FPS.")]
    [SerializeField] private TextMeshProUGUI fpsLabel;
    [Tooltip("How many frames to average FPS over.")]
    [SerializeField][Range(10, 120)] private int fpsAverageFrames = 30;

    // ── Private State ──────────────────────────────────────────────────────
    private float[] _frameTimes;
    private int _frameIndex;
    private float _frameTimeSum;

    // ── Unity Lifecycle ────────────────────────────────────────────────────
    private void Awake()
    {
        _frameTimes = new float[fpsAverageFrames];

        if (toggleButton != null)
            toggleButton.onClick.AddListener(OnToggleOcclusion);

        // Ensure initial label matches current state.
        RefreshToggleLabel();
    }

    private void Update()
    {
        // Rolling FPS average.
        float dt = Time.unscaledDeltaTime;
        _frameTimeSum -= _frameTimes[_frameIndex];
        _frameTimes[_frameIndex] = dt;
        _frameTimeSum += dt;
        _frameIndex = (_frameIndex + 1) % fpsAverageFrames;

        float fps = fpsAverageFrames / _frameTimeSum;

        if (fpsLabel != null)
            fpsLabel.text = $"{fps:F0} FPS";
    }

    // ── Button Handler ─────────────────────────────────────────────────────

    private void OnToggleOcclusion()
    {
        if (occlusionManager == null) return;

        occlusionManager.OcclusionEnabled = !occlusionManager.OcclusionEnabled;
        RefreshToggleLabel();

        Debug.Log($"[DemoUI] Occlusion toggled → {(occlusionManager.OcclusionEnabled ? "ON" : "OFF")}");
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private void RefreshToggleLabel()
    {
        if (toggleButtonLabel == null || occlusionManager == null) return;
        toggleButtonLabel.text = occlusionManager.OcclusionEnabled
            ? "Occlusion: ON"
            : "Occlusion: OFF";
    }
}
