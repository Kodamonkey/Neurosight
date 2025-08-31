using UnityEngine;
using SimpleFileBrowser;
using System.Collections;
using System.IO;

public class FileSelector : MonoBehaviour
{
    [Tooltip("Arrastra aquí el GameObject con el MedicalMeshLoader")]
    public MedicalMeshLoader loader;

    // Botón: Abrir diálogo y cargar NIfTI
    public void SelectNifti()
    {
        if (loader == null)
        {
            Debug.LogError("❌ Falta asignar 'loader' (MedicalMeshLoader) en el inspector.");
            return;
        }
        StartCoroutine(ShowLoadDialogCoroutine());
    }

    private IEnumerator ShowLoadDialogCoroutine()
    {
        // Acepta .nii y .nii.gz
        FileBrowser.SetFilters(true, new FileBrowser.Filter("NIfTI", ".nii", ".nii.gz"));
        FileBrowser.SetDefaultFilter(".nii");

        // Diálogo de selección
        yield return FileBrowser.WaitForLoadDialog(
            FileBrowser.PickMode.Files,
            allowMultiSelection: false,
            initialPath: null,
            initialFilename: null
        );

        if (!FileBrowser.Success || FileBrowser.Result == null || FileBrowser.Result.Length == 0)
        {
            Debug.Log("❌ Selección cancelada o sin archivo.");
            yield break;
        }

        string selectedPath = FileBrowser.Result[0];

        // Genera un nombre limpio (sin primary%3A... ni timestamps) + extensión correcta
        string cleanBase = MedicalMeshLoader.CleanNiftiBaseName(selectedPath);
        string ext = MedicalMeshLoader.GetNiftiExtension(selectedPath); // ".nii" o ".nii.gz"

        // Staging local (asegura accesibilidad desde File.Copy)
        string stagingFolder = Path.Combine(Application.persistentDataPath, "inputs");
        if (!Directory.Exists(stagingFolder)) Directory.CreateDirectory(stagingFolder);

        string stagedPath = Path.Combine(stagingFolder, cleanBase + ext);

        try
        {
            // Copia usando helper compatible con SAF/Android
            FileBrowserHelpers.CopyFile(selectedPath, stagedPath);
            Debug.Log($"✅ NIfTI copiado a staging: {stagedPath}");

            // Lanza el flujo del loader (él creará /cases/<Id>/ y hará las llamadas a la API)
            loader.LoadNiftiFile(stagedPath);
        }
        catch (System.Exception e)
        {
            Debug.LogError("❌ Error copiando archivo con FileBrowserHelpers: " + e.Message);
        }
    }
}
