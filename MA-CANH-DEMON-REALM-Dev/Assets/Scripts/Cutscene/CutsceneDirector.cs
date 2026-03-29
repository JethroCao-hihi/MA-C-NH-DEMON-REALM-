using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Điều khiển chính cutscene mở đầu game.
/// Hỗ trợ camera movement, subtitle, letterbox cinematic, và skip.
/// ENHANCED: Cinematic film-quality experience với letterbox, dramatic timing, và visual effects.
/// </summary>
public class CutsceneDirector : MonoBehaviour
{
    #region === SINGLETON ===
    public static CutsceneDirector Instance { get; private set; }
    #endregion

    #region === EVENTS ===
    public static event Action OnCutsceneStarted;
    public static event Action OnCutsceneEnded;
    public static event Action OnCutsceneSkipped;
    #endregion

    #region === SETTINGS ===
    [Header("Cutscene Settings")]
    [SerializeField] private float cutsceneDuration = 35f; // Increased for more dramatic pacing
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool disablePlayerDuringCutscene = true;
    [SerializeField] private string cutsceneId = "game1_intro";
    [SerializeField] private bool autoSkipIfWatched = false;
    [SerializeField] private bool skipCutsceneIfCheckpointExists = true;

    [Header("Skip Settings")]
    [SerializeField] private bool useHoldToSkip = true;
    [SerializeField] private float holdToSkipDuration = 1.2f;

    [Header("Cinematic Settings")]
    [SerializeField] private bool enableLetterbox = true;
    [SerializeField] private float letterboxSize = 0.12f; // 12% top and bottom
    [SerializeField] private float letterboxAnimDuration = 0.8f;
    [SerializeField] private bool enableVignette = true;
    [SerializeField] private bool enableDramaticPause = false;

    [Header("Timeline Playback (Optional)")]
    [SerializeField] private bool useTimelineIfAvailable = true;
    [SerializeField] private PlayableDirector playableDirector;

