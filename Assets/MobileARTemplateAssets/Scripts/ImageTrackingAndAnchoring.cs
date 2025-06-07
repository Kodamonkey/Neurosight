using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(ARTrackedImageManager))]
public class ImageTrackingAndAnchoring : MonoBehaviour
{
    [SerializeField] ARTrackedImageManager trackedImageManager;
    [SerializeField] GameObject           brainPrefab;

    bool placed = false;

    void OnEnable()  => trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
    void OnDisable() => trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;

    void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs args)
    {
        if (placed) return;

        foreach (var img in args.added)
        {
            if (img.trackingState != TrackingState.Tracking)
                continue;

            // 1) Crear un GameObject vacío en la pose de la imagen
            var anchorGO = new GameObject("ImageAnchor");
            anchorGO.transform.position = img.transform.position;
            anchorGO.transform.rotation = img.transform.rotation;

            // 2) Añadirle el componente ARAnchor
            ARAnchor anchorComp = anchorGO.AddComponent<ARAnchor>();
            if (anchorComp == null)
                continue;

            // 3) Instanciar tu prefab como hijo del anchor
            var go = Instantiate(brainPrefab, anchorGO.transform);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;

            // 4) Marcar y deshabilitar más trackeos
            placed = true;
            trackedImageManager.enabled = false;
            break;
        }
    }
}
