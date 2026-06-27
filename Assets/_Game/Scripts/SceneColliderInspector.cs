using UnityEngine;
using System.IO;
using System.Text;

public class SceneColliderInspector : MonoBehaviour
{
    void Start()
    {
        string logPath = Path.Combine(Application.persistentDataPath, "collider_inspection_log.txt");
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("=== COLLIDER INSPECTION LOG ===");

        // Find all colliders in the scene
        var colliders = Object.FindObjectsByType<Collider>(FindObjectsSortMode.None);
        sb.AppendLine($"Total Colliders in Scene: {colliders.Length}");

        foreach (var col in colliders)
        {
            sb.AppendLine($"- Name: '{col.gameObject.name}' | Type: {col.GetType().Name} | Enabled: {col.enabled} | Trigger: {col.isTrigger} | Bounds Center: {col.bounds.center} | Bounds Size: {col.bounds.size}");
        }

        // Specifically inspect STATIC_MODELS hierarchy
        var staticModels = GameObject.Find("STATIC_MODELS");
        if (staticModels != null)
        {
            sb.AppendLine("\n=== STATIC_MODELS HIERARCHY ===");
            InspectTransform(staticModels.transform, sb, 0);
        }
        else
        {
            sb.AppendLine("\nSTATIC_MODELS root not found!");
        }

        File.WriteAllText(logPath, sb.ToString());
        Debug.Log("[Inspector] Collider inspection complete. Log saved to collider_inspection_log.txt");
    }

    void InspectTransform(Transform t, StringBuilder sb, int indent)
    {
        string indentStr = new string(' ', indent * 2);
        var col = t.GetComponent<Collider>();
        string colInfo = col != null ? $" [Collider: {col.GetType().Name}, Enabled: {col.enabled}]" : "";
        var mf = t.GetComponent<MeshFilter>();
        string meshInfo = mf != null && mf.sharedMesh != null ? $" [Mesh: {mf.sharedMesh.name}]" : "";
        
        sb.AppendLine($"{indentStr}- '{t.name}'{colInfo}{meshInfo} | Pos: {t.position} | Active: {t.gameObject.activeSelf}");

        for (int i = 0; i < t.childCount; i++)
        {
            InspectTransform(t.GetChild(i), sb, indent + 1);
        }
    }
}
