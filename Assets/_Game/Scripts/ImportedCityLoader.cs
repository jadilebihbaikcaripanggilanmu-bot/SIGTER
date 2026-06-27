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
        // If imported environment already present, nothing to do
        if (GameObject.Find("Imported_DemoCity_Environment") != null)
            return;

        // Create a marker so CityBuilder will skip procedural generation
        if (GameObject.Find("UseImportedCityMarker") == null)
        {
            var m = new GameObject("UseImportedCityMarker");
            m.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
        }

        // Remove any leftover procedural city root named "CityBuilder"
        var cityRoot = GameObject.Find("CityBuilder");
        if (cityRoot != null)
        {
            // Destroy at runtime to ensure no geometry remains
            Destroy(cityRoot);
        }

        // If no Imported_DemoCity_Environment exists, at least ensure spawn markers exist
        if (GameObject.Find("PlayerSpawn") == null)
        {
            var ps = new GameObject("PlayerSpawn");
            ps.transform.position = new Vector3(0f, 1f, -2f);
            ps.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
        }

        if (GameObject.Find("TotalStationPlacement") == null)
        {
            var ts = new GameObject("TotalStationPlacement");
            ts.transform.position = new Vector3(4f, 0f, 4f);
            ts.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
        }
    }
}
