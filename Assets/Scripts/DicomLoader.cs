using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using SimpleFileBrowser;       // plugin

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
                // Aquí podrías parsear:
                // var dcm = DicomFile.Open(path);
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
