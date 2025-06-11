using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(ARTrackedImageManager))]
[RequireComponent(typeof(ARAnchorManager))]
public class ImageTrackingAndAnchoring : MonoBehaviour
{
    [SerializeField]
    private ARTrackedImageManager trackedImageManager;

    [SerializeField]
    private ARAnchorManager anchorManager;

    [SerializeField]
    private GameObject brainPrefab;

    private bool placed = false;

    void Awake()
    {
        if (trackedImageManager == null)
            trackedImageManager = GetComponent<ARTrackedImageManager>();
        if (anchorManager == null)
            anchorManager = GetComponent<ARAnchorManager>();
    }

    void OnEnable()
    {
        trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
    }

    void OnDisable()
    {
        trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }

    private void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs args)
    {
        foreach (var img in args.added.Concat(args.updated))
        {
            if (placed || img.trackingState != TrackingState.Tracking)
                continue;

            // Lanza la creación asíncrona del anchor
            CreateAnchorAsync(img);
            break; // Solo uno
        }
    }

    private async void CreateAnchorAsync(ARTrackedImage img)
    {
        // Genera el pose de anclaje
        var pose = new Pose(img.transform.position, img.transform.rotation);

        // Intenta crear el anchor de forma asíncrona
        var result = await anchorManager.TryAddAnchorAsync(pose);
        if (result.status.IsSuccess())
        {
            var anchor = result.value;
            Debug.Log($"[AnchorScript] Anchor creado en posición: {pose.position}");

            // Instanciar el prefab como hijo del anchor
            var go = Instantiate(brainPrefab, anchor.transform);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;

            Debug.Log($"[AnchorScript] Instanciado prefab en {anchor.transform.position}");

            // Marcar y desuscribir
            placed = true;
            trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
        }
        else
        {
            Debug.LogWarning($"[AnchorScript] TryAddAnchorAsync falló con estado: {result.status}");
        }
    }
}
