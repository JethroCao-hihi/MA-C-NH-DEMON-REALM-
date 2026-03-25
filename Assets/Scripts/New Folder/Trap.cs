using UnityEngine;

public class Trap : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private bool onlyAffectPlayerTag = true;

    [Header("Damage")]
    [SerializeField] private int damageOnTouch = 10;
    [SerializeField] private float damageInterval = 5f;

    private float nextDamageTime;

    private void Reset()
    {
        // G?i ý c?u hình: trap th??ng dùng trigger collider
        if (TryGetComponent<Collider2D>(out var col))
            col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsValidTarget(other)) return;
        ApplyDamage(other.gameObject, immediate: true);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!IsValidTarget(other)) return;
        ApplyDamage(other.gameObject, immediate: false);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsValidTarget(collision.collider)) return;
        ApplyDamage(collision.gameObject, immediate: true);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!IsValidTarget(collision.collider)) return;
        ApplyDamage(collision.gameObject, immediate: false);
    }

    private bool IsValidTarget(Collider2D col)
    {
        if (col == null) return false;
        if (onlyAffectPlayerTag && !col.CompareTag("Player")) return false;
        return true;
    }

    private void ApplyDamage(GameObject target, bool immediate)
    {
        if (target == null) return;
        if (damageOnTouch <= 0) return;

        if (immediate)
        {
            nextDamageTime = Time.time + damageInterval;
            DealDamage(target);
            return;
        }

        if (Time.time < nextDamageTime) return;

        nextDamageTime = Time.time + damageInterval;
        DealDamage(target);
    }

    private void DealDamage(GameObject target)
    {
        // ?u tiên PlayerHealth
        var health = target.GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.TakeRawDamage(damageOnTouch);
            return;
        }

        // Fallback n?u target không dùng PlayerHealth
        target.SendMessage("TakeDamage", damageOnTouch, SendMessageOptions.DontRequireReceiver);
    }
}
