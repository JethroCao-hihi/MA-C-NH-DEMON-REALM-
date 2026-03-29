using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    #region === VARIABLES ===

    [Header("Attack Settings - Hollow Knight Style")]
    [Tooltip("Time between attacks (lower = faster)")]
    [SerializeField] private float attackCooldown = 0.25f;

    [Header("Jump Attack")]
    [SerializeField] private float airAttackCooldown = 0.3f;

    [Header("Parry / Barry")]
    [Tooltip("Thời gian cửa sổ parry (giây)")]
    [SerializeField] private float parryWindow = 0.25f;
    [Tooltip("Cooldown sau khi parry (giây)")]
    [SerializeField] private float parryCooldown = 0.4f;

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
    private float lastAirAttackTime = -999f;
    private int attackIndex = 1;

    private float lastParryTime = -999f;
    private bool isParrying;

    // Animator param safety
    private int isParryingHash;
    private bool hasIsParryingParam;

    private int parryTriggerHash;
    private bool hasParryTrigger;
    private int attackAirTriggerHash;
    private bool hasAttackAirTrigger;

    private int attack1TriggerHash;
    private int attack2TriggerHash;
    private int attack3TriggerHash;
    private bool hasAttack1Trigger;
    private bool hasAttack2Trigger;
    private bool hasAttack3Trigger;

    #endregion

    #region === PROPERTIES ===

    public bool IsParrying => isParrying;

    #endregion

    #region === UNITY CALLBACKS ===

    private void Awake()
    {
        anim = GetComponent<Animator>();
        movement = GetComponent<PlayerMovement>();

        // Cache hashes
        isParryingHash = Animator.StringToHash("IsParrying");
        parryTriggerHash = Animator.StringToHash("Parry");
        attackAirTriggerHash = Animator.StringToHash("AttackAir");
        attack1TriggerHash = Animator.StringToHash("Attack1");
        attack2TriggerHash = Animator.StringToHash("Attack2");
        attack3TriggerHash = Animator.StringToHash("Attack3");

        // Cache param/trigger existence
        hasIsParryingParam = AnimatorHasParam(anim, isParryingHash, AnimatorControllerParameterType.Bool);
        hasParryTrigger = AnimatorHasParam(anim, parryTriggerHash, AnimatorControllerParameterType.Trigger);
        hasAttackAirTrigger = AnimatorHasParam(anim, attackAirTriggerHash, AnimatorControllerParameterType.Trigger);
        hasAttack1Trigger = AnimatorHasParam(anim, attack1TriggerHash, AnimatorControllerParameterType.Trigger);
        hasAttack2Trigger = AnimatorHasParam(anim, attack2TriggerHash, AnimatorControllerParameterType.Trigger);
        hasAttack3Trigger = AnimatorHasParam(anim, attack3TriggerHash, AnimatorControllerParameterType.Trigger);
    }

    private void Update()
    {
        if (movement == null || movement.IsDead) return;
        if (movement.IsDashing) return;

        bool attackPressed = Input.GetKeyDown(KeyCode.J) || Input.GetMouseButtonDown(0);

        // Attack (nhấn) - không cho tấn công khi đang block
        if (movement.IsBlocking) return;

        if (attackPressed)
        {
            if (movement.IsGrounded)
                TryGroundAttack();
            else
                TryAirAttack();
        }
    }

    #endregion

    #region === PUBLIC API ===

    // PlayerMovement sẽ gọi khi phát hiện tap (nhấn nhanh) để parry
    public void RequestParry()
    {
        if (movement == null || movement.IsDead) return;
        if (movement.IsDashing) return;

        TryParry();
    }

    #endregion

    #region === ATTACK LOGIC ===

    private void TryGroundAttack()
    {
        if (Time.time < lastAttackTime + attackCooldown) return;

        lastAttackTime = Time.time;

        // Reset triggers nếu có tồn tại
        if (hasAttack1Trigger) anim.ResetTrigger(attack1TriggerHash);
        if (hasAttack2Trigger) anim.ResetTrigger(attack2TriggerHash);
        if (hasAttack3Trigger) anim.ResetTrigger(attack3TriggerHash);

        int triggerToFire = attackIndex switch
        {
            1 => attack1TriggerHash,
            2 => attack2TriggerHash,
            _ => attack3TriggerHash
        };

        bool canFire = attackIndex switch
        {
            1 => hasAttack1Trigger,
            2 => hasAttack2Trigger,
            _ => hasAttack3Trigger
        };

        if (canFire)
            anim.SetTrigger(triggerToFire);

        attackIndex++;
        if (attackIndex > 3) attackIndex = 1;

        DealDamage();
    }

    private void TryAirAttack()
    {
        if (Time.time < lastAirAttackTime + airAttackCooldown) return;

        lastAirAttackTime = Time.time;

        if (hasAttackAirTrigger)
        {
            anim.ResetTrigger(attackAirTriggerHash);
            anim.SetTrigger(attackAirTriggerHash);
        }

        DealDamage();
    }

    private void TryParry()
    {
        if (Time.time < lastParryTime + parryCooldown) return;

        lastParryTime = Time.time;

        if (hasParryTrigger)
        {
            anim.ResetTrigger(parryTriggerHash);
            anim.SetTrigger(parryTriggerHash);
        }

        // Có thể dùng bool để transition nếu animator cần
        isParrying = true;
        if (hasIsParryingParam)
            anim.SetBool(isParryingHash, true);

        CancelInvoke(nameof(EndParryWindow));
        Invoke(nameof(EndParryWindow), parryWindow);
    }

    private void EndParryWindow()
    {
        isParrying = false;
        if (hasIsParryingParam && anim != null)
            anim.SetBool(isParryingHash, false);
    }

    public void DealDamage()
    {
        if (attackPoint == null) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);

        foreach (var hit in hits)
            hit.SendMessage("TakeDamage", attackDamage, SendMessageOptions.DontRequireReceiver);
    }

    #endregion

    #region === ANIMATOR HELPERS ===

    private static bool AnimatorHasParam(Animator animator, int nameHash, AnimatorControllerParameterType type)
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