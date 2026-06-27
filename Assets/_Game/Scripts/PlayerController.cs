using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// WASD movement, sprint (Left Shift), jump (Space).
/// Uses New Input System via Keyboard.current – no UnityEngine.Input calls.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float WalkSpeed   = 5f;
    public float RunSpeed    = 10f;
    public float JumpHeight  = 1.4f;   // metres
    public float Gravity     = -22f;

    [Header("References")]
    public Transform CamRig; // set by GameManager

    private CharacterController _cc;
    private Vector3 _velocity;
    private bool _movementEnabled = true;

    /// <summary>Freeze horizontal movement (e.g. during mini-games). Gravity still applied.</summary>
    public void SetMovementEnabled(bool enabled) => _movementEnabled = enabled;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        bool grounded = _cc.isGrounded;
        if (grounded && _velocity.y < 0f) _velocity.y = -2f;

        // When movement is frozen (mini-games), only apply gravity
        if (!_movementEnabled)
        {
            _velocity.y += Gravity * Time.deltaTime;
            _cc.Move(Vector3.up * _velocity.y * Time.deltaTime);
            return;
        }



        // ── Directional input ──────────────────────────────────────────────
        float h = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
        float v = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);

        Vector3 fwd = CamRig != null ? Flat(CamRig.forward) : Flat(transform.forward);
        Vector3 rgt = CamRig != null ? Flat(CamRig.right)   : Flat(transform.right);

        bool sprinting = kb.leftShiftKey.isPressed;
        float speed    = sprinting ? RunSpeed : WalkSpeed;
        Vector3 move   = (fwd * v + rgt * h).normalized * speed;

        // ── Jump ──────────────────────────────────────────────────────────
        if (kb.spaceKey.wasPressedThisFrame && grounded)
            _velocity.y = Mathf.Sqrt(JumpHeight * -2f * Gravity);

        // ── Gravity ───────────────────────────────────────────────────────
        _velocity.y += Gravity * Time.deltaTime;
        _cc.Move((move + Vector3.up * _velocity.y) * Time.deltaTime);

        // ── Rotate body yaw to match camera so TPV looks correct ──────────
        if (CamRig != null)
        {
            var e = transform.eulerAngles;
            e.y = CamRig.eulerAngles.y;
            transform.eulerAngles = e;
        }
    }

    static Vector3 Flat(Vector3 v) { v.y = 0f; return v.normalized; }
}
