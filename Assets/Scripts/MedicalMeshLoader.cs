using UnityEngine;
using System.IO;
using System.Diagnostics;      // Para ProcessStartInfo, Process
using System.Collections;
using System.Text;
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
    [Header("Salida modelo fusionado")]
    public string fusedObjPath = "Assets/Models/fused_model.obj";
    public bool showModelsInScene = false;

    private Mesh brainMesh;
    private Mesh tumorMesh;


    [Header("Materiales")]
    public Material brainMaterial;   // blanco semitransparente
    public Material tumorMaterial;   // rojo sólido

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
        if (goName == "BrainModel") brainMesh = mesh;
        else if (goName == "TumorModel") tumorMesh = mesh;
        if (!showModelsInScene) {
            TrySaveMergedMesh();
            yield break;
        }

        // Si ya existía, lo destruye
        var oldGO = GameObject.Find(goName);
        if (oldGO != null)
            DestroyImmediate(oldGO);

        // Crea y configura el GameObject
        var go = new GameObject(goName);
        var mf = go.AddComponent<MeshFilter>();   mf.mesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();

        // Instancia el material para modificar color/alpha
        var matInstance = mat != null ? new Material(mat) : new Material(Shader.Find("Standard"));
        mr.material = matInstance;

        // Ajusta color según tipo de modelo
        if (goName == "BrainModel")
        {
            Color c = matInstance.color;
            c.a = 0.4f;                     // semitransparente
            matInstance.color = c;
            matInstance.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            matInstance.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            matInstance.SetInt("_ZWrite", 0);
            matInstance.DisableKeyword("_ALPHATEST_ON");
            matInstance.EnableKeyword("_ALPHABLEND_ON");
            matInstance.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
        else if (goName == "TumorModel")
        {
            matInstance.color = Color.red;  // color distintivo
        }

        // Mismos transform para superponer
        go.transform.position   = Vector3.zero;
        go.transform.rotation   = Quaternion.Euler(-90, 0, 0);
        go.transform.localScale = Vector3.one;

        if (showModelsInScene)
            TryMergeModels();
    }

    void TryMergeModels()
    {
        var brain = GameObject.Find("BrainModel");
        var tumor = GameObject.Find("TumorModel");
        if (brain != null && tumor != null && tumor.transform.parent != brain.transform)
        {
            tumor.transform.SetParent(brain.transform, true);
        }
    }
    void TrySaveMergedMesh()
    {
        if (brainMesh != null && tumorMesh != null)
        {
            var combine = new CombineInstance[2];
            combine[0].mesh = brainMesh;
            combine[0].transform = Matrix4x4.identity;
            combine[1].mesh = tumorMesh;
            combine[1].transform = Matrix4x4.identity;
            var merged = new Mesh();
            merged.CombineMeshes(combine, true, false);
            string full = Path.GetFullPath(fusedObjPath).Replace("\\","/");
            File.WriteAllText(full, MeshToObj(merged));
#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif
            Debug.Log($"✅ Modelos fusionados guardados en {fusedObjPath}");
        }
    }

    string MeshToObj(Mesh m)
    {
        var sb = new StringBuilder();
        foreach (var v in m.vertices) sb.AppendLine($"v {v.x} {v.y} {v.z}");
        foreach (var n in m.normals) sb.AppendLine($"vn {n.x} {n.y} {n.z}");
        foreach (var uv in m.uv) sb.AppendLine($"vt {uv.x} {uv.y}");
        int[] tris = m.triangles;
        for (int i = 0; i < tris.Length; i += 3)
        {
            int a = tris[i] + 1;
            int b = tris[i+1] + 1;
            int c = tris[i+2] + 1;
            sb.AppendLine($"f {a}/{a}/{a} {b}/{b}/{b} {c}/{c}/{c}");
        }
        return sb.ToString();
    }

}


