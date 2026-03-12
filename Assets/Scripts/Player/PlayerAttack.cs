using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    #region === VARIABLES ===

    [Header("Attack Settings - Hollow Knight Style")]
    [Tooltip("Time between attacks (lower = faster)")]
    [SerializeField] private float attackCooldown = 0.25f;

    [Header("Damage Settings")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRange = 0.5f;
    [SerializeField] private LayerMask enemyLayers;
    [SerializeField] private int attackDamage = 20;

    // Cached Components
    private Animator anim;
    private PlayerMovement movement;

    // State
    private float lastAttackTime = -999f;
    private int attackIndex = 1;

    #endregion

    #region === UNITY CALLBACKS ===

    private void Awake()
    {
        anim = GetComponent<Animator>();
        movement = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        if (movement == null || movement.IsDead) return;
        if (movement.IsBlocking || movement.IsDashing) return;

        if (Input.GetKeyDown(KeyCode.J) || Input.GetMouseButtonDown(0))
            TryAttack();
    }

    #endregion

    #region === ATTACK LOGIC ===

    private void TryAttack()
    {
        if (Time.time < lastAttackTime + attackCooldown) return;

        lastAttackTime = Time.time;

        // Reset ALL triggers first to prevent queue
        anim.ResetTrigger("Attack1");
        anim.ResetTrigger("Attack2");
        anim.ResetTrigger("Attack3");

        // Set current attack trigger
        string triggerName = "Attack" + attackIndex;
        anim.SetTrigger(triggerName);

        // Rotate: 1 -> 2 -> 3 -> 1
        attackIndex++;
        if (attackIndex > 3) attackIndex = 1;

        // Deal damage
        DealDamage();
    }

    public void DealDamage()
    {
        if (attackPoint == null) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);

        foreach (var hit in hits)
        {
            // Không phụ thuộc class enemy cụ thể để tránh lỗi compile.
            // Enemy script chỉ cần có hàm: void TakeDamage(int damage)
            hit.SendMessage("TakeDamage", attackDamage, SendMessageOptions.DontRequireReceiver);
        }
    }

    #endregion

    #region === GIZMOS ===

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
#endif

    #endregion
}