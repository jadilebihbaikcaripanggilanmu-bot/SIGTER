using System.Collections;
using UnityEngine;

/// <summary>
/// Runs as a coroutine BEFORE the player is spawned.
/// 
/// Phase A (same frame as Start):
///   1. Find the imported city root.
///   2. Add MeshColliders to all city mesh objects.
///   3. Fix broken/pink materials with URP-compatible fallbacks.
///   4. Calculate true city bounds.
///   5. Reposition Safety_Ground_Collider under the actual city.
///
/// Phase B (after two physics ticks — colliders are now registered):
///   6. Raycast grid to find lowest walkable ground.
///   7. Expose SpawnPosition, StationPosition, and SetupComplete for GameManager.
///
/// GameManager uses IEnumerator Start() + WaitUntil(SetupComplete) to
/// guarantee the player only spawns AFTER real ground is found.
/// </summary>
public class CityGroundFixer : MonoBehaviour
{
    // ── Static signals — read by GameManager ─────────────────────────────────
    public static bool    SetupComplete   { get; private set; } = false;
    public static Vector3 SpawnPosition   { get; private set; } = new Vector3(0f, 5f, 0f);
    public static Vector3 StationPosition { get; private set; } = new Vector3(4f, 0.5f, 4f);

    // Reset statics when a new Play session starts (important in Editor)
    void Awake() => SetupComplete = false;

    void Start() => StartCoroutine(Run());

