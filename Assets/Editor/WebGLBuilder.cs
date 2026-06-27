using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Linq;
using UnityEngine;

public class WebGLBuilder
{
    private const string CityPrefabPath =
        "Assets/Versatile Studio Assets/Demo City By Versatile Studio/Prefabs/demo_city_by_versatile_studio.prefab";

    [MenuItem("Build/Build WebGL")]
    public static void Build()
    {
        // ── 1. Setup and Save the scene programmatically to ensure it has the city ──
        string scenePath = "Assets/Scenes/SampleScene.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        
        // Instantiate city if not already present
        GameObject cityInstance = GameObject.Find("Imported_DemoCity_Environment");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CityPrefabPath);
        if (prefab == null)
        {
            Debug.LogError("City prefab not found at: " + CityPrefabPath);
            return;
        }

        if (cityInstance == null)
        {
            cityInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            cityInstance.name = "Imported_DemoCity_Environment";
            cityInstance.transform.position = Vector3.zero;
            cityInstance.SetActive(true);
            Debug.Log("[WebGLBuilder] Pre-instantiated city prefab in scene.");
        }

        // Ensure UseImportedCityMarker exists
        if (GameObject.Find("UseImportedCityMarker") == null)
        {
            new GameObject("UseImportedCityMarker");
            Debug.Log("[WebGLBuilder] Created UseImportedCityMarker.");
        }

        // Ensure Bootstrap exists
        var bootstrapGO = GameObject.Find("Bootstrap");
        if (bootstrapGO == null)
        {
            bootstrapGO = new GameObject("Bootstrap");
            bootstrapGO.AddComponent<Bootstrap>();
            Debug.Log("[WebGLBuilder] Created Bootstrap.");
        }

        // Ensure CitySceneSetupHelper exists
        var helperGO = GameObject.Find("CitySceneSetupHelper");
        if (helperGO == null)
        {
            helperGO = new GameObject("CitySceneSetupHelper");
            Debug.Log("[WebGLBuilder] Created CitySceneSetupHelper.");
        }
        var helper = helperGO.GetComponent<CitySceneSetupHelper>();
        if (helper == null)
            helper = helperGO.AddComponent<CitySceneSetupHelper>();
        helper.CityPrefab = prefab;

        // Save scene
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[WebGLBuilder] Scene saved successfully.");

        // ── 2. Run WebGL Build ──────────────────────────────────────────────────
        string buildPath = Path.Combine(Directory.GetCurrentDirectory(), "Builds/WebGL");
        
        // Ensure build directory exists
        if (Directory.Exists(buildPath))
        {
            try
            {
                Directory.Delete(buildPath, true);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Failed to delete existing build folder: " + e.Message);
            }
        }
        Directory.CreateDirectory(buildPath);

        // Configure WebGL settings
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new string[] { scenePath };
        buildPlayerOptions.locationPathName = buildPath;
        buildPlayerOptions.target = BuildTarget.WebGL;
        buildPlayerOptions.options = BuildOptions.None;

        Debug.Log("Starting WebGL build to: " + buildPath);
        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        var summary = report.summary;

        if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log("WebGL build succeeded! Size: " + summary.totalSize + " bytes.");
        }
        else if (summary.result == UnityEditor.Build.Reporting.BuildResult.Failed)
        {
            Debug.LogError("WebGL build failed!");
        }
    }
}
