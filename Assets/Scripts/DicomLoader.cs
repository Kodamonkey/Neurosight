using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using SimpleFileBrowser;       // plugin
using Dicom;

public class DicomLoader : MonoBehaviour
{
    [Header("UI References")]
    public TMPro.TMP_Text fileNameText;

    // Llamado desde BtnLoadDicom OnClick()
    public void OnLoadDicomButtonPressed()
    {
        // Configura filtros sólo .dcm
        FileBrowser.SetFilters( true, new FileBrowser.Filter( "DICOM files", ".dcm" ) );
        // Abre diálogo
        FileBrowser.ShowLoadDialog( ( paths ) => {
                string path = paths[0];
                fileNameText.text = System.IO.Path.GetFileName(path);

                var checker = FindObjectOfType<ARCoreSupportChecker>();
                try
                {
                    var dcm = DicomFile.Open(path);
                    if (checker != null)
                        checker.ShowPopup("Archivo DICOM cargado correctamente");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Error al cargar DICOM: {ex.Message}");
                    if (checker != null)
                        checker.ShowPopup("Error al cargar DICOM. Intenta de nuevo");
                }
                // …crear tu objeto 3D, mesh, textura, etc.
            },
            () => {
                Debug.Log("Canceled");
            },
            FileBrowser.PickMode.Files, false, null, null, "Cargar .dcm", "Cancelar"
        );
    }

    // Llamado desde BtnCamera OnClick()
    public void OnCameraButtonPressed()
    {
        SceneManager.LoadScene("ARQR");
    }
}
