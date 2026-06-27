#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Tools/Setup Survey Prototype Scene
/// 
/// Automates the one-time setup of SampleScene:
///   1. Removes stale generated objects.
///   2. Creates a Bootstrap GameObject.
///   3. Creates a CitySceneSetupHelper with the city prefab already assigned.
///   4. Creates PlayerSpawn and TotalStationPlacement markers at good locations.
///   5. Saves the scene.
///   6. Tells the user to press Play.
/// </summary>
public static class SurveySceneSetupTool
{
    private const string PrefabPath =
        "Assets/Versatile Studio Assets/Demo City By Versatile Studio/Prefabs/demo_city_by_versatile_studio.prefab";

    [MenuItem("Tools/Setup Survey Prototype Scene")]
    static void SetupScene()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.name.Contains("Sample") && !scene.name.Contains("sample") &&
            !scene.name.Contains("Survey") && !scene.name.Contains("Prototype"))
        {
            if (!EditorUtility.DisplayDialog("Scene Check",
                $"Active scene is '{scene.name}', not SampleScene.\nContinue anyway?", "Yes", "Cancel"))
                return;
        }

        // ── 1. Remove stale auto-generated objects ────────────────────────
        string[] staleNames = {
            "Bootstrap", "GameManager", "CityBuilder", "Sun",
            "TotalStation", "Player", "CameraRig", "MainCamera",
            "UIManager", "ImportedCityLoader", "UseImportedCityMarker",
            "Safety_Ground_Collider", "Imported_DemoCity_Environment"
        };
        foreach (var n in staleNames)
        {
            var go = GameObject.Find(n);
            if (go) Object.DestroyImmediate(go);
        }
        // Also remove any prism objects
        foreach (var go in Object.FindObjectsOfType<GameObject>())
            if (go.name.StartsWith("Prism_")) Object.DestroyImmediate(go);

        // ── 2. Create Bootstrap ──────────────────────────────────────────
        var bootstrap = new GameObject("Bootstrap");
        bootstrap.AddComponent<Bootstrap>();

        // ── 3. Create CitySceneSetupHelper with prefab reference ─────────
        var helperGO = new GameObject("CitySceneSetupHelper");
        var helper   = helperGO.AddComponent<CitySceneSetupHelper>();

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab != null)
        {
            helper.CityPrefab = prefab;
            Debug.Log("[SurveySceneSetupTool] City prefab assigned to CitySceneSetupHelper.");
        }
        else
        {
            Debug.LogWarning($"[SurveySceneSetupTool] Could not find prefab at:\n{PrefabPath}\n" +
                             "Please assign it manually in the Inspector.");
        }

        // ── 4. Create spawn markers at sensible locations ─────────────────
        // Try to find a good ground location; fall back to default if no prefab
        Vector3 playerSpawn = new Vector3(5f, 2f, 5f);
        Vector3 tsPlacement = new Vector3(15f, 0.5f, 15f);

        if (prefab != null)
        {
            // Instantiate temporarily to sample bounds
            var temp = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            var renderers = temp.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds totalBounds = renderers[0].bounds;
                foreach (var r in renderers) totalBounds.Encapsulate(r.bounds);
                Vector3 centre = totalBounds.center;
                centre.y = totalBounds.min.y + 2f;
                playerSpawn = centre;
                tsPlacement = new Vector3(centre.x + 10f, totalBounds.min.y + 0.5f, centre.z + 10f);
            }
            Object.DestroyImmediate(temp);
        }

        // PlayerSpawn marker
        var psGO = new GameObject("PlayerSpawn");
        psGO.transform.position = playerSpawn;

        // TotalStationPlacement marker
        var tsGO = new GameObject("TotalStationPlacement");
        tsGO.transform.position = tsPlacement;

        // ── 5. Save scene ─────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        // ── 6. Inform user ────────────────────────────────────────────────
        EditorUtility.DisplayDialog(
            "Survey Prototype — Scene Ready",
            "SampleScene has been configured.\n\n" +
            (prefab != null
                ? $"City prefab: demo_city_by_versatile_studio ✓\n"
                : "⚠ City prefab NOT found — assign it manually on CitySceneSetupHelper.\n") +
            "\n▶ Press PLAY to start.\n\n" +
            "Controls:\n" +
            "• WASD        = Move\n" +
            "• Mouse       = Look\n" +
            "• Left Shift  = Sprint\n" +
            "• Space       = Jump\n" +
            "• V           = FPV / TPV\n" +
            "• E           = Interact\n" +
            "• F1 / ESC    = Camera Settings\n",
            "OK"
        );
    }
}
#endif
