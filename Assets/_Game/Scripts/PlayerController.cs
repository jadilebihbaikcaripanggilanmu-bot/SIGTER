using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// WASD movement, sprint (Left Shift), jump (Space).
/// Uses New Input System via Keyboard.current.
/// Falls back to legacy Input if Keyboard.current is null.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float WalkSpeed   = 12f;
    public float RunSpeed    = 24f;
    public float JumpHeight  = 2.5f;   // metres
    public float Gravity     = -28f;

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

    void Start()
    {
        _velocity = Vector3.zero;
    }

    private bool IsGroundedCustom()
    {
        if (_cc.isGrounded) return true;

        // Perform a short spherecast downward from the bottom of the CharacterController capsule
        float radius = _cc.radius * 0.9f;
        // Start origin slightly above the bottom of the capsule (0.1m up)
        Vector3 origin = transform.position + Vector3.up * radius;
        float castDistance = 0.25f; // check slightly below feet

        if (Physics.SphereCast(origin, radius, Vector3.down, out RaycastHit hit, castDistance))
        {
            // If we hit any solid collider (not trigger), we are grounded
            if (!hit.collider.isTrigger)
            {
                return true;
            }
        }
        return false;
    }

    void Update()
    {
        bool grounded = IsGroundedCustom();
        if (grounded && _velocity.y < 0f) _velocity.y = -2f;

        // Reset velocity if it drops too low instantly (protection for WebGL startup frames)
        if (_velocity.y < -30f && Time.frameCount < 10) _velocity.y = -2f;

        // When movement is frozen, only apply gravity
        if (!_movementEnabled)
        {
            _velocity.y += Gravity * Time.deltaTime;
            _cc.Move(Vector3.up * _velocity.y * Time.deltaTime);
            return;
        }

        // ── Directional input (New Input System with legacy fallback) ────
        float h = 0f, v = 0f;
        bool sprint = false;
        bool jump   = false;

        var kb = Keyboard.current;
        if (kb != null)
        {
            h = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
            v = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);
            sprint = kb.leftShiftKey.isPressed;
            jump   = kb.spaceKey.wasPressedThisFrame;

            // Also support arrow keys
            if (h == 0f) h = (kb.rightArrowKey.isPressed ? 1f : 0f) - (kb.leftArrowKey.isPressed ? 1f : 0f);
            if (v == 0f) v = (kb.upArrowKey.isPressed ? 1f : 0f) - (kb.downArrowKey.isPressed ? 1f : 0f);
        }
        else
        {
            // Legacy Input fallback (works if New Input System is missing or unfocused)
            h = Input.GetAxisRaw("Horizontal");
            v = Input.GetAxisRaw("Vertical");
            sprint = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            jump = Input.GetKeyDown(KeyCode.Space);
        }

        // ── Teleport key (T) to cycle spawn locations ────────────────────
        if ((kb != null && kb.tKey.wasPressedThisFrame) || Input.GetKeyDown(KeyCode.T))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TeleportToNextSpawn();
            }
        }

        Vector3 fwd = CamRig != null ? Flat(CamRig.forward) : Flat(transform.forward);
        Vector3 rgt = CamRig != null ? Flat(CamRig.right)   : Flat(transform.right);

        float speed    = sprint ? RunSpeed : WalkSpeed;
        Vector3 move   = (fwd * v + rgt * h).normalized * speed;

        // ── Jump ──────────────────────────────────────────────────────────
        if (jump && grounded)
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

    public void Teleport(Vector3 position)
    {
        if (_cc != null) _cc.enabled = false;
        transform.position = position;
        if (_cc != null) _cc.enabled = true;
        _velocity = Vector3.zero;
    }

    static Vector3 Flat(Vector3 v) { v.y = 0f; return v.normalized; }
}
