using UnityEngine;
using System.IO;
using System.Diagnostics;      // Para ProcessStartInfo, Process
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;             // AssetDatabase
#endif
// Alias para desambiguar Debug
using Debug = UnityEngine.Debug;

public class MedicalMeshLoader : MonoBehaviour
{
    [Header("Ruta al converter .exe (medical_to_obj.exe)")]
    public string converterExePath;

    [Header("Paths de salida OBJ dentro de Assets/Models/")]
    public string brainObjPath = "Assets/Models/brain_model.obj";
    public string tumorObjPath = "Assets/Models/tumor_model.obj";

    [Header("Materiales")]
    public Material brainMaterial;   // blanco semitransparente
    public Material tumorMaterial;   // rojo sólido

    [Header("Objeto raíz para la fusión")]
    public string parentObjectName = "BrainTumorCombined";

    void Awake()
    {
        // Si no se asignaron materiales desde el inspector, crea unos por defecto
        if (brainMaterial == null)
        {
            brainMaterial = new Material(Shader.Find("Standard"));
            var c = Color.white;
            c.a = 0.3f;                    // Semitransparente
            brainMaterial.color = c;
            brainMaterial.SetFloat("_Mode", 3);
            brainMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            brainMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            brainMaterial.SetInt("_ZWrite", 0);
            brainMaterial.DisableKeyword("_ALPHATEST_ON");
            brainMaterial.EnableKeyword("_ALPHABLEND_ON");
            brainMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            brainMaterial.renderQueue = 3000;
        }
        if (tumorMaterial == null)
        {
            tumorMaterial = new Material(Shader.Find("Standard"));
            tumorMaterial.color = Color.red;
        }
    }

    /// <summary>
    /// Invocado desde FileSelector para cargar el cerebro
    /// </summary>
    public void LoadBrainFile(string inputPath)
    {
        StartCoroutine(RunConvertAndImport(
            inputPath,
            brainObjPath,
            "nifti",
            brainMaterial,
            "BrainModel"
        ));
    }

    /// <summary>
    /// Invocado desde FileSelector para cargar el tumor
    /// </summary>
    public void LoadTumorFile(string inputPath)
    {
        StartCoroutine(RunConvertAndImport(
            inputPath,
            tumorObjPath,
            "nifti",
            tumorMaterial,
            "TumorModel"
        ));
    }

    IEnumerator RunConvertAndImport(
        string input,
        string output,
        string tipo,
        Material mat,
        string goName
    )
    {
        // Normaliza rutas para el converter
        string exe     = Path.GetFullPath(converterExePath).Replace("\\","/");
        string inNorm  = Path.GetFullPath(input).Replace("\\","/");
        string outNorm = Path.GetFullPath(output).Replace("\\","/");
        string args    = $"\"{inNorm}\" \"{outNorm}\" {tipo}";

        Debug.Log($"▶️ Convirtiendo {goName}…");

        if (!File.Exists(exe))
        {
            Debug.LogError("❌ No se encontró el ejecutable: " + exe);
            yield break;
        }
        if (!File.Exists(inNorm))
        {
            Debug.LogError("❌ No se encontró el archivo de entrada: " + inNorm);
            yield break;
        }

        var psi = new ProcessStartInfo {
            FileName               = exe,
            Arguments              = args,
            UseShellExecute        = false,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            CreateNoWindow         = true
        };
        var proc = Process.Start(psi);
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();

        // Espera a que termine el converter
        yield return new WaitUntil(() => proc.HasExited);

        // Importa y renderiza
        yield return StartCoroutine(
            ImportAndDisplayModel(output, mat, goName)
        );
    }

    IEnumerator ImportAndDisplayModel(
        string path,
        Material mat,
        string goName
    )
    {
    #if UNITY_EDITOR
        // Fuerza a Unity a refrescar e importar el OBJ
        AssetDatabase.Refresh();
        AssetDatabase.ImportAsset(path);
    #endif
        // Espera un frame a que termine
        yield return null;

        // Ruta relativa para AssetDatabase
        string relativePath = path.Replace(Application.dataPath, "Assets");
    #if UNITY_EDITOR
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(relativePath);
    #else
        Mesh mesh = null; // En build necesitarías un loader OBJ en runtime
    #endif

        if (mesh == null)
        {
            Debug.LogError("❌ No se pudo cargar el mesh: " + relativePath);
            yield break;
        }

        // Si ya existía, lo destruye
        var oldGO = GameObject.Find(goName);
        if (oldGO != null)
            DestroyImmediate(oldGO);

        // Crea y configura el GameObject
        var go = new GameObject(goName);
        var mf = go.AddComponent<MeshFilter>();   mf.mesh     = mesh;
        var mr = go.AddComponent<MeshRenderer>(); mr.material = mat;

        // Mismos transform para superponer
        go.transform.position   = Vector3.zero;
        go.transform.rotation   = Quaternion.Euler(-90, 0, 0);
        go.transform.localScale = Vector3.one;

        // Asegura objeto raíz para agrupar cerebro y tumor
        var parent = GameObject.Find(parentObjectName);
        if (parent == null)
        {
            parent = new GameObject(parentObjectName);
            parent.transform.position = Vector3.zero;
            parent.transform.rotation = Quaternion.identity;
            parent.transform.localScale = Vector3.one;
        }
        go.transform.SetParent(parent.transform, false);

        Debug.Log($"✅ {goName} cargado y añadido a la escena.");
    }
}


