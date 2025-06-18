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

        Debug.Log($"✅ {goName} cargado y añadido a la escena.");
    }
}


