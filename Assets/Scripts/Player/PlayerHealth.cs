using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Chỉ số Máu")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;

    [Header("Miễn nhiễm sau khi trúng đòn")]
    [SerializeField] private float invincibleTimeAfterHit = 1.2f;

    [Header("Hiệu ứng bị thương")]
    [Tooltip("Thời gian nhấp nháy sau khi bị đánh (giây)")]
    [SerializeField] private float hurtBlinkDuration = 1.2f;
    [Tooltip("Tốc độ nhấp nháy (giây/lần đổi trạng thái)")]
    [SerializeField] private float hurtBlinkInterval = 0.12f;

    [Header("Knockback")]
    [SerializeField] private float knockbackForceX = 6f;
    [SerializeField] private float knockbackForceY = 2f;

    [Header("Giao diện")]
    [SerializeField] private Slider healthBar;

    // Cached
    private float invincibleTimer;
    private bool isDead;
    private PlayerMovement movementScript;
    private PlayerAttack attackScript;
    private Animator anim;
    private SpriteRenderer spriteRenderer;

    private Coroutine blinkCoroutine;

    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public bool IsDead => isDead;

    private void Awake()
    {
        movementScript = GetComponent<PlayerMovement>();
        attackScript = GetComponent<PlayerAttack>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        currentHealth = Mathf.Clamp(currentHealth <= 0 ? maxHealth : currentHealth, 1, maxHealth);
        SetupUI();
        UpdateUI();
    }

    private void OnDisable()
    {
        StopBlink();
    }

    private void Update()
    {
        if (invincibleTimer > 0f)
            invincibleTimer -= Time.deltaTime;
    }

    private void SetupUI()
    {
        if (healthBar == null) return;
        healthBar.maxValue = maxHealth;
    }

    private void UpdateUI()
    {
        if (healthBar == null) return;
        healthBar.value = currentHealth;
    }

    public void SetMaxHealth(int value, bool healToFull = false)
    {
        maxHealth = Mathf.Max(1, value);
        if (healToFull) currentHealth = maxHealth;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        SetupUI();
        UpdateUI();
    }

    // Quái vật hoặc bẫy sẽ gọi hàm này
    public void TakeDamage(int damage)
    {
        ApplyDamage(damage, allowDefense: true);
    }

    public void TakeRawDamage(int damage)
    {
        ApplyDamage(damage, allowDefense: false);
    }

    private void ApplyDamage(int damage, bool allowDefense)
    {
        if (isDead) return;
        if (damage <= 0) return;
        if (invincibleTimer > 0f) return;

        if (allowDefense)
        {
            if (attackScript != null && attackScript.IsParrying)
            {
                if (SoundManager.Instance != null)
                    SoundManager.Instance.PlayPlayerSfx("Parry");
                return;
            }

            if (movementScript != null && movementScript.IsBlocking)
                return;
        }

        currentHealth = Mathf.Max(0, currentHealth - damage);
        invincibleTimer = invincibleTimeAfterHit;

        UpdateUI();

        if (anim != null) anim.SetTrigger("Hurt");
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayPlayerSfx("Hurt");

        ApplyKnockback();
        StartBlink();

        if (currentHealth <= 0)
            Die();
    }

    private void ApplyKnockback()
    {
        if (movementScript == null) return;

        // Đẩy lùi ngược hướng đang nhìn
        int dir = transform.localScale.x >= 0f ? -1 : 1;
        movementScript.ApplyKnockback(new Vector2(dir * knockbackForceX, knockbackForceY));
    }

    private void StartBlink()
    {
        if (spriteRenderer == null || hurtBlinkDuration <= 0f) return;

        StopBlink();
        blinkCoroutine = StartCoroutine(BlinkCoroutine());
    }

    private void StopBlink()
    {
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }

        if (spriteRenderer != null)
            spriteRenderer.enabled = true;
    }

    private IEnumerator BlinkCoroutine()
    {
        float endTime = Time.time + hurtBlinkDuration;
        bool visible = true;

        while (Time.time < endTime)
        {
            visible = !visible;
            spriteRenderer.enabled = visible;
            yield return new WaitForSeconds(hurtBlinkInterval);
        }

        spriteRenderer.enabled = true;
        blinkCoroutine = null;
    }

    public void Heal(int amount)
    {
        if (isDead) return;
        if (amount <= 0) return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        UpdateUI();
    }

    public void Respawn(int healthPercent = 100)
    {
        isDead = false;
        invincibleTimer = 0f;

        int hpPercent = Mathf.Clamp(healthPercent, 1, 100);
        currentHealth = Mathf.Max(1, (maxHealth * hpPercent) / 100);

        StopBlink();

        // Mở lại control
        if (movementScript != null) movementScript.enabled = true;
        if (attackScript != null) attackScript.enabled = true;

        if (anim != null)
        {
            anim.ResetTrigger("Die");
            anim.ResetTrigger("Hurt");
        }

        UpdateUI();

        SendMessage("OnPlayerRespawned", SendMessageOptions.DontRequireReceiver);
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        StopBlink();

        if (anim != null) anim.SetTrigger("Die");
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayPlayerSfx("Die");

        // Khóa không cho người chơi di chuyển hay chém nữa
        if (movementScript != null) movementScript.enabled = false;
        if (attackScript != null) attackScript.enabled = false;

        SendMessage("OnPlayerDied", SendMessageOptions.DontRequireReceiver);

        if (GameOverMenu.Instance != null)
            GameOverMenu.Instance.ShowGameOver();

        // (Sau này có thể thêm UI Restart ở đây)
    }
}