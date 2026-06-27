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
        bool cityExists = GameObject.Find("Imported_DemoCity_Environment") != null
                       || GameObject.Find("demo_city_by_versatile_studio") != null;

        GameObject cityInstance = null;
        if (!cityExists)
        {
            cityInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            cityInstance.name = "Imported_DemoCity_Environment";
            cityInstance.transform.position = Vector3.zero;
            Debug.Log("[CitySceneSetup] ✅ City prefab instantiated.");
        }
        else
        {
            cityInstance = GameObject.Find("Imported_DemoCity_Environment")
                       ?? GameObject.Find("demo_city_by_versatile_studio");
            Debug.Log("[CitySceneSetup] City already in scene — skipped instantiation.");
        }

        // ── 3. Create UseImportedCityMarker ──────────────────────────────────
        if (GameObject.Find("UseImportedCityMarker") == null)
        {
            new GameObject("UseImportedCityMarker");
            Debug.Log("[CitySceneSetup] ✅ UseImportedCityMarker created.");
        }

        // ── 4. Ensure Bootstrap exists ───────────────────────────────────────
        var bootstrapGO = GameObject.Find("Bootstrap");
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
        var helperGO = GameObject.Find("CitySceneSetupHelper");
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

    [MenuItem("Surveyor/Open Demo City Night Scene", false, 20)]
    public static void OpenDemoCityScene()
    {
        const string scenePath =
            "Assets/Versatile Studio Assets/Demo City By Versatile Studio/Scenes/demo_city_night.unity";

        if (!System.IO.File.Exists(scenePath))
        {
            EditorUtility.DisplayDialog("Scene Not Found",
                $"Could not find:\n{scenePath}", "OK");
            return;
        }

        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.OpenScene(scenePath);
    }
}
#endif
