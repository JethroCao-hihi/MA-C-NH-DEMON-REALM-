using UnityEngine;

public class ShieldEnemy : EnemyBase
{
    // NOTE:
    // This enemy's sprite faces LEFT by default.
    // In the Inspector (EnemyBase -> Facing), set "Facing Sign" to -1 for this prefab.

    [Header("Shield Enemy - Behaviour")]
    [SerializeField] private float detectRange = 6f;
    [SerializeField] private float attackRange = 1.3f;

    [Header("Push")]
    [SerializeField] private float pushCooldown = 2.0f;
    [SerializeField] private int pushDamage = 8;
    [Tooltip("Horizontal knockback velocity applied to the player when hit by Push. Increase this to push ~2 tiles.")]
    [SerializeField] private float pushKnockbackX = 10f;
    [SerializeField] private float pushKnockbackY = 1.5f;
    [Tooltip("While pushing, Shield will rush towards the player using this speed multiplier.")]
    [SerializeField] private float pushApproachSpeedMultiplier = 2.25f;

    [Header("Attack")]
    [SerializeField] private float attackCooldown = 1.25f;
    [SerializeField] private int attackDamage = 12;

    [Header("Hit Detection (Animation Events)")]
    [SerializeField] private Transform hitPoint;
    [SerializeField] private float hitRadius = 0.9f;
    [SerializeField] private LayerMask playerLayer;

    [Header("Tuning")]
    [Tooltip("When in detect range, enemy walks slightly faster before pushing.")]
    [SerializeField] private float chaseSpeedMultiplier = 1.15f;

    private float baseMoveSpeed;

    private float nextPushTime;
    private float nextAttackTime;

    private bool isPushing;
    private bool isAttacking;

    // Once the enemy has detected the player, it will try to open with Push (if available).
    private bool hasDetectedPlayer;

    private static readonly int PushHash = Animator.StringToHash("Push");
    private static readonly int AttackHash = Animator.StringToHash("Attack");

    protected override void Start()
    {
        base.Start();

        if (health <= 0f) health = 80f;
        if (moveSpeed <= 0f) moveSpeed = 1.0f;

        baseMoveSpeed = moveSpeed;

        enablePatrol = true;
        patrolSpeedMultiplier = 0.7f;
        if (patrolDistance <= 0f) patrolDistance = 3f;

        nextPushTime = Time.time;
        nextAttackTime = Time.time;

        if (hitPoint == null)
            hitPoint = transform;

        if (pushDamage <= 0) pushDamage = damage;
        if (attackDamage <= 0) attackDamage = damage;
    }

    protected override void Update()
    {
        if (isDead) return;

        // During attack animation: do not move
        if (isAttacking)
        {
            if (player == null)
                TryFindPlayer();

            if (player != null)
                FlipToFacePlayer();

            return;
        }

        // During push: rush towards player (NOT pushing self back; pushing player via knockback on hit)
        if (isPushing)
        {
            if (player == null)
                TryFindPlayer();

            if (player == null)
                return;

            FlipToFacePlayer();

            float speed = baseMoveSpeed * Mathf.Max(0f, pushApproachSpeedMultiplier);
            Vector2 before = transform.position;
            transform.position = Vector2.MoveTowards(transform.position, player.position, speed * Time.deltaTime);
            SetSpeedParam(before, transform.position);

            return;
        }

        base.Update();
    }

    protected override void Move()
    {
        if (player == null)
        {
            moveSpeed = baseMoveSpeed;
            if (enablePatrol) Patrol();
            return;
        }

        float dist = Vector2.Distance(transform.position, player.position);

        // Outside detect range => patrol and reset detection state
        if (dist > detectRange)
        {
            moveSpeed = baseMoveSpeed;
            hasDetectedPlayer = false;
            if (enablePatrol) Patrol();
            return;
        }

        // Player is detected
        if (!hasDetectedPlayer)
            hasDetectedPlayer = true;

        // First action when detected: Push (if off cooldown)
        if (hasDetectedPlayer && Time.time >= nextPushTime)
        {
            BeginPush();
            return;
        }

        // Otherwise walk towards player
        moveSpeed = baseMoveSpeed * Mathf.Max(1f, chaseSpeedMultiplier);

        // Attack only when close enough and off cooldown
        if (dist <= attackRange && Time.time >= nextAttackTime)
        {
            BeginAttack();
            return;
        }

        base.Move();
    }

    private void BeginPush()
    {
        isPushing = true;
        nextPushTime = Time.time + pushCooldown;

        if (anim != null && AnimatorHasParam(anim, PushHash, AnimatorControllerParameterType.Trigger))
        {
            anim.ResetTrigger(PushHash);
            anim.SetTrigger(PushHash);
        }

        // End by animation event ideally
        Invoke(nameof(EndPush), 0.6f);
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

    public void DealPushDamage()
    {
        if (isDead) return;

        var target = FindPlayerInHitRange();
        if (target == null) return;

        if (pushDamage > 0)
            target.TakeDamage(pushDamage);

        if (pushKnockbackX > 0f || pushKnockbackY > 0f)
        {
            var movement = target.GetComponent<PlayerMovement>();
            if (movement != null)
            {
                float dir = transform.localScale.x >= 0f ? 1f : -1f;
                movement.ApplyKnockback(new Vector2(dir * pushKnockbackX, pushKnockbackY));
            }
        }
    }

    public void DealAttackDamage()
    {
        if (isDead) return;

        var target = FindPlayerInHitRange();
        if (target == null) return;

        if (attackDamage > 0)
            target.TakeDamage(attackDamage);
    }

    private PlayerHealth FindPlayerInHitRange()
    {
        if (hitPoint == null) return null;

        Collider2D hit = Physics2D.OverlapCircle(hitPoint.position, hitRadius, playerLayer);
        if (hit == null) return null;

        var playerHealth = hit.GetComponent<PlayerHealth>();
        if (playerHealth == null || playerHealth.IsDead) return null;

        return playerHealth;
    }

    public void EndPush()
    {
        isPushing = false;
        CancelInvoke(nameof(EndPush));

        moveSpeed = baseMoveSpeed;

        // Small buffer so it transitions back to Walk before deciding to attack
        nextAttackTime = Mathf.Max(nextAttackTime, Time.time + 0.25f);
    }

    public void EndAttack()
    {
        isAttacking = false;
        CancelInvoke(nameof(EndAttack));

        moveSpeed = baseMoveSpeed;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (hitPoint != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(hitPoint.position, hitRadius);
        }
    }
#endif
}
