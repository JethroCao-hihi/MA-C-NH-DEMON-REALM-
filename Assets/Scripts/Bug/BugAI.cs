using UnityEngine;
using System.Collections;

public class BugAI : Enemy
{
    [Header("Cài đặt tầm nhìn")]
    public float detectionRange = 6f;
    public float attackRange = 2f;
    public float patrolRange = 3f;

    [Header("Cài đặt di chuyển")]
    public float flyingSpeed = 3f;
    public float groundY = -1f; // CHỈNH CÁI NÀY: Độ cao mặt đất trong game của bạn

    private Vector2 startPosition;
    private int patrolDirection = 1;
    private bool isAttacking = false;
    private Rigidbody2D rb;

    public override void Start()
    {
        base.Start();
        rb = GetComponent<Rigidbody2D>();
        startPosition = rb.position;
    }

    void Update()
    {
        if (player == null || health <= 0 || isAttacking) return;

        float distance = Vector2.Distance(rb.position, player.position);

        if (distance <= attackRange)
        {
            AttackPlayer();
        }
        else if (distance <= detectionRange)
        {
            ChasePlayer();
        }
        else
        {
            Patrol();
        }
    }

    void Patrol()
    {
        anim.SetFloat("speed", 0.5f);
        float leftLimit = startPosition.x - patrolRange;
        float rightLimit = startPosition.x + patrolRange;

        // Đi tuần trên không (giữ nguyên Y của startPosition)
        Vector2 nextPos = rb.position + Vector2.right * patrolDirection * flyingSpeed * Time.deltaTime;
        nextPos.y = startPosition.y; 

        if (nextPos.x >= rightLimit) { patrolDirection = -1; Flip(false); }
        else if (nextPos.x <= leftLimit) { patrolDirection = 1; Flip(true); }

        rb.MovePosition(nextPos);
    }

    void ChasePlayer()
    {
        anim.SetFloat("speed", 1f);
        // Khi đuổi theo, vẫn giữ độ cao bay trên đầu player một chút
        Vector2 targetPos = new Vector2(player.position.x, startPosition.y);
        rb.MovePosition(Vector2.MoveTowards(rb.position, targetPos, flyingSpeed * Time.deltaTime));
        Flip(player.position.x > transform.position.x);
    }

    void AttackPlayer()
    {
        if (!isAttacking) StartCoroutine(AttackRoutine());
    }

    IEnumerator AttackRoutine()
    {
        isAttacking = true;
        anim.SetFloat("speed", 0f);

        // 1. ĐÁP XUỐNG ĐẤT
        Vector2 landPos = new Vector2(rb.position.x, groundY);
        while (Vector2.Distance(rb.position, landPos) > 0.1f)
        {
            rb.MovePosition(Vector2.MoveTowards(rb.position, landPos, flyingSpeed * 2f * Time.deltaTime));
            yield return null;
        }

        // 2. TẤN CÔNG
        anim.SetTrigger("AttackTrig");
        yield return new WaitForSeconds(1f); // Đợi diễn hoạt ảnh đánh

        // 3. BAY TRỞ LAI ĐỘ CAO CŨ
        Vector2 flyBackPos = new Vector2(rb.position.x, startPosition.y);
        while (Vector2.Distance(rb.position, flyBackPos) > 0.1f)
        {
            rb.MovePosition(Vector2.MoveTowards(rb.position, flyBackPos, flyingSpeed * Time.deltaTime));
            yield return null;
        }

        isAttacking = false;
    }

    void Flip(bool faceRight)
    {
        float x = Mathf.Abs(transform.localScale.x);
        transform.localScale = new Vector3(faceRight ? x : -x, transform.localScale.y, transform.localScale.z);
    }
}