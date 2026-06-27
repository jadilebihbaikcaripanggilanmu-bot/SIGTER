using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Simple HUD elements for 3D City exploration.
/// Spawns a circular UI Canvas-based minimap dynamically (no square corners on screen).
/// Draws the camera mode toggle and Esc settings menu with OnGUI.
/// </summary>
public class UIManager : MonoBehaviour
{
    // ─── OnGUI Styles ────────────────────────────────────────────────────────
    private GUIStyle _boxStyle;
    private GUIStyle _titleStyle;
    private GUIStyle _bodyStyle;
    private GUIStyle _camStyle;
    private bool _stylesInit = false;

    // Colors
    private Color _panelBg   = new Color(0.05f, 0.10f, 0.18f, 0.85f);
    private Color _camBg     = new Color(0f, 0f, 0f, 0.55f);

    // ─── Minimap Canvas Elements ──────────────────────────────────────────────
    private GameObject _minimapCanvasInstance;
    private RectTransform _northTextRT;
    private Sprite _circleSprite;

    Sprite GetOrCreateCircleSprite()
    {
        if (_circleSprite != null) return _circleSprite;

        int size = 256;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color[size * size];
        float center = size * 0.5f;
        float radius = size * 0.5f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                
                if (dist <= radius)
                {
                    pixels[y * size + x] = Color.white;
                }
                else
                {
                    pixels[y * size + x] = Color.clear;
                }
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        
        _circleSprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
        return _circleSprite;
    }

    void Start()
    {
        CreateMinimapUI();
    }

    void InitStyles()
    {
        if (_stylesInit) return;
        _stylesInit = true;

        _boxStyle = new GUIStyle(GUI.skin.box);
        _boxStyle.normal.background = MakeTex(2, 2, _panelBg);
        _boxStyle.border = new RectOffset(4, 4, 4, 4);

        _titleStyle = new GUIStyle(GUI.skin.label);
        _titleStyle.fontSize = 15;
        _titleStyle.fontStyle = FontStyle.Bold;
        _titleStyle.normal.textColor = new Color(0.25f, 0.85f, 0.55f);
        _titleStyle.alignment = TextAnchor.MiddleLeft;

        _bodyStyle = new GUIStyle(GUI.skin.label);
        _bodyStyle.fontSize = 13;
        _bodyStyle.normal.textColor = new Color(0.85f, 0.90f, 0.95f);
        _bodyStyle.wordWrap = true;

        _camStyle = new GUIStyle(GUI.skin.box);
        _camStyle.fontSize = 11;
        _camStyle.fontStyle = FontStyle.Bold;
        _camStyle.normal.background = MakeTex(2, 2, _camBg);
        _camStyle.normal.textColor = new Color(0.85f, 0.85f, 1f);
        _camStyle.alignment = TextAnchor.MiddleCenter;
        _camStyle.padding = new RectOffset(6, 6, 4, 4);
    }

    // ── Camera Settings UI state ─────────────────────────────────────────
    private bool _showCameraSettings = false;
    private float _ui_sensX = 0.25f;
    private float _ui_sensY = 0.25f;
    private float _ui_smooth = 0.03f;
    private float _ui_fov = 65f;

