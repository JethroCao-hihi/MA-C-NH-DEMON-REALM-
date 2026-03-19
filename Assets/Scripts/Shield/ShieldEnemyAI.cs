using UnityEngine;
using System.Collections;

public class ShieldEnemyAI : Enemy 
{
    [Header("Khoảng cách hoạt động")]
    public float detectionRange = 5f; // Khoảng cách bắt đầu đuổi
    public float attackRange = 1.5f;  // Khoảng cách bắt đầu tấn công
    public float patrolRange = 2f;    // Khoảng cách đi tuần qua lại khi rảnh

    [Header("Tốc độ tuần tra")]
    public float patrolSpeed = 1.5f;

    [Header("Trạng thái nội bộ")]
    private bool isAttacking = false;
    private Vector2 startPosition;
    private int patrolDirection = 1; // 1 là phải, -1 là trái

    public override void Start()
    {
        base.Start(); // Lấy 'anim' và 'player' từ lớp cha
        startPosition = transform.position; // Lưu vị trí ban đầu để tuần tra
    }

    void Update()
    {
        if (health <= 0 || isAttacking) return;

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance <= attackRange)
        {
            PerformAttack();
        }
        else if (distance <= detectionRange)
        {
            MoveTowardsPlayer();
        }
        else
        {
            Patrol(); 
        }
    }

    void Patrol()
    {
        anim.SetFloat("Speed", 0.5f);

        float leftLimit = startPosition.x - patrolRange;
        float rightLimit = startPosition.x + patrolRange;

        transform.Translate(Vector2.right * patrolDirection * patrolSpeed * Time.deltaTime);

        if (transform.position.x >= rightLimit) {
            patrolDirection = -1;
            Flip(false);
        }
        else if (transform.position.x <= leftLimit) {
            patrolDirection = 1;
            Flip(true);
        }
    }

    void MoveTowardsPlayer()
    {
        transform.position = Vector2.MoveTowards(transform.position, player.position, speed * Time.deltaTime);
        anim.SetFloat("Speed", 1f);
        
        // Quay mặt theo hướng Player khi đang đuổi
        Flip(player.position.x > transform.position.x);
    }

    void PerformAttack()
    {
        anim.SetFloat("Speed", 0f); // Dừng lại để đánh
        
        // --- CHỖ SỬA QUAN TRỌNG ---
        // Ép con quái phải Flip về phía Player một lần nữa trước khi đánh
        Flip(player.position.x > transform.position.x);
        // ---------------------------

        StartCoroutine(AttackSequence());
    }

    IEnumerator AttackSequence()
    {
        isAttacking = true;
        anim.SetTrigger("AttackTrigger"); 
        
        yield return new WaitForSeconds(1.5f); // Thời gian chờ của animation
        
        isAttacking = false;
    }

    void Flip(bool faceRight)
    {
        // Nếu Player ở bên phải (faceRight = true), scale.x phải dương.
        // Nếu nó vẫn quay lưng, bạn hãy đổi dấu '-' ở 2 dòng dưới cho nhau.
        if (faceRight) 
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, 1);
        else 
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, 1);
    }
}