using UnityEngine;
using System.Collections;
using System.IO;
using UnityEngine.Networking;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

[Serializable]
public class CaseMeta
{
    public string Id;             // Name + "_" + timestamp
    public string Name;           // nombre limpio del NIfTI (sin extensión)
    public string Nii_Path;       // ruta absoluta al .nii/.nii.gz dentro de la carpeta del caso
    public string Tumor_Path;     // ruta absoluta a tumor.obj
    public string Brain_Path;     // ruta absoluta a brain.obj
    public string Date_Creation;  // ISO UTC
    public string Date_Lastopen;  // ISO UTC o "" si nunca se abrió
}

[Serializable]
public class CasesIndex
{
    public List<CaseMeta> Items = new List<CaseMeta>();
}

public class MedicalMeshLoader : MonoBehaviour
{
    [Header("API")]
    //public string apiUrl = "https://converapi.onrender.com/convert/";
    public string apiUrl = "http://192.168.1.9:8000/convert";
    private string casesRoot;      // …/persistentDataPath/cases
    private string casesIndexPath; // …/persistentDataPath/cases/cases.json

    private enum MeshType { Brain, Tumor }

    private void Awake()
    {
        casesRoot = Path.Combine(Application.persistentDataPath, "cases");
        if (!Directory.Exists(casesRoot))
            Directory.CreateDirectory(casesRoot);

        casesIndexPath = Path.Combine(casesRoot, "cases.json");
        if (!File.Exists(casesIndexPath))
            File.WriteAllText(casesIndexPath, JsonUtility.ToJson(new CasesIndex(), true));

        Debug.Log("persistentDataPath = " + Application.persistentDataPath);
        Debug.Log("casesRoot = " + casesRoot);
    }

    /// <summary>
    /// Entrada principal: copia el NIfTI a la carpeta del caso (con nombre limpio),
    /// llama API para brain/tumor y actualiza el índice global.
    /// </summary>
    public void LoadNiftiFile(string niftiPath)
    {
        if (!File.Exists(niftiPath))
        {
            Debug.LogError(" El archivo NIfTI no existe: " + niftiPath);
            return;
        }
        StartCoroutine(ProcessNiftiCase(niftiPath));
    }

    private IEnumerator ProcessNiftiCase(string niftiPath)
    {
        // 1) Preparar identificadores y carpeta de caso
        string nameNoExt = CleanNiftiBaseName(niftiPath); // nombre limpio sin extensión
        string caseId = BuildCaseId(nameNoExt);           // Name + timestamp
        string caseDir = Path.Combine(casesRoot, caseId);
        Directory.CreateDirectory(caseDir);

        // Copiar NIfTI al caso con nombre limpio y extensión correcta
        string ext = GetNiftiExtension(niftiPath);        // ".nii" o ".nii.gz"
        string caseNiiPath = Path.Combine(caseDir, nameNoExt + ext);
        try
        {
            File.Copy(niftiPath, caseNiiPath, overwrite: true);
        }
        catch (Exception e)
        {
            Debug.LogError(" No se pudo copiar el NIfTI al caso: " + e.Message);
            yield break;
        }

        // 2) Llamadas a la API → guarda brain/tumor
        string brainObjPath = Path.Combine(caseDir, "brain.obj");
        string tumorObjPath = Path.Combine(caseDir, "tumor.obj");

        yield return RequestMeshAndSave(caseNiiPath, MeshType.Brain, brainObjPath);
        yield return RequestMeshAndSave(caseNiiPath, MeshType.Tumor, tumorObjPath);

        // 3) Actualizar índice global único
        var meta = new CaseMeta
        {
            Id = caseId,
            Name = nameNoExt,
            Nii_Path = caseNiiPath,
            Brain_Path = brainObjPath,
            Tumor_Path = tumorObjPath,
            Date_Creation = DateTime.UtcNow.ToString("o"),
            Date_Lastopen = "" // aún no se ha abierto
        };
        UpsertCaseIntoIndex(meta);

        Debug.Log($"Caso creado: {caseId}\n" +
                  $"   NIfTI: {caseNiiPath}\n" +
                  $"   Brain: {brainObjPath}\n" +
                  $"   Tumor: {tumorObjPath}");
    }

