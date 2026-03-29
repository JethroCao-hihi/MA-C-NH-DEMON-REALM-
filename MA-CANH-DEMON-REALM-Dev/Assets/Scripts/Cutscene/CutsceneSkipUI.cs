using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// UI hiển thị gợi ý Skip và progress bar cho cutscene.
/// FIX: Added OnDestroy for event cleanup, unscaledDeltaTime for animations
/// </summary>
public class CutsceneSkipUI : MonoBehaviour
{
    #region === SETTINGS ===
    [Header("Skip Hint")]
    [SerializeField] private GameObject skipHintContainer;
    [SerializeField] private TextMeshProUGUI skipHintText;
    [SerializeField] private string skipHintMessage = "Nhấn SPACE hoặc ESC để bỏ qua";
    [SerializeField] private float showDelay = 2f;
    [SerializeField] private float blinkInterval = 0.8f;

    [Header("Progress Bar")]
    [SerializeField] private GameObject progressBarContainer;
    [SerializeField] private Slider progressSlider;
    #endregion

    #region === STATE ===
    private bool isShowing;
    private bool hasHandledEnd; // Guard against double-handling
    private bool isSubscribed;  // Track subscription state
    private Coroutine blinkCoroutine;
    private Coroutine showHintCoroutine;
    #endregion

    #region === RUNTIME SETUP API ===
    /// <summary>
    /// Inject UI references at runtime (used by CutsceneSetup).
    /// </summary>
    public void SetupReferences(GameObject skipContainer, TextMeshProUGUI skipText, 
                                 GameObject progressContainer, Slider slider)
    {
        skipHintContainer = skipContainer;
        skipHintText = skipText;
        progressBarContainer = progressContainer;
        progressSlider = slider;
        Debug.Log("[CutsceneSkipUI] References injected via SetupReferences()");
    }
    #endregion

    #region === UNITY CALLBACKS ===
    private void OnEnable()
    {
        SubscribeToEvents();
    }

    private void OnDisable()
    {
        UnsubscribeFromEvents();
    }

    private void OnDestroy()
    {
        // FIX: Ensure cleanup even if destroyed while disabled
        UnsubscribeFromEvents();
        StopAllRunningCoroutines();
    }

    private void SubscribeToEvents()
    {
        if (isSubscribed) return;
        CutsceneDirector.OnCutsceneStarted += OnCutsceneStarted;
        CutsceneDirector.OnCutsceneEnded += OnCutsceneEndedHandler;
        CutsceneDirector.OnCutsceneSkipped += OnCutsceneSkippedHandler;
        isSubscribed = true;
    }

    private void UnsubscribeFromEvents()
    {
        if (!isSubscribed) return;
        CutsceneDirector.OnCutsceneStarted -= OnCutsceneStarted;
        CutsceneDirector.OnCutsceneEnded -= OnCutsceneEndedHandler;
        CutsceneDirector.OnCutsceneSkipped -= OnCutsceneSkippedHandler;
        isSubscribed = false;
    }

    private void Start()
    {
        // Hide initially
        if (skipHintContainer != null)
            skipHintContainer.SetActive(false);

        if (progressBarContainer != null)
            progressBarContainer.SetActive(false);
    }

    private void Update()
    {
        if (!isShowing) return;

        // FIX: Cache instance to avoid race condition
        var director = CutsceneDirector.Instance;
        if (progressSlider != null && director != null)
        {
            if (director.IsHoldToSkipEnabled && director.IsSkippingByHold)
            {
                progressSlider.value = director.SkipHoldProgress;
            }
            else
            {
                progressSlider.value = director.Progress;
            }
        }
    }
    #endregion

    #region === EVENT HANDLERS ===
    private void OnCutsceneStarted()
    {
        isShowing = true;
        hasHandledEnd = false; // Reset guard for new cutscene session
        
        showHintCoroutine = StartCoroutine(ShowSkipHintDelayed());

        if (progressBarContainer != null)
        {
            progressBarContainer.SetActive(true);
            if (progressSlider != null)
                progressSlider.value = 0f;
        }

        if (skipHintText != null)
        {
            skipHintText.text = CutsceneDirector.Instance != null && CutsceneDirector.Instance.IsHoldToSkipEnabled
                ? "Giữ SPACE hoặc ESC để bỏ qua"
                : skipHintMessage;
        }

        Debug.Log("[CutsceneSkipUI] Cutscene started - UI shown");
    }

    /// <summary>
    /// Called when cutscene is skipped (fires BEFORE OnCutsceneEnded)
    /// </summary>
    private void OnCutsceneSkippedHandler()
    {
        // Do cleanup immediately on skip - mark as handled
        if (hasHandledEnd) return;
        hasHandledEnd = true;
        
        Debug.Log("[CutsceneSkipUI] Cutscene skipped - hiding UI");
        CleanupUI();
    }

    /// <summary>
    /// Called when cutscene ends naturally or after skip sequence completes
    /// </summary>
    private void OnCutsceneEndedHandler()
    {
        // Guard: if already handled by skip, don't double-process
        if (hasHandledEnd)
        {
            Debug.Log("[CutsceneSkipUI] OnCutsceneEnded skipped (already handled by skip)");
            return;
        }
        hasHandledEnd = true;
        
        Debug.Log("[CutsceneSkipUI] Cutscene ended naturally - hiding UI");
        CleanupUI();
    }

    /// <summary>
    /// Common cleanup logic for UI hiding
    /// </summary>
    private void CleanupUI()
    {
        isShowing = false;
        StopAllRunningCoroutines();

        if (skipHintContainer != null)
            skipHintContainer.SetActive(false);

        if (progressBarContainer != null)
            progressBarContainer.SetActive(false);
    }

    private void StopAllRunningCoroutines()
    {
        if (showHintCoroutine != null)
        {
            StopCoroutine(showHintCoroutine);
            showHintCoroutine = null;
        }

        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }
    }
    #endregion

    #region === SKIP HINT ===
    private IEnumerator ShowSkipHintDelayed()
    {
        // FIX: Use WaitForSecondsRealtime for pause-safe timing
        yield return new WaitForSecondsRealtime(showDelay);

        if (!isShowing) yield break;

        if (skipHintContainer != null)
        {
            skipHintContainer.SetActive(true);

            if (skipHintText != null)
            {
                skipHintText.text = CutsceneDirector.Instance != null && CutsceneDirector.Instance.IsHoldToSkipEnabled
                    ? "Giữ SPACE hoặc ESC để bỏ qua"
                    : skipHintMessage;
            }
        }

        // Start blinking
        blinkCoroutine = StartCoroutine(BlinkSkipHint());
    }

    private IEnumerator BlinkSkipHint()
    {
        while (isShowing && skipHintText != null)
        {
            // FIX: Use unscaledDeltaTime for pause-safe animation
            // Fade out
            float elapsed = 0f;
            Color startColor = skipHintText.color;
            Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0.3f);

            while (elapsed < blinkInterval * 0.5f)
            {
                elapsed += Time.unscaledDeltaTime;
                skipHintText.color = Color.Lerp(startColor, endColor, elapsed / (blinkInterval * 0.5f));
                yield return null;
            }

            // Fade in
            elapsed = 0f;
            while (elapsed < blinkInterval * 0.5f)
            {
                elapsed += Time.unscaledDeltaTime;
                skipHintText.color = Color.Lerp(endColor, startColor, elapsed / (blinkInterval * 0.5f));
                yield return null;
            }
        }
    }
    #endregion
}
