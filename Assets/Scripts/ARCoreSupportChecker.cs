using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;

public class ARCoreSupportChecker : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text warningText;

    IEnumerator Start()
    {
        // Verifica la compatibilidad AR en el dispositivo actual.
        yield return ARSession.CheckAvailability();

        if (ARSession.state == ARSessionState.NeedsInstall)
        {
            // Solicita la instalación del paquete AR.
            yield return ARSession.Install();
        }

        if (ARSession.state == ARSessionState.Unsupported)
        {
            if (warningText != null)
            {
                warningText.text = "ARCore no soportado";
                warningText.gameObject.SetActive(true);
            }
        }
        else
        {
            // Si es compatible, muestra un mensaje indicando lo mismo.
            if (warningText != null)
            {
                warningText.text = "ARCore soportado";
                warningText.gameObject.SetActive(true);
            }
        }
    }
}
