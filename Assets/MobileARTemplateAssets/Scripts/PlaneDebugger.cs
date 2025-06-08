using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(ARPlaneManager))]
public class PlaneDebugger : MonoBehaviour
{
    ARPlaneManager _planeManager;

    void Awake()
    {
        _planeManager = GetComponent<ARPlaneManager>();
        _planeManager.planesChanged += OnPlanesChanged;
    }

    void OnPlanesChanged(ARPlanesChangedEventArgs args)
    {
        foreach (var p in args.added)
            Debug.Log($"[PlaneDebugger] Plano añadido en {p.center}");
        foreach (var p in args.updated)
            Debug.Log($"[PlaneDebugger] Plano actualizado en {p.center}");
        foreach (var p in args.removed)
            Debug.Log($"[PlaneDebugger] Plano eliminado");
    }

    void OnDestroy()
    {
        _planeManager.planesChanged -= OnPlanesChanged;
    }
}
