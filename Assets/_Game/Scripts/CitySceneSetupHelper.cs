using UnityEngine;

/// <summary>
/// Attach this to a GameObject in SampleScene (e.g. "CitySceneSetupHelper").
/// Drag the demo_city_by_versatile_studio prefab into the CityPrefab slot in the Inspector.
///
/// At runtime (Play mode), this instantiates the city prefab if it is not already present.
/// It also adds a Safety_Ground_Collider so the player cannot fall through the map,
/// then uses a downward raycast to find solid ground for the player spawn point.
///
/// IMPORTANT: After adding this component, assign the prefab field in the Inspector BEFORE pressing Play.
/// </summary>
public class CitySceneSetupHelper : MonoBehaviour
{
    [Header("Drag the city prefab here in the Inspector")]
    [Tooltip("Assign: Assets/Versatile Studio Assets/Demo City By Versatile Studio/Prefabs/demo_city_by_versatile_studio.prefab")]
    public GameObject CityPrefab;

    [Header("Spawn Settings")]
    [Tooltip("Where to look for the ground (scan origin).")]
    public Vector3 RaycastOrigin = new Vector3(0f, 200f, 0f);

    [Tooltip("Fallback player Y if raycast finds nothing.")]
    public float FallbackPlayerY = 5f;

    // Read by GameManager to place the player correctly
    public static Vector3 PlayerSpawnPosition { get; private set; } = new Vector3(0f, 5f, 0f);
    public static bool    SpawnPositionReady  { get; private set; } = false;

    void Awake()
    {
        // ── Auto-load prefab if not assigned in Inspector ──────────────────
        if (CityPrefab == null)
        {
            // Try loading from Resources folder first
            CityPrefab = Resources.Load<GameObject>("demo_city_by_versatile_studio");

            // If still null, try to find any existing city object in the scene
            if (CityPrefab == null)
            {
                var existing = GameObject.Find("demo_city_by_versatile_studio");
                if (existing == null) existing = GameObject.Find("Imported_DemoCity_Environment");
                if (existing != null)
                {
                    Debug.Log("[CitySceneSetupHelper] Found existing city in scene — skipping instantiation.");
                    EnsureMarker();
                    AddSafetyGround();
                    FindPlayerSpawn();
                    return;
                }
                else
                {
                    Debug.LogWarning("[CitySceneSetupHelper] CityPrefab not assigned and not found! " +
                                     "Use menu Surveyor > Setup City Scene in the Editor.");
                }
            }
        }

        // ── Spawn city prefab if not already in scene ──────────────────────
        if (CityPrefab != null && GameObject.Find("Imported_DemoCity_Environment") == null)
        {
            var city = Instantiate(CityPrefab, Vector3.zero, Quaternion.identity);
            city.name = "Imported_DemoCity_Environment";
            Debug.Log("[CitySceneSetupHelper] Spawned city prefab.");
            EnsureMarker();
        }
        else if (GameObject.Find("Imported_DemoCity_Environment") != null)
        {
            Debug.Log("[CitySceneSetupHelper] City already present in scene.");
            EnsureMarker();
        }

        // ── Safety ground collider (prevents infinite fall) ────────────────
        AddSafetyGround();

        // ── Find player spawn via raycast ──────────────────────────────────
        FindPlayerSpawn();
    }

    void EnsureMarker()
    {
        if (GameObject.Find("UseImportedCityMarker") == null)
        {
            var marker = new GameObject("UseImportedCityMarker");
            marker.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
        }
    }

    void AddSafetyGround()
    {
        if (GameObject.Find("Safety_Ground_Collider") != null) return;

        var go = new GameObject("Safety_Ground_Collider");
        go.transform.position = new Vector3(0f, -2f, 0f);
        go.transform.localScale = new Vector3(1f, 1f, 1f); // collider defines its own size

        var col = go.AddComponent<BoxCollider>();
        col.size   = new Vector3(1000f, 0.5f, 1000f);
        col.center = Vector3.zero;

        // No renderer — invisible
        Debug.Log("[CitySceneSetupHelper] Safety_Ground_Collider added.");
    }

    void FindPlayerSpawn()
    {
        // ── Check for a PlayerSpawn marker already in the scene ────────────
        var marker = GameObject.Find("PlayerSpawn");
        if (marker != null)
        {
            PlayerSpawnPosition = marker.transform.position;
            SpawnPositionReady  = true;
            Debug.Log($"[CitySceneSetupHelper] PlayerSpawn marker found at {PlayerSpawnPosition}");
            return;
        }

        // ── Raycast downward from origin to find real ground ───────────────
        // Try several candidate positions spread across the city
        Vector3[] candidates = new Vector3[]
        {
            new Vector3(  0f, 200f,   0f),
            new Vector3(  5f, 200f,   5f),
            new Vector3( -5f, 200f,  -5f),
            new Vector3( 10f, 200f,  10f),
            new Vector3(-10f, 200f, -10f),
            new Vector3(  0f, 200f,  20f),
            new Vector3( 20f, 200f,   0f),
        };

        foreach (var origin in candidates)
        {
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 400f))
            {
                // Make sure we land on something that isn't a building roof
                // (check y is below 10 to avoid rooftop spawns)
                if (hit.point.y < 10f)
                {
                    PlayerSpawnPosition = hit.point + Vector3.up * 2f;
                    SpawnPositionReady  = true;
                    Debug.Log($"[CitySceneSetupHelper] Ground found at {hit.point} → spawn at {PlayerSpawnPosition}");
                    return;
                }
            }
        }

        // ── Fallback ───────────────────────────────────────────────────────
        PlayerSpawnPosition = new Vector3(0f, FallbackPlayerY, 0f);
        SpawnPositionReady  = true;
        Debug.LogWarning("[CitySceneSetupHelper] Raycast found no low ground — using fallback spawn.");
    }
}
