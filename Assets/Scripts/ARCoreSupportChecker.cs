using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;

public class ARCoreSupportChecker : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text warningText;

    async void Start()
    {
        SessionAvailability availability = await ARSession.CheckAvailability();

        if (availability == SessionAvailability.Supported)
        {
            return; // ARCore is ready
        }
        else if (availability == SessionAvailability.NeedsInstall)
        {
            await ARSession.Install();
        }
        else if (availability == SessionAvailability.Unsupported)
        {
            if (warningText != null)
            {
                warningText.text = "ARCore no soportado";
                warningText.gameObject.SetActive(true);
            }
        }
    }
}
