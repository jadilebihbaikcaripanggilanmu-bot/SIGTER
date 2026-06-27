using UnityEngine;

/// <summary>
/// Procedural city builder – Unity primitives only, no paid assets.
/// URP/Lit shader is used when available; falls back to Standard automatically.
/// City layout: road grid at ±20 units, building blocks between roads,
/// park (west), plaza (east), survey zone near Total Station.
/// </summary>
public class CityBuilder : MonoBehaviour
{
    // If true, the builder will respect the UseImportedCityMarker and skip generation.
    public static bool useImportedCity = true;
    // ─── Shader detection (static, shared across all instances) ──────────────
    private static Shader _shader;
    private static bool   _isURP;

    static void EnsureShader()
    {
        if (_shader != null) return;
        _shader = Shader.Find("Universal Render Pipeline/Lit");
        if (_shader != null) { _isURP = true; return; }
        _shader = Shader.Find("Standard");
        _isURP  = false;
    }

    // ─── Shared materials ────────────────────────────────────────────────────
    private Material _road, _roadMark, _sidewalk, _curb, _crosswalk;
    private Material _bldgA, _bldgB, _bldgC, _bldgD, _roofMat, _windowMat, _concreteMat;
    private Material _grass, _parkPath, _water, _plazaTile, _plazaAccent;
    private Material _trunk, _foliageA, _foliageB;
    private Material _lampPost, _lampBulb, _signPost, _signBoard;
    private Material _yellowMat, _orangeMat, _whiteMat, _benchMat, _barrierMat;

    // ─── Entry point ─────────────────────────────────────────────────────────
    public void Build()
    {
        // If an imported city is being used for this scene, skip procedural generation
        if (useImportedCity && GameObject.Find("UseImportedCityMarker") != null)
        {
            // still ensure shaders/materials are prepared for any smaller visual pieces
            EnsureShader();
            MakeMaterials();
            return;
        }

        EnsureShader();
        MakeMaterials();

        BuildGround();
        BuildRoadGrid();
        BuildBuildings();
        BuildPark();
        BuildPlaza();
        BuildStreetFurniture();
        BuildSurveyZone();
        BuildBackgroundSkyline();

        // Ensure a directional light and nicer ambient settings
        SetupLighting();
    }

