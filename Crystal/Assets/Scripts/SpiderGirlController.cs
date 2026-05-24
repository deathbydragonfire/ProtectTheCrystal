using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Character controller for the spider hybrid girl.
/// Gravity is always active except when she is stuck to the ceiling.
/// The floor is handled purely by physics collision.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class SpiderGirlController : MonoBehaviour
{
    // ── Inspector ────────────────────────────────────────────────────────────

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private Transform enemyTransform;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 14f;
    [SerializeField] private float gravityScale = 3f;

    [Header("Surface Detection")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Transform ceilingCheck;
    [SerializeField] private float checkRadius = 0.15f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask ceilingLayer;

    // ── Animator parameter names ─────────────────────────────────────────────

    private static readonly int AnimRunning   = Animator.StringToHash("running");
    private static readonly int AnimCeiling   = Animator.StringToHash("ceiling");
    private static readonly int AnimJump      = Animator.StringToHash("jump");
    private static readonly int AnimBackwards = Animator.StringToHash("backwards");
    private static readonly int AnimLanded    = Animator.StringToHash("landed");

    // ── Constants ────────────────────────────────────────────────────────────

    private const float JumpDelay = 0.1f;

    // ── State ────────────────────────────────────────────────────────────────

    private Rigidbody2D _rb;
    private Animator    _animator;
    private SpriteFlip  _spriteFlip;
    private bool _isOnGround;
    private bool _isOnCeiling;
    private bool _isAirborne;
    private bool _jumpRequested;
    private bool _jumpInProgress;

    /// <summary>Horizontal input axis value from the Move action.</summary>
    public float HorizontalInput { get; private set; }

    /// <summary>True while the character is between surfaces (airborne).</summary>
    public bool IsAirborne => _isAirborne;

    /// <summary>True while the character is standing on the floor.</summary>
    public bool IsOnGround => _isOnGround;

    /// <summary>True while the character is standing on the ceiling.</summary>
    public bool IsOnCeiling => _isOnCeiling;

    // ── Surface normal helpers ───────────────────────────────────────────────

    /// <summary>
    /// The surface the character is currently sticking to.
    /// Floor → down, Ceiling → up, Airborne → last known normal.
    /// </summary>
    public SurfaceType CurrentSurface { get; private set; } = SurfaceType.Floor;

    public enum SurfaceType { Floor, Ceiling }

    // ── Unity Lifecycle ──────────────────────────────────────────────────────

    private void Awake()
    {
        _rb       = GetComponent<Rigidbody2D>();
        _animator = GetComponentInChildren<Animator>();

        _rb.gravityScale  = gravityScale;
        _rb.freezeRotation = true;

        _spriteFlip = GetComponentInChildren<SpriteFlip>();
        if (_spriteFlip == null)
            Debug.LogWarning("[SpiderGirl] Awake — no SpriteFlip found in children.");

        if (_animator == null)
            Debug.LogWarning("[SpiderGirl] Awake — no Animator found in children.");

        Debug.Log($"[SpiderGirl] Awake — Rigidbody2D initialised. gravityScale={gravityScale}, freezeRotation=true");
    }

    private void FixedUpdate()
    {
        DetectSurfaces();
        ApplyGravityMode();

        if (_jumpRequested)
        {
            _jumpRequested = false;
            Debug.Log("[SpiderGirl] FixedUpdate — consuming jump request");
            TryJump();
        }
        else
        {
            ApplyMovement();
        }
    }

    // ── Input Callbacks (wired via Player Input component) ───────────────────

    /// <summary>Receives the Move action value.</summary>
    public void OnMove(InputValue value)
    {
        HorizontalInput = value.Get<Vector2>().x;
        _animator?.SetBool(AnimRunning, HorizontalInput != 0f);
        
        UpdateBackwardsParameter();
        
        Debug.Log($"[SpiderGirl] OnMove — horizontalInput={HorizontalInput:F2}");
    }

    /// <summary>Updates the 'backwards' animator parameter based on movement relative to the enemy.</summary>
    private void UpdateBackwardsParameter()
    {
        if (_animator == null || enemyTransform == null || HorizontalInput == 0f)
        {
            _animator?.SetBool(AnimBackwards, false);
            return;
        }

        // Determine direction to enemy (positive if enemy is to the right)
        float directionToEnemy = enemyTransform.position.x - transform.position.x;
        
        // "Backwards" is true if moving left while enemy is right, or moving right while enemy is left.
        bool isMovingAway = (HorizontalInput > 0f && directionToEnemy < 0f) || 
                            (HorizontalInput < 0f && directionToEnemy > 0f);
        
        _animator.SetBool(AnimBackwards, isMovingAway);
    }

    /// <summary>Receives the Jump action.</summary>
    public void OnJump(InputValue value)
    {
        if (!value.isPressed) return;
        _jumpRequested = true;
        Debug.Log("[SpiderGirl] OnJump — jump request buffered");
    }

    // ── Private Logic ────────────────────────────────────────────────────────

    private void DetectSurfaces()
    {
        bool wasOnGround  = _isOnGround;
        bool wasOnCeiling = _isOnCeiling;

        _isOnGround  = Physics2D.OverlapCircle(groundCheck.position,  checkRadius, groundLayer);
        _isOnCeiling = Physics2D.OverlapCircle(ceilingCheck.position, checkRadius, ceilingLayer);
        _isAirborne  = !_isOnGround && !_isOnCeiling;

        // CurrentSurface tracks the last surface touched, for jump direction.
        if (_isOnCeiling)
            CurrentSurface = SurfaceType.Ceiling;
        else if (_isOnGround)
            CurrentSurface = SurfaceType.Floor;

        if (_isOnGround != wasOnGround)
        {
            Debug.Log($"[SpiderGirl] DetectSurfaces — onGround changed to {_isOnGround}");
            if (_isOnGround)
                _animator?.SetTrigger(AnimLanded);
        }

        if (_isOnCeiling != wasOnCeiling)
        {
            Debug.Log($"[SpiderGirl] DetectSurfaces — onCeiling changed to {_isOnCeiling}");
            _spriteFlip?.Flip();
            _animator?.SetBool(AnimCeiling, _isOnCeiling);
            if (_isOnCeiling)
                _animator?.SetTrigger(AnimLanded);
        }
    }

    private void ApplyGravityMode()
    {
        if (_isOnCeiling && !_jumpInProgress)
        {
            // Stick to ceiling — disable gravity and zero vertical velocity.
            if (_rb.gravityScale != 0f)
            {
                _rb.gravityScale = 0f;
                Debug.Log("[SpiderGirl] ApplyGravityMode — ceiling contact, gravity disabled");
            }

            Vector2 velocity = _rb.linearVelocity;
            velocity.y = 0f;
            _rb.linearVelocity = velocity;
        }
        else
        {
            // On ground, airborne, or mid-jump — gravity always active.
            if (_rb.gravityScale != gravityScale)
            {
                _rb.gravityScale = gravityScale;
                Debug.Log($"[SpiderGirl] ApplyGravityMode — gravity restored. onGround={_isOnGround}, airborne={_isAirborne}, gravityScale={gravityScale}");
            }
        }
    }

    private void ApplyMovement()
    {
        if (_isOnCeiling && !_jumpInProgress)
        {
            // Full horizontal control on ceiling; vertical is locked by ApplyGravityMode.
            _rb.linearVelocity = new Vector2(HorizontalInput * moveSpeed, 0f);
        }
        else
        {
            // Preserve vertical velocity — gravity and physics handle it.
            _rb.linearVelocity = new Vector2(HorizontalInput * moveSpeed, _rb.linearVelocity.y);
        }

        Debug.Log($"[SpiderGirl] ApplyMovement — velocity={_rb.linearVelocity}, onGround={_isOnGround}, onCeiling={_isOnCeiling}, airborne={_isAirborne}");
    }

    private void TryJump()
    {
        if (!_isOnGround && !_isOnCeiling)
        {
            Debug.Log("[SpiderGirl] TryJump — blocked, not on any surface");
            ApplyMovement();
            return;
        }

        // Jump direction is always away from the current surface.
        float direction = _isOnGround ? 1f : -1f;

        // Trigger animation first, then defer the physics impulse to let it lead.
        _animator?.ResetTrigger(AnimLanded);
        _animator?.SetTrigger(AnimJump);
        _jumpInProgress = true;
        StartCoroutine(ApplyJumpDelayed(direction));
        Debug.Log($"[SpiderGirl] TryJump — jump animation triggered from {CurrentSurface}; impulse deferred by {JumpDelay}s");
    }

    private System.Collections.IEnumerator ApplyJumpDelayed(float direction)
    {
        yield return new WaitForSeconds(JumpDelay);
        ApplyJumpImpulse(direction);
    }

    /// <summary>Applies the actual physics velocity for a jump.</summary>
    private void ApplyJumpImpulse(float direction)
    {
        _jumpInProgress = false;

        // Ensure gravity is on so the arc resolves correctly.
        _rb.gravityScale   = gravityScale;
        _rb.linearVelocity = new Vector2(HorizontalInput * moveSpeed, direction * jumpForce);

        Debug.Log($"[SpiderGirl] ApplyJumpImpulse — launched from {CurrentSurface}. direction={direction}, velocity={_rb.linearVelocity}");
    }

    // ── Gizmos ───────────────────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, checkRadius);
        }

        if (ceilingCheck != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(ceilingCheck.position, checkRadius);
        }
    }
}
