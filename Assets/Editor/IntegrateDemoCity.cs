#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Editor helper: copy imported demo city scene into a new playable scene
public static class IntegrateDemoCity
{
    [MenuItem("Tools/Integrate Demo City As Main Scene")]
    public static void Integrate()
    {
        string importedPath = "Assets/Versatile Studio Assets/Demo City By Versatile Studio/Scenes/demo_city_night.unity";
        string targetPath = "Assets/_Game/Scenes/MainCityPrototype.unity";

        if (!System.IO.File.Exists(importedPath))
        {
            Debug.LogError($"Imported demo scene not found at: {importedPath}");
            return;
        }

        // Open the imported scene additively in the editor to copy its root objects
        var importedScene = EditorSceneManager.OpenScene(importedPath, OpenSceneMode.Additive);
        if (!importedScene.IsValid())
        {
            Debug.LogError("Failed to open imported demo scene.");
            return;
        }

        // Create a new empty scene for our main prototype
        var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Copy root objects from imported scene into the new scene
        var roots = importedScene.GetRootGameObjects();
        foreach (var go in roots)
        {
            // Skip editor-only helpers, cameras or lights named "Main Camera" or "Directional Light" to avoid duplicates
            if (go.name.ToLower().Contains("camera") || go.name.ToLower().Contains("directional"))
                continue;

            var copy = Object.Instantiate(go);
            copy.name = go.name;
            EditorSceneManager.MoveGameObjectToScene(copy, newScene);
        }

        // Add a marker so runtime knows to use the imported city and skip CityBuilder
        var marker = new GameObject("UseImportedCityMarker");
        EditorSceneManager.MoveGameObjectToScene(marker, newScene);

        // Add a PlayerSpawn marker so GameManager will place the player nearby at runtime
        var spawn = new GameObject("PlayerSpawn");
        spawn.transform.position = new Vector3(0f, 1.0f, 0f);
        EditorSceneManager.MoveGameObjectToScene(spawn, newScene);

        // Add a TotalStation placement marker for later positioning
        var tsPlace = new GameObject("TotalStationPlacement");
        tsPlace.transform.position = new Vector3(4f, 0f, 4f);
        EditorSceneManager.MoveGameObjectToScene(tsPlace, newScene);
        // Ensure GameManager exists in the scene (so runtime will spawn player/camera/UI)
        var gmGO = new GameObject("GameManager");
        gmGO.AddComponent<GameManager>();
        EditorSceneManager.MoveGameObjectToScene(gmGO, newScene);

        // Save the new scene asset
        var saved = EditorSceneManager.SaveScene(newScene, targetPath);
        if (saved)
        {
            Debug.Log($"Saved MainCityPrototype scene to: {targetPath}");
            // Close the imported additive scene we opened earlier
            EditorSceneManager.CloseScene(importedScene, true);
            // Open the new scene
            EditorSceneManager.OpenScene(targetPath, OpenSceneMode.Single);
        }
        else
        {
            Debug.LogError("Failed to save the new MainCityPrototype scene.");
        }
    }
}
#endif
