using UnityEngine;
using System.IO;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public class MedicalMeshLoader : MonoBehaviour
{
    [Header("Ruta al converter .exe (medical_to_obj.exe)")]
    public string converterExePath;

    [Header("Paths de salida OBJ dentro de Assets/Models/")]
    public string brainObjPath = "Assets/Models/brain_model.obj";
    public string tumorObjPath = "Assets/Models/tumor_model.obj";

    [Header("Materiales para cada mesh")]
    public Material brainMaterial;
    public Material tumorMaterial;

    [Header("Ruta de salida para el prefab combinado")]  
    public string combinedPrefabPath = "Assets/Models/CombinedMedicalModel.prefab";

    private Mesh brainMesh;
    private Mesh tumorMesh;

    public void LoadBrainFile(string inputPath)
    {
        StartCoroutine(ConvertAndImport(inputPath, brainObjPath, ModelType.Brain));
    }

    public void LoadTumorFile(string inputPath)
    {
        StartCoroutine(ConvertAndImport(inputPath, tumorObjPath, ModelType.Tumor));
    }

    private enum ModelType { Brain, Tumor }

    private IEnumerator ConvertAndImport(string inputFullPath, string objAssetPath, ModelType type)
    {
        string exeFull = Path.GetFullPath(converterExePath).Replace("\\", "/");
        string inFull  = Path.GetFullPath(inputFullPath).Replace("\\", "/");
        string outFull = Path.GetFullPath(objAssetPath).Replace("\\", "/");
        string args    = $"\"{inFull}\" \"{outFull}\" nifti";

        if (!File.Exists(exeFull) || !File.Exists(inFull)) yield break;

        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = exeFull,
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        var proc = System.Diagnostics.Process.Start(psi);
        proc.WaitForExit();

#if UNITY_EDITOR
        AssetDatabase.Refresh();
        AssetDatabase.ImportAsset(objAssetPath);
        yield return null;

        brainMesh  = (type == ModelType.Brain) ? LoadMesh(objAssetPath) : brainMesh;
        tumorMesh  = (type == ModelType.Tumor) ? LoadMesh(objAssetPath) : tumorMesh;

        if (brainMesh != null && tumorMesh != null)
        {
            CreateCombinedPrefab();
        }
#endif
    }

#if UNITY_EDITOR
    private Mesh LoadMesh(string path)
    {
        Mesh m = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (m == null)
            Debug.LogError($"❌ No se pudo cargar mesh de: {path}");
        return m;
    }

    private void CreateCombinedPrefab()
    {
        // Crea un root GameObject vacío
        GameObject root = new GameObject("CombinedMedicalModel");

        // Cerebro
        GameObject brainGO = new GameObject("Brain");
        brainGO.transform.SetParent(root.transform, false);
        var bf = brainGO.AddComponent<MeshFilter>(); bf.sharedMesh = brainMesh;
        var br = brainGO.AddComponent<MeshRenderer>(); br.sharedMaterial = brainMaterial;

        // Tumor
        GameObject tumorGO = new GameObject("Tumor");
        tumorGO.transform.SetParent(root.transform, false);
        var tf = tumorGO.AddComponent<MeshFilter>(); tf.sharedMesh = tumorMesh;
        var tr = tumorGO.AddComponent<MeshRenderer>(); tr.sharedMaterial = tumorMaterial;

        // Guardar como prefab
        string dir = Path.GetDirectoryName(combinedPrefabPath);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        PrefabUtility.SaveAsPrefabAsset(root, combinedPrefabPath, out bool success);
        if (success)
            Debug.Log($"✅ Prefab combinado guardado en: {combinedPrefabPath}");
        else
            Debug.LogError($"❌ Error al guardar prefab en: {combinedPrefabPath}");

        // Limpia el root temporal de la escena
        Object.DestroyImmediate(root);
    }
#endif
}


