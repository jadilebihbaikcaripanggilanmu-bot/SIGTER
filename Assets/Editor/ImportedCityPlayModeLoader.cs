using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

#if UNITY_EDITOR
[InitializeOnLoad]
public static class ImportedCityPlayModeLoader
{
    static ImportedCityPlayModeLoader()
    {
        // Also run when entering PlayMode via the callback
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                EnsureImportedCityInCurrentScene();
            }
        };
    }

    public static void EnsureImportedCityInCurrentScene()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.IsValid()) return;

        // Only run for SampleScene
        if (!scene.path.EndsWith("SampleScene.unity") && !scene.name.Equals("SampleScene"))
            return;

        // If the environment already exists, nothing to do
        if (GameObject.Find("Imported_DemoCity_Environment") != null)
            return;

        // Search for prefabs in the imported asset folder
        string baseFolder = "Assets/Versatile Studio Assets/Demo City By Versatile Studio";
        string prefabsFolder = baseFolder + "/Prefabs";
        string modelsFolder  = baseFolder + "/Models";

        GameObject rootParent = new GameObject("Imported_DemoCity_Environment");
        rootParent.transform.position = Vector3.zero;

        bool instantiated = false;

        if (AssetDatabase.IsValidFolder(prefabsFolder))
        {
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { prefabsFolder });
            // Prefer a single root prefab named like 'city' or 'environment'
            string chosenPath = null;
            foreach (var g in guids)
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                var name = System.IO.Path.GetFileNameWithoutExtension(p).ToLower();
                if (name.Contains("city") || name.Contains("environment") || name.Contains("demo")) { chosenPath = p; break; }
            }

            if (chosenPath != null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(chosenPath);
                if (prefab != null)
                {
                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    inst.transform.SetParent(rootParent.transform, false);
                    inst.transform.localPosition = Vector3.zero;
                    inst.transform.localRotation = Quaternion.identity;
                    inst.transform.localScale = Vector3.one;
                    instantiated = true;
                }
            }
            else
            {
                // No single root prefab; try instantiating several sensible prefabs (large ones)
                foreach (var g in guids)
                {
                    var p = AssetDatabase.GUIDToAssetPath(g);
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                    if (prefab == null) continue;
                    // Heuristic: skip very small prefabs by checking renderer bounds when possible
                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    inst.transform.SetParent(rootParent.transform, false);
                    inst.transform.localPosition = Vector3.zero;
                    inst.transform.localRotation = Quaternion.identity;
                    inst.transform.localScale = Vector3.one;
                    instantiated = true;
                }
            }
        }

        // If no prefabs instantiated, try models folder
        if (!instantiated && AssetDatabase.IsValidFolder(modelsFolder))
        {
            var guids = AssetDatabase.FindAssets("t:Model", new[] { modelsFolder });
            foreach (var g in guids)
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                if (model == null) continue;
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(model);
                inst.transform.SetParent(rootParent.transform, false);
                inst.transform.localPosition = Vector3.zero;
                inst.transform.localRotation = Quaternion.identity;
                inst.transform.localScale = Vector3.one;
                instantiated = true;
            }
        }

        if (!instantiated)
        {
            Debug.LogWarning("[ImportedCityPlayModeLoader] Could not find prefabs or models in '" + baseFolder + "'. Please inspect that folder in the Project window.");
            // ensure marker so CityBuilder won't generate
            if (GameObject.Find("UseImportedCityMarker") == null)
                new GameObject("UseImportedCityMarker");
            return;
        }

        // Place parent at origin
        rootParent.transform.position = Vector3.zero;
        rootParent.transform.rotation = Quaternion.identity;
        rootParent.transform.localScale = Vector3.one;

        // Create marker object so runtime knows imported city is present
        if (GameObject.Find("UseImportedCityMarker") == null)
            new GameObject("UseImportedCityMarker");

        // Remove any cameras or audio listeners that came with the imported prefabs
        var cams = rootParent.GetComponentsInChildren<Camera>(true);
        foreach (var c in cams)
        {
            // disable to avoid duplicate rendering in Game view
            c.enabled = false;
        }
        var listeners = rootParent.GetComponentsInChildren<AudioListener>(true);
        foreach (var l in listeners)
        {
            Object.DestroyImmediate(l);
        }

        // Fix magenta/error materials by assigning a URP Lit fallback when available
        Shader urp = Shader.Find("Universal Render Pipeline/Lit");
        Shader std = Shader.Find("Standard");
        Shader fallback = urp != null ? urp : std;
        if (fallback != null)
        {
            var rends = rootParent.GetComponentsInChildren<Renderer>(true);
            foreach (var r in rends)
            {
                var mats = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    if (m == null) { mats[i] = new Material(fallback); changed = true; continue; }
                    if (m.shader == null || m.shader.name == "Hidden/InternalErrorShader")
                    {
                        var nm = new Material(fallback);
                        nm.color = m.HasProperty("_Color") ? m.color : Color.white;
                        mats[i] = nm;
                        changed = true;
                    }
                }
                if (changed) r.sharedMaterials = mats;
            }
        }

        // Try to find a flat, open spot for PlayerSpawn and TotalStationPlacement
        Vector3 playerPos = Vector3.zero; bool foundPlayer = false;
        Vector3 tsPos = new Vector3(4f, 0f, 4f); bool foundTS = false;

        // Search by name for likely ground objects first
        var candidates = rootParent.GetComponentsInChildren<Transform>(true)
            .Where(t => t.gameObject.GetComponent<Renderer>() != null)
            .Select(t => t.gameObject).ToArray();

        foreach (var go in candidates)
        {
            var name = go.name.ToLower();
            if (!foundPlayer && (name.Contains("road") || name.Contains("sidewalk") || name.Contains("plaza") || name.Contains("parking") || name.Contains("ground")))
            {
                var r = go.GetComponent<Renderer>();
                if (r != null)
                {
                    playerPos = r.bounds.center + Vector3.up * 1.0f;
                    tsPos = r.bounds.center + Vector3.up * 0.0f + Vector3.forward * 2f;
                    foundPlayer = foundTS = true;
                    break;
                }
            }
        }

        // Fallback: search for any large flat renderer
        if (!foundPlayer)
        {
            foreach (var go in candidates)
            {
                var r = go.GetComponent<Renderer>();
                if (r == null) continue;
                var s = r.bounds.size;
                if (s.x > 5f && s.z > 5f && s.y < 3f)
                {
                    playerPos = r.bounds.center + Vector3.up * 1.0f;
                    tsPos = r.bounds.center + Vector3.up * 0.0f + Vector3.forward * 2f;
                    foundPlayer = foundTS = true;
                    break;
                }
            }
        }

        // Create or move markers
        var psObj = GameObject.Find("PlayerSpawn");
        if (psObj == null) psObj = new GameObject("PlayerSpawn");
        psObj.transform.position = playerPos;

        var tsObj = GameObject.Find("TotalStationPlacement");
        if (tsObj == null) tsObj = new GameObject("TotalStationPlacement");
        tsObj.transform.position = tsPos;

        Debug.Log("[ImportedCityPlayModeLoader] Imported city instantiated into SampleScene as 'Imported_DemoCity_Environment'. PlayerSpawn and TotalStationPlacement created.");
    }
}
#endif
