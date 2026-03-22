using UnityEngine;
using UnityEngine;

public class BossBullet : MonoBehaviour
{
    public float speed = 5f;
    public float lifeTime = 5f;
    public int damage = 10;

    private Vector2 direction = Vector2.right;

    public void SetDirection(Vector2 dir)
    {
        // Fallback to right if an invalid direction is provided
        direction = dir.sqrMagnitude > 0f ? dir.normalized : Vector2.right;
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        // Use the assigned direction (world space)
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!enabled) return;

        if (collision.CompareTag("Player"))
        {
            PlayerHealth playerHealth = collision.GetComponent<PlayerHealth>();
            if (playerHealth != null)
                playerHealth.TakeDamage(damage);
        }

        Destroy(gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!enabled) return;

        if (collision.collider.CompareTag("Player"))
        {
            PlayerHealth playerHealth = collision.collider.GetComponent<PlayerHealth>();
            if (playerHealth != null)
                playerHealth.TakeDamage(damage);
        }

        Destroy(gameObject);
    }
}
