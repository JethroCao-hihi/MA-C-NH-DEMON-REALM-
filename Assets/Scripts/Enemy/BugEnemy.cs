using UnityEngine;

public class BugEnemy : EnemyBase
{
    [Header("Bug - Flight")]
    [SerializeField] private float hoverHeight = 3f;
    [SerializeField] private float hoverSpeed = 2f;
    [SerializeField] private float followSpeed = 3.5f;

    [Header("Bug - Detection")]
    [SerializeField] private float detectRange = 6f;
    [SerializeField] private float attackRange = 1.2f;

    [Header("Bug - Attack")]
    [SerializeField] private float attackCooldown = 1.2f;
    [SerializeField] private int attackDamage = 10;
    [Tooltip("Point used for hit detection when dealing damage (Animation Event).")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRadius = 0.8f;
    [SerializeField] private LayerMask playerLayer;

    private float nextAttackTime;
    private bool isAttacking;

    private static readonly int AttackHash = Animator.StringToHash("Attack");

    protected override void Start()
    {
        base.Start();

        nextAttackTime = Time.time;

        // Enable patrol by default for this enemy (can turn off in Inspector)
        enablePatrol = true;

        if (health <= 0f) health = 40f;
        if (moveSpeed <= 0f) moveSpeed = 2f;
        if (damage <= 0) damage = attackDamage;
        if (attackDamage <= 0) attackDamage = damage;

        if (attackPoint == null)
            attackPoint = transform;
    }

    protected override void Update()
    {
        if (isDead) return;
        base.Update();
    }

    protected override void Move()
    {
        if (player == null)
        {
            if (enablePatrol) Patrol();
            return;
        }

        if (isAttacking)
            return;

        float dist = Vector2.Distance(transform.position, player.position);

        if (dist <= detectRange)
        {
            Vector2 before = transform.position;

            Vector3 target = new Vector3(player.position.x, player.position.y, transform.position.z);
            transform.position = Vector3.MoveTowards(transform.position, target, followSpeed * Time.deltaTime);

            // Flip by movement delta X
            FlipToXDirection(((Vector2)transform.position).x - before.x);

            if (dist <= attackRange && Time.time >= nextAttackTime)
                BeginAttack();
        }
        else
        {
            if (enablePatrol) Patrol();
        }
    }

    /// <summary>
    /// Flying patrol override: left/right around spawnPos while keeping a hover height.
    /// Uses EnemyBase.patrolDistance and EnemyBase.patrolDir.
    /// </summary>
    protected override void Patrol()
    {
        float dist = Mathf.Max(0f, patrolDistance);
        float leftX = spawnPos.x - dist;
        float rightX = spawnPos.x + dist;

        float targetX = patrolDir > 0 ? rightX : leftX;
        float targetY = spawnPos.y + hoverHeight;

        Vector3 target = new Vector3(targetX, targetY, transform.position.z);

        Vector2 before = transform.position;
        transform.position = Vector3.MoveTowards(transform.position, target, hoverSpeed * Time.deltaTime);

        FlipToXDirection(((Vector2)transform.position).x - before.x);

        if (Mathf.Abs(transform.position.x - targetX) <= 0.05f)
            patrolDir *= -1;
    }

    private void BeginAttack()
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

    public void DealAttackDamage()
    {
        if (isDead) return;
        if (attackDamage <= 0) return;
        if (attackPoint == null) return;

        Collider2D hit = Physics2D.OverlapCircle(attackPoint.position, attackRadius, playerLayer);
        if (hit == null) return;

        var playerHealth = hit.GetComponent<PlayerHealth>();
        if (playerHealth != null && !playerHealth.IsDead)
            playerHealth.TakeDamage(attackDamage);
    }

    public void EndAttack()
    {
        isAttacking = false;
        CancelInvoke(nameof(EndAttack));
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);

        Gizmos.color = Color.cyan;
        Vector3 basePos = Application.isPlaying ? spawnPos : transform.position;
        Gizmos.DrawLine(new Vector3(basePos.x - patrolDistance, basePos.y + hoverHeight, basePos.z),
            new Vector3(basePos.x + patrolDistance, basePos.y + hoverHeight, basePos.z));

        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
        }
    }
#endif
}
