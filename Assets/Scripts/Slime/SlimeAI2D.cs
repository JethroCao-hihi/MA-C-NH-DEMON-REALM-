using UnityEngine;
using System.Collections;

public class SlimeAI : Enemy 
{
    [Header("Phạm vi")]
    public float detectionRange = 5f;  // Tầm phát hiện Player
    public float attackRange = 1.2f;   // Tầm để bắt đầu tấn công
    public float patrolRange = 3f;    // Khoảng cách đi tuần qua lại

    [Header("Tốc độ tuần tra")]
    public float patrolSpeed = 2f;

    private Vector2 startPosition;
    private int patrolDirection = 1;   // 1: Phải, -1: Trái
    private bool isAttacking = false;

    public override void Start()
    {
        base.Start(); // Lấy Animator và Player từ lớp Cha (Enemy)
        startPosition = transform.position; // Lưu vị trí gốc để tuần tra quanh đó
    }

    void Update()
    {
        if (isAttacking) return; // Nếu đang bận đánh thì không di chuyển

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance <= attackRange)
        {
            // 1. Tấn công nếu sát cạnh Player
            StartCoroutine(AttackRoutine());
        }
        else if (distance <= detectionRange)
        {
            // 2. Đuổi theo nếu thấy Player
            ChasePlayer();
        }
        else
        {
            // 3. Đi tuần nếu Player ở xa
            Patrol();
        }
    }

    void Patrol()
    {
        anim.SetFloat("Speed", 0.5f); // Chuyển sang animation đi bộ

        // Tính giới hạn trái phải
        float leftLimit = startPosition.x - patrolRange;
        float rightLimit = startPosition.x + patrolRange;

        // Di chuyển qua lại
        transform.Translate(Vector2.right * patrolDirection * patrolSpeed * Time.deltaTime);

        // Đổi hướng khi chạm biên
        if (transform.position.x >= rightLimit) {
            patrolDirection = -1;
            Flip(false);
        }
        else if (transform.position.x <= leftLimit) {
            patrolDirection = 1;
            Flip(true);
        }
    }

    void ChasePlayer()
    {
        anim.SetFloat("Speed", 1f); // Chuyển sang animation chạy
        transform.position = Vector2.MoveTowards(transform.position, player.position, speed * Time.deltaTime);
        
        // Quay mặt về phía Player
        Flip(player.position.x > transform.position.x);
    }

    IEnumerator AttackRoutine()
    {
        isAttacking = true;
        anim.SetFloat("Speed", 0f); // Dừng lại để đánh
        anim.SetTrigger("Attack");  // Kích hoạt Trigger tấn công
        
        yield return new WaitForSeconds(1f); // Đợi đánh xong (tùy độ dài animation)
        isAttacking = false;
    }

    void Flip(bool facingRight)
    {
        if (facingRight) transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, 1);
        else transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, 1);
    }
}