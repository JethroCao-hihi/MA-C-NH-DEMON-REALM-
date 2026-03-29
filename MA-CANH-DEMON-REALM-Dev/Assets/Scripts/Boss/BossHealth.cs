using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

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
    [SerializeField] private CutsceneDirector bossOutroCutsceneDirector;
    [SerializeField] private bool playBossOutroCutsceneOnDeath = true;
    [SerializeField] private bool playAfterCreditsOnBossDeath = true;
    [SerializeField] private string teamTitle = "MA CẢNH: DEMON REALM";
    [SerializeField] private string[] teamCreditsLines = new string[]
    {
        "Creative Direction: [Tên đạo diễn sáng tạo]",
        "Game Design & Balance: [Tên nhóm thiết kế]",
        "Art Direction & VFX: [Tên nhóm mỹ thuật]",
        "Engineering & Systems: [Tên nhóm lập trình]",
        "Audio Direction: [Tên nhóm âm thanh]",
        "Special Thanks: [Danh sách cảm ơn]"
    };
    [SerializeField] private float bossOutroTimeout = 45f;
    [SerializeField] private float afterCreditsTimeout = 40f;
    [SerializeField] private float afterCreditsStaticHoldDuration = 1.2f;
    [SerializeField, Range(0.3f, 1f)] private float afterCreditsScrollDurationRatio = 0.65f;
    [SerializeField] private float afterCreditsSpeedMultiplier = 1.0f;
    [SerializeField] private float afterCreditsTopStartOffset = -40f;
    [SerializeField] private float afterCreditsBottomPadding = 200f;

    [Header("SFX")]
    [SerializeField] private string hurtSfxName = "hurt";
    [SerializeField] private string dieSfxName = "die";

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
    private bool isDying;
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
            if (SoundManager.Instance != null)
                SoundManager.Instance.PlayBossSfx(hurtSfxName);
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
        if (isDead || isDying) return;
        isDying = true;
        isDead = true;

        Trigger(dieTriggerName);

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayBossSfx(dieSfxName);

        // Stop boss logic if present
        var controller = GetComponent<BossController>();
        if (controller != null)
            controller.enabled = false;

        if (playBossOutroCutsceneOnDeath)
        {
            bossOutroCutsceneDirector = EnsureBossCutsceneDirector();
            if (bossOutroCutsceneDirector != null)
            {
                StartCoroutine(PlayBossOutroThenDestroy());
                return;
            }
        }

        Destroy(gameObject, Mathf.Max(0f, destroyDelayOnDie));
    }

    private SubtitleEntry[] BuildDefaultBossOutroNarrative()
    {
        return new SubtitleEntry[]
        {
            new SubtitleEntry { text = "Khi nhát kiếm cuối cùng khép lại, vương tọa hắc ám rung chuyển trong tiếng than của địa ngục.", triggerTime = 0.5f, displayDuration = 3.6f },
            new SubtitleEntry { text = "Mạch ấn cổ vỡ ra từng mảnh, giải thoát những linh hồn bị xiềng trong máu và bóng đêm.", triggerTime = 4.4f, displayDuration = 3.5f },
            new SubtitleEntry { text = "Ma Chủ quỵ xuống giữa biển tro, để lại đôi mắt tàn lửa vẫn nhìn chòng chọc vào kẻ chiến thắng.", triggerTime = 8.3f, displayDuration = 3.6f },
            new SubtitleEntry { text = "Gió đêm cuốn lời nguyền ra khỏi đại điện, nhưng mặt đất vẫn run lên như còn giữ một bí mật chưa gọi tên.", triggerTime = 12.4f, displayDuration = 3.8f },
            new SubtitleEntry { text = "Trận chiến đã khép màn, và từ chân trời nứt toác, một định mệnh đen tối hơn đang chờ được thức tỉnh.", triggerTime = 16.8f, displayDuration = 3.9f }
        };
    }

    private CutsceneDirector EnsureBossCutsceneDirector()
    {
        if (bossOutroCutsceneDirector != null)
        {
            BindDirectorToSharedCutsceneUI(bossOutroCutsceneDirector);
            return bossOutroCutsceneDirector;
        }

        if (CutsceneDirector.Instance != null)
        {
            bossOutroCutsceneDirector = CutsceneDirector.Instance;
            BindDirectorToSharedCutsceneUI(bossOutroCutsceneDirector);
            return bossOutroCutsceneDirector;
        }

        bossOutroCutsceneDirector = Object.FindFirstObjectByType<CutsceneDirector>();
        if (bossOutroCutsceneDirector != null)
        {
            BindDirectorToSharedCutsceneUI(bossOutroCutsceneDirector);
            return bossOutroCutsceneDirector;
        }

        GameObject runtimeCutsceneSystem = new GameObject("===== BOSS OUTRO CUTSCENE SYSTEM =====");
        bossOutroCutsceneDirector = runtimeCutsceneSystem.AddComponent<CutsceneDirector>();
        bossOutroCutsceneDirector.SetPlayOnStartForRuntime(false);
        runtimeCutsceneSystem.AddComponent<CutsceneSetup>();
        BindDirectorToSharedCutsceneUI(bossOutroCutsceneDirector);
        return bossOutroCutsceneDirector;
    }

    private void BindDirectorToSharedCutsceneUI(CutsceneDirector director)
    {
        if (director == null)
            return;

        GameObject existingCutsceneUI = GameObject.Find("CutsceneUI");
        if (existingCutsceneUI == null)
            return;

        CanvasGroup existingFade = existingCutsceneUI.transform.Find("FadePanel")?.GetComponent<CanvasGroup>();
        if (existingFade == null)
            return;

        director.SetupReferences(existingCutsceneUI, existingFade);
    }

    private List<string> BuildAfterCreditsRollLines()
    {
        List<string> lines = new List<string>();

        if (!string.IsNullOrWhiteSpace(teamTitle))
        {
            lines.Add(teamTitle);
        }

        if (teamCreditsLines != null)
        {
            for (int i = 0; i < teamCreditsLines.Length; i++)
            {
                string line = teamCreditsLines[i];
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                lines.Add(line);
            }
        }

        if (lines.Count == 0)
        {
            lines.Add("Hẹn gặp lại ở cánh cổng tiếp theo.");
        }

        return lines;
    }

    private static bool IsCreditsSkipRequested()
    {
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape))
            return true;

