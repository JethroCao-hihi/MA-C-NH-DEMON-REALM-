using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Tham chieu")]
    public Animator anim;
    public PlayerMovement movementScript;
    private Rigidbody2D rb;

    [Header("Cai dat Combo")]
    public float comboResetTime = 1f;
    private int comboStep = 0;
    private float lastAttackTime = 0f;

    [Header("Cai dat sat thuong")]
    public Transform attackPoint;
    public float attackRange = 0.5f;
    public LayerMask enemyLayers;
    public int attackDamage = 20;

    [Header("Thoi gian Animation (Khoa di chuyen)")]
    public float attack1Duration = 0.3f;
    public float attack2Duration = 0.35f;
    public float attack3Duration = 0.4f;

    [Header("Input Buffer Settings")]
    [Tooltip("Thời gian cho phép nhập input trước khi animation hiện tại kết thúc")]
    public float inputBufferWindow = 0.25f;

    // Bộ đếm thời gian mở khóa di chuyển thay cho Coroutine
    private float unlockMovementTime = 0f;

    // Thời điểm animation hiện tại kết thúc
    private float currentAttackEndTime = 0f;

    // Input Buffer - lưu lại input nếu người chơi bấm trong lúc đang đánh
    private bool hasBufferedInput = false;

    // Thời gian tối thiểu giữa các đòn (để animator kịp chuyển state)
    private float minTimeBetweenAttacks = 0.08f;
    private float lastAttackExecuteTime = 0f;

    void Start()
    {
        anim = GetComponent<Animator>();
        movementScript = GetComponent<PlayerMovement>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // 1. Tự động mở khóa di chuyển khi chạy hết thời gian chờ của animation
        if (movementScript.IsAttacking && Time.time >= unlockMovementTime)
        {
            movementScript.SetAttacking(false);

            // Nếu combo đã hết (sau đòn 3 hoặc hết thời gian combo) thì reset
            if (comboStep >= 3 && !hasBufferedInput)
            {
                comboStep = 0;
            }
        }

        // 2. Reset combo nếu ngừng chém quá lâu VÀ không còn đang đánh
        if (Time.time - lastAttackTime > comboResetTime && comboStep > 0 && !movementScript.IsAttacking)
        {
            comboStep = 0;
            hasBufferedInput = false;
        }

        // 3. Nhận input tấn công
        if (Input.GetKeyDown(KeyCode.J) || Input.GetMouseButtonDown(0))
        {
            HandleAttackInput();
        }

        // 4. Xử lý buffered input - thực hiện đòn tiếp theo khi animation gần kết thúc
        if (hasBufferedInput && CanExecuteBufferedAttack())
        {
            ExecuteBufferedAttack();
        }
    }

    private void HandleAttackInput()
    {
        if (movementScript == null || movementScript.IsBlocking)
        {
            return;
        }

        // Nếu KHÔNG đang đánh -> thực hiện đòn ngay lập tức
        if (!movementScript.IsAttacking)
        {
            PerformAttack();
        }
        // Nếu ĐANG đánh và combo chưa hết -> lưu input vào buffer
        else if (comboStep < 3)
        {
            hasBufferedInput = true;
        }
    }

    private bool CanExecuteBufferedAttack()
    {
        // Có thể thực hiện buffered attack khi:
        // 1. Animation hiện tại còn lại ít hơn inputBufferWindow
        // 2. Đã qua thời gian tối thiểu giữa các đòn
        float timeRemaining = currentAttackEndTime - Time.time;
        bool inBufferWindow = timeRemaining <= inputBufferWindow && timeRemaining > 0;
        bool passedMinTime = Time.time - lastAttackExecuteTime >= minTimeBetweenAttacks;

        return inBufferWindow && passedMinTime;
    }

    private void ExecuteBufferedAttack()
    {
        hasBufferedInput = false;
        PerformAttack();
    }

    private void PerformAttack()
    {
        if (movementScript == null)
        {
            return;
        }

        if (movementScript.IsBlocking)
        {
            return;
        }

        lastAttackTime = Time.time;
        lastAttackExecuteTime = Time.time;
        comboStep++;

        // Nếu chém lố 3 nhát thì tự quay về nhát 1
        if (comboStep > 3) comboStep = 1;

        movementScript.SetAttacking(true);

        if (rb != null)
        {
            // Unity 2023+ dùng linearVelocity
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }

        float currentAnimDuration = 0f;

        if (comboStep == 1)
        {
            anim.SetTrigger("Attack1");
            currentAnimDuration = attack1Duration;
        }
        else if (comboStep == 2)
        {
            anim.SetTrigger("Attack2");
            currentAnimDuration = attack2Duration;
        }
        else if (comboStep == 3)
        {
            anim.SetTrigger("Attack3");
            currentAnimDuration = attack3Duration;
        }

        // Đặt mốc thời gian để mở khóa di chuyển dựa trên đòn đánh hiện tại
        unlockMovementTime = Time.time + currentAnimDuration;

        // Lưu thời điểm animation kết thúc để tính input buffer
        currentAttackEndTime = Time.time + currentAnimDuration;
    }

    // --- CÁC HÀM VẼ GIZMOS VÀ DEAL DAMAGE GIỮ NGUYÊN NHƯ CỦA BẠN ---
    public void DealDamage()
    {
        if (attackPoint == null) return;

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
    }

    private void OnDrawGizmos()
    {
        if (attackPoint != null && Application.isPlaying && movementScript != null && movementScript.IsAttacking)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
            Gizmos.DrawSphere(attackPoint.position, attackRange);
        }
    }
}