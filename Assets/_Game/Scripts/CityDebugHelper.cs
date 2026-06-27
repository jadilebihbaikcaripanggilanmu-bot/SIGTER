using UnityEngine;
using System.IO;
using System.Text;

public class CityDebugHelper : MonoBehaviour
{
    private string logPath;

    System.Collections.IEnumerator Start()
    {
        logPath = Path.Combine(Application.persistentDataPath, "city_debug_log.txt");
        File.WriteAllText(logPath, "=== CITY DEBUG LOG ===\n");
        Log("Game Started. Waiting 1.5 seconds for setup to complete...");
        
        yield return new UnityEngine.WaitForSeconds(1.5f);
        
        InspectScene();
    }

    void InspectScene()
    {
        Log($"Current Active Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");

        // Find city root(s) — support both imported prefab and demo scene objects
        var cityRoots = new System.Collections.Generic.List<GameObject>();
        var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        Log($"Total Root GameObjects: {roots.Length}");
        
        foreach (var r in roots)
        {
            Log($"Root: '{r.name}' | ActiveSelf: {r.activeSelf} | Tag: {r.tag}");
            if (r.name.Contains("Imported_DemoCity") || r.name.Contains("demo_city") ||
                r.name == "ENVIRONMENT_OBJECTS" || r.name == "STATIC_MODELS")
            {
                cityRoots.Add(r);
            }
        }

        if (cityRoots.Count == 0)
        {
            Log("ERROR: No city root objects found in scene!");
            return;
        }

        foreach (var city in cityRoots)
        {
            Log($"Found City Root: '{city.name}'");
            Log($"City Transform - Position: {city.transform.position}, Rotation: {city.transform.rotation.eulerAngles}, Scale: {city.transform.localScale}");
            Log($"City ActiveSelf: {city.activeSelf}, ActiveInHierarchy: {city.activeInHierarchy}");
        }

        // Check markers
        var markerPS = GameObject.Find("PlayerSpawn");
        if (markerPS != null)
        {
            Log($"PlayerSpawn Marker Position: {markerPS.transform.position}");
        }
        else
        {
            Log("PlayerSpawn Marker NOT found!");
        }

        var markerTS = GameObject.Find("TotalStationPlacement");
        if (markerTS != null)
        {
            Log($"TotalStationPlacement Marker Position: {markerTS.transform.position}");
        }
        else
        {
            Log("TotalStationPlacement Marker NOT found!");
        }

        // Check child meshes across all city roots
        int totalMeshes = 0;
        int activeMeshes = 0;
        int activeRenderers = 0;

        foreach (var city in cityRoots)
        {
            var meshFilters = city.GetComponentsInChildren<MeshFilter>(true);
            Log($"City Root '{city.name}' has {meshFilters.Length} MeshFilters");

            for (int i = 0; i < meshFilters.Length; i++)
            {
                var mf = meshFilters[i];
                if (mf.gameObject.activeInHierarchy) activeMeshes++;
                
                var mr = mf.GetComponent<MeshRenderer>();
                if (mr != null && mr.enabled) activeRenderers++;

                if (totalMeshes < 30) // Log first 30 meshes with shader info
                {
                    string shaderName = "N/A";
                    if (mr != null && mr.sharedMaterial != null && mr.sharedMaterial.shader != null)
                        shaderName = mr.sharedMaterial.shader.name;
                    Log($"  Mesh {totalMeshes}: '{mf.name}' | Active: {mf.gameObject.activeInHierarchy} | Shader: {shaderName} | WorldPos: {mf.transform.position}");
                }
                totalMeshes++;
            }
        }
        Log($"Total MeshFilters: {totalMeshes}");
        Log($"Active Mesh GameObjects: {activeMeshes}/{totalMeshes}");
        Log($"Enabled MeshRenderers: {activeRenderers}/{totalMeshes}");

        // Find Player
        var player = GameObject.FindWithTag("Player");
        if (player == null) player = GameObject.Find("Player");
        if (player != null)
        {
            Log($"Player Found - Position: {player.transform.position}");
            var cc = player.GetComponent<CharacterController>();
            if (cc != null) Log($"  CharacterController Enabled: {cc.enabled}, Center: {cc.center}, Height: {cc.height}");
        }
        else
        {
            Log("ERROR: Player GameObject not found!");
        }

        // Find Main Camera
        var cam = Camera.main;
        if (cam != null)
        {
            Log($"Main Camera Found - Position: {cam.transform.position}, Rotation: {cam.transform.rotation.eulerAngles}, Near: {cam.nearClipPlane}, Far: {cam.farClipPlane}");
        }
        else
        {
            Log("ERROR: Main Camera not found!");
        }
    }

    void Log(string message)
    {
        Debug.Log("[CityDebug] " + message);
        try
        {
            File.AppendAllText(logPath, message + "\n");
        }
        catch {}
    }
}
