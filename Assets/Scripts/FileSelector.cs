using UnityEngine;
using SFB;  // Standalone File Browser

public class FileSelector : MonoBehaviour
{
    [Tooltip("Arrastra aquí el GameObject con el MedicalMeshLoader")]
    public MedicalMeshLoader loader;

    /// <summary>
    /// Selecciona un .nii y lo carga como “Brain”
    /// </summary>
    public void SelectBrainNifti()
    {
        // TERCER PARÁMETRO: extensión como string, NO string[]
        string[] paths = StandaloneFileBrowser.OpenFilePanel(
            "Selecciona volúmen NIfTI (cerebro)",  // título
            "",                                    // directorio inicial
            "nii",                                 // extensión permitida
            false                                  // no multiselección
        );

        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
            loader.LoadBrainFile(paths[0]);
    }

    /// <summary>
    /// Selecciona un .nii y lo carga como “Tumor”
    /// </summary>
    public void SelectTumorNifti()
    {
        string[] paths = StandaloneFileBrowser.OpenFilePanel(
            "Selecciona volúmen NIfTI (tumor)",
            "",
            "nii",
            false
        );

        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
            loader.LoadTumorFile(paths[0]);
    }
}

