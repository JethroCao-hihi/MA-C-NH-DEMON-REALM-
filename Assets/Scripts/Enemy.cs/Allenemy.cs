using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Chỉ số cơ bản")]
    public float maxHealth = 100f;
    [HideInInspector] public float health; // Máu hiện tại (ẩn trong Inspector để tránh nhầm)
    public float speed = 2f;

    [Header("References")]
    protected Transform player;
    protected Animator anim;

    // Hàm ảo Start
    public virtual void Start()
    {
        health = maxHealth; // Đặt máu hiện tại bằng máu tối đa khi bắt đầu
        anim = GetComponent<Animator>();

        // Cách tìm Player an toàn hơn
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogError("Cảnh báo: Không tìm thấy đối tượng nào có Tag là 'Player' trong Scene!");
        }
    }

    // Hàm nhận sát thương
    public virtual void TakeDamage(float damage)
    {
        if (health <= 0) return; // Nếu chết rồi thì không nhận sát thương nữa

        health -= damage;
        Debug.Log(gameObject.name + " trúng đòn! Máu còn: " + health);

        // Kích hoạt hiệu ứng bị đau (Hurt) nếu có animator
        if (anim != null)
        {
            anim.SetTrigger("HurtTrigger");
        }

        if (health <= 0)
        {
            Die();
        }
    }

    // Hàm xử lý khi chết
    public virtual void Die()
    {
        Debug.Log(gameObject.name + " đã bị tiêu diệt!");
        
        // Nếu có animator, chạy animation chết thay vì biến mất ngay
        if (anim != null)
        {
            anim.SetTrigger("DieTrigger");
        }

        // Tắt va chạm để không cản đường Player sau khi chết
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // Xóa vật thể sau 1-2 giây để kịp diễn hoạt ảnh chết
        Destroy(gameObject, 1.5f); 
    }
}