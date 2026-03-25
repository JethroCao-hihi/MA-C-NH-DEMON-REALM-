using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    #region === VARIABLES ===

    [Header("Movement & Jump")]
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float jumpForce = 14f;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;

    [Header("Dash - Xuyên qua")]
    [Tooltip("Những layer sẽ bị bỏ collision với Player trong lúc dash (ví dụ: Enemy, Bullet)")]
    [SerializeField] private LayerMask dashIgnoreLayers;

    [Header("Knockback")]
    [SerializeField] private float knockbackLockTime = 0.12f;

    [Header("Block / Parry Input")]
    [Tooltip("Nhấn nhanh rồi thả < ngưỡng này => Parry. Giữ lâu hơn => Block")]
    [SerializeField] private float blockHoldThreshold = 0.18f;

    [Header("Wall Slide")]
    [SerializeField] private Transform wallCheckLeft;
    [SerializeField] private Transform wallCheckRight;
    [SerializeField] private float wallCheckRadius = 0.3f;
    [SerializeField] private float wallSlideSpeed = 1f;
    [SerializeField] private float wallSnapDistance = 1f;
    [SerializeField, Range(0f, 0.5f)] private float wallOffset = 0.02f;

    [Header("Wall Jump")]
    [SerializeField] private float wallJumpHorizontalForce = 12f;
    [SerializeField] private float wallJumpVerticalForce = 14f;
    [SerializeField] private float wallJumpCooldown = 0.15f;
    [SerializeField] private float wallJumpInputLockTime = 0.1f;

    [Header("Collision Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float checkRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask wallLayer;

    // Cached Components
    private Rigidbody2D rb;
    public Animator anim;
    private Collider2D playerCollider;
    private PlayerHealth health;
    private PlayerAttack attack;

    // State
    private float horizontalInput;
    private float originalGravity;

    private bool canDash = true;
    private bool canWallSlide = true;

    private bool isDashing;
    private bool isAttacking;
    private bool isBlocking;

    private bool isGrounded;
    private bool isWallSliding;
    private bool isWallJumping;

    private float wallJumpInputLockTimer;
    private int wallDirection; // -1 left, 1 right, 0 none

    private static readonly Vector3 ScaleRight = new Vector3(1, 1, 1);
    private static readonly Vector3 ScaleLeft = new Vector3(-1, 1, 1);
    private bool isFacingRight = true;

    private float knockbackLockTimer;

    // Block/Parry input state
    private bool blockKeyHeld;
    private float blockKeyDownTime;
    private bool blockHoldActivated;

    // WaitForSeconds cache
    private WaitForSeconds dashDurationWait;
    private WaitForSeconds dashCooldownWait;
    private WaitForSeconds wallJumpCooldownWait;

    #endregion

    #region === PROPERTIES ===

    public bool IsAttacking => isAttacking;
    public bool IsBlocking => isBlocking;
    public bool IsDashing => isDashing;
    public bool IsGrounded => isGrounded;
    public bool IsWallSliding => isWallSliding;
    public bool IsDead => health != null && health.IsDead;

    #endregion

    #region === UNITY CALLBACKS ===

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        playerCollider = GetComponent<Collider2D>();
        health = GetComponent<PlayerHealth>();
        attack = GetComponent<PlayerAttack>();

        if (rb != null)
            originalGravity = rb.gravityScale;

        dashDurationWait = new WaitForSeconds(dashDuration);
        dashCooldownWait = new WaitForSeconds(dashCooldown);
        wallJumpCooldownWait = new WaitForSeconds(wallJumpCooldown);
    }

    private void Update()
    {
        if (IsDead) return;

        if (knockbackLockTimer > 0f)
            knockbackLockTimer -= Time.deltaTime;

        UpdateBlockParryInput();

        // Chặn input khác khi dash/attack/block
        if (isDashing || isAttacking)
        {
            UpdateAnimations();
            return;
        }

        if (isBlocking)
        {
            if (rb != null)
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

            UpdateAnimations();
            return;
        }

        UpdateWallJumpTimer();
        HandleInput();
        CheckWallSlide();
        Flip();
        UpdateAnimations();
    }

    private void FixedUpdate()
    {
        if (IsDead || isDashing || isAttacking || isBlocking) return;
        if (rb == null) return;

        // Ground check
        if (groundCheck != null)
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);

        if (isGrounded) canWallSlide = true;

        if (knockbackLockTimer > 0f)
            return;

        // Movement
        if (!isWallSliding && !isWallJumping)
            rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);
    }

    #endregion

    #region === BLOCK / PARRY INPUT ===

    private void UpdateBlockParryInput()
    {
        bool down = Input.GetKeyDown(KeyCode.L) || Input.GetMouseButtonDown(1);
        bool held = Input.GetKey(KeyCode.L) || Input.GetMouseButton(1);
        bool up = Input.GetKeyUp(KeyCode.L) || Input.GetMouseButtonUp(1);

        if (down)
        {
            blockKeyHeld = true;
            blockKeyDownTime = Time.time;
            blockHoldActivated = false;
            // Chưa set block ngay, đợi vượt threshold
        }

        if (blockKeyHeld && held && !blockHoldActivated)
        {
            if (Time.time - blockKeyDownTime >= blockHoldThreshold)
            {
                blockHoldActivated = true;
                isBlocking = true;
            }
        }

        // Chỉ xử lý "up" nếu đã từng nhận "down" trước đó
        if (up && blockKeyHeld)
        {
            float heldTime = Time.time - blockKeyDownTime;

            // Nếu chưa kích hoạt hold block và thả nhanh => parry
            if (!blockHoldActivated && heldTime < blockHoldThreshold)
            {
                if (attack != null)
                    attack.RequestParry();
            }

            // Thả ra luôn tắt block
            isBlocking = false;
            blockKeyHeld = false;
            blockHoldActivated = false;
            blockKeyDownTime = 0f;
        }

        // Nếu đang block mà không còn giữ phím (trường hợp mất focus) thì tắt
        if (isBlocking && !held)
        {
            isBlocking = false;
            blockKeyHeld = false;
            blockHoldActivated = false;
        }
    }

    #endregion

    #region === PUBLIC API (for other scripts) ===

    public void SetAttacking(bool value) => isAttacking = value;

    // Giữ lại để script khác dùng, nhưng hiện isBlocking được điều khiển bởi input tap/hold
    public void SetBlocking(bool value) => isBlocking = value;

    public void ApplyKnockback(Vector2 velocity)
    {
        if (rb == null) return;
        if (IsDead) return;
        if (isDashing) return;

        rb.linearVelocity = velocity;
        knockbackLockTimer = knockbackLockTime;
    }

    public void Respawn()
    {
        isAttacking = false;
        isBlocking = false;
        isDashing = false;
        isWallSliding = false;
        isWallJumping = false;

        canDash = true;
        canWallSlide = true;
        horizontalInput = 0f;
        knockbackLockTimer = 0f;

        blockKeyHeld = false;
        blockHoldActivated = false;

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = originalGravity;
            rb.linearVelocity = Vector2.zero;
        }

        if (playerCollider != null)
            playerCollider.enabled = true;

        EnableDashGhostCollision(false);

        if (anim != null)
        {
            anim.ResetTrigger("Die");
            anim.ResetTrigger("Hurt");
            anim.SetFloat("Speed", 0f);
            anim.SetBool("isGrounded", true);
            anim.SetFloat("yVelocity", 0f);
            anim.SetBool("isWallSliding", false);
            anim.SetBool("isBlocking", false);
        }

        isFacingRight = true;
        transform.localScale = ScaleRight;
    }

    private void OnDisable()
    {
        EnableDashGhostCollision(false);
    }

    private void OnPlayerDied()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        if (playerCollider != null)
            playerCollider.enabled = false;

        EnableDashGhostCollision(false);
    }

    #endregion

    #region === INPUT HANDLING ===

    private void UpdateWallJumpTimer()
    {
        if (wallJumpInputLockTimer > 0f)
            wallJumpInputLockTimer -= Time.deltaTime;
        else
            isWallJumping = false;
    }

    private void HandleInput()
    {
        if (!isWallJumping)
            horizontalInput = Input.GetAxisRaw("Horizontal");

        if (rb == null) return;

        // Jump
        if (Input.GetButtonDown("Jump"))
        {
            if (isGrounded)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                if (anim != null) anim.SetTrigger("Jump");
                if (SoundManager.Instance != null)
                    SoundManager.Instance.PlayPlayerSfx("Jump");
            }
            else if (isWallSliding && canWallSlide)
            {
                PerformWallJump();
            }
        }

        // Dash
        if ((Input.GetKeyDown(KeyCode.K) || Input.GetKeyDown(KeyCode.LeftShift)) && canDash && !isDashing)
            StartCoroutine(DashCoroutine());
    }

    #endregion

    #region === WALL MECHANICS ===

    private void PerformWallJump()
    {
        if (rb == null) return;

        float jumpDirection = -wallDirection;

        isWallSliding = false;
        rb.gravityScale = originalGravity;

        isWallJumping = true;
        wallJumpInputLockTimer = wallJumpInputLockTime;

        rb.linearVelocity = new Vector2(jumpDirection * wallJumpHorizontalForce, wallJumpVerticalForce);

        transform.localScale = jumpDirection > 0 ? ScaleRight : ScaleLeft;
        isFacingRight = jumpDirection > 0;

        if (anim != null) anim.SetTrigger("Jump");
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayPlayerSfx("Jump");

        StartCoroutine(WallJumpCooldownCoroutine());
    }

    private IEnumerator WallJumpCooldownCoroutine()
    {
        canWallSlide = false;
        yield return wallJumpCooldownWait;
        canWallSlide = true;
    }

    private void CheckWallSlide()
    {
        if (!canWallSlide || isWallJumping) return;
        if (rb == null) return;
        if (wallCheckLeft == null || wallCheckRight == null) return;

        Vector2 rightCheckPos = (Vector2)transform.position + new Vector2(Mathf.Abs(wallCheckRight.localPosition.x), wallCheckRight.localPosition.y);
        Vector2 leftCheckPos = (Vector2)transform.position + new Vector2(-Mathf.Abs(wallCheckLeft.localPosition.x), wallCheckLeft.localPosition.y);

        bool isTouchingRight = Physics2D.OverlapCircle(rightCheckPos, wallCheckRadius, wallLayer);
        bool isTouchingLeft = Physics2D.OverlapCircle(leftCheckPos, wallCheckRadius, wallLayer);

        if (isTouchingRight) wallDirection = 1;
        else if (isTouchingLeft) wallDirection = -1;
        else wallDirection = 0;

        bool isPushingToWall = (isTouchingRight && horizontalInput > 0) || (isTouchingLeft && horizontalInput < 0);

        if (!isGrounded && (isTouchingRight || isTouchingLeft))
        {
            isWallSliding = true;

            SnapToWall();

            rb.gravityScale = 0f;
            rb.linearVelocity = isPushingToWall ? Vector2.zero : new Vector2(0f, -wallSlideSpeed);

            transform.localScale = wallDirection > 0 ? ScaleLeft : ScaleRight;
            isFacingRight = wallDirection < 0;
        }
        else
        {
            if (isWallSliding)
                rb.gravityScale = originalGravity;

            isWallSliding = false;
        }
    }

    private void SnapToWall()
    {
        if (playerCollider == null) return;
        if (wallDirection == 0) return;

        Bounds bounds = playerCollider.bounds;
        Vector2 rayOrigin = bounds.center;
        Vector2 rayDirection = wallDirection > 0 ? Vector2.right : Vector2.left;

        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, rayDirection, wallSnapDistance, wallLayer);

        if (hit.collider == null) return;

        float halfWidth = bounds.extents.x;
        float targetX = wallDirection > 0
            ? hit.point.x - halfWidth - wallOffset
            : hit.point.x + halfWidth + wallOffset;

        transform.position = new Vector3(targetX, transform.position.y, transform.position.z);
    }

    #endregion

    #region === DASH ===

    private IEnumerator DashCoroutine()
    {
        if (rb == null) yield break;

        canDash = false;
        isDashing = true;
        EnableDashGhostCollision(true);

        if (anim != null) anim.SetTrigger("Dash");
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayPlayerSfx("Dash");

        float savedGravity = rb.gravityScale;
        rb.gravityScale = 0f;
        rb.linearVelocity = new Vector2(transform.localScale.x * dashSpeed, 0f);

        yield return dashDurationWait;

        rb.gravityScale = savedGravity;
        isDashing = false;
        EnableDashGhostCollision(false);

        yield return dashCooldownWait;
        canDash = true;
    }

    private void EnableDashGhostCollision(bool enable)
    {
        if (dashIgnoreLayers == 0) return;

        int playerLayer = gameObject.layer;

        for (int layer = 0; layer < 32; layer++)
        {
            if ((dashIgnoreLayers.value & (1 << layer)) == 0)
                continue;

            Physics2D.IgnoreLayerCollision(playerLayer, layer, enable);
        }
    }

    #endregion

    #region === HELPERS ===

    private void Flip()
    {
        if (isWallSliding || isWallJumping) return;

        if ((isFacingRight && horizontalInput < 0f) || (!isFacingRight && horizontalInput > 0f))
        {
            isFacingRight = !isFacingRight;
            transform.localScale = isFacingRight ? ScaleRight : ScaleLeft;
        }
    }

    private void UpdateAnimations()
    {
        if (anim == null || rb == null) return;

        anim.SetFloat("Speed", Mathf.Abs(horizontalInput));
        anim.SetBool("isGrounded", isGrounded);
        anim.SetFloat("yVelocity", rb.linearVelocity.y);
        anim.SetBool("isWallSliding", isWallSliding);
        anim.SetBool("isBlocking", isBlocking);
    }

    #endregion

    #region === GIZMOS ===

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, checkRadius);
        }

        Gizmos.color = Color.blue;
        if (wallCheckRight != null)
        {
            Vector2 rightPos = (Vector2)transform.position + new Vector2(Mathf.Abs(wallCheckRight.localPosition.x), wallCheckRight.localPosition.y);
            Gizmos.DrawWireSphere(rightPos, wallCheckRadius);
        }

        if (wallCheckLeft != null)
        {
            Vector2 leftPos = (Vector2)transform.position + new Vector2(-Mathf.Abs(wallCheckLeft.localPosition.x), wallCheckLeft.localPosition.y);
            Gizmos.DrawWireSphere(leftPos, wallCheckRadius);
        }

        if (playerCollider != null)
        {
            Gizmos.color = Color.yellow;
            Bounds bounds = playerCollider.bounds;
            Gizmos.DrawLine(bounds.center, bounds.center + Vector3.right * wallSnapDistance);
            Gizmos.DrawLine(bounds.center, bounds.center + Vector3.left * wallSnapDistance);
        }
    }
#endif

    #endregion
}
