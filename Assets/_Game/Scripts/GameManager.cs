using System.Collections;
using UnityEngine;

/// <summary>
/// Central game manager.
/// Spawns the Player, Camera, and UI after CityGroundFixer has finished setup.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // ── Populated during Start ────────────────────────────────────────────────
    public PlayerController              Player;
    public CameraController              CamController;
    public UIManager                     UI;

    // ── Awake — just set singleton ────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Start — coroutine so we can yield until city ground is ready ──────────
    IEnumerator Start()
    {
        // ── 1. Skip procedural city if imported city is present ───────────────
        bool hasImported = GameObject.Find("Imported_DemoCity_Environment") != null
                        || GameObject.Find("Imported_Demo City_Environment") != null
                        || GameObject.Find("UseImportedCityMarker") != null;

        if (!hasImported)
        {
            Debug.Log("[GameManager] No imported city — building procedural city.");
            var cityGO = new GameObject("CityBuilder");
            cityGO.AddComponent<CityBuilder>().Build();
        }
        else
        {
            Debug.Log("[GameManager] Imported city detected — skipping procedural CityBuilder.");
        }

        // ── 2. Launch CityGroundFixer if not already running ──────────────────
        if (GameObject.Find("CityGroundFixer") == null)
            new GameObject("CityGroundFixer").AddComponent<CityGroundFixer>();

        // ── 3. Wait until CityGroundFixer has found real ground ───────────────
        float waitTimeout = 15f;
        float elapsed     = 0f;
        while (!CityGroundFixer.SetupComplete)
        {
            elapsed += Time.deltaTime;
            if (elapsed > waitTimeout)
            {
                Debug.LogWarning("[GameManager] CityGroundFixer timed out — spawning at fallback.");
                break;
            }
            yield return null;
        }

        // ── 4. Retrieve validated spawn positions ─────────────────────────────
        Vector3 spawnPos = CityGroundFixer.SpawnPosition;

        Debug.Log($"[GameManager] Player spawn received: {spawnPos:F2}");

        // ── 5. Spawn Player ───────────────────────────────────────────────────
        var playerGO = new GameObject("Player");
        playerGO.tag = "Player";
        playerGO.transform.position = spawnPos;

        var cc        = playerGO.AddComponent<CharacterController>();
        cc.height     = 1.8f;
        cc.radius     = 0.35f;
        cc.center     = Vector3.up * 0.9f;
        cc.stepOffset = 0.4f;          // helps with small curbs/steps
        cc.slopeLimit = 55f;

        Player = playerGO.AddComponent<PlayerController>();

        // ── 6. Spawn Camera rig ───────────────────────────────────────────────
        var rigGO = new GameObject("CameraRig");
        rigGO.transform.SetParent(playerGO.transform);
        rigGO.transform.localPosition = Vector3.up * 0.82f;   // eye height

        var camGO = new GameObject("MainCamera");
        camGO.tag = "MainCamera";
        camGO.transform.SetParent(rigGO.transform);
        camGO.transform.localPosition = Vector3.zero;

        var cam            = camGO.AddComponent<Camera>();
        cam.fieldOfView    = 65f;
        cam.nearClipPlane  = 0.12f;
        cam.farClipPlane   = 800f;
        cam.clearFlags     = CameraClearFlags.Skybox;
        camGO.AddComponent<AudioListener>();

        CamController             = rigGO.AddComponent<CameraController>();
        CamController.PlayerTransform = playerGO.transform;
        CamController.Cam         = cam;
        Player.CamRig             = rigGO.transform;

        // ── 7. Spawn UI ───────────────────────────────────────────────────────
        UI = new GameObject("UIManager").AddComponent<UIManager>();

        Debug.Log($"[GameManager] Setup complete. Player={spawnPos:F2}");
    }
}
}
