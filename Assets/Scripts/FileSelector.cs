using UnityEngine;
using SFB;

public class FileSelector : MonoBehaviour
{
    public MedicalMeshLoader loader;

    public void SelectNifti()
    {
        var paths = StandaloneFileBrowser.OpenFilePanel("Selecciona archivo NIfTI", "", "nii", false);
        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
        {
            loader.LoadMedicalFile(paths[0], false);
        }
    }

    public void SelectDicomFolder()
    {
        var path = StandaloneFileBrowser.OpenFolderPanel("Selecciona carpeta DICOM", "", false);
        if (path.Length > 0 && !string.IsNullOrEmpty(path[0]))
        {
            loader.LoadMedicalFile(path[0], true);
        }
    }
}
