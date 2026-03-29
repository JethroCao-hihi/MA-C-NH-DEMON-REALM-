using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif

/// <summary>
/// Editor script để tạo Cutscene Prefab sẵn sàng sử dụng.
/// Chạy từ menu: Tools > Cutscene > Create Cutscene Prefab
/// </summary>
#if UNITY_EDITOR
public static class CutscenePrefabCreator
{
    private static bool IsPlayModeBlocked(string toolName)
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Cutscene Tool", $"{toolName} không dùng được khi đang Play Mode. Hãy Stop Play trước.", "OK");
            return true;
        }

        return false;
    }

    [MenuItem("Tools/Cutscene/Create Cutscene System in Scene")]
    public static void CreateCutsceneSystemInScene()
    {
        if (IsPlayModeBlocked("Create Cutscene System in Scene"))
            return;

        // Check if already exists
        if (Object.FindFirstObjectByType<CutsceneDirector>() != null)
        {
            EditorUtility.DisplayDialog("Cutscene System", 
                "CutsceneDirector đã tồn tại trong scene!", "OK");
            return;
        }

        // Create main container
        GameObject cutsceneSystem = new GameObject("===== CUTSCENE SYSTEM =====");
        Undo.RegisterCreatedObjectUndo(cutsceneSystem, "Create Cutscene System");

        // Add CutsceneDirector
        CutsceneDirector director = cutsceneSystem.AddComponent<CutsceneDirector>();

        // Find or create Canvas
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("CutsceneCanvas");
            canvasObj.transform.SetParent(cutsceneSystem.transform);
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Create UI hierarchy
        GameObject cutsceneUI = CreateCutsceneUI(canvas.transform);

        // Set references on director using SerializedObject
        SerializedObject serializedDirector = new SerializedObject(director);
        serializedDirector.FindProperty("cutsceneUI").objectReferenceValue = cutsceneUI;
        
        // Find fade panel
        CanvasGroup fadePanel = cutsceneUI.transform.Find("FadePanel")?.GetComponent<CanvasGroup>();
        if (fadePanel != null)
            serializedDirector.FindProperty("fadePanel").objectReferenceValue = fadePanel;

        serializedDirector.ApplyModifiedProperties();

        // Select the created object
        Selection.activeGameObject = cutsceneSystem;

        EditorUtility.DisplayDialog("Cutscene System", 
            "Cutscene System đã được tạo!\n\n" +
            "Các bước tiếp theo:\n" +
            "1. Điều chỉnh settings trong CutsceneDirector\n" +
            "2. (Tuỳ chọn) Thêm Camera Waypoints\n" +
            "3. (Tuỳ chọn) Thêm Subtitles\n" +
            "4. Test bằng cách Play scene", "OK");
    }

    private static GameObject CreateCutsceneUI(Transform canvasTransform)
    {
        // Main container
        GameObject cutsceneUI = new GameObject("CutsceneUI");
        cutsceneUI.transform.SetParent(canvasTransform, false);
        RectTransform mainRect = cutsceneUI.AddComponent<RectTransform>();
        mainRect.anchorMin = Vector2.zero;
        mainRect.anchorMax = Vector2.one;
        mainRect.sizeDelta = Vector2.zero;

        // === FADE PANEL ===
        GameObject fadePanel = CreateFadePanel(cutsceneUI.transform);

        // === SUBTITLE CONTAINER ===
        GameObject subtitleContainer = CreateSubtitleContainer(cutsceneUI.transform);

        // === SKIP HINT ===
        GameObject skipHint = CreateSkipHint(cutsceneUI.transform);

        // === PROGRESS BAR ===
        GameObject progressBar = CreateProgressBar(cutsceneUI.transform);

        // Add controllers
        SubtitleController subtitleController = subtitleContainer.AddComponent<SubtitleController>();
        SerializedObject serializedSubtitle = new SerializedObject(subtitleController);
        serializedSubtitle.FindProperty("subtitleText").objectReferenceValue = 
            subtitleContainer.transform.Find("SubtitleText")?.GetComponent<TextMeshProUGUI>();
        serializedSubtitle.FindProperty("subtitleCanvasGroup").objectReferenceValue = 
            subtitleContainer.GetComponent<CanvasGroup>();
        serializedSubtitle.FindProperty("subtitleContainer").objectReferenceValue = subtitleContainer;
        serializedSubtitle.ApplyModifiedProperties();

        CutsceneSkipUI skipUI = cutsceneUI.AddComponent<CutsceneSkipUI>();
        SerializedObject serializedSkip = new SerializedObject(skipUI);
        serializedSkip.FindProperty("skipHintContainer").objectReferenceValue = skipHint;
        serializedSkip.FindProperty("skipHintText").objectReferenceValue = 
            skipHint.transform.Find("SkipHintText")?.GetComponent<TextMeshProUGUI>();
        serializedSkip.FindProperty("progressBarContainer").objectReferenceValue = progressBar;
        serializedSkip.FindProperty("progressSlider").objectReferenceValue = 
            progressBar.GetComponent<Slider>();
        serializedSkip.ApplyModifiedProperties();

        return cutsceneUI;
    }

    private static GameObject CreateFadePanel(Transform parent)
    {
        GameObject fadePanel = new GameObject("FadePanel");
        fadePanel.transform.SetParent(parent, false);
        
        RectTransform rect = fadePanel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        Image image = fadePanel.AddComponent<Image>();
        image.color = Color.black;

        CanvasGroup canvasGroup = fadePanel.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = false;

        return fadePanel;
    }

    private static GameObject CreateSubtitleContainer(Transform parent)
    {
        GameObject container = new GameObject("SubtitleContainer");
        container.transform.SetParent(parent, false);

        RectTransform rect = container.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.1f, 0.05f);
        rect.anchorMax = new Vector2(0.9f, 0.18f);
        rect.sizeDelta = Vector2.zero;

        container.AddComponent<CanvasGroup>();

        // Background
        GameObject bg = new GameObject("SubtitleBackground");
        bg.transform.SetParent(container.transform, false);
        RectTransform bgRect = bg.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = new Vector2(20, 10);

        Image bgImage = bg.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.7f);

        // Text
        GameObject textObj = new GameObject("SubtitleText");
        textObj.transform.SetParent(container.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = new Vector2(-20, -10);

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 36;
        text.color = Color.white;
        text.text = "";

        container.SetActive(false);

        return container;
    }

    private static GameObject CreateSkipHint(Transform parent)
    {
        GameObject container = new GameObject("SkipHintContainer");
        container.transform.SetParent(parent, false);

        RectTransform rect = container.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.92f);
        rect.anchorMax = new Vector2(0.5f, 0.98f);
        rect.sizeDelta = new Vector2(500, 50);

        GameObject textObj = new GameObject("SkipHintText");
        textObj.transform.SetParent(container.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 24;
        text.color = new Color(1, 1, 1, 0.7f);
        text.text = "Nhấn SPACE hoặc ESC để bỏ qua";

        container.SetActive(false);

        return container;
    }

    private static GameObject CreateProgressBar(Transform parent)
    {
        GameObject container = new GameObject("ProgressBarContainer");
        container.transform.SetParent(parent, false);

        RectTransform rect = container.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.25f, 0.02f);
        rect.anchorMax = new Vector2(0.75f, 0.035f);
        rect.sizeDelta = Vector2.zero;

        // Background
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(container.transform, false);
        RectTransform bgRect = bg.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        Image bgImage = bg.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);

        // Slider
        Slider slider = container.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.interactable = false;

        // Fill Area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(container.transform, false);
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
        fillImage.color = new Color(0.9f, 0.3f, 0.2f, 0.9f);

        slider.fillRect = fillRect;

        container.SetActive(false);

        return container;
    }

    [MenuItem("Tools/Cutscene/Add Default Subtitles to Director")]
    public static void AddDefaultSubtitles()
    {
        if (IsPlayModeBlocked("Add Default Subtitles to Director"))
            return;

        CutsceneDirector director = Object.FindFirstObjectByType<CutsceneDirector>();
        if (director == null)
        {
            EditorUtility.DisplayDialog("Error", "Không tìm thấy CutsceneDirector trong scene!", "OK");
            return;
        }

        SerializedObject serialized = new SerializedObject(director);
        SerializedProperty subtitlesProp = serialized.FindProperty("subtitles");

        subtitlesProp.ClearArray();

        // Add default subtitles
        AddSubtitle(subtitlesProp, "Trong vương quốc bị lãng quên...", 2f, 4f);
        AddSubtitle(subtitlesProp, "Bóng tối đã thức tỉnh từ cõi Ma Cảnh...", 7f, 4f);
        AddSubtitle(subtitlesProp, "Chỉ có một người có thể ngăn chặn thảm họa...", 12f, 4f);
        AddSubtitle(subtitlesProp, "Hành trình bắt đầu...", 18f, 4f);

        serialized.ApplyModifiedProperties();

        EditorUtility.DisplayDialog("Success", "Đã thêm 4 subtitles mặc định vào CutsceneDirector!", "OK");
    }

    private static void AddSubtitle(SerializedProperty arrayProp, string text, float triggerTime, float duration)
    {
        int index = arrayProp.arraySize;
        arrayProp.InsertArrayElementAtIndex(index);
        SerializedProperty element = arrayProp.GetArrayElementAtIndex(index);
        
        element.FindPropertyRelative("text").stringValue = text;
        element.FindPropertyRelative("triggerTime").floatValue = triggerTime;
        element.FindPropertyRelative("displayDuration").floatValue = duration;
    }

    [MenuItem("Tools/Cutscene/Fix Current Scene (Auto)")]
    public static void FixCurrentSceneAuto()
    {
        if (IsPlayModeBlocked("Fix Current Scene (Auto)"))
            return;

        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() || !activeScene.isLoaded)
        {
            EditorUtility.DisplayDialog("Cutscene Auto Fix", "Không có scene đang mở.", "OK");
            return;
        }

        // Disable runtime auto-setup to avoid duplicate generation.
        CutsceneSetup[] setups = Object.FindObjectsByType<CutsceneSetup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var setup in setups)
        {
            setup.enabled = false;
            EditorUtility.SetDirty(setup);
        }

        // Ensure one director.
        CutsceneDirector[] directors = Object.FindObjectsByType<CutsceneDirector>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        CutsceneDirector director;
        if (directors.Length == 0)
        {
            GameObject root = new GameObject("===== CUTSCENE SYSTEM =====");
            Undo.RegisterCreatedObjectUndo(root, "Create Cutscene System");
            director = root.AddComponent<CutsceneDirector>();
        }
        else
        {
            director = directors[0];
            for (int i = 1; i < directors.Length; i++)
            {
                Undo.DestroyObjectImmediate(directors[i].gameObject);
            }
        }

        // Clean duplicated CutsceneUI across whole scene first.
        RemoveGlobalCutsceneUIDuplicates();

        // Ensure canvas.
        Canvas canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("CutsceneCanvas");
            Undo.RegisterCreatedObjectUndo(canvasObj, "Create Cutscene Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Ensure CutsceneUI root.
        Transform cutsceneUITransform = DeduplicateCutsceneUIRoots(canvas.transform);
        GameObject cutsceneUI = cutsceneUITransform != null
            ? cutsceneUITransform.gameObject
            : CreateCutsceneUI(canvas.transform);

        // Ensure components and references.
        CanvasGroup fadePanel = EnsureFadePanel(cutsceneUI.transform);
        SubtitleController subtitleController = EnsureSubtitleController(cutsceneUI.transform);
        CutsceneSkipUI skipUI = EnsureSkipUI(cutsceneUI.transform);

        TextMeshProUGUI subtitleText = cutsceneUI.transform.Find("SubtitleContainer/SubtitleText")?.GetComponent<TextMeshProUGUI>();
        CanvasGroup subtitleCanvas = cutsceneUI.transform.Find("SubtitleContainer")?.GetComponent<CanvasGroup>();
        GameObject subtitleContainer = cutsceneUI.transform.Find("SubtitleContainer")?.gameObject;
        if (subtitleController != null && subtitleText != null && subtitleCanvas != null && subtitleContainer != null)
        {
            subtitleController.SetupReferences(subtitleText, subtitleCanvas, subtitleContainer);
            EditorUtility.SetDirty(subtitleController);
        }

        GameObject skipContainer = cutsceneUI.transform.Find("SkipHintContainer")?.gameObject;
        TextMeshProUGUI skipText = cutsceneUI.transform.Find("SkipHintContainer/SkipHintText")?.GetComponent<TextMeshProUGUI>();
        GameObject progressContainer = cutsceneUI.transform.Find("ProgressBarContainer")?.gameObject;
        Slider progressSlider = progressContainer != null ? progressContainer.GetComponent<Slider>() : null;
        if (skipUI != null && skipContainer != null && skipText != null && progressContainer != null && progressSlider != null)
        {
            skipUI.SetupReferences(skipContainer, skipText, progressContainer, progressSlider);
            EditorUtility.SetDirty(skipUI);
        }

        SerializedObject serializedDirector = new SerializedObject(director);
        serializedDirector.FindProperty("cutsceneUI").objectReferenceValue = cutsceneUI;
        serializedDirector.FindProperty("fadePanel").objectReferenceValue = fadePanel;
        serializedDirector.ApplyModifiedProperties();
        director.SetupReferences(cutsceneUI, fadePanel);
        EditorUtility.SetDirty(director);

        if (activeScene.IsValid() && activeScene.isLoaded)
            EditorSceneManager.MarkSceneDirty(activeScene);
        Selection.activeObject = director.gameObject;
        EditorUtility.DisplayDialog("Cutscene Auto Fix", "Đã tự sửa setup cutscene cho scene hiện tại.", "OK");
    }

    [MenuItem("Tools/Cutscene/Apply Cinematic 30s Preset")]
    public static void ApplyCinematic30sPreset()
    {
        if (IsPlayModeBlocked("Apply Cinematic 30s Preset"))
            return;

        CutsceneDirector director = Object.FindFirstObjectByType<CutsceneDirector>(FindObjectsInactive.Include);
        if (director == null)
        {
            EditorUtility.DisplayDialog("Cinematic Preset", "Không tìm thấy CutsceneDirector. Hãy chạy Fix Current Scene (Auto) trước.", "OK");
            return;
        }

        SerializedObject serializedDirector = new SerializedObject(director);
        serializedDirector.FindProperty("cutsceneDuration").floatValue = 30f;
        serializedDirector.FindProperty("playOnStart").boolValue = true;
        serializedDirector.FindProperty("disablePlayerDuringCutscene").boolValue = true;
        serializedDirector.FindProperty("fadeInDuration").floatValue = 1.2f;
        serializedDirector.FindProperty("fadeOutDuration").floatValue = 1f;

        SerializedProperty cameraTransformProp = serializedDirector.FindProperty("cameraTransform");
        if (cameraTransformProp.objectReferenceValue == null)
        {
            GameObject cine = GameObject.Find("CinemachineCamera");
            if (cine != null)
                cameraTransformProp.objectReferenceValue = cine.transform;
            else if (Camera.main != null)
                cameraTransformProp.objectReferenceValue = Camera.main.transform;
        }

        SerializedProperty subtitlesProp = serializedDirector.FindProperty("subtitles");
        subtitlesProp.ClearArray();
        AddSubtitle(subtitlesProp, "Đêm nay, ranh giới giữa nhân giới và Ma Cảnh đã rạn nứt...", 2f, 4f);
        AddSubtitle(subtitlesProp, "Những linh hồn canh giữ cổ ấn lần lượt lụi tàn.", 6f, 4f);
        AddSubtitle(subtitlesProp, "Một nhịp tim thức tỉnh giữa tro tàn và bóng tối.", 11f, 4f);
        AddSubtitle(subtitlesProp, "Nếu thất bại, bình minh sẽ không còn quay lại nữa.", 16f, 4f);
        AddSubtitle(subtitlesProp, "Hãy bước vào Ma Cảnh, trước khi cánh cổng đóng vĩnh viễn.", 21f, 4f);
        AddSubtitle(subtitlesProp, "Hành trình bắt đầu.", 27f, 3f);

        serializedDirector.ApplyModifiedProperties();
        EditorUtility.SetDirty(director);

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.IsValid() && activeScene.isLoaded)
            EditorSceneManager.MarkSceneDirty(activeScene);

        Selection.activeObject = director.gameObject;
        EditorUtility.DisplayDialog("Cinematic Preset", "Đã áp dụng preset cinematic 30s.\nNhấn Play để xem.", "OK");
    }

    private static CanvasGroup EnsureFadePanel(Transform cutsceneUI)
    {
        DeduplicateChildrenByName(cutsceneUI, "FadePanel");
        Transform fade = cutsceneUI.Find("FadePanel");
        if (fade == null)
        {
            CreateFadePanel(cutsceneUI);
            fade = cutsceneUI.Find("FadePanel");
        }

        CanvasGroup group = fade.GetComponent<CanvasGroup>();
        if (group == null) group = fade.gameObject.AddComponent<CanvasGroup>();
        Image image = fade.GetComponent<Image>();
        if (image == null) image = fade.gameObject.AddComponent<Image>();
        image.color = Color.black;
        return group;
    }

    private static SubtitleController EnsureSubtitleController(Transform cutsceneUI)
    {
        DeduplicateChildrenByName(cutsceneUI, "SubtitleContainer");
        Transform subtitleContainer = cutsceneUI.Find("SubtitleContainer");
        if (subtitleContainer == null)
        {
            CreateSubtitleContainer(cutsceneUI);
            subtitleContainer = cutsceneUI.Find("SubtitleContainer");
        }

        SubtitleController[] subtitles = subtitleContainer.GetComponents<SubtitleController>();
        SubtitleController subtitle = null;
        for (int i = 0; i < subtitles.Length; i++)
        {
            if (i == 0) subtitle = subtitles[i];
            else Undo.DestroyObjectImmediate(subtitles[i]);
        }
        if (subtitle == null) subtitle = subtitleContainer.gameObject.AddComponent<SubtitleController>();
        return subtitle;
    }

    private static CutsceneSkipUI EnsureSkipUI(Transform cutsceneUI)
    {
        DeduplicateChildrenByName(cutsceneUI, "SkipHintContainer");
        DeduplicateChildrenByName(cutsceneUI, "ProgressBarContainer");
        Transform skipContainer = cutsceneUI.Find("SkipHintContainer");
        if (skipContainer == null) CreateSkipHint(cutsceneUI);

        Transform progressContainer = cutsceneUI.Find("ProgressBarContainer");
        if (progressContainer == null) CreateProgressBar(cutsceneUI);

        CutsceneSkipUI[] skipUis = cutsceneUI.GetComponents<CutsceneSkipUI>();
        CutsceneSkipUI skipUI = null;
        for (int i = 0; i < skipUis.Length; i++)
        {
            if (i == 0) skipUI = skipUis[i];
            else Undo.DestroyObjectImmediate(skipUis[i]);
        }
        if (skipUI == null) skipUI = cutsceneUI.gameObject.AddComponent<CutsceneSkipUI>();
        return skipUI;
    }

    private static Transform DeduplicateCutsceneUIRoots(Transform canvasTransform)
    {
        Transform primary = null;
        for (int i = canvasTransform.childCount - 1; i >= 0; i--)
        {
            Transform child = canvasTransform.GetChild(i);
            if (child.name != "CutsceneUI") continue;

            if (primary == null)
            {
                primary = child;
            }
            else
            {
                Undo.DestroyObjectImmediate(child.gameObject);
            }
        }
        return primary;
    }

    private static void RemoveGlobalCutsceneUIDuplicates()
    {
        GameObject[] allGameObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        GameObject primary = null;
        for (int i = 0; i < allGameObjects.Length; i++)
        {
            GameObject go = allGameObjects[i];
            if (go == null) continue;
            if (!go) continue;
            if (go.name != "CutsceneUI") continue;

            if (primary == null)
            {
                primary = go;
            }
            else
            {
                Undo.DestroyObjectImmediate(go);
            }
        }
    }

    private static void DeduplicateChildrenByName(Transform parent, string childName)
    {
        Transform primary = null;
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (child.name != childName) continue;

            if (primary == null)
            {
                primary = child;
            }
            else
            {
                Undo.DestroyObjectImmediate(child.gameObject);
            }
        }
    }
}
#endif
