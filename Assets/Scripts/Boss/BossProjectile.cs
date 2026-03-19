using UnityEngine;

public class BossProjectile : MonoBehaviour
{
    public float speed = 8f;
    public float damage = 15f;
    private Vector2 direction;

    public void SetDirection(Vector2 dir)
    {
        direction = dir;
    }

    void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Gọi hàm trừ máu Player ở đây (ví dụ: other.GetComponent<Player>().TakeDamage(damage))
            Debug.Log("Player dính đạn của Boss!");
            Destroy(gameObject);
        }
        
        // Tự hủy nếu chạm tường
        if(other.gameObject.layer == LayerMask.NameToLayer("Ground")) Destroy(gameObject);
    }
}