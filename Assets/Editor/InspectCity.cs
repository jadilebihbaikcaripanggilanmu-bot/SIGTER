#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public static class InspectCity
{
    [MenuItem("Surveyor/Inspect City Environment", false, 30)]
    public static void Inspect()
    {
        var city = GameObject.Find("Imported_DemoCity_Environment");
        if (city == null)
        {
            // Search inactive too
            var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var r in roots)
            {
                if (r.name.Contains("Imported_DemoCity_Environment") || r.name.Contains("demo_city"))
                {
                    city = r;
                    break;
                }
            }
        }

        if (city == null)
        {
            Debug.LogError("[InspectCity] City environment object not found in scene!");
            return;
        }

        Debug.Log($"[InspectCity] Found City: '{city.name}' | ActiveSelf: {city.activeSelf} | ActiveInHierarchy: {city.activeInHierarchy}");
        Debug.Log($"[InspectCity] Position: {city.transform.position} | Rotation: {city.transform.rotation.eulerAngles} | Scale: {city.transform.localScale}");

        var filters = city.GetComponentsInChildren<MeshFilter>(true);
        Debug.Log($"[InspectCity] Total MeshFilters in children: {filters.Length}");

        if (filters.Length > 0)
        {
            Bounds b = filters[0].GetComponent<Renderer>().bounds;
            for (int i = 1; i < filters.Length; i++)
            {
                var r = filters[i].GetComponent<Renderer>();
                if (r != null) b.Encapsulate(r.bounds);
            }
            Debug.Log($"[InspectCity] Calculated Bounds: Center={b.center} | Size={b.size} | Min={b.min} | Max={b.max}");

            // Log first 5 children positions
            int count = Mathf.Min(5, filters.Length);
            for (int i = 0; i < count; i++)
            {
                Debug.Log($"[InspectCity] Child Mesh {i}: '{filters[i].name}' | LocalPos: {filters[i].transform.localPosition} | WorldPos: {filters[i].transform.position}");
            }
        }
    }
}
#endif
