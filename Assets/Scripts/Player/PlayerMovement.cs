using UnityEngine;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    [Header("Di chuyển & Nhảy")]
    public float moveSpeed = 7f;
    public float jumpForce = 14f;

    [Header("Lướt (Dash)")]
    public float dashSpeed = 20f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;
    private bool canDash = true;
    private bool isDashing = false;

    [Header("Trượt tường (Wall Slide)")]
    public Transform wallCheckLeft;  // Kéo cục WallCheckLeft vào đây
    public Transform wallCheckRight; // Kéo cục WallCheckRight vào đây
    public float wallCheckRadius = 0.3f; // Tăng lên để phát hiện tường dễ hơn
    public float wallSlideSpeed = 1f; // Tốc độ trượt xuống
    public float wallSnapDistance = 1f; // Khoảng cách tối đa để snap vào tường (tăng lên)
    [Tooltip("Khoảng cách từ nhân vật đến tường khi bám (0 = sát tường hoàn toàn)")]
    [Range(0f, 0.5f)]
    public float wallOffset = 0.02f; // Khoảng cách tùy chỉnh từ nhân vật đến tường
    private bool isWallSliding;
    private bool isHoldingWall = false; // Đang giữ bám tường
    private int wallDirection = 0; // -1 = tường bên trái, 1 = tường bên phải, 0 = không chạm tường

    [Header("Kiểm tra va chạm")]
    public Transform groundCheck;
    public float checkRadius = 0.2f;
    public LayerMask groundLayer;
    public LayerMask wallLayer;

    [Header("Trạng thái chiến đấu")]
    private bool isAttacking = false;
    private bool isBlocking = false;

    // Properties để các script khác có thể đọc trạng thái
    public bool IsAttacking => isAttacking;
    public bool IsBlocking => isBlocking;
    public bool IsDashing => isDashing;
    public bool IsGrounded => isGrounded;

    [Header("Tham chiếu")]
    public Animator anim;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Collider2D playerCollider; // Thêm reference đến collider
    private float horizontalInput;
    private bool isFacingRight = true;
    private bool isGrounded;
    private bool isHangingOnWall = false;
    private float originalGravity;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerCollider = GetComponent<Collider2D>(); // Lấy collider của nhân vật
        originalGravity = rb.gravityScale;
    }

    void Update()
    {
        // Nếu đang lướt, đang chém, hoặc đã chết thì không cho phép làm gì khác
        if (isDashing || isAttacking) return;

        horizontalInput = Input.GetAxisRaw("Horizontal");

        // 1. Nhảy
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
        
        // Nhảy tường (Wall Jump) - khi đang bám tường và nhấn Jump
        if (Input.GetButtonDown("Jump") && isWallSliding)
        {
            // Nhảy ra khỏi tường theo hướng ngược lại
            float wallJumpDirection = -wallDirection;
            rb.linearVelocity = new Vector2(wallJumpDirection * moveSpeed, jumpForce * 0.8f);
            isWallSliding = false;
            isHoldingWall = false;
            rb.gravityScale = originalGravity;
        }

        // 2. Lướt (Phím K hoặc Shift)
        if ((Input.GetKeyDown(KeyCode.K) || Input.GetKeyDown(KeyCode.LeftShift)) && canDash)
        {
            StartCoroutine(Dash());
        }

        // 3. Đỡ đòn (Giữ phím L hoặc Chuột phải) - GIỮ NÚT để block liên tục
        bool isHoldingBlock = Input.GetKey(KeyCode.L) || Input.GetMouseButton(1);
        
        // Cập nhật trạng thái block dựa trên việc giữ nút
        isBlocking = isHoldingBlock;
        
        // Dừng di chuyển khi đang block
        if (isBlocking)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        CheckWallSlide();
        Flip();
        UpdateAnimations();
    }

    void FixedUpdate()
    {
        if (isDashing || isAttacking || isBlocking) return;

        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);

        // Di chuyển bình thường
        if (!isWallSliding)
        {
            rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);
        }
    }

    // --- PHƯƠNG THỨC PUBLIC CHO CÁC SCRIPT KHÁC ---
    
    /// <summary>
    /// Đặt trạng thái tấn công (được gọi từ PlayerAttack)
    /// </summary>
    public void SetAttacking(bool value)
    {
        isAttacking = value;
    }

    /// <summary>
    /// Đặt trạng thái block (nếu cần điều khiển từ script khác)
    /// </summary>
    public void SetBlocking(bool value)
    {
        isBlocking = value;
    }

    // --- CÁC HÀM XỬ LÝ KỸ NĂNG ---

    private IEnumerator Dash()
    {
        canDash = false;
        isDashing = true;
        anim.SetTrigger("Dash");

        // Lướt đi với vận tốc cao (loại bỏ trọng lực tạm thời)
        float dashGravity = rb.gravityScale;
        rb.gravityScale = 0f;
        rb.linearVelocity = new Vector2(transform.localScale.x * dashSpeed, 0f);

        yield return new WaitForSeconds(dashDuration);

        // Hết thời gian lướt, trả lại trạng thái cũ
        rb.gravityScale = dashGravity;
        isDashing = false;

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    private void CheckWallSlide()
    {
        // 1. Quét vòng tròn tại vị trí của 2 cục Empty GameObject
        // Sử dụng world position offset thay vì child transform để tránh bị ảnh hưởng bởi localScale
        Vector2 rightCheckPos = (Vector2)transform.position + new Vector2(Mathf.Abs(wallCheckRight.localPosition.x), wallCheckRight.localPosition.y);
        Vector2 leftCheckPos = (Vector2)transform.position + new Vector2(-Mathf.Abs(wallCheckLeft.localPosition.x), wallCheckLeft.localPosition.y);
        
        bool isTouchingRight = Physics2D.OverlapCircle(rightCheckPos, wallCheckRadius, wallLayer);
        bool isTouchingLeft = Physics2D.OverlapCircle(leftCheckPos, wallCheckRadius, wallLayer);

        // Xác định hướng tường (dùng giá trị cố định, không phụ thuộc vào localScale)
        if (isTouchingRight) wallDirection = 1;
        else if (isTouchingLeft) wallDirection = -1;
        else wallDirection = 0;

        // Kiểm tra xem người chơi có đang giữ phím hướng vào tường không
        bool isPushingToWall = (isTouchingRight && horizontalInput > 0) || (isTouchingLeft && horizontalInput < 0);

        // 2. Xử lý bám tường - chỉ khi không chạm đất và đang chạm tường
        if (!isGrounded && (isTouchingRight || isTouchingLeft))
        {
            isWallSliding = true;
            
            // SNAP VÀO TƯỜNG - Sử dụng Raycast để đẩy nhân vật sát tường
            SnapToWall();
            
            // Nếu đang giữ phím hướng vào tường -> BÁM CHẶT (không rơi)
            if (isPushingToWall)
            {
                isHoldingWall = true;
                rb.gravityScale = 0f; // Tắt trọng lực
                rb.linearVelocity = Vector2.zero; // Đứng yên hoàn toàn
            }
            else
            {
                // Không giữ phím -> TRƯỢT XUỐNG CHẬM
                isHoldingWall = false;
                rb.gravityScale = 0f; // Tắt trọng lực để kiểm soát tốc độ rơi thủ công
                
                // Chỉ áp dụng vận tốc rơi xuống với tốc độ cố định
                rb.linearVelocity = new Vector2(0f, -wallSlideSpeed);
            }

            // 3. Logic lật mặt (Lưng dựa tường) - Sử dụng wallDirection thay vì so sánh position
            if (wallDirection > 0)
            {
                // Tường ở bên PHẢI -> Xoay mặt sang TRÁI (quay lưng vào tường)
                transform.localScale = new Vector3(-1, 1, 1);
                isFacingRight = false;
            }
            else if (wallDirection < 0)
            {
                // Tường ở bên TRÁI -> Xoay mặt sang PHẢI (quay lưng vào tường)
                transform.localScale = new Vector3(1, 1, 1);
                isFacingRight = true;
            }
        }
        else
        {
            // Không chạm tường hoặc đang chạm đất
            if (isWallSliding)
            {
                rb.gravityScale = originalGravity; // Khôi phục trọng lực
            }
            isWallSliding = false;
            isHoldingWall = false;
        }
    }

    // Hàm snap nhân vật sát vào tường - ĐÃ CẢI THIỆN
    private void SnapToWall()
    {
        if (playerCollider == null) return;
        
        // Lấy thông tin collider của nhân vật
        Bounds bounds = playerCollider.bounds;
        
        // Raycast từ TÂM của collider (không phải cạnh) để chính xác hơn
        Vector2 rayOrigin = bounds.center;
        Vector2 rayDirection = wallDirection > 0 ? Vector2.right : Vector2.left;
        
        // Raycast để tìm tường
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, rayDirection, wallSnapDistance, wallLayer);
        
        // Debug để xem raycast có hoạt động không
        Debug.DrawRay(rayOrigin, rayDirection * wallSnapDistance, hit.collider != null ? Color.green : Color.red);
        
        if (hit.collider != null)
        {
            // Tính khoảng cách từ cạnh collider đến tường
            float halfWidth = bounds.extents.x;
            float targetX;
            
            if (wallDirection > 0)
            {
                // Tường bên PHẢI
                // Vị trí X mới = điểm hit - nửa chiều rộng collider - offset
                targetX = hit.point.x - halfWidth - wallOffset;
            }
            else
            {
                // Tường bên TRÁI
                // Vị trí X mới = điểm hit + nửa chiều rộng collider + offset
                targetX = hit.point.x + halfWidth + wallOffset;
            }
            
            // Di chuyển nhân vật đến vị trí mới
            transform.position = new Vector3(targetX, transform.position.y, transform.position.z);
        }
    }

    // Hàm gọi khi nhận sát thương (Các script khác như quái vật sẽ gọi hàm này)
    public void TakeDamage(int damage)
    {
        if (isBlocking) return; // Đỡ đòn thành công
        if (isDashing) return;  // i-Frame khi đang lướt

        anim.SetTrigger("Hurt");
        // Trừ máu ở đây...
    }

    // Hàm gọi khi hết máu
    public void Die()
    {
        anim.SetTrigger("Die");
        this.enabled = false; // Tắt luôn script để không điều khiển được nữa
    }

    // --- CÁC HÀM HỖ TRỢ ---
    private void Flip()
    {
        if (isWallSliding) return; // Không lật khi đang trượt tường

        if (isFacingRight && horizontalInput < 0f || !isFacingRight && horizontalInput > 0f)
        {
            isFacingRight = !isFacingRight;
            Vector3 localScale = transform.localScale;
            localScale.x *= -1f;
            transform.localScale = localScale;
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        // Vẽ Ground Check (Màu đỏ)
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, checkRadius);
        }

        // Vẽ Wall Check (Màu xanh dương) - Vẽ ở vị trí cố định không phụ thuộc localScale
        Gizmos.color = Color.blue;
        if (wallCheckRight != null)
        {
            Vector2 rightCheckPos = (Vector2)transform.position + new Vector2(Mathf.Abs(wallCheckRight.localPosition.x), wallCheckRight.localPosition.y);
            Gizmos.DrawWireSphere(rightCheckPos, wallCheckRadius);
        }
        if (wallCheckLeft != null)
        {
            Vector2 leftCheckPos = (Vector2)transform.position + new Vector2(-Mathf.Abs(wallCheckLeft.localPosition.x), wallCheckLeft.localPosition.y);
            Gizmos.DrawWireSphere(leftCheckPos, wallCheckRadius);
        }
        
        // Vẽ raycast snap wall (Màu vàng)
        if (playerCollider != null)
        {
            Gizmos.color = Color.yellow;
            Bounds bounds = playerCollider.bounds;
            // Vẽ ray sang phải
            Gizmos.DrawLine(bounds.center, bounds.center + Vector3.right * wallSnapDistance);
            // Vẽ ray sang trái
            Gizmos.DrawLine(bounds.center, bounds.center + Vector3.left * wallSnapDistance);
        }
    }


    private void UpdateAnimations()
    {
        anim.SetFloat("Speed", Mathf.Abs(horizontalInput));
        anim.SetBool("isGrounded", isGrounded);
        anim.SetFloat("yVelocity", rb.linearVelocity.y);
        anim.SetBool("isWallSliding", isWallSliding);
        anim.SetBool("isBlocking", isBlocking); // THÊM: Cập nhật animation block (giữ nút = giữ animation)
    }
}