#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame))
            return true;
#endif

        return false;
    }

    private IEnumerator PlayBlackScreenCreditsRoll()
    {
        float timeout = Mathf.Max(0f, afterCreditsTimeout);
        float elapsed = 0f;

        GameObject cutsceneRoot = GameObject.Find("CutsceneUI");
        GameObject temporaryCanvasObject = null;
        GameObject temporaryBlackPanelObject = null;
        Transform parent;

        if (cutsceneRoot != null)
        {
            cutsceneRoot.SetActive(true);
            parent = cutsceneRoot.transform;
        }
        else
        {
            temporaryCanvasObject = new GameObject("BossAfterCreditsCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas temporaryCanvas = temporaryCanvasObject.GetComponent<Canvas>();
            temporaryCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            temporaryCanvas.sortingOrder = 10000;
            parent = temporaryCanvasObject.transform;
        }

        CanvasGroup fade = cutsceneRoot?.transform.Find("FadePanel")?.GetComponent<CanvasGroup>();
        bool hadFade = fade != null;
        bool previousFadeActive = hadFade && fade.gameObject.activeSelf;
        float previousFadeAlpha = hadFade ? fade.alpha : 0f;
        if (fade != null)
        {
            fade.gameObject.SetActive(true);
            fade.alpha = 1f;
        }
        else
        {
            temporaryBlackPanelObject = new GameObject("BossAfterCreditsBlackPanel", typeof(RectTransform), typeof(Image));
            temporaryBlackPanelObject.transform.SetParent(parent, false);
            RectTransform panelRect = temporaryBlackPanelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            Image panelImage = temporaryBlackPanelObject.GetComponent<Image>();
            panelImage.color = Color.black;
        }

        GameObject creditsTextObject = new GameObject("BossAfterCreditsRoll", typeof(RectTransform), typeof(TextMeshProUGUI));
        creditsTextObject.transform.SetParent(parent, false);
        creditsTextObject.transform.SetAsLastSibling();

        RectTransform creditsRect = creditsTextObject.GetComponent<RectTransform>();
        creditsRect.anchorMin = new Vector2(0.5f, 1f);
        creditsRect.anchorMax = new Vector2(0.5f, 1f);
        creditsRect.pivot = new Vector2(0.5f, 1f);
        creditsRect.anchoredPosition = new Vector2(0f, afterCreditsTopStartOffset);
        creditsRect.sizeDelta = new Vector2(1600f, 900f);

        TextMeshProUGUI creditsText = creditsTextObject.GetComponent<TextMeshProUGUI>();
        creditsText.alignment = TextAlignmentOptions.Top;
        creditsText.enableWordWrapping = true;
        creditsText.fontSize = 34f;
        creditsText.color = Color.white;
        List<string> lines = BuildAfterCreditsRollLines();
        System.Text.StringBuilder fullCredits = new System.Text.StringBuilder();
        for (int i = 0; i < lines.Count; i++)
        {
            if (i > 0)
                fullCredits.Append("\n\n");

            fullCredits.Append(i == 0 ? $"<b>{lines[i]}</b>" : lines[i]);
        }
        creditsText.text = fullCredits.ToString();
        creditsText.ForceMeshUpdate();

        float holdDuration = Mathf.Max(0f, afterCreditsStaticHoldDuration);
        float clampedRatio = Mathf.Clamp(afterCreditsScrollDurationRatio, 0.3f, 1f);
        float speedMultiplier = Mathf.Max(0.1f, afterCreditsSpeedMultiplier);
        float scrollDuration = Mathf.Max(1.2f, timeout * clampedRatio) / speedMultiplier;
        float startY = afterCreditsTopStartOffset;
        float endY = -(creditsText.preferredHeight + Mathf.Max(0f, afterCreditsBottomPadding));
        bool skipRequested = false;
        bool timeoutReached = false;

        Debug.Log($"[BossHealth] After-credits: start roll (hold={holdDuration:0.##}s, scroll={scrollDuration:0.##}s, speed={speedMultiplier:0.##}).");

        float holdElapsed = 0f;
        while (holdElapsed < holdDuration && elapsed < timeout)
        {
            if (IsCreditsSkipRequested())
            {
                skipRequested = true;
                break;
            }

            holdElapsed += Time.unscaledDeltaTime;
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!skipRequested)
        {
            if (elapsed >= timeout)
            {
                timeoutReached = true;
            }
            else
            {
                Debug.Log("[BossHealth] After-credits: static phase done.");
            }
        }

        bool scrollCompleted = false;
        if (!skipRequested && !timeoutReached)
        {
            float scrollElapsed = 0f;
            while (scrollElapsed < scrollDuration && elapsed < timeout)
            {
                if (IsCreditsSkipRequested())
                {
                    skipRequested = true;
                    break;
                }

                scrollElapsed += Time.unscaledDeltaTime;
                elapsed += Time.unscaledDeltaTime;

                float progress = scrollDuration > Mathf.Epsilon ? Mathf.Clamp01(scrollElapsed / scrollDuration) : 1f;
                creditsRect.anchoredPosition = new Vector2(0f, Mathf.Lerp(startY, endY, progress));
                yield return null;
            }

            if (!skipRequested)
            {
                if (elapsed >= timeout && scrollElapsed < scrollDuration)
                {
                    timeoutReached = true;
                }
                else
                {
                    scrollCompleted = true;
                    creditsRect.anchoredPosition = new Vector2(0f, endY);
                }
            }
        }

        if (scrollCompleted)
            Debug.Log("[BossHealth] After-credits: scroll phase done.");
        else
            Debug.Log($"[BossHealth] After-credits: scroll exit ({(skipRequested ? "skip" : "timeout")}).");

        if (creditsTextObject != null)
            Destroy(creditsTextObject);
        if (temporaryBlackPanelObject != null)
            Destroy(temporaryBlackPanelObject);
        if (temporaryCanvasObject != null)
            Destroy(temporaryCanvasObject);
        if (hadFade)
        {
            fade.alpha = previousFadeAlpha;
            fade.gameObject.SetActive(previousFadeActive);
        }
    }

    private IEnumerator PlayBossOutroThenDestroy()
    {
        bossOutroCutsceneDirector = EnsureBossCutsceneDirector();
        if (bossOutroCutsceneDirector != null)
        {
            if (bossOutroCutsceneDirector.IsPlaying)
            {
                Debug.LogWarning("[BossHealth] Director was already playing before boss outro setup. Forcing skip.");
                bossOutroCutsceneDirector.SkipCutscene();
                yield return WaitForDirectorToStop(bossOutroCutsceneDirector, 3f);
            }

            bossOutroCutsceneDirector.SetUseTimelineForRuntime(false);
            bossOutroCutsceneDirector.SetRuntimeCameraMovementEnabled(false);
            bossOutroCutsceneDirector.ConfigureRuntimeCutscene(
                "boss_outro",
                BuildDefaultBossOutroNarrative(),
                allowCheckpointSkip: false,
                allowWatchedSkip: false,
                replaceSubtitles: true);

            Debug.Log("[BossHealth] Boss outro: starting cutscene.");
            if (!bossOutroCutsceneDirector.IsPlaying)
                bossOutroCutsceneDirector.StartCutscene();

            float timeout = Mathf.Max(0f, bossOutroTimeout);
            float elapsed = 0f;
            while (bossOutroCutsceneDirector.IsPlaying && elapsed < timeout)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (bossOutroCutsceneDirector.IsPlaying)
            {
                Debug.LogWarning($"[BossHealth] Boss outro timeout after {timeout:0.##}s. Forcing skip before credits.");
                bossOutroCutsceneDirector.SkipCutscene();
                yield return WaitForDirectorToStop(bossOutroCutsceneDirector, 3f);
            }

            if (playAfterCreditsOnBossDeath)
            {
                if (bossOutroCutsceneDirector.IsPlaying)
                {
                    Debug.LogWarning("[BossHealth] Director still playing before after-credits. Forcing skip.");
                    bossOutroCutsceneDirector.SkipCutscene();
                    yield return WaitForDirectorToStop(bossOutroCutsceneDirector, 3f);
                }

                yield return WaitForDirectorToStop(bossOutroCutsceneDirector, 3f);
                Debug.Log("[BossHealth] After-credits: starting black-screen credits roll.");
                yield return PlayBlackScreenCreditsRoll();
            }
        }

        Destroy(gameObject);
    }

    private IEnumerator WaitForDirectorToStop(CutsceneDirector director, float timeout)
    {
        if (director == null)
            yield break;

        float waitTimeout = Mathf.Max(0f, timeout);
        float elapsed = 0f;
        while (director.IsPlaying && elapsed < waitTimeout)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }
}
