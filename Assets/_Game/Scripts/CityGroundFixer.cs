using System.Collections;
using UnityEngine;

/// <summary>
/// Re-positioned to ONLY handle safety ground positioning.
/// Kept active with no dynamic colliders (using built-in city_part_collider).
/// This prevents any physics overlapping/launching bugs that cause floating.
/// </summary>
public class CityGroundFixer : MonoBehaviour
{
    public static bool    SetupComplete   { get; private set; } = false;
    public static Vector3 SpawnPosition   { get; private set; } = new Vector3(136f, 5.5f, -92f);
    public static Vector3 StationPosition { get; private set; } = new Vector3(4f, 0.5f, 4f);

    void Awake() => SetupComplete = false;

    void Start() => StartCoroutine(Run());

    IEnumerator Run()
    {
        var cityRoots = FindCityRoots();
        
        // Calculate combined city bounds
        Bounds bounds = new Bounds(Vector3.zero, Vector3.one * 500f);
        bool boundsInit = false;
        foreach (var root in cityRoots)
        {
            var b = CalcBounds(root);
            if (b.size.magnitude > 0.1f)
            {
                if (!boundsInit) { bounds = b; boundsInit = true; }
                else bounds.Encapsulate(b);
            }
        }

        // Reposition Safety_Ground_Collider under the city
        FixSafetyGround(bounds);

        yield return new WaitForFixedUpdate();
        yield return null;
        Physics.SyncTransforms();

        // Signal GameManager that setup is ready
        SetupComplete = true;
        Destroy(gameObject);
    }

    GameObject[] FindCityRoots()
    {
        var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        var found = new System.Collections.Generic.List<GameObject>();

        foreach (var r in roots)
        {
            var nameLower = r.name.ToLower();
            if (nameLower.Contains("imported_democity") || 
                nameLower.Contains("imported_demo city") || 
                nameLower.Contains("demo_city_by_versatile") ||
                nameLower.Contains("demo city by versatile"))
            {
                r.SetActive(true);
                found.Add(r);
                continue;
            }

            if (r.name == "ENVIRONMENT_OBJECTS" || r.name == "STATIC_MODELS")
            {
                r.SetActive(true);
                found.Add(r);
                continue;
            }
        }
        return found.ToArray();
    }

    Bounds CalcBounds(GameObject root)
    {
        var rends = root.GetComponentsInChildren<Renderer>(true);
        if (rends.Length == 0) return new Bounds(Vector3.zero, Vector3.one * 500f);
        var b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        return b;
    }

    void FixSafetyGround(Bounds b)
    {
        var ground = GameObject.Find("Safety_Ground_Collider");
        if (ground != null)
        {
            ground.transform.position = new Vector3(b.center.x, b.min.y - 10f, b.center.z);
            var col = ground.GetComponent<BoxCollider>();
            if (col != null) col.size = new Vector3(b.size.x * 2.5f, 2f, b.size.z * 2.5f);
        }
    }
}
