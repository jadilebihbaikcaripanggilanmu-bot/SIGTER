using UnityEngine;

/// <summary>
/// Scene entry-point for SampleScene.
/// Destroys any stray default camera, then spawns GameManager and sets up lighting.
///
/// HOW TO USE:
///   1. Open SampleScene.
///   2. Make sure a GameObject named "Bootstrap" has this component.
///   3. Press ▶ Play — everything else is automatic.
/// </summary>
public class Bootstrap : MonoBehaviour
{
    void Awake()
    {
        // ── Remove any default Unity-created camera/audio listener ──────────
        var defaultCam = GameObject.FindGameObjectWithTag("MainCamera");
        if (defaultCam != null) Destroy(defaultCam);

        foreach (var al in FindObjectsOfType<AudioListener>())
            Destroy(al);

        // ── City-import helper (cleans up old procedural geo) ───────────────
        new GameObject("ImportedCityLoader").AddComponent<ImportedCityLoader>();

        // ── GameManager (spawns player, camera, UI, survey objects) ─────────
        new GameObject("GameManager").AddComponent<GameManager>();

        // ── Lighting / atmosphere ────────────────────────────────────────────
        SetupLighting();

        // Bootstrap no longer needed
        Destroy(gameObject);
    }

    void SetupLighting()
    {
        // Directional sun
        var sunGO = new GameObject("Sun");
        var sun   = sunGO.AddComponent<Light>();
        sun.type             = LightType.Directional;
        sun.color            = new Color(1.00f, 0.96f, 0.84f);
        sun.intensity        = 1.10f;
        sun.shadows          = LightShadows.Soft;
        sun.shadowStrength   = 0.70f;
        sun.shadowNormalBias = 0.4f;
        sunGO.transform.rotation = Quaternion.Euler(50f, -25f, 0f);

        QualitySettings.shadowDistance = 120f;

        // Ambient trilight
        RenderSettings.ambientMode         = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor     = new Color(0.50f, 0.62f, 0.82f);
        RenderSettings.ambientEquatorColor = new Color(0.55f, 0.58f, 0.55f);
        RenderSettings.ambientGroundColor  = new Color(0.22f, 0.26f, 0.20f);

        // Keep existing skybox if the imported city scene set one; only clear if null
        // (the Demo City scene uses its own skybox material)

        // Fog
        RenderSettings.fog             = true;
        RenderSettings.fogColor        = new Color(0.60f, 0.68f, 0.80f);
        RenderSettings.fogMode         = FogMode.Linear;
        RenderSettings.fogStartDistance = 60f;
        RenderSettings.fogEndDistance   = 250f;
    }
}
