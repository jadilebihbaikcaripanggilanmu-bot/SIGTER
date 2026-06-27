using UnityEngine;

/// <summary>
/// Runtime helper: ensures imported city marker exists, cleans up old procedural objects,
/// and provides spawn marker placement fallback in case editor-time import didn't run.
/// This component does not instantiate heavy imported assets at runtime (Editor handles that),
/// but it ensures the scene is safe to run and removes leftover procedural geometry.
/// </summary>
public class ImportedCityLoader : MonoBehaviour
{
    void Awake()
    {
        GameObject city = FindCityInActiveScene();
        if (city != null)
        {
            city.SetActive(true);
            Debug.Log($"[ImportedCityLoader] Found and activated city: '{city.name}'");
            return;
        }

        // Create a marker so CityBuilder will skip procedural generation
        if (FindGameObjectInActiveScene("UseImportedCityMarker") == null)
        {
            var m = new GameObject("UseImportedCityMarker");
            m.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
        }

        // Remove any leftover procedural city root named "CityBuilder"
        var cityRoot = FindGameObjectInActiveScene("CityBuilder");
        if (cityRoot != null)
        {
            Destroy(cityRoot);
        }

        // If no Imported_DemoCity_Environment exists, at least ensure spawn markers exist
        if (FindGameObjectInActiveScene("PlayerSpawn") == null)
        {
            var ps = new GameObject("PlayerSpawn");
            ps.transform.position = new Vector3(0f, 1f, -2f);
            ps.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
        }

        if (FindGameObjectInActiveScene("TotalStationPlacement") == null)
        {
            var ts = new GameObject("TotalStationPlacement");
            ts.transform.position = new Vector3(4f, 0f, 4f);
            ts.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
        }
    }

    private GameObject FindCityInActiveScene()
    {
        var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var r in roots)
        {
            var nameLower = r.name.ToLower();
            if (nameLower.Contains("imported_democity") || 
                nameLower.Contains("imported_demo city") || 
                nameLower.Contains("demo_city_by_versatile"))
            {
                return r;
            }
        }
        return null;
    }

    private GameObject FindGameObjectInActiveScene(string name)
    {
        var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var r in roots)
        {
            if (r.name == name) return r;
        }
        return null;
    }
}
