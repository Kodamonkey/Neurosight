using UnityEngine;
using TMPro;
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

#if UNITY_EDITOR
    private void ShowEditorPopup(string message)
    {
        EditorUtility.DisplayDialog("Neurosight", message, "OK");
    }
#endif

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
    string apiUrl = "https://converapi.onrender.com/convert/";

    // Leer el archivo .nii
    byte[] fileBytes = File.ReadAllBytes(inputFullPath);
    WWWForm form = new WWWForm();
    form.AddBinaryData("file", fileBytes, Path.GetFileName(inputFullPath), "application/octet-stream");
    form.AddField("tipo", "nifti"); // o "dicom"
    form.AddField("threshold", "1.0");

    using (UnityWebRequest www = UnityWebRequest.Post(apiUrl, form))
    {
        www.downloadHandler = new DownloadHandlerBuffer(); // 👈 agrega esto
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("❌ Error al llamar a la API: " + www.error);
            yield break;
        }

        File.WriteAllBytes(objAssetPath, www.downloadHandler.data);
        Debug.Log($"✅ OBJ recibido y guardado en: {objAssetPath}");
    }

#if UNITY_EDITOR
    // Recargar el archivo como asset
    AssetDatabase.Refresh();
    AssetDatabase.ImportAsset(objAssetPath);
    yield return null;

        brainMesh  = (type == ModelType.Brain) ? LoadMesh(objAssetPath) : brainMesh;
        tumorMesh  = (type == ModelType.Tumor) ? LoadMesh(objAssetPath) : tumorMesh;

        if (type == ModelType.Brain && brainMesh != null)
            ShowEditorPopup("NIFTI de Cerebro cargado correctamente.");
        else if (type == ModelType.Tumor && tumorMesh != null)
            ShowEditorPopup("NIFTI del Tumor cargado correctamente.");

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
        {
            Debug.Log($"✅ Prefab combinado guardado en: {combinedPrefabPath}");
            ShowEditorPopup($"Prefab combinado guardado en: {combinedPrefabPath}");
        }
        else
        {
            Debug.LogError($"❌ Error al guardar prefab en: {combinedPrefabPath}");
            ShowEditorPopup($"Error al guardar prefab en: {combinedPrefabPath}");
        }

        // Limpia el root temporal de la escena
        Object.DestroyImmediate(root);
    }
#endif
}