    void SetupLighting()
    {
        // Create a directional light if none exists
        if (GameObject.FindObjectOfType<Light>() == null)
        {
            var go = new GameObject("Directional Light");
            var l = go.AddComponent<Light>();
            l.type = LightType.Directional;
            l.color = new Color(1.0f, 0.95f, 0.88f);
            l.intensity = 1.0f;
            go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.62f, 0.68f, 0.75f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = 0.0035f;
        // Camera.main may not exist yet (created later by GameManager). Only set background if present.
        if (Camera.main != null)
        {
            Camera.main.backgroundColor = new Color(0.65f, 0.78f, 0.92f);
        }
    }

    // ─── Material palette (URP + Standard compatible) ─────────────────────────
    void MakeMaterials()
    {
        _road         = M(new Color(0.15f, 0.15f, 0.16f), sm: 0.10f);
        _roadMark     = M(new Color(0.95f, 0.88f, 0.20f), sm: 0.15f);  // yellow centre line
        _sidewalk     = M(new Color(0.72f, 0.70f, 0.65f), sm: 0.12f);
        _curb         = M(new Color(0.78f, 0.76f, 0.72f), sm: 0.15f);
        _crosswalk    = M(new Color(0.91f, 0.91f, 0.88f), sm: 0.12f);

        _bldgA        = M(new Color(0.82f, 0.79f, 0.74f), sm: 0.18f);
        _bldgB        = M(new Color(0.55f, 0.67f, 0.82f), sm: 0.25f);
        _bldgC        = M(new Color(0.70f, 0.57f, 0.51f), sm: 0.18f);
        _bldgD        = M(new Color(0.62f, 0.70f, 0.62f), sm: 0.18f);
        _roofMat      = M(new Color(0.20f, 0.28f, 0.42f), sm: 0.12f);
        _windowMat    = M(new Color(0.42f, 0.62f, 0.88f), sm: 0.85f, mt: 0.05f);
        _concreteMat  = M(new Color(0.65f, 0.63f, 0.60f), sm: 0.12f);

        _grass        = M(new Color(0.26f, 0.50f, 0.20f), sm: 0.00f);
        _parkPath     = M(new Color(0.75f, 0.70f, 0.60f), sm: 0.10f);
        _water        = M(new Color(0.18f, 0.48f, 0.75f), sm: 0.90f, mt: 0.05f);
        _plazaTile    = M(new Color(0.84f, 0.81f, 0.76f), sm: 0.22f);
        _plazaAccent  = M(new Color(0.74f, 0.71f, 0.66f), sm: 0.30f);

        _trunk        = M(new Color(0.36f, 0.24f, 0.14f), sm: 0.00f);
        _foliageA     = M(new Color(0.18f, 0.52f, 0.16f), sm: 0.00f);
        _foliageB     = M(new Color(0.25f, 0.62f, 0.22f), sm: 0.00f);

        _lampPost     = M(new Color(0.22f, 0.22f, 0.24f), sm: 0.40f, mt: 0.35f);
        _lampBulb     = M(new Color(1.00f, 0.96f, 0.82f), sm: 0.50f,
                           em: new Color(1.00f, 0.88f, 0.48f) * 1.4f);
        _signPost     = M(new Color(0.28f, 0.28f, 0.28f), sm: 0.30f, mt: 0.20f);
        _signBoard    = M(new Color(0.08f, 0.25f, 0.68f), sm: 0.25f);

        _yellowMat    = M(new Color(0.95f, 0.82f, 0.08f), sm: 0.20f);
        _orangeMat    = M(new Color(0.95f, 0.42f, 0.08f), sm: 0.20f);
        _whiteMat     = M(new Color(0.92f, 0.92f, 0.92f), sm: 0.15f);
        _benchMat     = M(new Color(0.50f, 0.33f, 0.18f), sm: 0.10f);
        _barrierMat   = M(new Color(0.62f, 0.60f, 0.56f), sm: 0.12f);
    }

    // ─── Ground ───────────────────────────────────────────────────────────────
    void BuildGround()
    {
        // Unity Plane: 10×10 world units at scale 1 → scale (20,1,20) = 200×200 m
        var g = P(PrimitiveType.Plane, "Ground", Vector3.zero, new Vector3(20f, 1f, 20f));
        Apply(g, _grass);
    }

    // ─── Road Grid ───────────────────────────────────────────────────────────
    void BuildRoadGrid()
    {
        var parent = Sub("Roads");

        const float roadW = 6f, roadL = 100f, swW = 1.8f, curbH = 0.12f, curbW = 0.22f;

        // North–South roads at x = −20, 0, +20
        foreach (float x in new[] { -20f, 0f, 20f })
        {
            Road(parent, new Vector3(x, 0.01f, 0f), roadW, roadL, ns: true);
            Sidewalk(parent, new Vector3(x - roadW / 2f - swW / 2f, 0f, 0f), swW, roadL, ns: true, curbH, curbW, left: true);
            Sidewalk(parent, new Vector3(x + roadW / 2f + swW / 2f, 0f, 0f), swW, roadL, ns: true, curbH, curbW, left: false);
        }

        // East–West roads at z = −20, 0, +20
        foreach (float z in new[] { -20f, 0f, 20f })
        {
            Road(parent, new Vector3(0f, 0.01f, z), roadW, roadL, ns: false);
            Sidewalk(parent, new Vector3(0f, 0f, z - roadW / 2f - swW / 2f), roadL, swW, ns: false, curbH, curbW, left: true);
            Sidewalk(parent, new Vector3(0f, 0f, z + roadW / 2f + swW / 2f), roadL, swW, ns: false, curbH, curbW, left: false);
        }

        // Crosswalks at every 3×3 intersection
        foreach (float x in new[] { -20f, 0f, 20f })
            foreach (float z in new[] { -20f, 0f, 20f })
                BuildCrosswalks(parent, x, z, roadW);
    }

    void Road(Transform parent, Vector3 centre, float roadW, float roadL, bool ns)
    {
        float w = ns ? roadW : roadL;
        float l = ns ? roadL : roadW;

        var road = P(PrimitiveType.Cube, "Road", centre, new Vector3(w, 0.02f, l), parent);
        Apply(road, _road);

        // Centre line (solid yellow stripe)
        var cl = P(PrimitiveType.Cube, "CentreLine", centre + Vector3.up * 0.005f,
                   ns ? new Vector3(0.14f, 0.01f, roadL) : new Vector3(roadL, 0.01f, 0.14f), parent);
        Apply(cl, _roadMark);
    }

    void Sidewalk(Transform parent, Vector3 centre, float w, float l, bool ns,
                  float curbH, float curbW, bool left)
    {
        // Main slab (slightly elevated)
        var slab = P(PrimitiveType.Cube, "Sidewalk", centre + Vector3.up * (curbH / 2f),
                     new Vector3(w, curbH, l), parent);
        Apply(slab, _sidewalk);

        // Curb strip on road side
        float curbOffset = left
            ? (ns ?  (w / 2f)         : (l / 2f))
            : (ns ? -(w / 2f)         : -(l / 2f));
        Vector3 curbPos = centre + (ns
            ? new Vector3(curbOffset, curbH / 2f, 0f)
            : new Vector3(0f, curbH / 2f, curbOffset));
        Vector3 curbScale = ns
            ? new Vector3(curbW, curbH * 1.8f, l)
            : new Vector3(w, curbH * 1.8f, curbW);
        var curb = P(PrimitiveType.Cube, "Curb", curbPos, curbScale, parent);
        Apply(curb, _curb);
    }

    void BuildCrosswalks(Transform parent, float ix, float iz, float roadW)
    {
        float half = roadW / 2f + 0.9f;   // just outside road edge
        float stripeW = 0.42f, stripeL = 1.5f, gap = 0.58f;

        for (int s = -2; s <= 2; s++)
        {
            float offset = s * gap;

            // North approach (walk N/S, stripes run E/W)
            Apply(P(PrimitiveType.Cube, "CW", new Vector3(ix + offset, 0.025f, iz + half),
                    new Vector3(stripeW, 0.02f, stripeL), parent), _crosswalk);
            // South approach
            Apply(P(PrimitiveType.Cube, "CW", new Vector3(ix + offset, 0.025f, iz - half),
                    new Vector3(stripeW, 0.02f, stripeL), parent), _crosswalk);
            // East approach
            Apply(P(PrimitiveType.Cube, "CW", new Vector3(ix + half, 0.025f, iz + offset),
                    new Vector3(stripeL, 0.02f, stripeW), parent), _crosswalk);
            // West approach
            Apply(P(PrimitiveType.Cube, "CW", new Vector3(ix - half, 0.025f, iz + offset),
                    new Vector3(stripeL, 0.02f, stripeW), parent), _crosswalk);
        }
    }

    // ─── Buildings ───────────────────────────────────────────────────────────
    void BuildBuildings()
    {
        var parent = Sub("Buildings");

        // Each entry: (cx, cz, width, depth, height, materialIndex)
        var defs = new (float cx, float cz, float w, float d, float h, int mat)[]
        {
            // NW block
            (-11f, -9f,  3.8f, 3.2f, 12f, 0), (-13f,-12f,  3.2f, 3.0f,  6f, 1),
            ( -9f,-13f,  3.0f, 3.5f,  8f, 2), (-12f, -7f,  4.0f, 3.0f, 18f, 0),
            // NE block
            ( 11f, -9f,  3.5f, 3.2f,  9f, 1), ( 13f,-12f,  3.0f, 3.8f, 14f, 3),
            ( 10f,-13f,  4.2f, 3.0f,  6f, 0), ( 12f, -7f,  3.2f, 3.5f, 20f, 2),
            // SW block
            (-11f,  9f,  3.8f, 3.2f,  7f, 3), (-13f, 12f,  3.2f, 3.0f, 11f, 0),
            ( -9f, 13f,  3.0f, 3.5f, 16f, 1), (-12f,  7f,  4.0f, 3.0f,  5f, 2),
            // SE block
            ( 11f,  9f,  3.5f, 3.5f, 13f, 2), ( 13f, 12f,  3.0f, 4.0f,  8f, 3),
            ( 10f, 13f,  4.0f, 3.0f, 22f, 0), ( 12f,  7f,  3.2f, 3.2f,  6f, 1),
            // Outer ring
            (-10f,-30f,  4.0f, 3.5f, 10f, 1), ( 10f,-30f,  3.5f, 3.0f,  7f, 0),
            (-10f, 30f,  3.8f, 3.8f, 14f, 3), ( 10f, 30f,  3.5f, 3.5f,  8f, 2),
            (-30f, -9f,  3.2f, 3.0f,  9f, 0), (-30f,  9f,  3.0f, 3.5f, 12f, 2),
            ( 30f, -9f,  3.5f, 3.2f,  7f, 1), ( 30f,  9f,  3.2f, 3.0f, 16f, 3),
        };

        Material[] mats = { _bldgA, _bldgB, _bldgC, _bldgD };

        foreach (var d in defs)
            PlaceBuilding(parent, d.cx, d.cz, d.w, d.d, d.h, mats[d.mat]);
    }

    void PlaceBuilding(Transform parent, float bx, float bz,
                       float w, float d, float h, Material mat)
    {
        // ── Main body ───────────────────────────────────────────────────
        Apply(P(PrimitiveType.Cube, "Bldg",
               new Vector3(bx, h * 0.5f, bz),
               new Vector3(w, h, d), parent), mat);

        // Setback upper volume on tall buildings
        if (h > 12f)
        {
            float topH = h * 0.3f;
            Apply(P(PrimitiveType.Cube, "BldgTop",
                   new Vector3(bx, h - topH * 0.5f, bz),
                   new Vector3(w * 0.72f, topH, d * 0.72f), parent), mat);
        }

        // ── Roof ────────────────────────────────────────────────────────
        Apply(P(PrimitiveType.Cube, "Roof",
               new Vector3(bx, h + 0.13f, bz),
               new Vector3(w + 0.18f, 0.24f, d + 0.18f), parent), _roofMat);

        // ── Cornice trim at base ─────────────────────────────────────────
        Apply(P(PrimitiveType.Cube, "Trim",
               new Vector3(bx, 1.05f, bz),
               new Vector3(w + 0.12f, 0.18f, d + 0.12f), parent), _concreteMat);

        // ── Window rows (front face +Z only for perf) ────────────────────
        int rows = Mathf.Clamp((int)(h / 3.2f), 1, 5);
        int cols = Mathf.Max(1, (int)(w / 1.4f));

        for (int row = 0; row < rows; row++)
        for (int col = 0; col < cols; col++)
        {
            float wy = 2.0f + row * (h - 2.5f) / Mathf.Max(rows - 1, 1);
            float wx = bx - (cols - 1) * 0.80f * 0.5f + col * 0.80f;
            float ws = Mathf.Min(0.55f, w * 0.20f);

             var win = P(PrimitiveType.Cube, "Win",
                 new Vector3(wx, wy, bz + d * 0.501f),
                 new Vector3(ws, ws * 1.25f, 0.04f), parent);
             Apply(win, _windowMat);

             // Window frame (thin border)
             var frameThickness = 0.03f;
             var frameL = P(PrimitiveType.Cube, "WinFrameL", new Vector3(wx - ws * 0.5f - frameThickness * 0.5f, wy, bz + d * 0.52f), new Vector3(frameThickness, ws * 1.25f + 0.02f, 0.02f), parent);
             var frameR = P(PrimitiveType.Cube, "WinFrameR", new Vector3(wx + ws * 0.5f + frameThickness * 0.5f, wy, bz + d * 0.52f), new Vector3(frameThickness, ws * 1.25f + 0.02f, 0.02f), parent);
             var frameT = P(PrimitiveType.Cube, "WinFrameT", new Vector3(wx, wy + (ws * 1.25f + frameThickness) * 0.5f, bz + d * 0.52f), new Vector3(ws + frameThickness * 2f, frameThickness, 0.02f), parent);
             Apply(frameL, _concreteMat); Apply(frameR, _concreteMat); Apply(frameT, _concreteMat);
        }

        // ── Roof details ────────────────────────────────────────────────
        if (h > 6f)
        {
            // AC / HVAC box
             Apply(P(PrimitiveType.Cube, "AC",
                 new Vector3(bx + w * 0.18f, h + 0.42f, bz + d * 0.08f),
                 new Vector3(0.75f, 0.55f, 0.55f), parent), _concreteMat);
        }
        if (h > 10f)
        {
            // Water tank (cylinder)
            Apply(P(PrimitiveType.Cylinder, "Tank",
                   new Vector3(bx - w * 0.18f, h + 0.65f, bz - d * 0.18f),
                   new Vector3(0.55f, 0.55f, 0.55f), parent), _concreteMat);
        }
        // Random balconies on some buildings (small ledges)
        if (Random.value > 0.75f && w > 2.2f && h > 6f)
        {
            int bcount = Mathf.Clamp((int)(h / 4f), 1, 4);
            for (int i = 0; i < bcount; i++)
            {
                float by = 1.8f + i * 3.2f;
                var balc = P(PrimitiveType.Cube, "Balcony", new Vector3(bx + w * 0.51f, by, bz + d * 0.05f), new Vector3(0.7f, 0.12f, 0.6f), parent);
                Apply(balc, _concreteMat);
            }
        }
        if (h > 14f)
        {
            // Antenna (thin cylinder)
            Apply(P(PrimitiveType.Cylinder, "Antenna",
                   new Vector3(bx, h + 1.25f, bz),
                   new Vector3(0.04f, 1.25f, 0.04f), parent), _lampPost);
        }
    }

    // ─── Park ─────────────────────────────────────────────────────────────────
    void BuildPark()
    {
        var parent = Sub("Park");
        var O = new Vector3(-32f, 0f, 0f); // park origin

        // Ground slab
        Apply(P(PrimitiveType.Cube, "ParkGround", O + V(0, 0.03f, 0), V(16f, 0.06f, 20f), parent), _grass);

        // Gravel path (N/S centre)
        Apply(P(PrimitiveType.Cube, "Path", O + V(0, 0.055f, 0), V(1.4f, 0.04f, 18f), parent), _parkPath);

        // Pond
        Apply(P(PrimitiveType.Cylinder, "Pond", O + V(0, 0.065f, -4f), V(3.2f, 0.04f, 3.2f), parent), _water);

        // Flower beds
        var pinkMat  = M(new Color(0.90f, 0.35f, 0.55f));
        var pinkMat2 = M(new Color(0.95f, 0.55f, 0.20f));
        Apply(P(PrimitiveType.Cylinder, "Bed1", O + V( 2.8f, 0.055f, -4f), V(1.8f, 0.04f, 1.8f), parent), pinkMat);
        Apply(P(PrimitiveType.Cylinder, "Bed2", O + V(-2.8f, 0.055f, -4f), V(1.8f, 0.04f, 1.8f), parent), pinkMat2);

        // Trees
        Tree(parent, O + V(-5.5f, 0, -7f), 5.2f); Tree(parent, O + V( 5.5f, 0, -7f), 4.8f);
        Tree(parent, O + V(-5.5f, 0,  6f), 6.0f); Tree(parent, O + V( 5.5f, 0,  6f), 5.5f);
        Tree(parent, O + V(-6.5f, 0,  0f), 4.5f); Tree(parent, O + V( 6.5f, 0,  0f), 5.0f);
        Tree(parent, O + V( 0f,   0,  8f), 5.8f);

        // Benches
        Bench(parent, O + V( 1.8f, 0, -1.5f));
        Bench(parent, O + V(-1.8f, 0,  1.5f));

        // Entrance columns
        Apply(P(PrimitiveType.Cube, "GateL", O + V(-1.2f, 1.3f,  10f), V(0.32f, 2.6f, 0.32f), parent), _concreteMat);
        Apply(P(PrimitiveType.Cube, "GateR", O + V( 1.2f, 1.3f,  10f), V(0.32f, 2.6f, 0.32f), parent), _concreteMat);
        Apply(P(PrimitiveType.Cube, "GateLB", O + V(-1.2f, 1.3f, -10f), V(0.32f, 2.6f, 0.32f), parent), _concreteMat);
        Apply(P(PrimitiveType.Cube, "GateRB", O + V( 1.2f, 1.3f, -10f), V(0.32f, 2.6f, 0.32f), parent), _concreteMat);

        // Park sign
        Sign(parent, O + V(0f, 0, 11.5f), "PARK");
    }

    // ─── Plaza ───────────────────────────────────────────────────────────────
    void BuildPlaza()
    {
        var parent = Sub("Plaza");
        var O = new Vector3(32f, 0f, 0f);
        const float pW = 16f, pD = 18f;

        // Main paving
        Apply(P(PrimitiveType.Cube, "PlazaBase", O + V(0, 0.03f, 0), V(pW, 0.06f, pD), parent), _plazaTile);
        // Accent inner band
        Apply(P(PrimitiveType.Cube, "PlazaInner", O + V(0, 0.04f, 0), V(pW * 0.62f, 0.02f, pD * 0.62f), parent), _plazaAccent);

        // Perimeter low walls
        float wH = 0.55f, wT = 0.30f;
        Apply(P(PrimitiveType.Cube, "WN", O + V(0, wH/2, pD/2+wT/2), V(pW+wT*2, wH, wT), parent), _concreteMat);
        Apply(P(PrimitiveType.Cube, "WS", O + V(0, wH/2, -pD/2-wT/2), V(pW+wT*2, wH, wT), parent), _concreteMat);
        Apply(P(PrimitiveType.Cube, "WE", O + V(pW/2+wT/2, wH/2, 0), V(wT, wH, pD), parent), _concreteMat);
        Apply(P(PrimitiveType.Cube, "WW", O + V(-pW/2-wT/2, wH/2, 0), V(wT, wH, pD), parent), _concreteMat);

        // Fountain
        Apply(P(PrimitiveType.Cylinder, "FBase",  O + V(0, 0.28f, 0),  V(3.8f, 0.28f, 3.8f), parent), _concreteMat);
        Apply(P(PrimitiveType.Cylinder, "FPool",  O + V(0, 0.44f, 0),  V(2.8f, 0.10f, 2.8f), parent), _water);
        Apply(P(PrimitiveType.Cylinder, "FPillar",O + V(0, 0.75f, 0),  V(0.28f, 0.62f, 0.28f), parent), _concreteMat);
        Apply(P(PrimitiveType.Sphere,   "FTop",   O + V(0, 1.20f, 0),  V(0.42f, 0.42f, 0.42f), parent), _water);

        // Trees & benches
        Tree(parent, O + V( 5.5f, 0,  5.5f), 5.5f); Tree(parent, O + V(-5.5f, 0,  5.5f), 5.0f);
        Tree(parent, O + V( 5.5f, 0, -5.5f), 4.8f); Tree(parent, O + V(-5.5f, 0, -5.5f), 5.2f);
        Bench(parent, O + V( 3.5f, 0, 0)); Bench(parent, O + V(-3.5f, 0, 0));

        // Sign
        Sign(parent, O + V(0, 0, pD / 2 + 1.8f), "PLAZA");
    }

    // ─── Street Furniture ────────────────────────────────────────────────────
    void BuildStreetFurniture()
    {
        var parent = Sub("StreetFurniture");

        // Lamps along main NS road
        float[] lampZ = { -17f, -9f, -1f, 7f, 15f };
        foreach (float z in lampZ)
        {
            Lamp(parent, new Vector3(-3.0f, 0f, z));
            Lamp(parent, new Vector3( 3.0f, 0f, z));
            Lamp(parent, new Vector3(-23.0f, 0f, z));
            Lamp(parent, new Vector3( 23.0f, 0f, z));
        }

        // Sidewalk trees along main NS road
        float[] treeZ = { -18f, -10f, -2f, 6f, 14f };
        foreach (float z in treeZ)
        {
            Tree(parent, new Vector3(-4.0f, 0f, z), 5.0f + z * 0.03f % 1.5f);
            Tree(parent, new Vector3( 4.0f, 0f, z), 4.5f + z * 0.04f % 1.5f);
        }

        // Traffic cones near survey zone
        for (int i = 0; i < 5; i++)
            Cone(parent, new Vector3(4f + i * 1.1f, 0f, 3.8f));

        // Road barriers near park/survey zone
        RoadBarrier(parent, new Vector3(-26f, 0f, -1.2f));
        RoadBarrier(parent, new Vector3(-26f, 0f,  1.2f));

        // Street signs
        Sign(parent, new Vector3(-3.5f, 0f,  4.5f), "SURVEY\nZONE");
        Sign(parent, new Vector3( 2.5f, 0f, 22.0f), "NORTH");
        Sign(parent, new Vector3(22.0f, 0f, -9.0f), "PLAZA  →");
        Sign(parent, new Vector3(-28f,  0f,  9.5f), "← PARK");
    }

    // ─── Survey Zone ─────────────────────────────────────────────────────────
    void BuildSurveyZone()
    {
        var parent = Sub("SurveyZone");
        var O = new Vector3(8f, 0f, 8f);  // Total Station centre
        const float R = 5.5f;

        // Yellow-tinted ground
        Apply(P(PrimitiveType.Cube, "ZoneFloor", O + V(0, 0.02f, 0), V(R*2, 0.04f, R*2), parent),
              M(new Color(0.96f, 0.90f, 0.55f), sm: 0.10f));

        // Border stripes
        Apply(P(PrimitiveType.Cube, "BN", O + V(0, 0.04f,  R), V(R*2, 0.04f, 0.3f), parent), _yellowMat);
        Apply(P(PrimitiveType.Cube, "BS", O + V(0, 0.04f, -R), V(R*2, 0.04f, 0.3f), parent), _yellowMat);
        Apply(P(PrimitiveType.Cube, "BE", O + V( R, 0.04f, 0), V(0.3f, 0.04f, R*2), parent), _yellowMat);
        Apply(P(PrimitiveType.Cube, "BW", O + V(-R, 0.04f, 0), V(0.3f, 0.04f, R*2), parent), _yellowMat);

        // Corner delineator posts
        foreach (var c in new[] {
            V(-R, 0, -R), V( R, 0, -R), V(-R, 0, R), V( R, 0, R)})
        {
            Apply(P(PrimitiveType.Cylinder, "Post", O + c + V(0, 0.75f, 0), V(0.12f, 0.75f, 0.12f), parent), _yellowMat);
            Apply(P(PrimitiveType.Sphere,   "Cap",  O + c + V(0, 1.58f, 0), V(0.18f, 0.18f, 0.18f), parent), _orangeMat);
        }

        // Signs
        Sign(parent, O + V(-R - 0.6f, 0, 0), "SURVEY\nZONE");
        Sign(parent, O + V(0, 0, -R - 0.6f), "TOTAL\nSTATION");
    }

    // ─── Background Skyline ──────────────────────────────────────────────────
    void BuildBackgroundSkyline()
    {
        var parent = Sub("Skyline");
        Material[] mats = { _bldgA, _bldgB, _bldgC, _bldgD };

        var skyDefs = new (float x, float z, float h, float w)[]
        {
            (-50f,-20f,26f,7f), (-50f,  0f,34f,6f), (-50f, 20f,22f,8f),
            ( 50f,-20f,30f,6f), ( 50f,  0f,38f,5f), ( 50f, 20f,24f,7f),
            (-20f,-52f,22f,7f), (  0f,-52f,35f,6f), ( 20f,-52f,28f,8f),
            (-20f, 52f,20f,7f), (  0f, 52f,30f,6f), ( 20f, 52f,24f,7f),
        };

        for (int i = 0; i < skyDefs.Length; i++)
        {
            var b = skyDefs[i];
            Apply(P(PrimitiveType.Cube, "Sky",
                   new Vector3(b.x, b.h * 0.5f, b.z),
                   new Vector3(b.w, b.h, b.w * 0.85f), parent), mats[i % mats.Length]);
            Apply(P(PrimitiveType.Cube, "SkyRoof",
                   new Vector3(b.x, b.h + 0.22f, b.z),
                   new Vector3(b.w + 0.2f, 0.42f, b.w * 0.85f + 0.2f), parent), _roofMat);
        }
    }

    // ─── Reusable sub-elements ────────────────────────────────────────────────
    void Tree(Transform parent, Vector3 pos, float height)
    {
        // Improved procedural tree: trunk + multiple canopy lobes + small branches
        height = Mathf.Max(3.5f, height);

        // Trunk
        var trunk = P(PrimitiveType.Cylinder, "Trunk", pos + V(0, height * 0.18f, 0), V(0.18f, height * 0.18f, 0.18f), parent);
        Apply(trunk, _trunk);

        // Slight random tilt/variation
        trunk.transform.localRotation = Quaternion.Euler(Random.Range(-4f, 4f), Random.Range(0f, 360f), Random.Range(-3f, 3f));

        // Create 3 small branches connecting trunk to canopy lobes
        for (int b = 0; b < 3; b++)
        {
            float ang = b * 120f + Random.Range(-15f, 15f);
            Vector3 dir = new Vector3(Mathf.Sin(ang * Mathf.Deg2Rad), 0.45f, Mathf.Cos(ang * Mathf.Deg2Rad));
            Vector3 branchPos = pos + dir * (height * 0.25f) + Vector3.up * (height * 0.45f);
            float blen = 0.6f;
            var branch = P(PrimitiveType.Cylinder, "Branch" + b, branchPos, V(0.06f, blen, 0.06f), parent);
            branch.transform.rotation = Quaternion.FromToRotation(Vector3.up, dir.normalized) * Quaternion.Euler(0f, 0f, 90f);
            Apply(branch, _trunk);
        }

        // Canopy - multiple overlapping ellipsoids for natural look
        int lobes = Random.Range(3, 5);
        for (int i = 0; i < lobes; i++)
        {
            float rx = Random.Range(0.9f, 1.4f);
            float ry = Random.Range(0.9f, 1.6f);
            float rz = Random.Range(0.9f, 1.4f);
            float ox = Random.Range(-0.35f, 0.35f);
            float oz = Random.Range(-0.35f, 0.35f);
            float oy = Random.Range(0.35f, 0.75f);
            var fol = P(PrimitiveType.Sphere, "Lobe" + i, pos + V(ox, height * oy, oz), new Vector3(rx, ry, rz), parent);
            Apply(fol, (i % 2 == 0) ? _foliageA : _foliageB);
        }
    }

    void Lamp(Transform parent, Vector3 pos)
    {
        Apply(P(PrimitiveType.Cylinder, "Pole",
               pos + V(0, 3.1f, 0), V(0.08f, 3.1f, 0.08f), parent), _lampPost);
        Apply(P(PrimitiveType.Cube, "Arm",
               pos + V(0.55f, 6.25f, 0), V(1.1f, 0.07f, 0.07f), parent), _lampPost);
        Apply(P(PrimitiveType.Cylinder, "Hood",
               pos + V(1.05f, 6.05f, 0), V(0.36f, 0.10f, 0.36f), parent), _lampPost);
        Apply(P(PrimitiveType.Sphere, "Bulb",
               pos + V(1.05f, 5.88f, 0), V(0.22f, 0.22f, 0.22f), parent), _lampBulb);
    }

    void Bench(Transform parent, Vector3 pos)
    {
        Apply(P(PrimitiveType.Cube, "Seat",
               pos + V(0, 0.47f, 0),   V(1.45f, 0.09f, 0.46f), parent), _benchMat);
        Apply(P(PrimitiveType.Cube, "Back",
               pos + V(0, 0.88f, -0.19f), V(1.45f, 0.55f, 0.07f), parent), _benchMat);
        Apply(P(PrimitiveType.Cube, "LegL",
               pos + V(-0.58f, 0.23f, 0), V(0.08f, 0.46f, 0.42f), parent), _lampPost);
        Apply(P(PrimitiveType.Cube, "LegR",
               pos + V( 0.58f, 0.23f, 0), V(0.08f, 0.46f, 0.42f), parent), _lampPost);
    }

    void Cone(Transform parent, Vector3 pos)
    {
        Apply(P(PrimitiveType.Cylinder, "ConeBase",
               pos + V(0, 0.05f, 0),  V(0.30f, 0.05f, 0.30f), parent), _orangeMat);
        Apply(P(PrimitiveType.Sphere,  "ConeBody",
               pos + V(0, 0.22f, 0),  V(0.18f, 0.36f, 0.18f), parent), _orangeMat);
        Apply(P(PrimitiveType.Sphere,  "ConeTop",
               pos + V(0, 0.45f, 0),  V(0.06f, 0.12f, 0.06f), parent), _whiteMat);
    }

    void RoadBarrier(Transform parent, Vector3 pos)
    {
        Apply(P(PrimitiveType.Cube, "Barrier",
               pos + V(0, 0.42f, 0), V(2.1f, 0.82f, 0.36f), parent), _barrierMat);
        Apply(P(PrimitiveType.Cube, "Stripe",
               pos + V(0, 0.42f, 0), V(2.1f, 0.14f, 0.37f), parent), _orangeMat);
    }

    void Sign(Transform parent, Vector3 pos, string label)
    {
        Apply(P(PrimitiveType.Cylinder, "Post",
               pos + V(0, 1.15f, 0), V(0.07f, 1.15f, 0.07f), parent), _signPost);
        Apply(P(PrimitiveType.Cube, "Board",
               pos + V(0, 2.35f, 0), V(1.35f, 0.55f, 0.08f), parent), _signBoard);
    }

    // ─── Core helpers ─────────────────────────────────────────────────────────
    /// <summary>Create a named child GameObject (empty).</summary>
    Transform Sub(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform);
        return go.transform;
    }

    /// <summary>Create a primitive, parent it, set world position and scale.</summary>
    GameObject P(PrimitiveType type, string name, Vector3 pos, Vector3 scale,
                 Transform parent = null)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent ?? transform);
        go.transform.position   = pos;
        go.transform.localScale = scale;
        return go;
    }

    void Apply(GameObject go, Material mat)
    {
        var r = go.GetComponent<Renderer>();
        if (r) r.material = mat;
    }

    /// <summary>Shorthand Vector3 constructor.</summary>
    static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

    /// <summary>Create a material: URP/Lit if available, else Standard.</summary>
    Material M(Color c, float sm = 0.3f, float mt = 0f, Color em = default)
    {
        var mat = new Material(_shader);

        if (_isURP)
        {
            mat.SetColor("_BaseColor",  c);
            mat.SetFloat("_Smoothness", sm);
            mat.SetFloat("_Metallic",   mt);
        }
        else
        {
            mat.color = c;
            mat.SetFloat("_Glossiness", sm);
            mat.SetFloat("_Metallic",   mt);
        }

        if (em.r > 0.01f || em.g > 0.01f || em.b > 0.01f)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", em);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        return mat;
    }
}
