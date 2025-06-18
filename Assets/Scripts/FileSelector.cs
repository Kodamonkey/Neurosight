using UnityEngine;
using SimpleFileBrowser;
using System.Collections;

public class FileSelector : MonoBehaviour
{
    [Tooltip("Arrastra aquí el GameObject con el MedicalMeshLoader")]
    public MedicalMeshLoader loader;

    public void SelectBrainNifti()
    {
        StartCoroutine(ShowLoadDialogCoroutine(loader.LoadBrainFile));
    }

    public void SelectTumorNifti()
    {
        StartCoroutine(ShowLoadDialogCoroutine(loader.LoadTumorFile));
    }

    private IEnumerator ShowLoadDialogCoroutine(System.Action<string> callback)
    {
        // Filtro para solo archivos .nii
        FileBrowser.SetFilters(true, new FileBrowser.Filter("NIfTI", ".nii"));
        FileBrowser.SetDefaultFilter(".nii");

        // Esperar al diálogo de selección de archivo (PickFiles, sin múltiples archivos)
        yield return FileBrowser.WaitForLoadDialog(
            FileBrowser.PickMode.Files,    // <- CORRECTO
            false,                         // No multiselección
            null,                          // Ruta inicial
            null                           // Nombre de archivo por defecto
        );

        // Si se seleccionó un archivo, ejecutar el callback
        if (FileBrowser.Success && FileBrowser.Result != null && FileBrowser.Result.Length > 0)
        {
            callback.Invoke(FileBrowser.Result[0]);
        }
    }
}
