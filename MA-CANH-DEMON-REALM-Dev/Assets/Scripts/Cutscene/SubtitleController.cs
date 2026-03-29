using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Hiển thị subtitle cho cutscene với hiệu ứng typewriter và fade.
/// ENHANCED: Cinematic text effects with better visual presentation.
/// </summary>
public class SubtitleController : MonoBehaviour
{
    #region === SINGLETON ===
    public static SubtitleController Instance { get; private set; }
    #endregion

    #region === SETTINGS ===
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private CanvasGroup subtitleCanvasGroup;
    [SerializeField] private GameObject subtitleContainer;

    [Header("Animation Settings")]
    [SerializeField] private bool useTypewriter = true;
    [SerializeField] private float typewriterSpeed = 0.025f; // Faster for cinematic feel
    [SerializeField] private float fadeInDuration = 0.4f;
    [SerializeField] private float fadeOutDuration = 0.5f;

    [Header("Cinematic Settings")]
    [SerializeField] private bool useCinematicStyle = true;
    [SerializeField] private float cinematicScale = 1.05f; // Subtle scale animation
    [SerializeField] private float scaleAnimDuration = 0.3f;

    [Header("Styling")]
    [SerializeField] private Color subtitleColor = Color.white;
    [SerializeField] private Color shadowColor = new Color(0, 0, 0, 0.8f);
    #endregion

    #region === STATE ===
    private Coroutine currentSubtitleCoroutine;
    private Coroutine scaleCoroutine;
    private bool isShowing;
    private Vector3 originalScale = Vector3.one;
    #endregion

    #region === PROPERTIES ===
    public bool IsShowing => isShowing;
    public bool LastShowUsedDirectFallback { get; private set; }
    #endregion

    #region === RUNTIME SETUP API ===
    /// <summary>
    /// Inject UI references at runtime (used by CutsceneSetup).
    /// </summary>
    public void SetupReferences(TextMeshProUGUI text, CanvasGroup canvasGroup, GameObject container)
    {
        subtitleText = text;
        subtitleCanvasGroup = canvasGroup;
        subtitleContainer = container;
        Debug.Log("[SubtitleController] References injected via SetupReferences()");
    }
    #endregion

