using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float health;
    public float speed;
    public Transform player;
    public Animator anim;

    // Hàm ảo để các con quái con có thể viết đè (override) logic riêng của chúng
    public virtual void Start()
    {
        anim = GetComponent<Animator>();
        if (player == null) 
            player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    public virtual void TakeDamage(float damage)
    {
        health -= damage;
        // Chạy hiệu ứng bị thương chung
        if (health <= 0) Die();
    }

    public virtual void Die()
    {
        // Logic chết chung (biến mất, rơi tiền...)
        Destroy(gameObject);
    }
}