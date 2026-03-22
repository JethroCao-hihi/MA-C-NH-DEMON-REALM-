using UnityEngine;

public class EnemyBase : MonoBehaviour
{
    [Header("Stats")]
    public float health = 100f;
    public float moveSpeed = 2f;
    public int damage = 10;

    [Header("Facing")]
    [Tooltip("Set to -1 if the sprite faces LEFT by default; +1 if it faces RIGHT by default.")]
    [SerializeField] private int facingSign = 1;
    [SerializeField] private bool flipToFacePlayer = true;
    [Tooltip("If enabled, enemy will also flip according to its movement direction (patrol/follow). Useful for patrolling enemies.")]
    [SerializeField] private bool flipToFaceMovement = true;

    [Header("Patrol (optional)")]
    [Tooltip("If enabled, the enemy will patrol left/right around its spawn position when not engaging the player.")]
    [SerializeField] protected bool enablePatrol = false;
    [Tooltip("How far (in world units) the enemy patrols left/right from its start X.")]
    [SerializeField] protected float patrolDistance = 3f;
    [Tooltip("Patrol speed multiplier (1 = use moveSpeed).")]
    [SerializeField] protected float patrolSpeedMultiplier = 1f;

    [Header("Death")]
    [Tooltip("Seconds to wait after triggering Die before destroying the enemy.")]
    [SerializeField] protected float destroyDelayOnDie = 5f;

    [Tooltip("Disable colliders on death so the enemy no longer interacts.")]
    [SerializeField] private bool disableCollidersOnDeath = false;

    [Tooltip("Animator state name to force play on death (recommended: 'Die').")]
    [SerializeField] private string dieStateName = "Die";

    protected Transform player;
    protected Animator anim;
    protected Rigidbody2D rb;

    protected bool isDead;

    // Animator param hashes
    protected static readonly int SpeedHash = Animator.StringToHash("Speed");
    protected static readonly int HurtHash = Animator.StringToHash("Hurt");
    protected static readonly int DieHash = Animator.StringToHash("Die");

    // Cached animator parameter existence
    private bool hasSpeedFloat;
    private bool hasHurtTrigger;
    private bool hasDieTrigger;

    // Patrol state
    protected Vector3 spawnPos;

    [System.NonSerialized]
    protected int patrolDir = 1;

    protected virtual void Awake()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();

