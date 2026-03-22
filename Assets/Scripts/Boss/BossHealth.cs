using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class BossHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 300f;
    [SerializeField] private float currentHealth = 300f;

    [Header("UI (Optional)")]
    [SerializeField] private Slider healthBar;

    [Header("Hit Stun / Invincibility")]
    [Tooltip("Minimum time between Hurt reactions. While active, the boss still loses HP but won't retrigger Hurt.")]
    [SerializeField] private float hurtCooldown = 0.5f;

    [Header("Death")]
    [SerializeField] private float destroyDelayOnDie = 3f;

    [Header("Animation")]
    [Tooltip("Base trigger name used for hurt. If variants are enabled, this is used as prefix (e.g., Hurt_1, Hurt_2).")]
    [SerializeField] private string hurtTriggerName = "Hurt";
    [Tooltip("Trigger name for death.")]
    [SerializeField] private string dieTriggerName = "Die";

    [Header("Hurt Variants (Optional)")]
    [SerializeField] private bool useHurtVariants = false;
    [Tooltip("How many hurt variants exist (expects triggers like Hurt_1 .. Hurt_N).")]
    [SerializeField, Min(1)] private int hurtVariantCount = 2;

    private Animator anim;
    private bool isDead;
    private float nextHurtAllowedTime;
    private HashSet<string> animatorTriggerNames;

    public bool IsDead => isDead;
    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        CacheAnimatorTriggers();
    }

    private void Start()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth <= 0f ? maxHealth : currentHealth, 0f, maxHealth);

        if (healthBar != null)
        {
            healthBar.maxValue = maxHealth;
            healthBar.value = currentHealth;
        }

        nextHurtAllowedTime = Time.time;
    }

    public void TakeDamage(int dmg) => TakeDamage((float)dmg);

    public void TakeDamage(float dmg)
    {
        if (isDead) return;
        if (dmg <= 0f) return;

        currentHealth = Mathf.Max(0f, currentHealth - dmg);
        UpdateUI();

        if (currentHealth <= 0f)
        {
            Die();
            return;
        }

        // Prevent Hurt spam: allow the boss to return to Idle/Attack even if player keeps hitting.
        if (Time.time >= nextHurtAllowedTime)
        {
            Trigger(GetHurtTriggerToPlay());
            nextHurtAllowedTime = Time.time + Mathf.Max(0f, hurtCooldown);
        }
    }

    private string GetHurtTriggerToPlay()
    {
        if (!useHurtVariants)
            return hurtTriggerName;

        int count = Mathf.Max(1, hurtVariantCount);
        int index = Random.Range(1, count + 1);
        string variant = $"{hurtTriggerName}_{index}";

        // Fallback if animator does not define variant triggers
        if (HasAnimatorTrigger(variant))
            return variant;

        return hurtTriggerName;
    }

    private void UpdateUI()
    {
        if (healthBar == null) return;
        healthBar.value = currentHealth;
    }

    private void Trigger(string triggerName)
    {
        if (anim == null) return;
        if (string.IsNullOrWhiteSpace(triggerName)) return;
        if (!HasAnimatorTrigger(triggerName)) return;

        anim.ResetTrigger(triggerName);
        anim.SetTrigger(triggerName);
    }

    private void CacheAnimatorTriggers()
    {
        animatorTriggerNames = new HashSet<string>();

        if (anim == null)
            return;

        foreach (var param in anim.parameters)
        {
            if (param.type == AnimatorControllerParameterType.Trigger)
                animatorTriggerNames.Add(param.name);
        }
    }

    private bool HasAnimatorTrigger(string triggerName)
    {
        if (string.IsNullOrWhiteSpace(triggerName))
            return false;

        if (animatorTriggerNames == null)
            CacheAnimatorTriggers();

        return animatorTriggerNames != null && animatorTriggerNames.Contains(triggerName);
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        Trigger(dieTriggerName);

        // Stop boss logic if present
        var controller = GetComponent<BossController>();
        if (controller != null)
            controller.enabled = false;

        Destroy(gameObject, Mathf.Max(0f, destroyDelayOnDie));
    }
}
