using System.Collections;
using UnityEngine;

/// <summary>
/// Central game manager.
/// Spawns the Player, Camera, and UI after CityGroundFixer has finished setup.
/// Spawns the player precisely at the PlayerSpawn marker height, using a raycast fallback.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // ── Populated during Start ────────────────────────────────────────────────
    public PlayerController              Player;
    public CameraController              CamController;
    public UIManager                     UI;

    // ── Minimap System ────────────────────────────────────────────────────────
    public RenderTexture                 MinimapTexture { get; private set; }
    private GameObject                   _minimapCamGO;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    IEnumerator Start()
    {
        // ── 1. Skip procedural city if imported city is present ───────────────
        bool hasImported = FindCityInActiveScene() != null
                        || FindGameObjectInActiveScene("UseImportedCityMarker") != null;

        if (!hasImported)
        {
            Debug.Log("[GameManager] No imported city — building procedural city.");
            var cityGO = new GameObject("CityBuilder");
            cityGO.AddComponent<CityBuilder>().Build();
        }

        // ── 2. Launch CityGroundFixer ──────────────────────────────────────────
        if (GameObject.Find("CityGroundFixer") == null)
            new GameObject("CityGroundFixer").AddComponent<CityGroundFixer>();

        // ── 3. Wait until CityGroundFixer has finished ─────────────────────────
        yield return new WaitUntil(() => CityGroundFixer.SetupComplete);

        // ── 4. Determine Spawn Position ────────────────────────────────────────
        Vector3 spawnXZ = DetermineSpawnPosition();
        float roadSurfaceY = FindRoadSurfaceY(spawnXZ);
        Vector3 spawnPos = new Vector3(spawnXZ.x, roadSurfaceY + 0.5f, spawnXZ.z);

        // ── 5. Spawn Player ───────────────────────────────────────────────────
        var playerGO = new GameObject("Player");
        playerGO.tag = "Player";
        playerGO.layer = 2; // Ignore Raycast
        playerGO.transform.position = spawnPos;

        var cc        = playerGO.AddComponent<CharacterController>();
        cc.height     = 1.8f;
        cc.radius     = 0.35f;
        cc.center     = Vector3.up * 0.9f;
        cc.stepOffset = 0.4f;
        cc.slopeLimit = 55f;
        cc.skinWidth  = 0.08f;

        Player = playerGO.AddComponent<PlayerController>();

        // ── 6. Spawn Camera rig (Reverted to "Mode Kucing" 0.82f height) ───────
        var rigGO = new GameObject("CameraRig");
        rigGO.transform.SetParent(playerGO.transform);
        rigGO.transform.localPosition = Vector3.up * 0.82f;   // mode kucing eye height

        var camGO = new GameObject("MainCamera");
        camGO.tag = "MainCamera";
        camGO.transform.SetParent(rigGO.transform);
        camGO.transform.localPosition = Vector3.zero;

        var cam            = camGO.AddComponent<Camera>();
        cam.fieldOfView    = 65f;
        cam.nearClipPlane  = 0.12f;
        cam.farClipPlane   = 1500f;
        cam.clearFlags     = CameraClearFlags.Skybox;
        camGO.AddComponent<AudioListener>();

        CamController                 = rigGO.AddComponent<CameraController>();
        CamController.PlayerTransform = playerGO.transform;
        CamController.Cam             = cam;
        Player.CamRig                 = rigGO.transform;

        // ── 7. Spawn UI ───────────────────────────────────────────────────────
        UI = new GameObject("UIManager").AddComponent<UIManager>();

        // ── 8. Setup Minimap Camera & Render Texture ──────────────────────────
        MinimapTexture = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32);
        MinimapTexture.filterMode = FilterMode.Bilinear;
        MinimapTexture.Create();

        _minimapCamGO = new GameObject("MinimapCamera");
        var miniCam = _minimapCamGO.AddComponent<Camera>();
        miniCam.orthographic = true;
        miniCam.orthographicSize = 35f; // Zoomed in
        miniCam.targetTexture = MinimapTexture;
        miniCam.clearFlags = CameraClearFlags.SolidColor;
        miniCam.backgroundColor = new Color(0.05f, 0.08f, 0.15f);
        miniCam.nearClipPlane = 0.5f;
        miniCam.farClipPlane = 50f; // Exclude fog
        
        // Remove white planes around trees by enabling alpha cutout/clipping
        FixTreeMaterials();

        Debug.Log($"[GameManager] ✅ Player spawned at {playerGO.transform.position} — Setup Complete.");
    }

    void FixTreeMaterials()
    {
        var renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        int fixedCount = 0;
        foreach (var r in renderers)
        {
            if (r == null || r.gameObject == null) continue;
            string nameLower = r.gameObject.name.ToLower();
            
            // Check if this renderer is a tree, vegetation, leaf, etc.
            if (nameLower.Contains("tree") || nameLower.Contains("vegetation") || nameLower.Contains("leaf") || nameLower.Contains("leave") || nameLower.Contains("plant") || nameLower.Contains("prop_shrub"))
            {
                var mats = r.materials;
                if (mats == null) continue;
                
                foreach (var mat in mats)
                {
                    if (mat == null) continue;
                    
                    // Enable URP standard Alpha Cutout (Clipping)
                    if (mat.HasProperty("_AlphaClip"))
                    {
                        mat.SetFloat("_AlphaClip", 1f);
                        mat.SetFloat("_Cutoff", 0.5f);
                        mat.SetOverrideTag("RenderType", "TransparentCutout");
                        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                        mat.EnableKeyword("_ALPHATEST_ON");
                        fixedCount++;
                    }
                }
            }
        }
        Debug.Log($"[GameManager] ✅ Enabled Alpha Clipping on {fixedCount} tree/vegetation material instances.");
    }

    void LateUpdate()
    {
        if (Player != null && _minimapCamGO != null)
        {
            Vector3 pos = Player.transform.position;
            pos.y = Player.transform.position.y + 40f;
            _minimapCamGO.transform.position = pos;
            
            // Map rotates with player heading
            _minimapCamGO.transform.rotation = Quaternion.Euler(90f, Player.transform.eulerAngles.y, 0f);
        }
    }

    void OnDestroy()
    {
        if (MinimapTexture != null)
        {
            MinimapTexture.Release();
            MinimapTexture = null;
        }
    }

    private readonly Vector3[] _teleportPoints = new Vector3[]
    {
        new Vector3(136f, 5.5f, -80f),   // Main street (Start) - open road
        new Vector3(195f, 6.5f, -150f),  // Near office building 2
        new Vector3(70f, 5.5f, -120f),   // Residential area
        new Vector3(25f, 5.5f, -50f),    // Downtown avenue
        new Vector3(210f, 6.0f, -20f)    // Outskirts street
    };
    private int _currentSpawnIndex = 0;

    public void TeleportToNextSpawn()
    {
        if (Player == null) return;
        
        _currentSpawnIndex = (_currentSpawnIndex + 1) % _teleportPoints.Length;
        Vector3 targetXZ = _teleportPoints[_currentSpawnIndex];
        float roadY = FindRoadSurfaceY(targetXZ);
        
        Player.Teleport(new Vector3(targetXZ.x, roadY + 0.5f, targetXZ.z));
        Debug.Log($"[GameManager] Teleported player to spawn point {_currentSpawnIndex} at {Player.transform.position}");
    }

    float FindRoadSurfaceY(Vector3 spawnXZ)
    {
        // Simple downward raycast from high above the spawn marker to hit the built-in collider, ignoring safety ground
        Ray ray = new Ray(new Vector3(spawnXZ.x, 25f, spawnXZ.z), Vector3.down);
        var hits = Physics.RaycastAll(ray, 50f);
        float highestY = -999f;
        bool foundCity = false;

        foreach (var hit in hits)
        {
            if (hit.collider != null && hit.collider.name != "Safety_Ground_Collider")
            {
                if (hit.point.y > highestY)
                {
                    highestY = hit.point.y;
                    foundCity = true;
                }
            }
        }

        if (foundCity) return highestY;
        return 4.84f; // Fallback to street level Y
    }

    Vector3 DetermineSpawnPosition()
    {
        var spawnMarker = GameObject.Find("PlayerSpawn");
        if (spawnMarker != null)
        {
            Vector3 pos = spawnMarker.transform.position;
            // If the marker is at the old bad platform position, override it to the open street asphalt
            if (Vector3.Distance(pos, new Vector3(136f, 5.5f, -104f)) < 1f)
            {
                pos = new Vector3(136f, 5.5f, -80f);
            }
            return pos;
        }

        return new Vector3(136f, 5.5f, -80f); // Default fallback XZ (middle of street)
    }

    GameObject FindCityInActiveScene()
    {
        foreach (var r in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            var nameLower = r.name.ToLower();
            if (nameLower.Contains("imported_democity") || nameLower.Contains("demo_city"))
                return r;
        }
        return null;
    }

    GameObject FindGameObjectInActiveScene(string name)
    {
        foreach (var r in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (r.name == name) return r;
        }
        return null;
    }
}
