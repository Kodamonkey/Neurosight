using UnityEngine;
using TMPro;
using System.IO;
using System.Collections;
using UnityEngine.Networking;
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

    // Pop-up visible en build (Android)
    GameObject runtimePopup;
    IEnumerator HideRuntimePopup()
    {
        yield return new WaitForSeconds(2f);
        if (runtimePopup != null) runtimePopup.SetActive(false);
    }

    void ShowRuntimePopup(string message)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // Intenta mostrar toast nativo en Android
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var toastClass = new AndroidJavaClass("android.widget.Toast"))
            {
                activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                {
                    AndroidJavaObject toast = toastClass.CallStatic<AndroidJavaObject>("makeText", activity, message, toastClass.GetStatic<int>("LENGTH_SHORT"));
                    toast.Call("show");
                }));
            }
        }
        catch (System.Exception e)
        {
            Debug.Log("Toast failed: " + e.Message);
        }
#else
        // Fallback usando Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.Log(message);
            return;
        }

        if (runtimePopup == null)
        {
            runtimePopup = new GameObject("RuntimePopup", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
            runtimePopup.transform.SetParent(canvas.transform, false);
            var rect = runtimePopup.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.2f, 0.4f);
            rect.anchorMax = new Vector2(0.8f, 0.6f);

            var img = runtimePopup.GetComponent<UnityEngine.UI.Image>();
            img.color = new Color(0f, 0f, 0f, 0.8f);

            GameObject textObj = new GameObject("Message", typeof(RectTransform));
            textObj.transform.SetParent(runtimePopup.transform, false);
            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TMP_Text tmp = textObj.AddComponent<TMP_Text>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 32;
            tmp.color = Color.white;
            TMP_Text refFont = FindObjectOfType<TMP_Text>();
            if (refFont != null && refFont.font != null) tmp.font = refFont.font;
        }

        var popupText = runtimePopup.GetComponentInChildren<TMP_Text>();
        if (popupText != null) popupText.text = message;
        runtimePopup.SetActive(true);
        StartCoroutine(HideRuntimePopup());
#endif
    }

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
        ShowRuntimePopup(type == ModelType.Brain ?
            "NIFTI de Cerebro cargado correctamente." :
            "NIFTI del Tumor cargado correctamente.");
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
            ShowRuntimePopup("Prefab combinado guardado correctamente.");
        }
        else
        {
            Debug.LogError($"❌ Error al guardar prefab en: {combinedPrefabPath}");
            ShowEditorPopup($"Error al guardar prefab en: {combinedPrefabPath}");
            ShowRuntimePopup("Error al guardar el prefab.");
        }

        // Limpia el root temporal de la escena
        Object.DestroyImmediate(root);
    }
#endif
}


