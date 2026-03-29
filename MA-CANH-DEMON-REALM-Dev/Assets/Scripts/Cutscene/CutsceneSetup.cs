using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Helper class để tạo UI cutscene trong runtime nếu chưa có.
/// Attach vào GameObject rỗng trong scene Game1.
/// Checks for existing components to avoid duplicates.
/// FIX: Added proper null checks and lifecycle handling.
/// </summary>
public class CutsceneSetup : MonoBehaviour
{
    [Header("Auto Create UI")]
    [SerializeField] private bool autoCreateUI = true;
    
    [Header("Optional: Manual References")]
    [SerializeField] private Canvas existingCanvas;
    [SerializeField] private TMP_FontAsset customFont;

    private void Awake()
    {
        // FIX: Check if this object is being destroyed
        if (this == null || gameObject == null) return;
        
        if (autoCreateUI)
        {
            SetupCutsceneUI();
        }
    }

    private void SetupCutsceneUI()
    {
        // FIX: Additional null guard
        if (this == null || !this) return;
        
        Debug.Log("[CutsceneSetup] Setting up cutscene UI...");

        // === CHECK FOR EXISTING COMPONENTS ===
        CutsceneDirector existingDirector = GetComponent<CutsceneDirector>();
        if (existingDirector == null)
            existingDirector = FindFirstObjectByType<CutsceneDirector>();

        // Check if CutsceneUI already exists in scene
        GameObject existingCutsceneUI = GameObject.Find("CutsceneUI");
        if (existingCutsceneUI != null)
        {
            Debug.Log("[CutsceneSetup] CutsceneUI already exists, skipping creation. Reusing existing.");

            Canvas parentCanvas = existingCutsceneUI.GetComponentInParent<Canvas>();
            if (parentCanvas == null || parentCanvas.renderMode == RenderMode.WorldSpace)
            {
                Canvas targetCanvas = FindBestCutsceneCanvas();
                if (targetCanvas == null || targetCanvas.renderMode == RenderMode.WorldSpace)
                {
                    targetCanvas = GetOrCreateCutsceneCanvas();
                }

                if (targetCanvas != null && existingCutsceneUI.transform.parent != targetCanvas.transform)
                {
                    existingCutsceneUI.transform.SetParent(targetCanvas.transform, false);
                }

                parentCanvas = targetCanvas;
            }

            if (parentCanvas != null && !parentCanvas.gameObject.activeSelf)
            {
                parentCanvas.gameObject.SetActive(true);
            }

            // Just ensure director has references if needed
            if (existingDirector != null)
            {
                CanvasGroup existingFade = existingCutsceneUI.transform.Find("FadePanel")?.GetComponent<CanvasGroup>();
                if (existingFade != null)
                {
                    existingDirector.SetupReferences(existingCutsceneUI, existingFade);
                }
            }

            // Ensure local cutscene components exist under existing hierarchy
            if (existingCutsceneUI.GetComponent<CutsceneSkipUI>() == null)
                existingCutsceneUI.AddComponent<CutsceneSkipUI>();

            Transform existingSubtitleContainer = existingCutsceneUI.transform.Find("SubtitleContainer");
            if (existingSubtitleContainer != null)
            {
                TextMeshProUGUI existingSubtitleText = existingSubtitleContainer.Find("SubtitleText")?.GetComponent<TextMeshProUGUI>();
                CanvasGroup existingSubtitleCanvasGroup = existingSubtitleContainer.GetComponent<CanvasGroup>();
                SubtitleController existingSubtitleController = existingSubtitleContainer.GetComponent<SubtitleController>();
                if (existingSubtitleController == null)
                {
                    existingSubtitleController = existingSubtitleContainer.gameObject.AddComponent<SubtitleController>();
                }

                if (existingSubtitleText != null && existingSubtitleCanvasGroup != null)
                {
                    existingSubtitleController.SetupReferences(
                        existingSubtitleText,
                        existingSubtitleCanvasGroup,
                        existingSubtitleContainer.gameObject);
                }
            }

            enabled = false;
            return;
        }

        // Find or create canvas
        Canvas canvas = existingCanvas;
        if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
        {
            canvas = null;
        }
        if (canvas == null)
        {
            canvas = FindBestCutsceneCanvas();
        }

        if (canvas == null || canvas.renderMode == RenderMode.WorldSpace)
        {
            canvas = GetOrCreateCutsceneCanvas();
        }

        if (canvas != null && !canvas.gameObject.activeSelf)
        {
            canvas.gameObject.SetActive(true);
        }

        // Find font
        TMP_FontAsset font = customFont;
        if (font == null)
        {
            font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (font == null)
            {
                // Try to find any TMP font in project
                TMP_FontAsset[] fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                if (fonts.Length > 0)
                    font = fonts[0];
            }
        }

        // Create CutsceneDirector if not exists
        CutsceneDirector director = existingDirector;
        if (director == null)
        {
            GameObject directorObj = new GameObject("CutsceneDirector");
            director = directorObj.AddComponent<CutsceneDirector>();
            Debug.Log("[CutsceneSetup] Created CutsceneDirector");
        }
        else
        {
            Debug.Log("[CutsceneSetup] Reusing existing CutsceneDirector");
        }

        // Create Cutscene UI Container
        GameObject cutsceneUI = new GameObject("CutsceneUI");
        cutsceneUI.transform.SetParent(canvas.transform, false);
        RectTransform cutsceneUIRect = cutsceneUI.AddComponent<RectTransform>();
        cutsceneUIRect.anchorMin = Vector2.zero;
        cutsceneUIRect.anchorMax = Vector2.one;
        cutsceneUIRect.sizeDelta = Vector2.zero;

        // === FADE PANEL ===
        GameObject fadePanel = new GameObject("FadePanel");
        fadePanel.transform.SetParent(cutsceneUI.transform, false);
        RectTransform fadeRect = fadePanel.AddComponent<RectTransform>();
        fadeRect.anchorMin = Vector2.zero;
        fadeRect.anchorMax = Vector2.one;
        fadeRect.sizeDelta = Vector2.zero;
        
        Image fadeImage = fadePanel.AddComponent<Image>();
        fadeImage.color = Color.black;
        
        CanvasGroup fadeCanvasGroup = fadePanel.AddComponent<CanvasGroup>();
        fadeCanvasGroup.alpha = 1f;
        fadeCanvasGroup.blocksRaycasts = false;

        // === SUBTITLE CONTAINER ===
        GameObject subtitleContainer = new GameObject("SubtitleContainer");
        subtitleContainer.transform.SetParent(cutsceneUI.transform, false);
        RectTransform subtitleRect = subtitleContainer.AddComponent<RectTransform>();
        subtitleRect.anchorMin = new Vector2(0.1f, 0.05f);
        subtitleRect.anchorMax = new Vector2(0.9f, 0.2f);
        subtitleRect.sizeDelta = Vector2.zero;
        
        CanvasGroup subtitleCanvasGroup = subtitleContainer.AddComponent<CanvasGroup>();

        // Subtitle background
        GameObject subtitleBg = new GameObject("SubtitleBackground");
        subtitleBg.transform.SetParent(subtitleContainer.transform, false);
        RectTransform bgRect = subtitleBg.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = new Vector2(20, 10);
        bgRect.anchoredPosition = Vector2.zero;
        
        Image bgImage = subtitleBg.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.7f);

