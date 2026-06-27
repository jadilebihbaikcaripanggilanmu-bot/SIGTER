using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Simple HUD elements for 3D City exploration drawn with OnGUI.
/// Shows: movement controls, camera mode, and camera settings panel.
/// </summary>
public class UIManager : MonoBehaviour
{
    // ─── Styles ───────────────────────────────────────────────────────────────
    private GUIStyle _boxStyle;
    private GUIStyle _titleStyle;
    private GUIStyle _bodyStyle;
    private GUIStyle _camStyle;
    private bool _stylesInit = false;

    // Colors
    private Color _panelBg   = new Color(0.05f, 0.10f, 0.18f, 0.85f);
    private Color _camBg     = new Color(0f, 0f, 0f, 0.55f);

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

    // Suggested presets
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

        // ── Controls Guide Panel ───────────────────────────────────────────────
        float objW = 280f;
        float objH = 120f;
        GUI.Box(new Rect(12, 12, objW, objH), "", _boxStyle);
        GUI.Label(new Rect(20, 16, objW - 16, 22), "🎮  CONTROLS", _titleStyle);
        
        string guideText = 
            "- Move: WASD / Arrow Keys\n" +
            "- Sprint: Hold Left Shift\n" +
            "- Jump: Space\n" +
            "- Camera View [V]: FPV / TPV\n" +
            "- Settings [Esc / F1]: Adjust Mouse Sensitivity";

        GUI.Label(new Rect(20, 40, objW - 20, objH - 44), guideText, _bodyStyle);
    }

    // ─── Helper ───────────────────────────────────────────────────────────────
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
