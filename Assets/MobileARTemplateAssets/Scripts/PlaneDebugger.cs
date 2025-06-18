using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.Events;

[RequireComponent(typeof(ARPlaneManager))]
public class PlaneDebugger : MonoBehaviour
{
    ARPlaneManager _planeManager;

    void Awake()
    {
        _planeManager = GetComponent<ARPlaneManager>();
    }

    void OnEnable()
    {
        if (_planeManager.trackablesChanged != null)
            _planeManager.trackablesChanged.AddListener(OnPlanesChanged);
        else
            Debug.LogWarning("trackablesChanged es null. Actualiza AR Foundation.");
    }

    void OnDisable()
    {
        if (_planeManager.trackablesChanged != null)
            _planeManager.trackablesChanged.RemoveListener(OnPlanesChanged);
    }

    void OnPlanesChanged(ARTrackablesChangedEventArgs<ARPlane> args)
    {
        foreach (var p in args.added)
            Debug.Log($"[PlaneDebugger] Plano añadido en {p.center}");
        foreach (var p in args.updated)
            Debug.Log($"[PlaneDebugger] Plano actualizado en {p.center}");
        foreach (var p in args.removed)
            Debug.Log($"[PlaneDebugger] Plano eliminado");
    }
}
