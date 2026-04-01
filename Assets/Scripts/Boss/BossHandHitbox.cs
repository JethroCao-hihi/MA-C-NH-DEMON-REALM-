using UnityEngine;

public class BossHandHitbox : MonoBehaviour
{
    [SerializeField] private BossHealth bossHealth;
    [SerializeField] private BossController bossController;

    private void Awake()
    {
        if (bossHealth == null)
            bossHealth = GetComponentInParent<BossHealth>();

        if (bossController == null)
            bossController = GetComponentInParent<BossController>();
    }

    public void TakeDamage(float damage)
    {
        if (damage <= 0f)
            return;

        if (bossHealth == null || bossHealth.IsDead)
            return;

        if (bossController != null && !bossController.AreHandsVulnerable)
            return;

        bossHealth.TakeDamage(damage);
    }

    public void TakeDamage(int damage)
    {
        TakeDamage((float)damage);
    }
}