        // Subtitle text
        GameObject subtitleTextObj = new GameObject("SubtitleText");
        subtitleTextObj.transform.SetParent(subtitleContainer.transform, false);
        RectTransform textRect = subtitleTextObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = new Vector2(-20, -10);
        textRect.anchoredPosition = Vector2.zero;
        
        TextMeshProUGUI subtitleText = subtitleTextObj.AddComponent<TextMeshProUGUI>();
        subtitleText.alignment = TextAlignmentOptions.Center;
        subtitleText.fontSize = 32;
        subtitleText.color = Color.white;
        subtitleText.text = "";
        if (font != null) subtitleText.font = font;

        // === SKIP HINT ===
        GameObject skipHintContainer = new GameObject("SkipHintContainer");
        skipHintContainer.transform.SetParent(cutsceneUI.transform, false);
        RectTransform skipRect = skipHintContainer.AddComponent<RectTransform>();
        skipRect.anchorMin = new Vector2(0.5f, 0.92f);
        skipRect.anchorMax = new Vector2(0.5f, 0.98f);
        skipRect.sizeDelta = new Vector2(400, 40);
        skipRect.anchoredPosition = Vector2.zero;

        GameObject skipTextObj = new GameObject("SkipHintText");
        skipTextObj.transform.SetParent(skipHintContainer.transform, false);
        RectTransform skipTextRect = skipTextObj.AddComponent<RectTransform>();
        skipTextRect.anchorMin = Vector2.zero;
        skipTextRect.anchorMax = Vector2.one;
        skipTextRect.sizeDelta = Vector2.zero;
        
