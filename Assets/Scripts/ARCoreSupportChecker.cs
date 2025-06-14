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
        // Check AR support on the current device
        yield return ARSession.CheckAvailability();

        if (ARSession.state == ARSessionState.NeedsInstall)
        {
            // Request AR package installation
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
    }
}
