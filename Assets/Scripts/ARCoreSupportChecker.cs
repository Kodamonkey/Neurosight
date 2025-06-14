using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;
using UnityEngine.UI;

public class ARCoreSupportChecker : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text warningText;
    public Canvas uiCanvas;

    GameObject popupInstance;

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
            ShowPopup("ARCore no soportado");
        }
        else
        {
            if (warningText != null)
            {
                warningText.text = "ARCore soportado";
                warningText.gameObject.SetActive(true);
            }
        }
    }

    void ShowPopup(string message)
    {
        if (uiCanvas == null)
        {
            if (warningText != null)
            {
                warningText.text = message;
                warningText.gameObject.SetActive(true);
            }
            return;
        }

        if (popupInstance == null)
        {
            popupInstance = new GameObject("ARCorePopup", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            popupInstance.transform.SetParent(uiCanvas.transform, false);

            var rect = popupInstance.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.2f, 0.4f);
            rect.anchorMax = new Vector2(0.8f, 0.6f);

            Image img = popupInstance.GetComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.8f);

            GameObject textObj = new GameObject("Message", typeof(RectTransform));
            textObj.transform.SetParent(popupInstance.transform, false);
            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TMP_Text tmp = textObj.AddComponent<TMP_Text>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 32;
            tmp.color = Color.white;
            if (warningText != null && warningText.font != null)
            {
                tmp.font = warningText.font;
            }
        }

        var popupText = popupInstance.GetComponentInChildren<TMP_Text>();
        if (popupText != null)
        {
            popupText.text = message;
        }

        popupInstance.SetActive(true);
    }
}