        TextMeshProUGUI skipText = skipTextObj.AddComponent<TextMeshProUGUI>();
        skipText.alignment = TextAlignmentOptions.Center;
        skipText.fontSize = 20;
        skipText.color = new Color(1, 1, 1, 0.7f);
        skipText.text = "Nhấn SPACE hoặc ESC để bỏ qua";
        if (font != null) skipText.font = font;

        // === PROGRESS BAR ===
        GameObject progressContainer = new GameObject("ProgressBarContainer");
        progressContainer.transform.SetParent(cutsceneUI.transform, false);
        RectTransform progressRect = progressContainer.AddComponent<RectTransform>();
        progressRect.anchorMin = new Vector2(0.2f, 0.02f);
        progressRect.anchorMax = new Vector2(0.8f, 0.04f);
        progressRect.sizeDelta = Vector2.zero;

        // Progress background
        GameObject progressBg = new GameObject("Background");
        progressBg.transform.SetParent(progressContainer.transform, false);
        RectTransform progressBgRect = progressBg.AddComponent<RectTransform>();
        progressBgRect.anchorMin = Vector2.zero;
        progressBgRect.anchorMax = Vector2.one;
        progressBgRect.sizeDelta = Vector2.zero;
        
        Image progressBgImage = progressBg.AddComponent<Image>();
        progressBgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);

        // Progress slider
        Slider progressSlider = progressContainer.AddComponent<Slider>();
        progressSlider.minValue = 0f;
        progressSlider.maxValue = 1f;
        progressSlider.interactable = false;
        progressSlider.transition = Selectable.Transition.None;

        // Fill area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(progressContainer.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.sizeDelta = Vector2.zero;

        // Fill
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(0, 1);
        fillRect.sizeDelta = Vector2.zero;
        
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.8f, 0.2f, 0.2f, 0.8f);

        progressSlider.fillRect = fillRect;

        // === ADD COMPONENTS ===
        SubtitleController subtitleController = subtitleContainer.AddComponent<SubtitleController>();
        Debug.Log("[CutsceneSetup] Created SubtitleController");
        // Inject references using public method (NO REFLECTION)
        subtitleController.SetupReferences(subtitleText, subtitleCanvasGroup, subtitleContainer);

        CutsceneSkipUI skipUI = cutsceneUI.AddComponent<CutsceneSkipUI>();
        Debug.Log("[CutsceneSetup] Created CutsceneSkipUI");
        // Inject references using public method (NO REFLECTION)
        skipUI.SetupReferences(skipHintContainer, skipText, progressContainer, progressSlider);

        // Inject references to CutsceneDirector using public method (NO REFLECTION)
        director.SetupReferences(cutsceneUI, fadeCanvasGroup);

        Debug.Log("[CutsceneSetup] Cutscene UI setup complete!");

        // Disable this setup script after initialization
        enabled = false;
    }

    private Canvas FindBestCutsceneCanvas()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        Canvas cameraCanvas = null;

        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas candidate = canvases[i];
            if (candidate == null) continue;

            if (candidate.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return candidate;
            }

            if (candidate.renderMode == RenderMode.ScreenSpaceCamera && cameraCanvas == null)
            {
                cameraCanvas = candidate;
            }
        }

        return cameraCanvas;
    }

    private Canvas GetOrCreateCutsceneCanvas()
    {
        GameObject canvasObj = GameObject.Find("CutsceneCanvas");
        Canvas canvas = canvasObj != null ? canvasObj.GetComponent<Canvas>() : null;

        if (canvas == null)
        {
            canvasObj = new GameObject("CutsceneCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = Mathf.Max(canvas.sortingOrder, 100);

        if (canvasObj != null && !canvasObj.activeSelf)
        {
            canvasObj.SetActive(true);
        }

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        if (canvasObj.GetComponent<GraphicRaycaster>() == null)
            canvasObj.AddComponent<GraphicRaycaster>();

        return canvas;
    }
}