    // ── Main coroutine ────────────────────────────────────────────────────────
    IEnumerator Run()
    {
        // ── 1. Find city root ────────────────────────────────────────────────
        var city = FindCityRoot();
        if (city != null)
            Debug.Log($"[CityGroundFixer] City root: '{city.name}'");
        else
            Debug.LogWarning("[CityGroundFixer] City root not found — colliders/materials skipped.");

        // ── 2. Add MeshColliders ─────────────────────────────────────────────
        int colCount = city != null ? AddColliders(city) : 0;
        Debug.Log($"[CityGroundFixer] Added {colCount} MeshColliders to city.");

        // ── 3. Fix broken (pink) materials ───────────────────────────────────
        int matCount = city != null ? FixMaterials(city) : 0;
        Debug.Log($"[CityGroundFixer] Fixed {matCount} broken materials.");

        // ── 4. Calculate city bounds ─────────────────────────────────────────
        Bounds bounds = city != null ? CalcBounds(city) : new Bounds(Vector3.zero, Vector3.one * 500f);
        Debug.Log($"[CityGroundFixer] Bounds: center={bounds.center:F1}  min={bounds.min:F1}  max={bounds.max:F1}");

        // ── 5. Reposition Safety_Ground_Collider under city ──────────────────
        FixSafetyGround(bounds);

        // ── Wait two physics ticks so new MeshColliders are registered ────────
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        yield return null;
        Physics.SyncTransforms();

        // ── 6. Find walkable ground via raycast grid ─────────────────────────
        SpawnPosition   = FindSpawn(bounds);
        StationPosition = RaycastGround(SpawnPosition + Vector3.forward * 5f, bounds, 0.5f);
        Debug.Log($"[CityGroundFixer] Player spawn : {SpawnPosition:F2}");
        Debug.Log($"[CityGroundFixer] Station pos  : {StationPosition:F2}");

        // ── Signal GameManager that it is safe to spawn the player ───────────
        SetupComplete = true;
        Destroy(gameObject);
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  CITY ROOT SEARCH
    // ══════════════════════════════════════════════════════════════════════════

    static readonly string[] CityRootNames =
    {
        "Imported_DemoCity_Environment",
        "Imported_Demo City_Environment",   // name with space (sometimes created)
        "demo_city_by_versatile_studio",
        "Demo City By Versatile Studio",
    };

    GameObject FindCityRoot()
    {
        foreach (var n in CityRootNames)
        {
            var go = GameObject.Find(n);
            if (go != null) return go;
        }
        // Broad fallback: large root-level object with city-related name
        foreach (var go in FindObjectsOfType<GameObject>())
        {
            if (go.transform.parent != null) continue;
            var nl = go.name.ToLower();
            if (nl.Contains("city") || nl.Contains("versatile") || nl.Contains("imported"))
                return go;
        }
        return null;
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  ADD MESH COLLIDERS
    // ══════════════════════════════════════════════════════════════════════════

    int AddColliders(GameObject root)
    {
        int count = 0;
        var meshFilters = root.GetComponentsInChildren<MeshFilter>(true);

        foreach (var mf in meshFilters)
        {
            var go = mf.gameObject;
            if (mf.sharedMesh == null) continue;
            if (go.GetComponent<Collider>() != null) continue;  // already has one

            // Skip tiny decorative pieces that won't affect gameplay
            var rend = go.GetComponent<Renderer>();
            if (rend != null && rend.bounds.size.magnitude < 0.4f) continue;

            var col        = go.AddComponent<MeshCollider>();
            col.sharedMesh = mf.sharedMesh;
            col.convex     = false;   // non-convex = correct shape for static geo
            count++;
        }
        return count;
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  FIX BROKEN (PINK) MATERIALS
    // ══════════════════════════════════════════════════════════════════════════

    int FixMaterials(GameObject root)
    {
        // Find best available URP-compatible shader
        var urpLit    = Shader.Find("Universal Render Pipeline/Lit");
        var urpSimple = Shader.Find("Universal Render Pipeline/Simple Lit");
        var standard  = Shader.Find("Standard");
        var fallback  = urpLit ?? urpSimple ?? standard;
        if (fallback == null) return 0;

        int count = 0;
        foreach (var rend in root.GetComponentsInChildren<Renderer>(true))
        {
            var mats   = rend.sharedMaterials;
            bool dirty = false;

            for (int i = 0; i < mats.Length; i++)
            {
                var m = mats[i];
                if (!NeedsReplacement(m)) continue;

                var nm = new Material(fallback);

                if (m != null)
                {
                    // Preserve main texture if possible
                    var tex = m.mainTexture;
                    if (tex != null)
                    {
                        nm.mainTexture = tex;
                        if (fallback == urpLit) nm.SetTexture("_BaseMap", tex);
                    }

                    // Preserve color
                    if (m.HasProperty("_Color"))
                    {
                        Color c = m.GetColor("_Color");
                        if (fallback == urpLit) nm.SetColor("_BaseColor", c);
                        else                    nm.color = c;
                    }
                    else if (m.HasProperty("_BaseColor") && fallback == urpLit)
                    {
                        nm.SetColor("_BaseColor", m.GetColor("_BaseColor"));
                    }
                }
                else
                {
                    // Null material — assign neutral color by object name
                    Color c = NeutralColor(rend.gameObject.name.ToLower());
                    if (fallback == urpLit) nm.SetColor("_BaseColor", c);
                    else                    nm.color = c;
                }

                mats[i] = nm;
                dirty    = true;
                count++;
            }

            if (dirty) rend.sharedMaterials = mats;
        }
        return count;
    }

    bool NeedsReplacement(Material m)
    {
        if (m == null) return true;
        if (m.shader == null) return true;
        var sn = m.shader.name;
        return sn == "Hidden/InternalErrorShader"
            || sn.StartsWith("Hidden/")
            || sn == "Standard"
            || sn == "Standard (Specular setup)"
            || sn.StartsWith("Mobile/")
            || sn.StartsWith("Legacy Shaders/");
    }

    Color NeutralColor(string name)
    {
        if (name.Contains("road")  || name.Contains("asphalt")) return new Color(0.20f, 0.20f, 0.22f);
        if (name.Contains("side")  || name.Contains("walk"))    return new Color(0.72f, 0.70f, 0.65f);
        if (name.Contains("build") || name.Contains("wall"))    return new Color(0.80f, 0.78f, 0.74f);
        if (name.Contains("tree")  || name.Contains("grass"))   return new Color(0.28f, 0.55f, 0.20f);
        if (name.Contains("roof"))                              return new Color(0.28f, 0.30f, 0.38f);
        if (name.Contains("glass") || name.Contains("window"))  return new Color(0.40f, 0.60f, 0.85f);
        return new Color(0.70f, 0.68f, 0.64f); // default: concrete gray
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  CITY BOUNDS
    // ══════════════════════════════════════════════════════════════════════════

    Bounds CalcBounds(GameObject root)
    {
        var rends = root.GetComponentsInChildren<Renderer>(true);
        if (rends.Length == 0) return new Bounds(Vector3.zero, Vector3.one * 500f);
        var b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        return b;
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  SAFETY GROUND — repositioned under actual city
    // ══════════════════════════════════════════════════════════════════════════

    void FixSafetyGround(Bounds b)
    {
        var go = GameObject.Find("Safety_Ground_Collider");
        if (go == null)
        {
            go = new GameObject("Safety_Ground_Collider");
            go.AddComponent<BoxCollider>();
        }

        // Disable any stray renderer on the safety object
        var mr = go.GetComponent<MeshRenderer>();
        if (mr) mr.enabled = false;

        float sizeX = Mathf.Max(b.size.x + 300f, 600f);
        float sizeZ = Mathf.Max(b.size.z + 300f, 600f);
        float posY  = b.min.y - 1.5f;   // just below lowest city surface

        go.transform.position = new Vector3(b.center.x, posY, b.center.z);

        var col    = go.GetComponent<BoxCollider>() ?? go.AddComponent<BoxCollider>();
        col.size   = new Vector3(sizeX, 0.5f, sizeZ);
        col.center = Vector3.zero;

        Debug.Log($"[CityGroundFixer] Safety_Ground_Collider → pos={go.transform.position:F1}  box={col.size:F0}");
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  SPAWN POSITION — grid raycast across city footprint
    // ══════════════════════════════════════════════════════════════════════════

    Vector3 FindSpawn(Bounds bounds)
    {
        float fromY      = bounds.max.y + 120f;
        float maxDist    = bounds.size.y  + 250f;

        // Accept ground hits that are within the lower 20% of city height
        // (streets and sidewalks, not building roofs)
        float groundCeil = bounds.min.y + Mathf.Min(bounds.size.y * 0.20f, 15f);

        Vector3 best  = Vector3.zero;
        float   bestY = float.MaxValue;
        bool    found = false;

        // 5×5 grid across city XZ footprint
        for (int xi = -2; xi <= 2; xi++)
        for (int zi = -2; zi <= 2; zi++)
        {
            float x = bounds.center.x + xi * bounds.size.x * 0.17f;
            float z = bounds.center.z + zi * bounds.size.z * 0.17f;

            var origin = new Vector3(x, fromY, z);
            if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, maxDist)) continue;

            // Skip the safety ground (we want real city geometry)
            if (hit.collider.gameObject.name == "Safety_Ground_Collider") continue;

            // Skip rooftops
            if (hit.point.y > groundCeil) continue;

            if (hit.point.y < bestY)
            {
                bestY = hit.point.y;
                best  = hit.point;
                found = true;
            }
        }

        if (found) return best + Vector3.up * 2f;

        // Nothing found — just hover above city bottom
        Debug.LogWarning("[CityGroundFixer] No walkable ground detected — landing on safety floor.");
        return new Vector3(bounds.center.x, bounds.min.y + 2.5f, bounds.center.z);
    }

    // ── Single-position downward raycast ─────────────────────────────────────
    Vector3 RaycastGround(Vector3 xzPos, Bounds bounds, float heightAbove)
    {
        float fromY   = bounds.max.y + 120f;
        var   origin  = new Vector3(xzPos.x, fromY, xzPos.z);

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, fromY + 50f))
        {
            if (hit.collider.gameObject.name != "Safety_Ground_Collider")
                return hit.point + Vector3.up * heightAbove;
        }

        return new Vector3(xzPos.x, SpawnPosition.y - 1f, xzPos.z);
    }
}