    // Presets
    private readonly (float x, float y) PresetLow = (0.15f, 0.15f);
    private readonly (float x, float y) PresetMedium = (0.35f, 0.35f);
    private readonly (float x, float y) PresetHigh = (0.75f, 0.75f);

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.f1Key.wasPressedThisFrame))
        {
            ToggleCameraSettings();
        }

        // Dynamically rotate North "N" indicator around circular minimap edge
        var gm = GameManager.Instance;
        if (gm != null && gm.Player != null && _northTextRT != null)
        {
            float mSize = 160f;
            float radius = mSize * 0.5f - 14f; // slightly inside border
            
            // In screen coordinates, North rotates counter-clockwise as player yaw increases
            float angleRad = -gm.Player.transform.eulerAngles.y * Mathf.Deg2Rad;
            float nx = Mathf.Sin(angleRad) * radius;
            float ny = -Mathf.Cos(angleRad) * radius; // invert Y since UI Y is down
            
            _northTextRT.anchoredPosition = new Vector2(nx, ny);
        }
    }

    void ToggleCameraSettings()
    {
        _showCameraSettings = !_showCameraSettings;
        var cam = GameManager.Instance?.CamController;
        if (_showCameraSettings)
        {
            if (cam != null)
            {
                _ui_sensX = cam.mouseSensitivityX;
                _ui_sensY = cam.mouseSensitivityY;
                _ui_smooth = cam.cameraSmoothTime;
                if (cam.Cam != null) _ui_fov = cam.Cam.fieldOfView;
                cam.SetInputEnabled(false);
                cam.SetCursorLocked(false);
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
        else
        {
            if (cam != null)
            {
                cam.SetCursorLocked(true);
                cam.SetInputEnabled(true);
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    void CreateMinimapUI()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.MinimapTexture == null) return;

        Sprite circleSprite = GetOrCreateCircleSprite();

        // 1. Create Canvas
        _minimapCanvasInstance = new GameObject("MinimapCanvas");
        var canvas = _minimapCanvasInstance.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _minimapCanvasInstance.AddComponent<UnityEngine.UI.CanvasScaler>();
        _minimapCanvasInstance.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // 2. Circle Border (Back Layer)
        var borderGO = new GameObject("MinimapBorder");
        borderGO.transform.SetParent(_minimapCanvasInstance.transform, false);
        var borderImg = borderGO.AddComponent<UnityEngine.UI.Image>();
        borderImg.sprite = circleSprite;
        borderImg.color = new Color(0.25f, 0.65f, 0.95f, 1f); // Glowing cyan circle outline
        
        var borderRT = borderGO.GetComponent<RectTransform>();
        borderRT.anchorMin = new Vector2(0f, 1f);
        borderRT.anchorMax = new Vector2(0f, 1f);
        borderRT.pivot = new Vector2(0f, 1f);
        borderRT.anchoredPosition = new Vector2(14f, -14f); // outline position
        borderRT.sizeDelta = new Vector2(164f, 164f);

        // 3. Circle Mask
        var maskGO = new GameObject("MinimapMask");
        maskGO.transform.SetParent(_minimapCanvasInstance.transform, false);
        var maskImg = maskGO.AddComponent<UnityEngine.UI.Image>();
        maskImg.sprite = circleSprite;
        
        var mask = maskGO.AddComponent<UnityEngine.UI.Mask>();
        mask.showMaskGraphic = false; // Mask is hidden, only clips children

        var maskRT = maskGO.GetComponent<RectTransform>();
        maskRT.anchorMin = new Vector2(0f, 1f);
        maskRT.anchorMax = new Vector2(0f, 1f);
        maskRT.pivot = new Vector2(0f, 1f);
        maskRT.anchoredPosition = new Vector2(16f, -16f);
        maskRT.sizeDelta = new Vector2(160f, 160f);

        // 4. RawImage (Minimap Camera Render Texture) - masked inside circle
        var rawGO = new GameObject("MinimapRaw");
        rawGO.transform.SetParent(maskGO.transform, false);
        var rawImg = rawGO.AddComponent<UnityEngine.UI.RawImage>();
        rawImg.texture = gm.MinimapTexture;

        var rawRT = rawGO.GetComponent<RectTransform>();
        rawRT.anchorMin = Vector2.zero;
        rawRT.anchorMax = Vector2.one;
        rawRT.sizeDelta = Vector2.zero;

        // 5. Player Pointer Dot (Center of Minimap Circle)
        var pDotGO = new GameObject("MinimapPlayerPointer");
        pDotGO.transform.SetParent(_minimapCanvasInstance.transform, false); // place on top
        var pDotImg = pDotGO.AddComponent<UnityEngine.UI.Image>();
        pDotImg.sprite = circleSprite;
        pDotImg.color = new Color(0.25f, 0.95f, 0.60f, 1f); // Glowing bright green

        var pDotRT = pDotGO.GetComponent<RectTransform>();
        pDotRT.anchorMin = new Vector2(0f, 1f);
        pDotRT.anchorMax = new Vector2(0f, 1f);
        pDotRT.pivot = new Vector2(0.5f, 0.5f);
        pDotRT.anchoredPosition = new Vector2(16f + 80f, -16f - 80f); // Center of 160x160 map
        pDotRT.sizeDelta = new Vector2(10f, 10f);

        // 6. Rotating North "N" Indicator (Floating above border)
        var northGO = new GameObject("MinimapNorthIndicator");
        northGO.transform.SetParent(_minimapCanvasInstance.transform, false);
        var northText = northGO.AddComponent<UnityEngine.UI.Text>();
        northText.text = "N";
        // Let Unity UI automatically use the default fallback font
        northText.fontSize = 15;
        northText.fontStyle = FontStyle.Bold;
        northText.color = new Color(1f, 0.2f, 0.2f, 1f); // Red
        northText.alignment = TextAnchor.MiddleCenter;

        _northTextRT = northGO.GetComponent<RectTransform>();
        _northTextRT.anchorMin = new Vector2(0f, 1f);
        _northTextRT.anchorMax = new Vector2(0f, 1f);
        _northTextRT.pivot = new Vector2(0.5f, 0.5f);
        _northTextRT.anchoredPosition = new Vector2(16f + 80f, -16f - 14f); // Top of circle
        _northTextRT.sizeDelta = new Vector2(20f, 20f);
    }

    void OnGUI()
    {
        InitStyles();

        float sw = Screen.width;
        float sh = Screen.height;

        // ── Crosshair ─────────────────────────────────────────────────────────
        float cx = sw * 0.5f, cy = sh * 0.5f;
        float cs = 8f;
        GUI.color = new Color(1, 1, 1, 0.6f);
        GUI.DrawTexture(new Rect(cx - cs, cy - 1, cs * 2, 2), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(cx - 1, cy - cs, 2, cs * 2), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // ── Camera mode ───────────────────────────────────────────────────────
        var cam = GameManager.Instance?.CamController;
        string camText = cam != null
            ? (cam.IsFPV ? "📷  FPV" : "🎥  TPV")
            : "📷  FPV";
        GUI.Box(new Rect(sw - 110, 12, 90, 30), camText, _camStyle);

        // ── Camera Settings Panel ─────────────────────────────────────────
        if (_showCameraSettings)
        {
            float w = 420f, h = 380f;
            float x = sw * 0.5f - w * 0.5f;
            float y = sh * 0.5f - h * 0.5f;
            GUI.Box(new Rect(x, y, w, h), "Camera Settings", _boxStyle);

            GUI.Label(new Rect(x + 16, y + 32, 200, 22), "Sensitivity X", _titleStyle);
            _ui_sensX = GUI.HorizontalSlider(new Rect(x + 16, y + 56, w - 32, 20), _ui_sensX, 0.05f, 2.0f);
            GUI.Label(new Rect(x + 16, y + 76, w - 32, 20), $"{_ui_sensX:F2}", _bodyStyle);

            GUI.Label(new Rect(x + 16, y + 100, 200, 22), "Sensitivity Y", _titleStyle);
            _ui_sensY = GUI.HorizontalSlider(new Rect(x + 16, y + 124, w - 32, 20), _ui_sensY, 0.05f, 2.0f);
            GUI.Label(new Rect(x + 16, y + 144, w - 32, 20), $"{_ui_sensY:F2}", _bodyStyle);

            GUI.Label(new Rect(x + 16, y + 168, 240, 22), "Camera Smoothness (damp time)", _titleStyle);
            _ui_smooth = GUI.HorizontalSlider(new Rect(x + 16, y + 192, w - 32, 20), _ui_smooth, 0.00f, 0.20f);
            GUI.Label(new Rect(x + 16, y + 212, w - 32, 20), $"{_ui_smooth:F3} s", _bodyStyle);

            GUI.Label(new Rect(x + 16, y + 236, 200, 22), "Field of View", _titleStyle);
            _ui_fov = GUI.HorizontalSlider(new Rect(x + 16, y + 260, w - 32, 20), _ui_fov, 55f, 85f);
            GUI.Label(new Rect(x + 16, y + 280, w - 32, 20), $"{_ui_fov:F0}°", _bodyStyle);

            // Presets
            if (GUI.Button(new Rect(x + 16, y + 306, 120, 28), "Low")) { _ui_sensX = PresetLow.x; _ui_sensY = PresetLow.y; }
            if (GUI.Button(new Rect(x + 148, y + 306, 120, 28), "Medium")) { _ui_sensX = PresetMedium.x; _ui_sensY = PresetMedium.y; }
            if (GUI.Button(new Rect(x + 280, y + 306, 120, 28), "High")) { _ui_sensX = PresetHigh.x; _ui_sensY = PresetHigh.y; }

            // Buttons: Apply / Reset Default / Close
            if (GUI.Button(new Rect(x + 16, y + 340, 120, 28), "Apply"))
            {
                if (cam != null)
                {
                    cam.ApplySettings(_ui_sensX, _ui_sensY, _ui_smooth, _ui_fov, cam.minVerticalAngle, cam.maxVerticalAngle, true);
                }
            }
            if (GUI.Button(new Rect(x + 156, y + 340, 140, 28), "Reset Default"))
            {
                _ui_sensX = 0.25f; _ui_sensY = 0.25f; _ui_smooth = 0.03f; _ui_fov = 65f;
                if (cam != null) cam.ApplySettings(_ui_sensX, _ui_sensY, _ui_smooth, _ui_fov, cam.minVerticalAngle, cam.maxVerticalAngle, true);
            }
            if (GUI.Button(new Rect(x + 308, y + 340, 96, 28), "Close"))
            {
                ToggleCameraSettings();
            }
        }
    }

    void OnDestroy()
    {
        if (_minimapCanvasInstance != null) Destroy(_minimapCanvasInstance);
    }

    Texture2D MakeTex(int w, int h, Color col)
    {
        var tex = new Texture2D(w, h);
        var pixels = new Color[w * h];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = col;
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }
}
