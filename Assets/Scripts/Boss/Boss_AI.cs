using UnityEngine;
using System.Collections;

public class BossAI : Enemy 
{
    [Header("Khoảng cách tấn công")]
    public float attackRange = 3f;   // Tầm đánh (ụ súng)
    public float attackCooldown = 2f; // Thời gian nghỉ giữa các đòn

    [Header("Cài đặt hình ảnh")]
    public bool spriteFacingRightByDefault = true;

    private bool isAttacking = false;
    private bool isOnCooldown = false;
    private bool isDead = false;

    // CHỖ SỬA LỖI ÉP KIỂU: previousHealth phải cùng kiểu Float với Enemy.health
    private float previousHealth; 

    public override void Start()
    {
        base.Start(); // Lấy anim, player, health từ Enemy.cs
        previousHealth = health; // Khởi tạo máu ban đầu
        
        // Đảm bảo Boss đứng im bằng cách set tốc độ di chuyển = 0
        speed = 0f; 
    }


    void Update()
    {
        // 1. Logic Chết
        if (health <= 0)
        {
            if (!isDead)
            {
                isDead = true;
                anim.SetTrigger("DieTrigger");
                GetComponent<Collider2D>().enabled = false;
                this.enabled = false; 
            }
            return;
        }

        // 2. Logic Bị Thương (Dùng biến Float đã sửa để so sánh)
        if (health < previousHealth)
        {
            previousHealth = health;
            // Chỉ chạy anim Hurt nếu không bận đánh
            if (!isAttacking) 
            {
                anim.SetTrigger("HurtTrigger");
            }
        }

        // 3. Logic Tấn Công (Dựa vào khoảng cách)
        float distance = Vector2.Distance(transform.position, player.position);

        if (distance <= attackRange && !isAttacking && !isOnCooldown)
        {
            PerformAttack();
        }
        else
        {
            // Boss đứng im, luôn ở trạng thái Idle
            anim.SetFloat("Speed", 0f); 
        }
    }

    void PerformAttack()
    {
        // Luôn quay mặt chuẩn xác trước khi tung chiêu
        Flip(player.position.x > transform.position.x);
        StartCoroutine(AttackRoutine());
    }

    IEnumerator AttackRoutine()
    {
        isAttacking = true;
        anim.SetTrigger("AttackTrigger"); 
        
        // Đợi Boss tung chiêu xong (Ví dụ 1 giây)
        yield return new WaitForSeconds(1f); 
        
        isAttacking = false;
        isOnCooldown = true;

        // Thời gian nghỉ giữa các đòn đánh
        yield return new WaitForSeconds(attackCooldown);
        isOnCooldown = false;
    }

    void Flip(bool faceRight)
    {
        // Logic xoay mặt chuẩn xác
        float direction = faceRight ? 1f : -1f;
        if (!spriteFacingRightByDefault) direction *= -1f;
        
        transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x) * direction, transform.localScale.y, 1);
    }
}