using UnityEngine;
using UnityEngine.UI;
using System.Diagnostics;
using System.IO;
using System.Collections;

public class MedicalMeshLoader : MonoBehaviour
{
    public string converterExePath;  // apunta al .exe, no al python.exe ni al script .py
    public string outputObjPath;
    public Material meshMaterial;
    public Button loadButton;


    void Start()
    {
        if (loadButton != null)
        {
            loadButton.onClick.RemoveAllListeners(); // <--- limpia antes
            loadButton.onClick.AddListener(OpenFileDialog);
        }
    }
    public void OpenFileDialog()
    {
#if UNITY_EDITOR
        string inputPath = UnityEditor.EditorUtility.OpenFilePanel("Selecciona imagen médica", "", "");
        if (!string.IsNullOrEmpty(inputPath))
        {
            string tipo = inputPath.EndsWith(".nii") ? "nifti" : "dicom";
            StartCoroutine(RunPythonAndLoadModel(inputPath, outputObjPath, tipo));
        }
#else
        UnityEngine.Debug.LogWarning("Carga manual solo soportada en el editor por ahora.");
#endif
    }
    public void LoadMedicalFile(string inputPath, bool isDicom)
    {
        string tipo = isDicom ? "dicom" : "nifti";
        StartCoroutine(RunPythonAndLoadModel(inputPath, outputObjPath, tipo));
    }

IEnumerator RunPythonAndLoadModel(string input, string output, string tipo)
{
    string exe = Path.GetFullPath(converterExePath).Replace("\\", "/");
    string normalizedInput = Path.GetFullPath(input).Replace("\\", "/");
    string normalizedOutput = Path.GetFullPath(output).Replace("\\", "/");

    string args = $"\"{normalizedInput}\" \"{normalizedOutput}\" {tipo}";

    UnityEngine.Debug.Log("Cargando Modelo Médico");

    if (!File.Exists(exe))
    {
        UnityEngine.Debug.LogError("No se encontró el ejecutable: " + exe);
        yield break;
    }

    if (!File.Exists(normalizedInput))
    {
        UnityEngine.Debug.LogError("No se encontró el archivo de entrada: " + normalizedInput);
        yield break;
    }

    ProcessStartInfo start = new ProcessStartInfo
    {
        FileName = exe,
        Arguments = args,
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true
    };

    Process process = Process.Start(start);
    process.OutputDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) UnityEngine.Debug.Log(e.Data); };
    process.ErrorDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) UnityEngine.Debug.LogError(e.Data); };
    process.BeginOutputReadLine();
    process.BeginErrorReadLine();

    yield return new WaitUntil(() => process.HasExited);

    ImportAndDisplayModel(output);
}





void ImportAndDisplayModel(string path)
{
#if UNITY_EDITOR
    UnityEditor.AssetDatabase.ImportAsset(path);
#endif

    string relativePath = path.Replace(Application.dataPath, "Assets");

    Mesh loadedMesh = UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>(relativePath);

    if (loadedMesh != null)
    {   
        GameObject oldModel = GameObject.Find("MedicalModel");
        if (oldModel != null)
        {
            DestroyImmediate(oldModel);
        }
        GameObject model = new GameObject("MedicalModel");
        MeshFilter mf = model.AddComponent<MeshFilter>();
        mf.mesh = loadedMesh;

        MeshRenderer mr = model.AddComponent<MeshRenderer>();
        mr.material = meshMaterial != null ? meshMaterial : new Material(Shader.Find("Standard"));

        // Coloca el modelo cerca del botón para asegurarte que sea visible
        model.transform.position = new Vector3(452f, 112f, -338f);

        // Asegura que la escala sea visible

        // Asegura rotación si está volteado
        model.transform.rotation = Quaternion.Euler(0, 0, -90);

        UnityEngine.Debug.Log("Modelo cargado correctamente y colocado.");
    }
    else
    {
        UnityEngine.Debug.LogError("No se pudo cargar el mesh desde: " + relativePath);
    }
}



}
