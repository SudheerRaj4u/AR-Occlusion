using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// ARTrackedImageHandler
/// ──────────────────────
/// Detects the target poster via ARTrackedImageManager and spawns the AR video
/// plane prefab when tracking begins.
///
/// ⚠️ AR Foundation 6.3.x (Unity 6) BREAKING CHANGES applied here:
///   - OLD: manager.trackablesChanged += OnTracked...  (C# event)
///   - NEW: manager.trackablesChanged.AddListener(...)  (UnityEvent)
///
///   - OLD: foreach (var kvp in args.updated) { var img = kvp.Value; ... }
///   - NEW: foreach (var img in args.updated) { ... }  (direct list, no kvp)
///
///   - OLD: foreach (var kvp in args.removed) { kvp.Key ... }
///   - NEW: foreach (var img in args.removed) { img.trackableId ... }
///
/// This script targets AR Foundation 6.3.4 / Unity 6 (6000.x).
/// Attach to the GameObject that holds ARTrackedImageManager.
/// </summary>
[RequireComponent(typeof(ARTrackedImageManager))]
public class ARTrackedImageHandler : MonoBehaviour
{
    // ── Inspector ──────────────────────────────────────────────────────────
    [Header("Prefab")]
    [Tooltip("Prefab containing: a 3D Plane with Video Render Texture material.")]
    [SerializeField] private GameObject arVideoPlanePrefab;

    [Header("Dependencies")]
    [SerializeField] private OcclusionManager occlusionManager;

    [Header("Tracking Loss")]
    [Tooltip("Seconds to wait before hiding content after tracking is lost.")]
    [SerializeField][Range(0.1f, 3f)] private float trackingLossGracePeriod = 0.5f;

    // ── Private State ──────────────────────────────────────────────────────
    private ARTrackedImageManager _manager;
    private readonly Dictionary<TrackableId, GameObject> _spawnedObjects = new();
    private readonly Dictionary<TrackableId, float> _lossTimers = new();

    // ── Unity Lifecycle ────────────────────────────────────────────────────
    private void Awake()
    {
        _manager = GetComponent<ARTrackedImageManager>();
    }

    private void OnEnable()
    {
        // AR Foundation 6.3.x: trackablesChanged is a UnityEvent — use AddListener
        _manager.trackablesChanged.AddListener(OnTrackablesChanged);
    }

    private void OnDisable()
    {
        _manager.trackablesChanged.RemoveListener(OnTrackablesChanged);
    }

    private void Update()
    {
        // Process grace-period timers for tracking-loss hiding.
        var toRemove = new List<TrackableId>();
        foreach (var kvp in _lossTimers)
        {
            if (Time.time - kvp.Value >= trackingLossGracePeriod)
            {
                if (_spawnedObjects.TryGetValue(kvp.Key, out var obj))
                    obj.SetActive(false);
                toRemove.Add(kvp.Key);
            }
        }
        foreach (var id in toRemove) _lossTimers.Remove(id);
    }

    // ── Tracking Callback (AR Foundation 6.3.x API) ────────────────────────

    /// <summary>
    /// AR Foundation 6.3.x: trackablesChanged is a UnityEvent.
    /// args.added   → IReadOnlyList<ARTrackedImage>  (iterate directly)
    /// args.updated → IReadOnlyList<ARTrackedImage>  (iterate directly, no .Value)
    /// args.removed → IReadOnlyList<ARTrackedImage>  (iterate directly, use .trackableId)
    /// </summary>
    private void OnTrackablesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> args)
    {
        // ── Newly detected images ──────────────────────────────────────────
        foreach (var trackedImage in args.added)
            SpawnOrActivate(trackedImage);

        // ── Updated (still tracking) ───────────────────────────────────────
        // In AR Foundation 6.3.x, args.updated is IReadOnlyList<ARTrackedImage>
        foreach (var trackedImage in args.updated)
        {
            if (trackedImage.trackingState == TrackingState.Tracking)
            {
                _lossTimers.Remove(trackedImage.trackableId);
                SpawnOrActivate(trackedImage);

                // Update pose every frame — smoothed to resist jitter when
                // the poster is partially occluded by a hand.
                if (_spawnedObjects.TryGetValue(trackedImage.trackableId, out var obj))
                {
                    obj.transform.SetPositionAndRotation(
                        Vector3.Lerp(obj.transform.position,
                                     trackedImage.transform.position, Time.deltaTime * 8f),
                        Quaternion.Slerp(obj.transform.rotation,
                                         trackedImage.transform.rotation, Time.deltaTime * 8f));

                    const float planeMeshSizeUpd = 10f;
                    obj.transform.localScale = new Vector3(
                        trackedImage.size.x / planeMeshSizeUpd,
                        1f,
                        trackedImage.size.y / planeMeshSizeUpd);
                }
            }
            else if (trackedImage.trackingState == TrackingState.Limited)
            {
                if (!_lossTimers.ContainsKey(trackedImage.trackableId))
                    _lossTimers[trackedImage.trackableId] = Time.time;
            }
        }

        // ── Removed entirely ───────────────────────────────────────────────
        // In AR Foundation 6.3.x, args.removed is IReadOnlyDictionary<TrackableId, ARTrackedImage>
        // Use .Key to get TrackableId (unlike args.updated which is a direct list)
        foreach (var kvp in args.removed)
        {
            if (_spawnedObjects.TryGetValue(kvp.Key, out var obj))
                obj.SetActive(false);
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private void SpawnOrActivate(ARTrackedImage trackedImage)
    {
        if (_spawnedObjects.TryGetValue(trackedImage.trackableId, out var existing))
        {
            existing.SetActive(true);

            // Re-register renderer on every activation — ARCore may have reset
            // the stencil pipeline while the poster was out of view.
            var r = existing.GetComponentInChildren<Renderer>();
            if (r != null && occlusionManager != null)
                occlusionManager.RegisterVideoPlane(r);

            return;
        }

        if (arVideoPlanePrefab == null)
        {
            Debug.LogError("[ARTrackedImageHandler] arVideoPlanePrefab is not assigned!");
            return;
        }

        // Instantiate the video plane prefab at the tracked image's pose.
        var spawned = Instantiate(
            arVideoPlanePrefab,
            trackedImage.transform.position,
            trackedImage.transform.rotation);

        // Scale to match physical poster dimensions.
        // Unity's Plane mesh is 10x10 local units, so divide physical metres by 10.
        const float planeMeshSize = 10f;
        spawned.transform.localScale = new Vector3(
            trackedImage.size.x / planeMeshSize, 1f, trackedImage.size.y / planeMeshSize);

        _spawnedObjects[trackedImage.trackableId] = spawned;

        // Start video playback automatically.
        var videoPlayer = spawned.GetComponentInChildren<VideoPlayer>();
        if (videoPlayer != null)
        {
            videoPlayer.Play();
            Debug.Log($"[ARTrackedImageHandler] Video started for: {trackedImage.referenceImage.name}");
        }

        // Register the video plane renderer with OcclusionManager.
        var planeRenderer = spawned.GetComponentInChildren<Renderer>();
        if (planeRenderer != null && occlusionManager != null)
        {
            occlusionManager.RegisterVideoPlane(planeRenderer);
            Debug.Log("[ARTrackedImageHandler] Video plane renderer registered with OcclusionManager.");
        }
    }
}
