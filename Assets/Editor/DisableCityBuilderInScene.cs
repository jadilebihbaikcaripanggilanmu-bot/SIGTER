#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class DisableCityBuilderInScene
{
    [MenuItem("Tools/Disable Procedural City In Current Scene")]
    public static void DisableCityBuilder()
    {
        var cityBuilders = GameObject.FindObjectsOfType<CityBuilder>();
        int count = 0;
        foreach (var cb in cityBuilders)
        {
            cb.gameObject.SetActive(false);
            count++;
        }

        // Add marker so runtime knows to skip generation
        if (GameObject.Find("UseImportedCityMarker") == null)
        {
            var marker = new GameObject("UseImportedCityMarker");
            Debug.Log("Added UseImportedCityMarker to current scene.");
        }

        if (count > 0)
            Debug.Log($"Disabled {count} CityBuilder objects in the current scene.");
        else
            Debug.Log("No CityBuilder objects found in the current scene.");

        EditorSceneManager.MarkAllScenesDirty();
    }
}
#endif