    // llamado a la API y guardar malla
    private IEnumerator RequestMeshAndSave(string niftiPath, MeshType type, string outPath)
    {
        const int REQUEST_TIMEOUT_SECONDS = 300;
        byte[] data = File.ReadAllBytes(niftiPath);

        WWWForm form = new WWWForm();
        form.AddBinaryData("file", data, Path.GetFileName(niftiPath), "application/octet-stream");
        form.AddField("tipo", "nifti");
        form.AddField("segment", type == MeshType.Brain ? "brain" : "tumor");
        Debug.Log($"POST → {apiUrl}  segment={(type==MeshType.Brain ? "brain" : "tumor")}");

        using (UnityWebRequest www = UnityWebRequest.Post(apiUrl, form))
        {
            www.downloadHandler = new DownloadHandlerBuffer();
            www.timeout = REQUEST_TIMEOUT_SECONDS;
            var op = www.SendWebRequest();
            while (!op.isDone) yield return null;

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($" Error API {type}: {www.error}");
                yield break;
            }

            File.WriteAllBytes(outPath, www.downloadHandler.data);
            long sz = new FileInfo(outPath).Length;
            Debug.Log($" Malla {type} guardada: {outPath} ({sz} bytes)");
        }
    }

    // ===== Índice global (leer / guardar / upsert / marcar abierto) =====

    private CasesIndex LoadIndex()
    {
        try
        {
            string json = File.ReadAllText(casesIndexPath);
            var idx = JsonUtility.FromJson<CasesIndex>(json);
            return idx ?? new CasesIndex();
        }
        catch { return new CasesIndex(); }
    }

    private void SaveIndex(CasesIndex idx)
    {
        try
        {
            // write-then-replace para minimizar corrupción si se cierra la app
            string tmp = casesIndexPath + ".tmp";
            File.WriteAllText(tmp, JsonUtility.ToJson(idx, true));
            if (File.Exists(casesIndexPath)) File.Delete(casesIndexPath);
            File.Move(tmp, casesIndexPath);
        }
        catch (Exception e)
        {
            Debug.LogError(" No se pudo guardar el índice de casos: " + e.Message);
        }
    }

    private void UpsertCaseIntoIndex(CaseMeta meta)
    {
        var idx = LoadIndex();
        int i = idx.Items.FindIndex(c => string.Equals(c.Id, meta.Id, StringComparison.OrdinalIgnoreCase));
        if (i >= 0) idx.Items[i] = meta; else idx.Items.Add(meta);
        SaveIndex(idx);
    }

    /// <summary> Marca un caso como abierto (Date_Lastopen = ahora UTC). </summary>
    public void MarkCaseOpened(string caseId)
    {
        var idx = LoadIndex();
        var item = idx.Items.FirstOrDefault(c => string.Equals(c.Id, caseId, StringComparison.OrdinalIgnoreCase));
        if (item == null)
        {
            Debug.LogWarning(" No existe el caso en índice: " + caseId);
            return;
        }
        item.Date_Lastopen = DateTime.UtcNow.ToString("o");
        SaveIndex(idx);
        Debug.Log($" Date_Lastopen actualizado para {caseId}");
    }

    public CaseMeta[] GetAllCases() => LoadIndex().Items.ToArray();

    // ===== Utilidades de nombres y extensiones =====

    /// <summary>
    /// Devuelve un nombre "limpio" (sin extensión), decodificando %3A/%2F del SAF,
    /// quitando prefijos tipo "primary:xd/", removiendo .nii/.nii.gz y un timestamp final si lo hubiera.
    /// </summary>
    public static string CleanNiftiBaseName(string niftiPath)
    {
        string rawName = Path.GetFileName(niftiPath); // puede venir con %3A, %2F
        string decoded = UnityWebRequest.UnEscapeURL(rawName);
        // Por si en algún flujo previo quedaron "_3A" o "_2F"
        decoded = decoded.Replace("_3A", ":").Replace("_2F", "/");

        int slash = Mathf.Max(decoded.LastIndexOf('/'), decoded.LastIndexOf('\\'));
        if (slash >= 0 && slash < decoded.Length - 1)
            decoded = decoded.Substring(slash + 1);

        string nameNoExt = Path.GetFileNameWithoutExtension(decoded); // quita .gz si existía
        if (nameNoExt.EndsWith(".nii", StringComparison.OrdinalIgnoreCase))
            nameNoExt = Path.GetFileNameWithoutExtension(nameNoExt);  // quita .nii

        // elimina timestamp final "_YYYYMMDD_HHMMSS" si ya venía
        nameNoExt = Regex.Replace(nameNoExt, @"_(\d{8}_\d{6})$", "");

        // sanitiza: letras, números, guion y guion bajo
        nameNoExt = Regex.Replace(nameNoExt, @"[^A-Za-z0-9_\-]+", "_").Trim('_');

        return string.IsNullOrWhiteSpace(nameNoExt) ? "nifti" : nameNoExt;
    }

    /// <summary> Devuelve ".nii" o ".nii.gz" (según el path fuente). </summary>
    public static string GetNiftiExtension(string path)
    {
        string fn = Path.GetFileName(path);
        return fn.EndsWith(".nii.gz", StringComparison.OrdinalIgnoreCase) ? ".nii.gz" : Path.GetExtension(fn);
    }

    /// <summary> Construye Id = baseName + "_" + timestamp (yyyyMMdd_HHmmss). </summary>
    private static string BuildCaseId(string baseName)
    {
        baseName ??= "nifti";
        // por si acaso, quita timestamp si existiera
        baseName = Regex.Replace(baseName, @"_(\d{8}_\d{6})$", "");
        string ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        return $"{baseName}_{ts}";
    }
}
