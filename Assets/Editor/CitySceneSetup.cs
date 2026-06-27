#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Editor-only helper: one-click city scene setup.
///
/// Menu:  Surveyor ▸ Setup City Scene
///
/// What it does:
///   1. Checks if the Demo City prefab is already in the scene.
///   2. If not, instantiates it from the known asset path.
///   3. Creates a Bootstrap GameObject (so the game runs correctly in Play mode).
///   4. Creates a CitySceneSetupHelper with the prefab pre-assigned.
///   5. Marks the scene dirty so you can Ctrl+S to save.
/// </summary>
public static class CitySceneSetup
{
    // Known path to the city prefab inside the project
    private const string CityPrefabPath =
        "Assets/Versatile Studio Assets/Demo City By Versatile Studio/Prefabs/demo_city_by_versatile_studio.prefab";

    [MenuItem("Surveyor/Setup City Scene", false, 10)]
    public static void SetupCityScene()
    {
        // ── 1. Load the prefab asset ─────────────────────────────────────────
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CityPrefabPath);
        if (prefab == null)
        {
            EditorUtility.DisplayDialog(
                "Prefab Not Found",
                $"Could not find city prefab at:\n{CityPrefabPath}\n\n" +
                "Make sure the Demo City asset is imported via Package Manager.",
                "OK");
            return;
        }

        // ── 2. Instantiate city if not already present ───────────────────────
        GameObject cityInstance = FindCityInActiveScene();
        if (cityInstance == null)
        {
            cityInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            cityInstance.name = "Imported_DemoCity_Environment";
            cityInstance.transform.position = Vector3.zero;
            cityInstance.SetActive(true);
            Debug.Log("[CitySceneSetup] ✅ City prefab instantiated and activated.");
        }
        else
        {
            cityInstance.SetActive(true);
            Debug.Log("[CitySceneSetup] City already in scene — activated it.");
        }

        // ── 3. Create UseImportedCityMarker ──────────────────────────────────
        if (FindGameObjectInActiveScene("UseImportedCityMarker") == null)
        {
            new GameObject("UseImportedCityMarker");
            Debug.Log("[CitySceneSetup] ✅ UseImportedCityMarker created.");
        }

        // ── 4. Ensure Bootstrap exists ───────────────────────────────────────
        var bootstrapGO = FindGameObjectInActiveScene("Bootstrap");
        if (bootstrapGO == null)
        {
            bootstrapGO = new GameObject("Bootstrap");
            bootstrapGO.AddComponent<Bootstrap>();
            Debug.Log("[CitySceneSetup] ✅ Bootstrap GameObject created.");
        }
        else if (bootstrapGO.GetComponent<Bootstrap>() == null)
        {
            bootstrapGO.AddComponent<Bootstrap>();
        }

        // ── 5. Ensure CitySceneSetupHelper exists with prefab assigned ───────
        var helperGO = FindGameObjectInActiveScene("CitySceneSetupHelper");
        if (helperGO == null)
        {
            helperGO = new GameObject("CitySceneSetupHelper");
            Debug.Log("[CitySceneSetup] ✅ CitySceneSetupHelper GameObject created.");
        }
        var helper = helperGO.GetComponent<CitySceneSetupHelper>();
        if (helper == null)
            helper = helperGO.AddComponent<CitySceneSetupHelper>();
        helper.CityPrefab = prefab;
        Debug.Log("[CitySceneSetup] ✅ CityPrefab assigned to CitySceneSetupHelper.");

        // ── 6. Mark scene dirty ──────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog(
            "City Scene Setup Complete! 🏙️",
            "The Demo City has been added to the scene.\n\n" +
            "Next steps:\n" +
            "1. Press Ctrl+S to save the scene\n" +
            "2. Press ▶ Play to test\n" +
            "3. Use WASD to walk, Mouse to look around",
            "Got it!");

        Debug.Log("[CitySceneSetup] ═══ Scene setup complete! Press Ctrl+S to save, then Play. ═══");
    }

    [MenuItem("Surveyor/Setup & Open Demo City Night Scene", false, 20)]
    public static void OpenDemoCityScene()
    {
        const string scenePath =
            "Assets/Versatile Studio Assets/Demo City By Versatile Studio/Scenes/demo_city_night.unity";

        if (!System.IO.File.Exists(scenePath))
        {
            EditorUtility.DisplayDialog("Scene Not Found",
                $"Could not find scene at:\n{scenePath}", "OK");
            return;
        }

        // Open the scene
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            var scene = EditorSceneManager.OpenScene(scenePath);
            
            // 1. Ensure Bootstrap exists in the demo scene
            var bootstrap = FindGameObjectInActiveScene("Bootstrap");
            if (bootstrap == null)
            {
                bootstrap = new GameObject("Bootstrap");
                bootstrap.AddComponent<Bootstrap>();
                Debug.Log("[CitySceneSetup] ✅ Created Bootstrap in Demo City scene.");
            }

            // 2. Ensure PlayerSpawn marker exists at correct ROAD-LEVEL coordinates
            //    Based on debug data: city_part_demo_main1 road at (136.21, 4.84, -104.56)
            //    Spawn player 2m above road surface (Y=5 + 2 = 7)
            var playerSpawn = FindGameObjectInActiveScene("PlayerSpawn");
            if (playerSpawn == null)
            {
                playerSpawn = new GameObject("PlayerSpawn");
            }
            // Always update position — on the ROAD, not on a building
            playerSpawn.transform.position = new Vector3(136f, 5.5f, -104f);
            Debug.Log("[CitySceneSetup] ✅ PlayerSpawn set to road coordinates (136, 5.5, -104).");

            // 3. Ensure UseImportedCityMarker exists
            if (FindGameObjectInActiveScene("UseImportedCityMarker") == null)
            {
                new GameObject("UseImportedCityMarker");
            }

            // 4. Save the configured scene
            EditorSceneManager.SaveScene(scene);
            
            EditorUtility.DisplayDialog("Demo City Configured! 🏙️",
                "The official Demo City scene has been successfully opened and configured with WASD controls!\n\n" +
                "Just press ▶ Play to walk around the city with skyscrapers!",
                "Got it!");
        }
    }

    private static GameObject FindCityInActiveScene()
    {
        var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var r in roots)
        {
            var nameLower = r.name.ToLower();
            if (nameLower.Contains("imported_democity") || 
                nameLower.Contains("imported_demo city") || 
                nameLower.Contains("demo_city_by_versatile") ||
                nameLower.Contains("demo city by versatile"))
            {
                return r;
            }
        }
        return null;
    }

    private static GameObject FindGameObjectInActiveScene(string name)
    {
        var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var r in roots)
        {
            if (r.name == name) return r;
        }
        return null;
    }
}
#endif
