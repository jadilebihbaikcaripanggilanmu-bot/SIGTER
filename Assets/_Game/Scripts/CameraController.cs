using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Smooth FPV / TPV camera — New Input System.
/// ► Tune mouseSensitivityX / Y in the Inspector to change speed.
///   Default = 0.08 (gentle). Increase toward 0.15 for faster feel.
/// </summary>
public class CameraController : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════
    //  Inspector fields — adjust these to tune the camera
    // ═══════════════════════════════════════════════════════

    [Header("Sensitivity  ◄ Adjust here if camera is too fast/slow")]
    [Tooltip("Horizontal speed")]
    public float mouseSensitivityX = 0.25f;

    [Tooltip("Vertical speed. Usually matches X.")]
    public float mouseSensitivityY = 0.25f;

    [Header("Smoothing")]
    [Tooltip("Damp time in seconds. 0 = instant. 0.03 = slight lag (default).")]
    public float cameraSmoothTime = 0.03f;

    [Header("Vertical Limits")]
    [Tooltip("Max downward look angle.")]
    public float minVerticalAngle = -45f;

    [Tooltip("Max upward look angle.")]
    public float maxVerticalAngle = 75f;

    [Header("References")]
    public Transform PlayerTransform;
    public Camera    Cam;

    [Header("Third-Person")]
    public float TPVDistance = 5f;
    public float TPVHeight   = 2f;

    // ── Private state ─────────────────────────────────────
    private float _targetYaw,   _yaw,   _yawVel;
    private float _targetPitch, _pitch, _pitchVel;

    private bool _isFPV        = true;
    private bool _cursorLocked = true;
    private bool _inputEnabled = true;   // false during centering/leveling mini-games

    private Vector3 _tpvOffset;
    // Converts raw mouse delta (pixels) into degrees. 1 = no conversion.
    private const float PixelToDegree = 1.0f;

    // PlayerPrefs keys
    private const string PrefSensX = "cam_sensitivity_x";
    private const string PrefSensY = "cam_sensitivity_y";
    private const string PrefSmooth = "cam_smooth_time";
    private const string PrefFov = "cam_field_of_view";

    // ── Lifecycle ─────────────────────────────────────────
    void Start()
    {
        _tpvOffset = new Vector3(0f, TPVHeight, -TPVDistance);
        _yaw = _targetYaw = transform.eulerAngles.y;
        // Initialize pitch from camera's current local rotation (handle 0-360 wrap)
        float initialPitch = Cam != null ? Cam.transform.localEulerAngles.x : 0f;
        if (initialPitch > 180f) initialPitch -= 360f;
        _targetPitch = _pitch = Mathf.Clamp(initialPitch, minVerticalAngle, maxVerticalAngle);

        // Load saved settings (if present)
        if (PlayerPrefs.HasKey(PrefSensX)) mouseSensitivityX = PlayerPrefs.GetFloat(PrefSensX);
        if (PlayerPrefs.HasKey(PrefSensY)) mouseSensitivityY = PlayerPrefs.GetFloat(PrefSensY);
        if (PlayerPrefs.HasKey(PrefSmooth)) cameraSmoothTime = PlayerPrefs.GetFloat(PrefSmooth);
        if (PlayerPrefs.HasKey(PrefFov) && Cam != null) Cam.fieldOfView = PlayerPrefs.GetFloat(PrefFov);

        LockCursor(true);
        ApplyView();
    }

    void Update()
    {
        var kb    = Keyboard.current;
        var mouse = Mouse.current;

        // Escape unlocks / re-locks cursor (works even in mini-games)
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
            LockCursor(!_cursorLocked);

        // V — switch FPV / TPV
        if (kb != null && kb.vKey.wasPressedThisFrame)
        {
            _isFPV = !_isFPV;
            ApplyView();
        }

        // Only rotate when cursor locked AND input enabled
        if (!_cursorLocked || !_inputEnabled || mouse == null) return;

        // Read raw mouse delta (pixels since last frame) and convert to degrees.
        Vector2 rawDelta = mouse.delta.ReadValue();
        Vector2 delta    = rawDelta * PixelToDegree;

        // Apply sensitivity (degrees per converted-pixel) and clamp vertical look.
        _targetYaw  += delta.x * mouseSensitivityX;
        _targetPitch = Mathf.Clamp(_targetPitch - delta.y * mouseSensitivityY,
                       minVerticalAngle, maxVerticalAngle);

        // Smooth-damp toward target (cameraSmoothTime controls lag)
        _yaw   = Mathf.SmoothDampAngle(_yaw,   _targetYaw,   ref _yawVel,   cameraSmoothTime);
        _pitch = Mathf.SmoothDamp      (_pitch, _targetPitch, ref _pitchVel, cameraSmoothTime);

        transform.rotation = Quaternion.Euler(0f, _yaw, 0f);

        if (_isFPV)
            Cam.transform.localEulerAngles = new Vector3(_pitch, 0f, 0f);
        else
            UpdateTPV();
    }

    void UpdateTPV()
    {
        Vector3 desired = Quaternion.Euler(_pitch * 0.35f, 0f, 0f) * _tpvOffset;
        float   t       = Mathf.Clamp01(Time.deltaTime / Mathf.Max(cameraSmoothTime * 2f, 0.01f));
        Cam.transform.localPosition = Vector3.Lerp(Cam.transform.localPosition, desired, t);
        if (PlayerTransform)
            Cam.transform.LookAt(PlayerTransform.position + Vector3.up * 1.1f);
    }

    void ApplyView()
    {
        if (_isFPV)
        {
            Cam.transform.localPosition    = Vector3.zero;
            Cam.transform.localEulerAngles = new Vector3(_pitch, 0f, 0f);
        }
        else
        {
            Cam.transform.localPosition = _tpvOffset;
            if (PlayerTransform) Cam.transform.LookAt(PlayerTransform.position + Vector3.up * 1.1f);
        }
    }

    void LockCursor(bool locked)
    {
        _cursorLocked    = locked;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible   = !locked;
    }

    // Public API to control cursor lock from other systems (UI)
    public void SetCursorLocked(bool locked) => LockCursor(locked);

    /// <summary>Apply camera settings at runtime (called from UI).</summary>
    public void ApplySettings(float sensX, float sensY, float smoothTime, float fov, float minAngle, float maxAngle, bool save)
    {
        mouseSensitivityX = sensX;
        mouseSensitivityY = sensY;
        cameraSmoothTime = smoothTime;
        minVerticalAngle = minAngle;
        maxVerticalAngle = maxAngle;
        if (Cam != null) Cam.fieldOfView = fov;

        // Ensure pitch stays clamped after changing limits
        _targetPitch = Mathf.Clamp(_targetPitch, minVerticalAngle, maxVerticalAngle);
        _pitch = Mathf.Clamp(_pitch, minVerticalAngle, maxVerticalAngle);

        if (save)
        {
            PlayerPrefs.SetFloat(PrefSensX, mouseSensitivityX);
            PlayerPrefs.SetFloat(PrefSensY, mouseSensitivityY);
            PlayerPrefs.SetFloat(PrefSmooth, cameraSmoothTime);
            if (Cam != null) PlayerPrefs.SetFloat(PrefFov, Cam.fieldOfView);
            PlayerPrefs.Save();
        }
    }

    // ── Public API ────────────────────────────────────────
    public bool IsFPV => _isFPV;

    /// <summary>Call with false to freeze camera during mini-games.</summary>
    public void SetInputEnabled(bool on) => _inputEnabled = on;

    /// <summary>Force-set camera orientation (instrument aiming mode).</summary>
    public void SetOrientation(float yaw, float pitch)
    {
        _targetYaw = _yaw = yaw;
        _targetPitch = _pitch = Mathf.Clamp(pitch, minVerticalAngle, maxVerticalAngle);
        transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
        if (_isFPV) Cam.transform.localEulerAngles = new Vector3(_pitch, 0f, 0f);
    }
}