        CacheAnimatorParams();
    }

    protected virtual void Start()
    {
        spawnPos = transform.position;
        TryFindPlayer();
    }

    protected virtual void Update()
    {
        if (isDead) return;

        // Player can be spawned later / destroyed & respawned
        if (player == null)
            TryFindPlayer();

        if (flipToFacePlayer)
            FlipToFacePlayer();

        Move();
    }

    protected void TryFindPlayer()
    {
        GameObject playerGo = GameObject.FindGameObjectWithTag("Player");
        player = playerGo != null ? playerGo.transform : null;
    }

    protected virtual void FlipToFacePlayer()
    {
        if (player == null) return;

        float dx = player.position.x - transform.position.x;
        FlipToXDirection(dx);
    }

    protected void FlipToXDirection(float dx)
    {
        if (Mathf.Abs(dx) < 0.001f) return;

        Vector3 s = transform.localScale;
        float sign = dx > 0f ? 1f : -1f;
        s.x = Mathf.Abs(s.x) * sign * Mathf.Sign(facingSign == 0 ? 1 : facingSign);
        transform.localScale = s;
    }

    protected virtual void Move()
    {
        if (player == null)
        {
            if (enablePatrol)
                Patrol();

            return;
        }

        Vector2 before = transform.position;
        transform.position = Vector2.MoveTowards(transform.position, player.position, moveSpeed * Time.deltaTime);

        if (flipToFaceMovement)
            FlipToXDirection(player.position.x - before.x);

        SetSpeedParam(before, transform.position);
    }

    /// <summary>
    /// Default patrol is left/right on X axis around spawnPos.
    /// Override if your enemy needs special patrol (e.g., flying bug).
    /// </summary>
    protected virtual void Patrol()
    {
        float dist = Mathf.Max(0f, patrolDistance);
        float leftX = spawnPos.x - dist;
        float rightX = spawnPos.x + dist;

        float targetX = patrolDir > 0 ? rightX : leftX;
        Vector3 target = new Vector3(targetX, transform.position.y, transform.position.z);

        Vector2 before = transform.position;

        float speed = moveSpeed * Mathf.Max(0f, patrolSpeedMultiplier);
        transform.position = Vector2.MoveTowards(transform.position, target, speed * Time.deltaTime);

        if (flipToFaceMovement)
            FlipToXDirection(targetX - before.x);

        SetSpeedParam(before, transform.position);

        if (Mathf.Abs(transform.position.x - targetX) <= 0.05f)
            patrolDir *= -1;
    }

    protected void SetSpeedParam(Vector2 before, Vector2 after)
    {
        if (anim == null || !hasSpeedFloat) return;

        float speed = (after - before).magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        anim.SetFloat(SpeedHash, speed);
    }

    // Support SendMessage("TakeDamage", int) as well
    public void TakeDamage(int dmg) => TakeDamage((float)dmg);

    public virtual void TakeDamage(float dmg)
    {
        if (isDead) return;
        if (dmg <= 0f) return;

        health -= dmg;
        bool lethal = health <= 0f;

        if (!lethal)
        {
            TriggerIfExists(HurtHash, hasHurtTrigger);
            OnDamaged(dmg);
        }
        else
        {
            // Ensure Hurt doesn't interrupt death
            ResetIfExists(HurtHash, hasHurtTrigger);
            Die();
        }
    }

    protected virtual void Die()
    {
        if (isDead) return;
        isDead = true;

        // Clear any pending triggers that can interrupt death
        ResetAllTriggersExcept(DieHash);

        if (anim != null)
        {
            if (hasSpeedFloat)
                anim.SetFloat(SpeedHash, 0f);

            TriggerIfExists(DieHash, hasDieTrigger);
            PlayStateIfExists(dieStateName);
        }

        StopPhysics();

        if (disableCollidersOnDeath)
            SetAllCollidersEnabled(false);

        // Stop further logic (movement/attacks). Animator still runs.
        enabled = false;

        OnDeath();

        Destroy(gameObject, Mathf.Max(0f, destroyDelayOnDie));
    }

    /// <summary>
    /// Called when damaged but not dead.
    /// </summary>
    protected virtual void OnDamaged(float dmg) { }

    /// <summary>
    /// Called once when death starts (after animator/physics setup).
    /// </summary>
    protected virtual void OnDeath() { }

    #region === Helpers ===

    private void CacheAnimatorParams()
    {
        if (anim == null) return;

        hasSpeedFloat = AnimatorHasParam(anim, SpeedHash, AnimatorControllerParameterType.Float);
        hasHurtTrigger = AnimatorHasParam(anim, HurtHash, AnimatorControllerParameterType.Trigger);
        hasDieTrigger = AnimatorHasParam(anim, DieHash, AnimatorControllerParameterType.Trigger);
    }

    protected void StopPhysics()
    {
        if (rb == null) return;

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.simulated = false;
    }

    protected void SetAllCollidersEnabled(bool enabledState)
    {
        foreach (var col in GetComponents<Collider2D>())
            col.enabled = enabledState;
    }

    protected void TriggerIfExists(int triggerHash, bool hasTrigger)
    {
        if (!hasTrigger || anim == null) return;
        anim.ResetTrigger(triggerHash);
        anim.SetTrigger(triggerHash);
    }

    protected void ResetIfExists(int triggerHash, bool hasTrigger)
    {
        if (!hasTrigger || anim == null) return;
        anim.ResetTrigger(triggerHash);
    }

    protected void ResetAllTriggersExcept(int exceptTriggerHash)
    {
        if (anim == null) return;

        foreach (var p in anim.parameters)
        {
            if (p.type == AnimatorControllerParameterType.Trigger && p.nameHash != exceptTriggerHash)
                anim.ResetTrigger(p.nameHash);
        }
    }

    protected void PlayStateIfExists(string stateName)
    {
        if (anim == null) return;
        if (string.IsNullOrWhiteSpace(stateName)) return;

        int hash = Animator.StringToHash(stateName);
        if (anim.HasState(0, hash))
            anim.Play(hash, 0, 0f);
    }

    protected static bool AnimatorHasParam(Animator animator, int nameHash, AnimatorControllerParameterType type)
    {
        if (animator == null) return false;

        foreach (var p in animator.parameters)
        {
            if (p.type == type && p.nameHash == nameHash)
                return true;
        }

        return false;
    }

    #endregion
}