    #region === UNITY CALLBACKS ===
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return; // FIX: Return immediately after destroy
        }

        // Initialize UI
        InitializeUI();
    }

    private void OnDestroy()
    {
        // FIX: Stop all coroutines on destroy
        StopAllCoroutines();
        
        if (Instance == this)
            Instance = null;
    }
    #endregion

    #region === INITIALIZATION ===
    private void InitializeUI()
    {
        // Auto-create UI if not assigned
        if (subtitleText == null)
        {
            subtitleText = GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (subtitleCanvasGroup == null)
        {
            subtitleCanvasGroup = GetComponentInChildren<CanvasGroup>(true);
            if (subtitleCanvasGroup == null && subtitleContainer != null)
            {
                subtitleCanvasGroup = subtitleContainer.AddComponent<CanvasGroup>();
            }
        }

        // Store original scale for animations
        if (subtitleContainer != null)
        {
            originalScale = subtitleContainer.transform.localScale;
        }

        // Hide initially
        if (subtitleContainer != null)
        {
            if (subtitleContainer != gameObject)
                subtitleContainer.SetActive(false);
        }

        if (subtitleCanvasGroup != null)
            subtitleCanvasGroup.alpha = 0f;

        if (subtitleText != null)
        {
            subtitleText.text = "";
            subtitleText.color = subtitleColor;
        }
    }
    #endregion

    #region === PUBLIC API ===
    /// <summary>
    /// Hiển thị subtitle với thời gian xác định
    /// </summary>
    public void ShowSubtitle(string text, float duration)
    {
        LastShowUsedDirectFallback = false;

        if (string.IsNullOrEmpty(text))
        {
            Debug.LogWarning("[SubtitleController] Text rỗng, bỏ qua.");
            return;
        }

        bool forcedActivation = false;

        if (subtitleContainer != null)
        {
            ForceActivateParentChain(subtitleContainer.transform, ref forcedActivation);
        }

        ForceActivateParentChain(transform, ref forcedActivation);

        if (!this.enabled)
        {
            this.enabled = true;
            forcedActivation = true;
        }

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
            forcedActivation = true;
        }

        if (subtitleContainer != null && !subtitleContainer.activeSelf)
        {
            subtitleContainer.SetActive(true);
            forcedActivation = true;
        }

        if (forcedActivation)
        {
            Debug.LogWarning("[SubtitleController] Forced activation to show subtitle.");
        }

        Debug.Log($"[SubtitleController] Showing: \"{text}\" for {duration}s");

        // Stop previous subtitle
        if (currentSubtitleCoroutine != null)
        {
            StopCoroutine(currentSubtitleCoroutine);
            currentSubtitleCoroutine = null;
        }
        
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
            scaleCoroutine = null;
        }

        if (!isActiveAndEnabled)
        {
            LastShowUsedDirectFallback = true;
            SetSubtitleDirect(text);

            if (isActiveAndEnabled)
            {
                currentSubtitleCoroutine = StartCoroutine(HideSubtitleAfterDelay(duration));
            }
            else
            {
                Debug.LogWarning("[SubtitleController] Inactive after activation attempt; keeping subtitle text.");
            }

            return;
        }

        LastShowUsedDirectFallback = false;
        currentSubtitleCoroutine = StartCoroutine(CinematicSubtitleRoutine(text, duration));
    }

    /// <summary>
    /// Ẩn subtitle ngay lập tức
    /// </summary>
    public void HideSubtitle()
    {
        if (currentSubtitleCoroutine != null)
        {
            StopCoroutine(currentSubtitleCoroutine);
            currentSubtitleCoroutine = null;
        }
        
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
            scaleCoroutine = null;
        }

        if (!isActiveAndEnabled)
        {
            SetSubtitleDirect(string.Empty);
            return;
        }

        StartCoroutine(FadeOutSubtitle());
    }

    /// <summary>
    /// Đặt text subtitle trực tiếp (không animation)
    /// </summary>
    public void SetSubtitleDirect(string text)
    {
        if (subtitleText != null)
            subtitleText.text = text;

        if (subtitleContainer != null)
        {
            bool shouldShowContainer = !string.IsNullOrEmpty(text);
            if (subtitleContainer != gameObject || shouldShowContainer)
                subtitleContainer.SetActive(shouldShowContainer);
                
            // Reset scale
            subtitleContainer.transform.localScale = originalScale;
        }

        if (subtitleCanvasGroup != null)
            subtitleCanvasGroup.alpha = string.IsNullOrEmpty(text) ? 0f : 1f;

        isShowing = !string.IsNullOrEmpty(text);
    }
    #endregion

    #region === SUBTITLE COROUTINES ===
    private IEnumerator HideSubtitleAfterDelay(float duration)
    {
        if (duration > 0f)
            yield return new WaitForSecondsRealtime(duration);

        if (isActiveAndEnabled)
            HideSubtitle();
    }

    /// <summary>
    /// Enhanced cinematic subtitle routine with scale and fade effects
    /// </summary>
    private IEnumerator CinematicSubtitleRoutine(string text, float duration)
    {
        isShowing = true;

        // Show container
        if (subtitleContainer != null)
        {
            if (subtitleContainer != gameObject)
                subtitleContainer.SetActive(true);
                
            // Start with slightly smaller scale for cinematic pop-in
            if (useCinematicStyle)
            {
                subtitleContainer.transform.localScale = originalScale * 0.95f;
            }
        }

        // Fade in with scale animation
        if (useCinematicStyle && subtitleContainer != null)
        {
            scaleCoroutine = StartCoroutine(AnimateScale(originalScale * 0.95f, originalScale, scaleAnimDuration));
        }
        yield return FadeInSubtitle();

        // Typewriter or instant
        if (useTypewriter)
        {
            yield return CinematicTypewriterEffect(text);
        }
        else
        {
            if (subtitleText != null)
                subtitleText.text = text;
        }

        // Wait for display duration (minus fade times)
        float waitTime = duration - fadeInDuration - fadeOutDuration;
        if (waitTime > 0)
        {
            yield return new WaitForSecondsRealtime(waitTime);
        }

        // Fade out with subtle scale
        if (useCinematicStyle && subtitleContainer != null)
        {
            scaleCoroutine = StartCoroutine(AnimateScale(originalScale, originalScale * cinematicScale, fadeOutDuration));
        }
        yield return FadeOutSubtitle();

        isShowing = false;
        currentSubtitleCoroutine = null;
    }

    /// <summary>
    /// Enhanced typewriter with variable speed for punctuation
    /// </summary>
    private IEnumerator CinematicTypewriterEffect(string fullText)
    {
        if (subtitleText == null) yield break;

        subtitleText.text = "";
        
        foreach (char c in fullText)
        {
            subtitleText.text += c;
            
            // Variable speed for dramatic effect
            float delay = typewriterSpeed;
            if (c == '.' || c == '!' || c == '?')
                delay = typewriterSpeed * 4f; // Longer pause at sentence end
            else if (c == ',' || c == ';' || c == ':')
                delay = typewriterSpeed * 2f; // Medium pause at commas
            else if (c == '—' || c == '-')
                delay = typewriterSpeed * 3f; // Dramatic pause at dashes
                
            yield return new WaitForSecondsRealtime(delay);
        }
    }

    private IEnumerator AnimateScale(Vector3 from, Vector3 to, float duration)
    {
        if (subtitleContainer == null) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / duration);
            subtitleContainer.transform.localScale = Vector3.Lerp(from, to, t);
            yield return null;
        }
        
        subtitleContainer.transform.localScale = to;
    }

    private IEnumerator FadeInSubtitle()
    {
        if (subtitleCanvasGroup == null) yield break;

        subtitleCanvasGroup.alpha = 0f;
        float elapsed = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            // Use ease-out for smooth appearance
            float t = 1f - Mathf.Pow(1f - (elapsed / fadeInDuration), 2f);
            subtitleCanvasGroup.alpha = t;
            yield return null;
        }

        subtitleCanvasGroup.alpha = 1f;
    }

    private IEnumerator FadeOutSubtitle()
    {
        if (subtitleCanvasGroup == null)
        {
            if (subtitleContainer != null)
            {
                if (subtitleContainer != gameObject)
                    subtitleContainer.SetActive(false);
            }
            yield break;
        }

        float startAlpha = subtitleCanvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            // Use ease-in for smooth disappearance
            float t = Mathf.Pow(elapsed / fadeOutDuration, 2f);
            subtitleCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
            yield return null;
        }

        subtitleCanvasGroup.alpha = 0f;

        if (subtitleContainer != null)
        {
            if (subtitleContainer != gameObject)
                subtitleContainer.SetActive(false);
                
            // Reset scale
            subtitleContainer.transform.localScale = originalScale;
        }

        if (subtitleText != null)
            subtitleText.text = "";

        isShowing = false;
    }
    #endregion

    #region === ACTIVATION HELPERS ===
    private static void ForceActivateParentChain(Transform start, ref bool forcedActivation)
    {
        Transform current = start;
        while (current != null)
        {
            if (!current.gameObject.activeSelf)
            {
                current.gameObject.SetActive(true);
                forcedActivation = true;
            }

            current = current.parent;
        }
    }
    #endregion
}
