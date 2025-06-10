using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.Events;    // ← imprescindible para UnityEvent<>

[RequireComponent(typeof(ARTrackedImageManager))]
public class ImageTrackingAndAnchoring : MonoBehaviour
{
    [SerializeField] ARTrackedImageManager trackedImageManager;
    [SerializeField] GameObject           brainPrefab;

    bool placed = false;

    void OnEnable()
    {
        if (trackedImageManager.trackablesChanged != null)
            trackedImageManager.trackablesChanged.AddListener(OnTrackedImagesChanged);
        else
            Debug.LogWarning("trackablesChanged es null. Actualiza AR Foundation a 6.0.0-pre.5 o superior.");
    }

    void OnDisable()
    {
        if (trackedImageManager.trackablesChanged != null)
            trackedImageManager.trackablesChanged.RemoveListener(OnTrackedImagesChanged);
    }

    void OnTrackedImagesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> args)
    {
        if (placed) return;

        foreach (var img in args.added)
        {
            if (img.trackingState != TrackingState.Tracking)
                continue;

            var anchorGO = new GameObject("ImageAnchor");
            anchorGO.transform.position = img.transform.position;
            anchorGO.transform.rotation = img.transform.rotation;

            var anchorComp = anchorGO.AddComponent<ARAnchor>();
            if (anchorComp == null) continue;

            var go = Instantiate(brainPrefab, anchorGO.transform);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;

            placed = true;
            trackedImageManager.enabled = false;
            break;
        }
    }
}