    [Header("Camera Movement")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private CameraWaypoint[] cameraWaypoints;

    [Header("Subtitles")]
    [SerializeField] private SubtitleEntry[] subtitles;

    [Header("Audio")]
    [SerializeField] private AudioSource cutsceneAudioSource;
    [SerializeField] private AudioClip cutsceneMusic;
    [SerializeField] private float musicFadeOutDuration = 1f;

    [Header("UI References")]
    [SerializeField] private GameObject cutsceneUI;
    [SerializeField] private CanvasGroup fadePanel;
    [SerializeField] private float fadeInDuration = 0.4f;
    [SerializeField] private float fadeOutDuration = 0.4f;

    [Header("Letterbox UI (Auto-created)")]
    [SerializeField] private RectTransform letterboxTop;
    [SerializeField] private RectTransform letterboxBottom;
    [SerializeField] private CanvasGroup vignetteOverlay;
    #endregion

    #region === RUNTIME SETUP API ===
    /// <summary>
    /// Inject UI references at runtime (used by CutsceneSetup).
    /// Call before StartCutscene().
    /// </summary>
    public void SetupReferences(GameObject uiRoot, CanvasGroup fade)
    {
        cutsceneUI = uiRoot;
        fadePanel = fade;
        Debug.Log("[CutsceneDirector] References injected via SetupReferences()");
    }

    public void SetPlayOnStartForRuntime(bool value)
    {
        playOnStart = value;
    }

    public void SetUseTimelineForRuntime(bool enabled)
    {
        hasRuntimeTimelineOverride = true;
        runtimeUseTimelineOverride = enabled;
    }

    public void SetRuntimeCameraMovementEnabled(bool enabled)
    {
        hasRuntimeCameraMovementOverride = true;
        runtimeCameraMovementEnabled = enabled;
    }

    public void ApplySubtitlesIfEmpty(SubtitleEntry[] entries, bool extendDurationToFit = true)
    {
        if (subtitles != null && subtitles.Length > 0)
            return;

        if (entries == null || entries.Length == 0)
            return;

        subtitles = entries;

        if (!extendDurationToFit)
            return;

        float requiredDuration = 0f;
        for (int i = 0; i < entries.Length; i++)
        {
            float candidateDuration = entries[i].triggerTime + entries[i].displayDuration + 1f;
            if (candidateDuration > requiredDuration)
                requiredDuration = candidateDuration;
        }

        if (requiredDuration > cutsceneDuration)
            cutsceneDuration = requiredDuration;
    }

    public void ConfigureRuntimeCutscene(
        string runtimeCutsceneId,
        SubtitleEntry[] runtimeSubtitles,
        bool allowCheckpointSkip,
        bool allowWatchedSkip,
        bool replaceSubtitles = true,
        bool extendDurationToFit = true)
    {
        if (!string.IsNullOrWhiteSpace(runtimeCutsceneId))
            cutsceneId = runtimeCutsceneId;

        skipCutsceneIfCheckpointExists = allowCheckpointSkip;
        autoSkipIfWatched = allowWatchedSkip;
        forceSubtitleCoroutineForRuntime = runtimeSubtitles != null && runtimeSubtitles.Length > 0;

        if (runtimeSubtitles == null || runtimeSubtitles.Length == 0)
            return;

        if (!replaceSubtitles)
        {
            ApplySubtitlesIfEmpty(runtimeSubtitles, extendDurationToFit);
            return;
        }

        subtitles = runtimeSubtitles;

        if (!extendDurationToFit)
            return;

        float requiredDuration = 0f;
        for (int i = 0; i < runtimeSubtitles.Length; i++)
        {
            float candidateDuration = runtimeSubtitles[i].triggerTime + runtimeSubtitles[i].displayDuration + 1f;
            if (candidateDuration > requiredDuration)
                requiredDuration = candidateDuration;
        }

        if (requiredDuration > cutsceneDuration)
            cutsceneDuration = requiredDuration;
    }

    private void EnsureCutsceneDurationFitsSubtitles(float padding = 1f)
    {
        if (subtitles == null || subtitles.Length == 0)
            return;

        float requiredDuration = 0f;
        for (int i = 0; i < subtitles.Length; i++)
        {
            float candidateDuration = subtitles[i].triggerTime + subtitles[i].displayDuration + padding;
            if (candidateDuration > requiredDuration)
                requiredDuration = candidateDuration;
        }

        if (requiredDuration > cutsceneDuration)
            cutsceneDuration = requiredDuration;
    }
    #endregion

    #region === STATE ===
    private bool isPlaying;
    private bool isSkipping;
    private bool isUsingTimelinePlayback;
    private bool timelineStopRequested;
    private bool timelineStoppedNaturally;
    private bool hasRuntimeTimelineOverride;
    private bool runtimeUseTimelineOverride;
    private bool hasRuntimeCameraMovementOverride;
    private bool runtimeCameraMovementEnabled = true;
    private bool forceSubtitleCoroutineForRuntime;
    private float cutsceneTimer;
    private float progressDuration;
    private float skipHoldTimer;
    private bool skipTriggered;
    private Coroutine cutsceneCoroutine;
    private Coroutine cameraCoroutine;
    private Coroutine subtitleCoroutine;
    private Coroutine letterboxCoroutine;
    private SubtitleController cachedSubtitleController;

    // Cached player references
    private GameObject playerObject;
    private PlayerMovement playerMovement;
    private PlayerAttack playerAttack;
    private Rigidbody2D playerRb;
    private Animator playerAnimator;

    // Cached Animator parameter availability
    private bool hasCachedAnimatorParams;
    private bool hasAnimSpeedParam;
    private bool hasAnimYVelocityParam;
    private bool hasAnimIsGroundedParam;
    private bool hasAnimIsWallSlidingParam;
    private bool hasAnimIsBlockingParam;
    private bool hasAnimJumpTrigger;
    private bool hasAnimDashTrigger;

    private static readonly int AnimSpeedHash = Animator.StringToHash("Speed");
    private static readonly int AnimYVelocityHash = Animator.StringToHash("yVelocity");
    private static readonly int AnimIsGroundedHash = Animator.StringToHash("isGrounded");
    private static readonly int AnimIsWallSlidingHash = Animator.StringToHash("isWallSliding");
    private static readonly int AnimIsBlockingHash = Animator.StringToHash("isBlocking");
    private static readonly int AnimJumpHash = Animator.StringToHash("Jump");
    private static readonly int AnimDashHash = Animator.StringToHash("Dash");

    // Cached Rigidbody2D state for proper restore
    private bool hasStoredRbState;
    private RigidbodyType2D storedBodyType;
    private float storedGravityScale;
    private bool storedSimulated;
    private RigidbodyConstraints2D storedConstraints;
    private Vector2 storedLinearVelocity;
    private float storedAngularVelocity;

    // Cached input component enabled state for accurate restore
    private bool didDisableInputThisSession;
    private bool hadStoredComponentState;
    private bool storedPlayerMovementEnabled;
    private bool storedPlayerAttackEnabled;

    private struct ForeignFadePanelState
    {
        public CanvasGroup panel;
        public bool wasActive;
        public float originalAlpha;
    }

    private readonly List<ForeignFadePanelState> foreignFadePanels = new();
    private bool hasCapturedForeignFadePanels;
    #endregion

    #region === PROPERTIES ===
    public bool IsPlaying => isPlaying;
    public float Progress => progressDuration > 0 ? Mathf.Clamp01(cutsceneTimer / progressDuration) : 0f;
    public float SkipHoldProgress => holdToSkipDuration > 0 ? Mathf.Clamp01(skipHoldTimer / holdToSkipDuration) : 0f;
    public bool IsHoldToSkipEnabled => useHoldToSkip;
    public bool IsSkippingByHold => useHoldToSkip && isPlaying && IsSkipKeyHeld();
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
            return;
        }
    }

    private void Start()
    {
        AutoResolveReferences();
        CreateCinematicUI();

        // Auto-find camera if not assigned
        if (cameraTransform == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                cameraTransform = mainCam.transform;
            }
            else
            {
                GameObject cineCam = GameObject.Find("CinemachineCamera");
                if (cineCam != null)
                    cameraTransform = cineCam.transform;
            }
        }

        // Find player
        FindPlayer();

        // Hide UI initially
        if (cutsceneUI != null)
            cutsceneUI.SetActive(false);

        if (playOnStart)
        {
            StartCutscene();
        }
    }

    private void OnEnable()
    {
        RegisterPlayableDirectorCallbacks();
    }

    private void OnDisable()
    {
        UnregisterPlayableDirectorCallbacks();
    }

    private void AutoResolveReferences()
    {
        if (cutsceneUI == null)
            cutsceneUI = GameObject.Find("CutsceneUI");

        if (fadePanel == null && cutsceneUI != null)
            fadePanel = cutsceneUI.transform.Find("FadePanel")?.GetComponent<CanvasGroup>();

        if (playableDirector == null)
            playableDirector = GetComponent<PlayableDirector>();

        if (playableDirector == null)
            playableDirector = FindFirstObjectByType<PlayableDirector>(FindObjectsInactive.Include);

        RegisterPlayableDirectorCallbacks();
        cachedSubtitleController = ResolveSubtitleController();
    }

    private void RegisterPlayableDirectorCallbacks()
    {
        if (playableDirector == null)
            return;

        playableDirector.stopped -= OnPlayableDirectorStopped;
        playableDirector.stopped += OnPlayableDirectorStopped;
    }

    private void UnregisterPlayableDirectorCallbacks()
    {
        if (playableDirector == null)
            return;

        playableDirector.stopped -= OnPlayableDirectorStopped;
    }

    private void OnPlayableDirectorStopped(PlayableDirector stoppedDirector)
    {
        if (stoppedDirector != playableDirector)
            return;

        if (!isPlaying || !isUsingTimelinePlayback)
            return;

        if (timelineStopRequested || isSkipping)
            return;

        timelineStoppedNaturally = true;
    }

    private bool TimelineHasVisualMotionTracks()
    {
        if (playableDirector == null)
            return false;

        TimelineAsset timelineAsset = playableDirector.playableAsset as TimelineAsset;
        if (timelineAsset == null)
            return false;

        foreach (TrackAsset track in timelineAsset.GetOutputTracks())
        {
            if (track == null)
                continue;

            bool hasVisualType = track is AnimationTrack
                                 || track.GetType().Name.Equals("CinemachineTrack", StringComparison.OrdinalIgnoreCase)
                                 || track.GetType().Name.Contains("Animation", StringComparison.OrdinalIgnoreCase)
                                 || track.GetType().Name.Contains("Cinemachine", StringComparison.OrdinalIgnoreCase);

            if (!hasVisualType)
                continue;

            foreach (TimelineClip _ in track.GetClips())
            {
                return true;
            }
        }

        return false;
    }

    private bool TimelineHasSignalMarkers()
    {
        if (playableDirector == null)
            return false;

        TimelineAsset timelineAsset = playableDirector.playableAsset as TimelineAsset;
        if (timelineAsset == null)
            return false;

        foreach (TrackAsset track in timelineAsset.GetOutputTracks())
        {
            if (track is not SignalTrack signalTrack)
                continue;

            foreach (IMarker _ in signalTrack.GetMarkers())
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Create cinematic UI elements (letterbox, vignette) at runtime
    /// </summary>
    private void CreateCinematicUI()
    {
        if (cutsceneUI == null) return;

        // Create letterbox bars if enabled and not existing
        if (enableLetterbox)
        {
            if (letterboxTop == null)
            {
                letterboxTop = CreateLetterboxBar("LetterboxTop", true);
            }
            if (letterboxBottom == null)
            {
                letterboxBottom = CreateLetterboxBar("LetterboxBottom", false);
            }
        }

        // Create vignette overlay if enabled
        if (enableVignette && vignetteOverlay == null)
        {
            vignetteOverlay = CreateVignetteOverlay();
        }
    }

    private RectTransform CreateLetterboxBar(string name, bool isTop)
    {
        GameObject bar = new GameObject(name);
        bar.transform.SetParent(cutsceneUI.transform, false);
        
        RectTransform rect = bar.AddComponent<RectTransform>();
        UnityEngine.UI.Image img = bar.AddComponent<UnityEngine.UI.Image>();
        img.color = Color.black;
        
        // Set anchors for top or bottom
        if (isTop)
        {
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
        }
        else
        {
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(0.5f, 0);
        }
        
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(0, 0); // Start hidden
        
        // Move to front
        bar.transform.SetAsLastSibling();
        
        return rect;
    }

    private CanvasGroup CreateVignetteOverlay()
    {
        GameObject vignette = new GameObject("VignetteOverlay");
        vignette.transform.SetParent(cutsceneUI.transform, false);
        
        RectTransform rect = vignette.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        
        UnityEngine.UI.Image img = vignette.AddComponent<UnityEngine.UI.Image>();
        // Create radial gradient effect (approximation with solid color + alpha)
        img.color = new Color(0, 0, 0, 0.3f);
        
        CanvasGroup cg = vignette.AddComponent<CanvasGroup>();
        cg.alpha = 0;
        cg.blocksRaycasts = false;
        cg.interactable = false;
        
        // Put behind letterbox but in front of other elements
        vignette.transform.SetSiblingIndex(0);
        
        return cg;
    }

    private void DisableForeignFadePanels()
    {
        if (hasCapturedForeignFadePanels)
            return;

        foreignFadePanels.Clear();

        CanvasGroup[] allFadePanels = FindObjectsByType<CanvasGroup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < allFadePanels.Length; i++)
        {
            CanvasGroup panel = allFadePanels[i];
            if (panel == null || panel.name != "FadePanel") continue;
            if (fadePanel != null && panel == fadePanel) continue;
            if (cutsceneUI != null && panel.transform.IsChildOf(cutsceneUI.transform)) continue;

            bool alreadyTracked = false;
            for (int j = 0; j < foreignFadePanels.Count; j++)
            {
                if (foreignFadePanels[j].panel == panel)
                {
                    alreadyTracked = true;
                    break;
                }
            }

            if (!alreadyTracked)
            {
                foreignFadePanels.Add(new ForeignFadePanelState
                {
                    panel = panel,
                    wasActive = panel.gameObject.activeSelf,
                    originalAlpha = panel.alpha
                });
            }

            panel.alpha = 0f;
            panel.gameObject.SetActive(false);
        }

        hasCapturedForeignFadePanels = true;
    }

    private void RestoreForeignFadePanels()
    {
        for (int i = 0; i < foreignFadePanels.Count; i++)
        {
            ForeignFadePanelState state = foreignFadePanels[i];
            if (state.panel == null) continue;

            state.panel.alpha = state.originalAlpha;
            state.panel.gameObject.SetActive(state.wasActive);
        }

        foreignFadePanels.Clear();
        hasCapturedForeignFadePanels = false;
    }

    private SubtitleController ResolveSubtitleController()
    {
        if (cachedSubtitleController != null && cachedSubtitleController.isActiveAndEnabled)
            return cachedSubtitleController;

        if (cachedSubtitleController != null && !cachedSubtitleController.isActiveAndEnabled)
            cachedSubtitleController = null;

        if (cutsceneUI != null)
        {
            SubtitleController[] uiControllers = cutsceneUI.GetComponentsInChildren<SubtitleController>(true);
            SubtitleController fallbackController = null;
            for (int i = 0; i < uiControllers.Length; i++)
            {
                SubtitleController controller = uiControllers[i];
                if (controller == null) continue;

                if (fallbackController == null)
                    fallbackController = controller;

                if (controller.isActiveAndEnabled)
                {
                    cachedSubtitleController = controller;
                    return cachedSubtitleController;
                }
            }

            if (fallbackController != null)
                cachedSubtitleController = fallbackController;
        }

        if (SubtitleController.Instance != null && SubtitleController.Instance.isActiveAndEnabled)
        {
            cachedSubtitleController = SubtitleController.Instance;
            return cachedSubtitleController;
        }

        SubtitleController[] allControllers = FindObjectsByType<SubtitleController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        SubtitleController anyController = null;
        for (int i = 0; i < allControllers.Length; i++)
        {
            SubtitleController controller = allControllers[i];
            if (controller == null) continue;

            if (anyController == null)
                anyController = controller;

            if (controller.isActiveAndEnabled)
            {
                cachedSubtitleController = controller;
                return cachedSubtitleController;
            }
        }

        cachedSubtitleController = anyController;
        return cachedSubtitleController;
    }

    private void Update()
    {
        if (!isPlaying) return;

        bool isHoldingSkipKey = IsSkipKeyHeld();

        if (useHoldToSkip)
        {
            if (isHoldingSkipKey && !skipTriggered)
            {
                skipHoldTimer += Time.unscaledDeltaTime;

                float requiredDuration = Mathf.Max(0f, holdToSkipDuration);
                if (skipHoldTimer >= requiredDuration)
                {
                    skipTriggered = true;
                    SkipCutscene();
                }
            }
            else if (!isHoldingSkipKey)
            {
                skipHoldTimer = 0f;
            }
        }
        else
        {
            // Skip input handling kiểu bấm 1 lần
            if (IsSkipKeyDown())
            {
                SkipCutscene();
            }
        }
    }

    private bool IsSkipKeyHeld()
    {
        bool held = Input.GetKey(KeyCode.Escape) || Input.GetKey(KeyCode.Space);
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
            held |= Keyboard.current.escapeKey.isPressed || Keyboard.current.spaceKey.isPressed;
#endif
        return held;
    }

    private bool IsSkipKeyDown()
    {
        bool down = Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space);
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
            down |= Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame;
#endif
        return down;
    }

    private void OnDestroy()
    {
        UnregisterPlayableDirectorCallbacks();

        if (isPlaying)
        {
            StopAllCutsceneCoroutines();
            StopAllCoroutines();
            EnablePlayerInput();
        }

        RestoreForeignFadePanels();

        if (Instance == this)
            Instance = null;
    }
    #endregion

    #region === PUBLIC API ===
    /// <summary>
    /// Bắt đầu phát cutscene
    /// </summary>
    public void StartCutscene()
    {
        if (isPlaying)
        {
            Debug.Log("[CutsceneDirector] Cutscene đang chạy, bỏ qua lệnh start.");
            return;
        }

        if (autoSkipIfWatched && CutsceneStateStore.IsWatched(cutsceneId))
        {
            if (cutsceneUI != null)
                cutsceneUI.SetActive(false);

            if (didDisableInputThisSession)
                EnablePlayerInput();
            Debug.Log($"[CutsceneDirector] Cutscene '{cutsceneId}' đã xem, tự động skip.");
            OnCutsceneSkipped?.Invoke();
            OnCutsceneEnded?.Invoke();
            return;
        }

        if (skipCutsceneIfCheckpointExists && PlayerRespawn.HasSavedCheckpointInActiveScene())
        {
            if (cutsceneUI != null)
                cutsceneUI.SetActive(false);

            if (didDisableInputThisSession)
                EnablePlayerInput();

            string activeSceneName = SceneManager.GetActiveScene().name;
            Debug.Log($"[CutsceneDirector] Skip cutscene vì đã có checkpoint ở scene '{activeSceneName}'.");
            OnCutsceneSkipped?.Invoke();
            OnCutsceneEnded?.Invoke();
            return;
        }

        Debug.Log("[CutsceneDirector] === BẮT ĐẦU CUTSCENE ===");
        isPlaying = true;
        isSkipping = false;
        isUsingTimelinePlayback = false;
        timelineStopRequested = false;
        timelineStoppedNaturally = false;
        cutsceneTimer = 0f;
        EnsureCutsceneDurationFitsSubtitles();
        Debug.Log($"[CutsceneDirector] Runtime cutscene config: duration={cutsceneDuration:F2}, subtitles={(subtitles != null ? subtitles.Length : 0)}");
        progressDuration = cutsceneDuration;
        skipHoldTimer = 0f;
        skipTriggered = false;

        didDisableInputThisSession = false;
        hadStoredComponentState = false;
        storedPlayerMovementEnabled = false;
        storedPlayerAttackEnabled = false;
        hasStoredRbState = false;

        // Disable player input
        if (disablePlayerDuringCutscene)
            DisablePlayerInput(true);

        // Show cutscene UI
        if (cutsceneUI != null)
        {
            cutsceneUI.SetActive(true);

            Canvas parentCanvas = cutsceneUI.GetComponentInParent<Canvas>(true);
            if (parentCanvas != null)
            {
                ActivateParentChain(parentCanvas.transform);

                if (parentCanvas.renderMode == RenderMode.WorldSpace)
                    parentCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

                if (parentCanvas.sortingOrder < 1000)
                    parentCanvas.sortingOrder = 1000;
            }
        }
        DisableForeignFadePanels();

        // Fire event
        OnCutsceneStarted?.Invoke();

        // Start main cutscene coroutine
        bool useTimeline = hasRuntimeTimelineOverride ? runtimeUseTimelineOverride : useTimelineIfAvailable;
        bool canUseTimeline = useTimeline
                              && playableDirector != null
                              && playableDirector.playableAsset != null;
        isUsingTimelinePlayback = canUseTimeline;
        cutsceneCoroutine = StartCoroutine(canUseTimeline ? TimelineCutsceneSequence() : CutsceneSequence());
    }

    /// <summary>
    /// Skip cutscene (gọi từ input hoặc UI)
    /// </summary>
    public void SkipCutscene()
    {
        if (!isPlaying || isSkipping) return;

        Debug.Log("[CutsceneDirector] === SKIP CUTSCENE ===");
        isSkipping = true;
        skipTriggered = true;
        skipHoldTimer = 0f;

        // Fire skip event (NOT OnCutsceneEnded - that fires at the end of EndCutsceneSequence)
        OnCutsceneSkipped?.Invoke();

        // Stop all running coroutines
        StopAllCutsceneCoroutines();

        if (isUsingTimelinePlayback && playableDirector != null)
        {
            timelineStopRequested = true;
            playableDirector.Stop();
        }

        // End immediately
        StartCoroutine(EndCutsceneSequence());
    }

    /// <summary>
    /// Stops all cutscene-related coroutines safely.
    /// </summary>
    private void StopAllCutsceneCoroutines()
    {
        if (cutsceneCoroutine != null)
        {
            StopCoroutine(cutsceneCoroutine);
            cutsceneCoroutine = null;
        }
        if (cameraCoroutine != null)
        {
            StopCoroutine(cameraCoroutine);
            cameraCoroutine = null;
        }
        if (subtitleCoroutine != null)
        {
            StopCoroutine(subtitleCoroutine);
            subtitleCoroutine = null;
        }
    }
    #endregion

    #region === CUTSCENE SEQUENCE ===
    private IEnumerator CutsceneSequence()
    {
        progressDuration = cutsceneDuration;
        bool canMoveCamera = IsCameraMovementEnabledForSession();

        // === PHASE 0: Dramatic Black Screen Pause ===
        if (enableDramaticPause)
        {
            Debug.Log("[CutsceneDirector] Phase 0: Dramatic Pause");
            yield return new WaitForSecondsRealtime(0.8f);
        }

        // === PHASE 1: Fade In with Letterbox ===
        Debug.Log("[CutsceneDirector] Phase 1: Cinematic Fade In");
        
        // Start letterbox animation in parallel with fade
        if (enableLetterbox)
        {
            letterboxCoroutine = StartCoroutine(AnimateLetterbox(true));
        }
        
        // Start vignette
        if (enableVignette && vignetteOverlay != null)
        {
            StartCoroutine(AnimateVignette(0.4f, 1.5f));
        }
        
        yield return FadeIn();

        // === PHASE 2: Play Music (if available) ===
        PlayCutsceneMusic();

        // === PHASE 3: Camera Movement + Subtitles ===
        Debug.Log("[CutsceneDirector] Phase 2: Cinematic Camera + Subtitles");
        if (canMoveCamera)
            cameraCoroutine = StartCoroutine(CinematicCameraSequence());

        // Show subtitles in parallel
        subtitleCoroutine = StartCoroutine(CinematicSubtitleSequence());

        // Wait for cutscene duration
        while (cutsceneTimer < cutsceneDuration && !isSkipping)
        {
            cutsceneTimer += Time.unscaledDeltaTime;
            yield return null;
        }

        // === PHASE 4: End Cutscene ===
        if (!isSkipping)
        {
            yield return EndCutsceneSequence();
        }
    }

    private IEnumerator TimelineCutsceneSequence()
    {
        progressDuration = cutsceneDuration;
        bool canMoveCamera = IsCameraMovementEnabledForSession();

        if (enableDramaticPause)
        {
            Debug.Log("[CutsceneDirector] Phase 0: Dramatic Pause");
            yield return new WaitForSecondsRealtime(0.8f);
        }

        Debug.Log("[CutsceneDirector] Phase 1: Cinematic Fade In");

        if (enableLetterbox)
        {
            letterboxCoroutine = StartCoroutine(AnimateLetterbox(true));
        }

        if (enableVignette && vignetteOverlay != null)
        {
            StartCoroutine(AnimateVignette(0.4f, 1.5f));
        }

        yield return FadeIn();

        PlayCutsceneMusic();

        if (playableDirector == null || playableDirector.playableAsset == null)
        {
            Debug.LogWarning("[CutsceneDirector] Timeline chưa sẵn sàng, fallback sang coroutine cutscene.");
            isUsingTimelinePlayback = false;
            yield return CutsceneSequence();
            yield break;
        }

        timelineStopRequested = false;
        timelineStoppedNaturally = false;

        double directorDuration = playableDirector.duration;
        if (directorDuration <= 0.01d)
        {
            Debug.LogWarning("[CutsceneDirector] Timeline chưa được cấu hình duration hợp lệ, fallback sang coroutine cutscene.");
            isUsingTimelinePlayback = false;
            yield return CutsceneSequence();
            yield break;
        }

        bool hasVisual = TimelineHasVisualMotionTracks();
        bool hasSignals = TimelineHasSignalMarkers();

        if (!hasVisual)
        {
            Debug.Log("[CutsceneDirector] Timeline thiếu track chuyển động hình ảnh, chạy fallback camera coroutine.");
            if (canMoveCamera)
                cameraCoroutine = StartCoroutine(CinematicCameraSequence());
        }

        if (!hasSignals || forceSubtitleCoroutineForRuntime)
        {
            if (forceSubtitleCoroutineForRuntime && hasSignals)
                Debug.Log("[CutsceneDirector] Runtime yêu cầu subtitle coroutine, bỏ qua signal marker của Timeline.");
            else
                Debug.Log("[CutsceneDirector] Timeline thiếu signal marker, chạy fallback subtitle coroutine.");
            subtitleCoroutine = StartCoroutine(CinematicSubtitleSequence());
        }

        if (directorDuration > 0d)
        {
            progressDuration = Mathf.Max(0.01f, (float)directorDuration);
        }

        cutsceneTimer = 0f;
        playableDirector.time = 0d;
        playableDirector.Evaluate();
        playableDirector.Play();

        bool timelineStarted = false;
        while (!isSkipping)
        {
            if (playableDirector.state == PlayState.Playing)
            {
                timelineStarted = true;
            }

            double currentTime = playableDirector.time;
            double currentDuration = playableDirector.duration;
            if (currentDuration > 0d)
            {
                progressDuration = Mathf.Max(0.01f, (float)currentDuration);
            }

            cutsceneTimer = Mathf.Clamp((float)currentTime, 0f, progressDuration);

            bool reachedEndByTime = currentDuration > 0d && currentTime >= currentDuration - 0.02d;
            bool stoppedAfterPlay = timelineStarted && playableDirector.state != PlayState.Playing;
            if (timelineStoppedNaturally || reachedEndByTime || stoppedAfterPlay)
            {
                break;
            }

            yield return null;
        }

        if (!isSkipping)
        {
            if (playableDirector.duration > 0d)
            {
                cutsceneTimer = Mathf.Clamp(Mathf.Max(cutsceneTimer, (float)playableDirector.duration), 0f, progressDuration);
            }

            yield return EndCutsceneSequence();
        }
    }

    private IEnumerator EndCutsceneSequence()
    {
        Debug.Log("[CutsceneDirector] Phase 3: Ending Cutscene");
        bool canMoveCamera = IsCameraMovementEnabledForSession();

        if (isUsingTimelinePlayback && playableDirector != null)
        {
            timelineStopRequested = true;
            playableDirector.Stop();
        }

        // Stop subtitle
        SubtitleController subtitleController = ResolveSubtitleController();
        if (subtitleController != null)
            subtitleController.HideSubtitle();

        // Fade out vignette
        if (enableVignette && vignetteOverlay != null)
        {
            StartCoroutine(AnimateVignette(0f, 0.8f));
        }

        // Retract letterbox
        if (enableLetterbox)
        {
            StartCoroutine(AnimateLetterbox(false));
        }

        // Fade out music
        yield return FadeOutMusic();

        // Fade out screen
        yield return FadeOut();

        // Hide cutscene UI
        if (cutsceneUI != null)
            cutsceneUI.SetActive(false);

        // Re-enable player
        EnablePlayerInput();

        if (canMoveCamera && cameraTransform != null)
        {
            if (playerObject == null)
                FindPlayer();

            if (playerObject != null)
            {
                Vector3 gameplayCenter = playerObject.transform.position;
                cameraTransform.position = new Vector3(gameplayCenter.x, gameplayCenter.y, cameraTransform.position.z);
            }
        }

        // Fade back in to gameplay
        yield return FadeIn();

        isPlaying = false;
        isSkipping = false;
        isUsingTimelinePlayback = false;
        timelineStopRequested = false;
        timelineStoppedNaturally = false;
        progressDuration = cutsceneDuration;
        skipTriggered = false;
        skipHoldTimer = 0f;
        hasRuntimeTimelineOverride = false;
        runtimeUseTimelineOverride = false;
        hasRuntimeCameraMovementOverride = false;
        runtimeCameraMovementEnabled = true;
        forceSubtitleCoroutineForRuntime = false;

        Debug.Log("[CutsceneDirector] === CUTSCENE KẾT THÚC ===");
        CutsceneStateStore.MarkWatched(cutsceneId);
        RestoreForeignFadePanels();
        OnCutsceneEnded?.Invoke();
    }

    private bool IsCameraMovementEnabledForSession()
    {
        return !hasRuntimeCameraMovementOverride || runtimeCameraMovementEnabled;
    }

    public void EndCutsceneFromSignal()
    {
        if (!isPlaying || isSkipping) return;

        Debug.Log("[CutsceneDirector] === END CUTSCENE (Signal) ===");

        StopAllCutsceneCoroutines();

        if (isUsingTimelinePlayback && playableDirector != null)
        {
            timelineStopRequested = true;
            playableDirector.Stop();
        }

        StartCoroutine(EndCutsceneSequence());
    }
    #endregion

    #region === LETTERBOX & VIGNETTE ===
    private IEnumerator AnimateLetterbox(bool show)
    {
        if (letterboxTop == null || letterboxBottom == null) yield break;

        float targetHeight = show ? Screen.height * letterboxSize : 0;
        float startHeightTop = letterboxTop.sizeDelta.y;
        float startHeightBottom = letterboxBottom.sizeDelta.y;
        float elapsed = 0f;

        while (elapsed < letterboxAnimDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / letterboxAnimDuration);
            
            float currentHeight = Mathf.Lerp(startHeightTop, targetHeight, t);
            letterboxTop.sizeDelta = new Vector2(0, currentHeight);
            letterboxBottom.sizeDelta = new Vector2(0, currentHeight);
            
            yield return null;
        }

        letterboxTop.sizeDelta = new Vector2(0, targetHeight);
        letterboxBottom.sizeDelta = new Vector2(0, targetHeight);
    }

    private IEnumerator AnimateVignette(float targetAlpha, float duration)
    {
        if (vignetteOverlay == null) yield break;

        float startAlpha = vignetteOverlay.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            vignetteOverlay.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }

        vignetteOverlay.alpha = targetAlpha;
    }
    #endregion

    #region === CAMERA MOVEMENT ===
    private IEnumerator CinematicCameraSequence()
    {
        if (cameraWaypoints == null || cameraWaypoints.Length == 0 || cameraTransform == null)
        {
            Debug.Log("[CutsceneDirector] Không có camera waypoints, sử dụng cinematic default.");
            yield return CinematicDefaultCamera();
            yield break;
        }

        foreach (var waypoint in cameraWaypoints)
        {
            if (isSkipping) yield break;

            yield return MoveCameraTo(waypoint);
        }
    }

    /// <summary>
    /// Enhanced cinematic camera sequence - film-quality opening
    /// </summary>
    private IEnumerator CinematicDefaultCamera()
    {
        if (cameraTransform == null) yield break;

        Vector3 startPos = cameraTransform.position;
        Vector3 baseFocus = playerObject != null
            ? new Vector3(playerObject.transform.position.x, playerObject.transform.position.y + 1.8f, startPos.z)
            : startPos;

        // === 8-SHOT CINEMATIC SEQUENCE ===
        // Shot 1: Establishing wide shot - panning across dark landscape
        Vector3 shot1Start = baseFocus + new Vector3(-12f, 5f, 0f);
        Vector3 shot1End = baseFocus + new Vector3(-6f, 4f, 0f);
        
        // Shot 2: Atmospheric sweep - slow pan revealing the world
        Vector3 shot2 = baseFocus + new Vector3(8f, 3.5f, 0f);
        
        // Shot 3: Ominous overhead - bird's eye hint at danger
        Vector3 shot3 = baseFocus + new Vector3(2f, 7f, 0f);
        
        // Shot 4: Mystery reveal - slow zoom toward focal point
        Vector3 shot4 = baseFocus + new Vector3(-2f, 2.5f, 0f);
        
        // Shot 5: Tension build - subtle camera drift
        Vector3 shot5 = baseFocus + new Vector3(1f, 2f, 0f);
        
        // Shot 6: Impact moment - quick pan with shake
        Vector3 shot6 = baseFocus + new Vector3(-0.5f, 1.8f, 0f);
        
        // Shot 7: Hero reveal - dramatic zoom to character
        Vector3 shot7 = baseFocus + new Vector3(0.3f, 1.2f, 0f);
        
        // Shot 8: Final settle - gameplay ready position
        Vector3 shot8 = baseFocus;

        // Calculate dynamic durations based on cutscene length
        float totalTime = cutsceneDuration;
        float d1 = totalTime * 0.14f; // Establishing shot
        float d2 = totalTime * 0.14f; // Atmospheric sweep
        float d3 = totalTime * 0.12f; // Overhead
        float d4 = totalTime * 0.12f; // Mystery zoom
        float d5 = totalTime * 0.10f; // Tension
        float d6 = totalTime * 0.10f; // Impact
        float d7 = totalTime * 0.14f; // Hero reveal
        float d8 = totalTime * 0.10f; // Settle

        // Execute sequence
        cameraTransform.position = shot1Start;
        
        // Shot 1: Slow establishing pan
        yield return MoveCameraBetween(shot1Start, shot1End, d1, true, EaseType.SineInOut);
        if (isSkipping) yield break;
        
        // Dramatic pause
        yield return new WaitForSecondsRealtime(0.3f);
        if (isSkipping) yield break;
        
        // Shot 2: Sweeping pan
        yield return MoveCameraBetween(shot1End, shot2, d2, true, EaseType.QuadOut);
        if (isSkipping) yield break;
        
        // Shot 3: Rise to overhead
        yield return MoveCameraBetween(shot2, shot3, d3, true, EaseType.SineInOut);
        if (isSkipping) yield break;
        
        // Shot 4: Slow zoom in
        yield return MoveCameraBetween(shot3, shot4, d4, true, EaseType.QuadIn);
        if (isSkipping) yield break;
        
        // Shot 5: Subtle drift with tension
        yield return MoveCameraBetween(shot4, shot5, d5, true, EaseType.Linear);
        if (isSkipping) yield break;
        
        // Camera shake for impact
        yield return CameraShake(0.5f, 0.2f);
        if (isSkipping) yield break;
        
        // Shot 6: Quick impact pan
        yield return MoveCameraBetween(shot5, shot6, d6 * 0.6f, false, EaseType.QuadOut);
        if (isSkipping) yield break;
        
        // Dramatic beat
        yield return new WaitForSecondsRealtime(0.4f);
        if (isSkipping) yield break;
        
        // Shot 7: Hero reveal - dramatic zoom
        yield return MoveCameraBetween(shot6, shot7, d7, true, EaseType.SineOut);
        if (isSkipping) yield break;
        
        // Final micro-shake
        yield return CameraShake(0.25f, 0.08f);
        if (isSkipping) yield break;
        
        // Shot 8: Final settle
        yield return MoveCameraBetween(shot7, shot8, d8, true, EaseType.SineInOut);
    }

    private enum EaseType { Linear, SineInOut, SineOut, QuadIn, QuadOut }

    private IEnumerator MoveCameraBetween(Vector3 from, Vector3 to, float duration, bool smooth, EaseType easeType = EaseType.SineInOut)
    {
        float safeDuration = Mathf.Max(0.05f, duration);
        float elapsed = 0f;

        while (elapsed < safeDuration && !isSkipping)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(elapsed / safeDuration);
            float t = ApplyEasing(normalized, easeType);
            cameraTransform.position = Vector3.Lerp(from, to, t);
            yield return null;
        }

        cameraTransform.position = to;
    }

    private float ApplyEasing(float t, EaseType easeType)
    {
        switch (easeType)
        {
            case EaseType.Linear:
                return t;
            case EaseType.SineInOut:
                return -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f;
            case EaseType.SineOut:
                return Mathf.Sin((t * Mathf.PI) / 2f);
            case EaseType.QuadIn:
                return t * t;
            case EaseType.QuadOut:
                return 1f - (1f - t) * (1f - t);
            default:
                return Mathf.SmoothStep(0f, 1f, t);
        }
    }

    private IEnumerator CameraShake(float duration, float magnitude)
    {
        if (cameraTransform == null) yield break;

        Vector3 original = cameraTransform.position;
        float elapsed = 0f;
        
        // Use perlin noise for smoother shake
        float seed = UnityEngine.Random.Range(0f, 100f);
        
        while (elapsed < duration && !isSkipping)
        {
            elapsed += Time.unscaledDeltaTime;
            float damping = 1f - Mathf.Clamp01(elapsed / duration);
            damping = damping * damping; // Quadratic falloff for natural feel
            
            // Perlin noise based shake
            float x = (Mathf.PerlinNoise(seed, elapsed * 15f) - 0.5f) * 2f * magnitude * damping;
            float y = (Mathf.PerlinNoise(seed + 100f, elapsed * 15f) - 0.5f) * 2f * magnitude * damping;
            
            cameraTransform.position = original + new Vector3(x, y, 0f);
            yield return null;
        }

        cameraTransform.position = original;
    }

    private IEnumerator MoveCameraTo(CameraWaypoint waypoint)
    {
        if (cameraTransform == null) yield break;

        Vector3 startPos = cameraTransform.position;
        Vector3 targetPos = waypoint.position;
        targetPos.z = startPos.z; // Keep camera Z

        float elapsed = 0f;

        while (elapsed < waypoint.duration && !isSkipping)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = waypoint.useSmoothStep
                ? Mathf.SmoothStep(0f, 1f, elapsed / waypoint.duration)
                : elapsed / waypoint.duration;

            cameraTransform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        // Hold at position
        if (waypoint.holdTime > 0f && !isSkipping)
        {
            yield return new WaitForSecondsRealtime(waypoint.holdTime);
        }
    }
    #endregion

    #region === SUBTITLES ===
    private IEnumerator CinematicSubtitleSequence()
    {
        if (subtitles == null || subtitles.Length == 0)
        {
            Debug.Log("[CutsceneDirector] Không có subtitles, sử dụng cinematic default.");
            yield return CinematicDefaultSubtitles();
            yield break;
        }

        SubtitleController startSubtitleController = ResolveSubtitleController();
        Debug.Log($"[CutsceneDirector] CinematicSubtitleSequence start: count={subtitles.Length}, controller={(startSubtitleController != null ? startSubtitleController.name : "null")}");
        float sequenceStart = Time.unscaledTime;

        foreach (var subtitle in subtitles)
        {
            if (isSkipping || !isPlaying) yield break;

            // Wait until trigger time
            while ((Time.unscaledTime - sequenceStart) < subtitle.triggerTime && !isSkipping && isPlaying)
            {
                yield return null;
            }

            if (isSkipping || !isPlaying) yield break;

            // Show subtitle
            SubtitleController subtitleController = ResolveSubtitleController();
            if (subtitleController != null)
            {
                ActivateParentChain(subtitleController.transform);
                subtitleController.SetSubtitleDirect(subtitle.text);

                float displayEndTime = Time.unscaledTime + Mathf.Max(0f, subtitle.displayDuration);
                while (Time.unscaledTime < displayEndTime && !isSkipping && isPlaying)
                {
                    yield return null;
                }

                subtitleController.SetSubtitleDirect(string.Empty);
            }
            else
            {
                Debug.Log($"[Subtitle] {subtitle.text}");
                float displayEndTime = Time.unscaledTime + Mathf.Max(0f, subtitle.displayDuration);
                while (Time.unscaledTime < displayEndTime && !isSkipping && isPlaying)
                {
                    yield return null;
                }
            }

            if (!isPlaying)
                yield break;
        }
    }

    /// <summary>
    /// Enhanced cinematic subtitles - dramatic narrative flow
    /// </summary>
    private IEnumerator CinematicDefaultSubtitles()
    {
        // Dramatic opening narrative - each line timed with camera movements
        string[] cinematicNarrative = new string[]
        {
            // Opening mystery
            "Trong bóng đêm vĩnh hằng, nơi ánh sáng không dám bước qua...",
            
            // World building
            "Ma Cảnh — vùng đất bị lãng quên bởi cả thần linh lẫn ác quỷ.",
            
            // Rising tension
            "Cổ ấn đã giữ ranh giới hàng ngàn năm, nay đang tan vỡ từng mảnh.",
            
            // Personal stakes
            "Những linh hồn gác cổng lần lượt lụi tàn trong im lặng.",
            
            // Call to action
            "Một nhịp tim thức tỉnh giữa tro tàn — không phải ngẫu nhiên.",
            
            // Urgency
            "Nếu cánh cổng đóng lại, nhân giới sẽ chìm vào bóng tối vĩnh viễn.",
            
            // Dramatic pause before hero shot
            "...",
            
            // Hero moment
            "Số phận đã chọn. Hành trình bắt đầu.",
        };

        // Timing synchronized with 8-shot camera sequence
        // Total 35 seconds, calculated as percentages
        float[] triggerPercent = new float[] 
        { 
            0.02f,  // Opening - during establishing shot
            0.14f,  // World building - during sweep
            0.28f,  // Rising tension - during overhead
            0.42f,  // Personal stakes - during mystery zoom
            0.54f,  // Call to action - during tension
            0.66f,  // Urgency - during impact
            0.78f,  // Dramatic pause - brief
            0.85f   // Hero moment - during reveal
        };
        
        float[] durationPercent = new float[] 
        { 
            0.12f,  // Opening
            0.12f,  // World building
            0.12f,  // Rising tension
            0.11f,  // Personal stakes
            0.11f,  // Call to action
            0.10f,  // Urgency
            0.05f,  // Dramatic pause (short "...")
            0.12f   // Hero moment
        };

        for (int i = 0; i < cinematicNarrative.Length; i++)
        {
            if (isSkipping) yield break;

            float triggerAt = cutsceneDuration * triggerPercent[i];
            while (cutsceneTimer < triggerAt && !isSkipping)
            {
                yield return null;
            }

            if (isSkipping) yield break;

            float duration = Mathf.Clamp(cutsceneDuration * durationPercent[i], 1.5f, 5f);
            
            SubtitleController subtitleController = ResolveSubtitleController();
            if (subtitleController != null)
            {
                subtitleController.ShowSubtitle(cinematicNarrative[i], duration);
            }
            else
            {
                Debug.Log($"[Subtitle] {cinematicNarrative[i]}");
            }

            // Wait for subtitle to finish before next
            yield return new WaitForSecondsRealtime(duration + 0.3f);
        }
    }
    #endregion

    #region === AUDIO ===
    private void PlayCutsceneMusic()
    {
        if (cutsceneMusic != null && cutsceneAudioSource != null)
        {
            cutsceneAudioSource.clip = cutsceneMusic;
            cutsceneAudioSource.volume = 1f;
            cutsceneAudioSource.Play();
            Debug.Log("[CutsceneDirector] Playing cutscene music");
        }
        else if (MusicManager.Instance != null)
        {
            // Use existing music manager - play game music
            MusicManager.Instance.PlayGame1Music();
            Debug.Log("[CutsceneDirector] Using MusicManager for background music");
        }
    }

    private IEnumerator FadeOutMusic()
    {
        if (cutsceneAudioSource != null && cutsceneAudioSource.isPlaying)
        {
            float startVolume = cutsceneAudioSource.volume;
            float elapsed = 0f;

            while (elapsed < musicFadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                cutsceneAudioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / musicFadeOutDuration);
                yield return null;
            }

            cutsceneAudioSource.Stop();
            cutsceneAudioSource.volume = startVolume;
        }
    }
    #endregion

    #region === FADE EFFECTS ===
    private IEnumerator FadeIn()
    {
        if (fadePanel == null)
        {
            Debug.Log("[CutsceneDirector] No fade panel, skipping fade in");
            yield break;
        }

        fadePanel.gameObject.SetActive(true);
        fadePanel.alpha = 1f;

        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            fadePanel.alpha = 1f - (elapsed / fadeInDuration);
            yield return null;
        }

        fadePanel.alpha = 0f;
        fadePanel.gameObject.SetActive(false);
    }

    private IEnumerator FadeOut()
    {
        if (fadePanel == null)
        {
            Debug.Log("[CutsceneDirector] No fade panel, skipping fade out");
            yield break;
        }

        fadePanel.gameObject.SetActive(true);
        fadePanel.alpha = 0f;

        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            fadePanel.alpha = elapsed / fadeOutDuration;
            yield return null;
        }

        fadePanel.alpha = 1f;
    }
    #endregion

    #region === PLAYER INPUT CONTROL ===
    private void FindPlayer()
    {
        playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null)
        {
            // Fallback: find by name
            playerObject = GameObject.Find("Player");
        }

        if (playerObject != null)
        {
            playerMovement = playerObject.GetComponent<PlayerMovement>();
            playerAttack = playerObject.GetComponent<PlayerAttack>();
            playerRb = playerObject.GetComponent<Rigidbody2D>();
            playerAnimator = playerObject.GetComponent<Animator>();
            TryCachePlayerAnimatorParams();
            Debug.Log("[CutsceneDirector] Found player: " + playerObject.name);
        }
        else
        {
            playerAnimator = null;
            hasCachedAnimatorParams = false;
            Debug.LogWarning("[CutsceneDirector] Player không tìm thấy! Input control sẽ không hoạt động.");
        }
    }

    private void TryCachePlayerAnimatorParams()
    {
        hasCachedAnimatorParams = false;
        hasAnimSpeedParam = false;
        hasAnimYVelocityParam = false;
        hasAnimIsGroundedParam = false;
        hasAnimIsWallSlidingParam = false;
        hasAnimIsBlockingParam = false;
        hasAnimJumpTrigger = false;
        hasAnimDashTrigger = false;

        if (playerAnimator == null)
            return;

        AnimatorControllerParameter[] parameters = playerAnimator.parameters;
        if (parameters == null || parameters.Length == 0)
            return;

        for (int i = 0; i < parameters.Length; i++)
        {
            AnimatorControllerParameter parameter = parameters[i];
            int hash = parameter.nameHash;

            if (parameter.type == AnimatorControllerParameterType.Float)
            {
                if (hash == AnimSpeedHash) hasAnimSpeedParam = true;
                else if (hash == AnimYVelocityHash) hasAnimYVelocityParam = true;
            }
            else if (parameter.type == AnimatorControllerParameterType.Bool)
            {
                if (hash == AnimIsGroundedHash) hasAnimIsGroundedParam = true;
                else if (hash == AnimIsWallSlidingHash) hasAnimIsWallSlidingParam = true;
                else if (hash == AnimIsBlockingHash) hasAnimIsBlockingParam = true;
            }
            else if (parameter.type == AnimatorControllerParameterType.Trigger)
            {
                if (hash == AnimJumpHash) hasAnimJumpTrigger = true;
                else if (hash == AnimDashHash) hasAnimDashTrigger = true;
            }
        }

        hasCachedAnimatorParams = true;
    }

    private void ForcePlayerAnimatorNeutralState()
    {
        if (playerAnimator == null)
            return;

        if (!hasCachedAnimatorParams)
            TryCachePlayerAnimatorParams();

        try
        {
            if (hasAnimSpeedParam)
                playerAnimator.SetFloat(AnimSpeedHash, 0f);
            if (hasAnimYVelocityParam)
                playerAnimator.SetFloat(AnimYVelocityHash, 0f);
            if (hasAnimIsGroundedParam)
                playerAnimator.SetBool(AnimIsGroundedHash, true);
            if (hasAnimIsWallSlidingParam)
                playerAnimator.SetBool(AnimIsWallSlidingHash, false);
            if (hasAnimIsBlockingParam)
                playerAnimator.SetBool(AnimIsBlockingHash, false);
            if (hasAnimJumpTrigger)
                playerAnimator.ResetTrigger(AnimJumpHash);
            if (hasAnimDashTrigger)
                playerAnimator.ResetTrigger(AnimDashHash);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[CutsceneDirector] Could not neutralize animator state: {ex.Message}");
        }
    }

    private void StoreRigidbodyState()
    {
        // FIX: Re-find player if null
        if (playerRb == null)
        {
            FindPlayer();
        }
        
        if (playerRb == null)
        {
            hasStoredRbState = false;
            return;
        }

        storedBodyType = playerRb.bodyType;
        storedGravityScale = playerRb.gravityScale;
        storedSimulated = playerRb.simulated;
        storedConstraints = playerRb.constraints;
        storedLinearVelocity = playerRb.linearVelocity;
        storedAngularVelocity = playerRb.angularVelocity;
        hasStoredRbState = true;

        Debug.Log($"[CutsceneDirector] Stored Rigidbody2D state: bodyType={storedBodyType}, gravityScale={storedGravityScale}, simulated={storedSimulated}");
    }

    private void RestoreRigidbodyState()
    {
        // FIX: Re-find player if null (could be destroyed and recreated)
        if (playerRb == null)
        {
            FindPlayer();
        }
        
        if (playerRb == null || !hasStoredRbState)
        {
            Debug.LogWarning("[CutsceneDirector] Cannot restore Rigidbody2D state - no stored state or null rb");
            return;
        }

        playerRb.bodyType = storedBodyType;
        playerRb.gravityScale = storedGravityScale;
        playerRb.simulated = storedSimulated;
        playerRb.constraints = storedConstraints;
        playerRb.linearVelocity = storedLinearVelocity;
        playerRb.angularVelocity = storedAngularVelocity;
        hasStoredRbState = false;

        Debug.Log($"[CutsceneDirector] Restored Rigidbody2D state: bodyType={storedBodyType}, gravityScale={storedGravityScale}");
    }

    private void DisablePlayerInput(bool fromCutsceneStart = false)
    {
        // Re-find player/components if references are null
        if (playerObject == null || playerMovement == null || playerAttack == null || playerRb == null)
        {
            FindPlayer();
        }

        // Store Rigidbody state BEFORE modifying
        StoreRigidbodyState();
        ForcePlayerAnimatorNeutralState();

        hadStoredComponentState = false;

        if (playerMovement != null)
        {
            storedPlayerMovementEnabled = playerMovement.enabled;
            hadStoredComponentState = true;
            playerMovement.enabled = false;
            Debug.Log("[CutsceneDirector] PlayerMovement disabled");
        }

        if (playerAttack != null)
        {
            storedPlayerAttackEnabled = playerAttack.enabled;
            hadStoredComponentState = true;
            playerAttack.enabled = false;
            Debug.Log("[CutsceneDirector] PlayerAttack disabled");
        }

        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector2.zero;
            playerRb.bodyType = RigidbodyType2D.Kinematic;
        }

        if (fromCutsceneStart)
        {
            didDisableInputThisSession = true;
        }
    }

    private void EnablePlayerInput()
    {
        if (didDisableInputThisSession && (playerObject == null || playerMovement == null || playerAttack == null || playerRb == null))
        {
            FindPlayer();
        }

        if (didDisableInputThisSession && hadStoredComponentState && playerMovement != null)
        {
            playerMovement.enabled = storedPlayerMovementEnabled;
            Debug.Log("[CutsceneDirector] PlayerMovement enabled");
        }

        if (didDisableInputThisSession && hadStoredComponentState && playerAttack != null)
        {
            playerAttack.enabled = storedPlayerAttackEnabled;
            Debug.Log("[CutsceneDirector] PlayerAttack enabled");
        }

        // Restore Rigidbody state to original values only for sessions that disabled input
        if (didDisableInputThisSession)
            RestoreRigidbodyState();

        ForcePlayerAnimatorNeutralState();

        didDisableInputThisSession = false;
        hadStoredComponentState = false;
    }
    #endregion

    #region === HELPERS ===
    private static void ActivateParentChain(Transform start)
    {
        Transform current = start;
        while (current != null)
        {
            if (!current.gameObject.activeSelf)
                current.gameObject.SetActive(true);

            current = current.parent;
        }
    }
    #endregion
}

#region === DATA STRUCTURES ===
[Serializable]
public struct CameraWaypoint
{
    public Vector3 position;
    public float duration;
    public float holdTime;
    public bool useSmoothStep;
}

[Serializable]
public struct SubtitleEntry
{
    [TextArea(2, 4)]
    public string text;
    public float triggerTime;
    public float displayDuration;
}
#endregion
