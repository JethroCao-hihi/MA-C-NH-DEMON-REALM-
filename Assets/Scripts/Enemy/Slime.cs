using UnityEngine;

public class Slime : EnemyBase
{
    [Header("Slime Settings")]
    [SerializeField] private float detectRange = 6f;
    [SerializeField] private float attackRange = 1.2f;
    [SerializeField] private float attackCooldown = 1.25f;

    [Header("Damage Player")]
    [SerializeField] private int contactDamage = 5;
    [SerializeField] private float contactDamageCooldown = 0.75f;

    private float nextAttackTime;
    private bool isAttacking;

    private float nextContactDamageTime;

    private static readonly int AttackHash = Animator.StringToHash("Attack");

    protected override void Start()
    {
        base.Start();

        if (health <= 0f) health = 50f;
        if (moveSpeed <= 0f) moveSpeed = 1.5f;
        if (damage <= 0) damage = 5;
        if (contactDamage <= 0) contactDamage = damage;

        // Enable patrol by default (you can toggle off in Inspector)
        enablePatrol = true;
        if (patrolDistance <= 0f) patrolDistance = 3f;

        nextAttackTime = Time.time;
    }

    protected override void Update()
    {
        if (isDead) return;

        // Let EnemyBase handle patrol (if enabled) when player isn't found.
        if (player == null)
        {
            base.Update();
            return;
        }

        float dist = Vector2.Distance(transform.position, player.position);

        // If player is outside detect range => keep patrolling
        if (dist > detectRange)
        {
            PatrolIfEnabled();
            return;
        }

        // Attack when close enough
        if (!isAttacking && Time.time >= nextAttackTime && dist <= attackRange)
        {
            DoAttack();
            return;
        }

        // Otherwise follow player (EnemyBase.Move)
        base.Update();
    }

    private void PatrolIfEnabled()
    {
        // Do the same as base.Update() would, but without following player.
        if (enablePatrol)
            Patrol();
        else
            SetSpeedParam(transform.position, transform.position);

        // Flip/movement handled by EnemyBase.Patrol
    }

    protected override void Move()
    {
        if (isAttacking)
        {
            // Stop during attack
            if (anim != null)
                anim.SetFloat(SpeedHash, 0f);
            return;
        }

        base.Move();
    }

    private void DoAttack()
    {
        isAttacking = true;
        nextAttackTime = Time.time + attackCooldown;

        if (anim != null && AnimatorHasParam(anim, AttackHash, AnimatorControllerParameterType.Trigger))
        {
            anim.ResetTrigger(AttackHash);
            anim.SetTrigger(AttackHash);
        }

        Invoke(nameof(EndAttack), 0.6f);
    }

    public void EndAttack()
    {
        isAttacking = false;
        CancelInvoke(nameof(EndAttack));
    }

    private void TryDamagePlayer(GameObject other)
    {
        if (isDead) return;
        if (contactDamage <= 0) return;
        if (Time.time < nextContactDamageTime) return;

        var playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth == null) return;
        if (playerHealth.IsDead) return;

        nextContactDamageTime = Time.time + contactDamageCooldown;
        playerHealth.TakeDamage(contactDamage);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;
        if (!other.CompareTag("Player")) return;
        TryDamagePlayer(other.gameObject);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other == null) return;
        if (!other.CompareTag("Player")) return;
        TryDamagePlayer(other.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision?.collider == null) return;
        if (!collision.collider.CompareTag("Player")) return;
        TryDamagePlayer(collision.collider.gameObject);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision?.collider == null) return;
        if (!collision.collider.CompareTag("Player")) return;
        TryDamagePlayer(collision.collider.gameObject);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
#endif
}
