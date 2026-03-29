using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Receiver cho Timeline Signal track để gọi sự kiện cutscene.
/// Mapping subtitle dùng cue id/index để designer dễ thao tác.
/// </summary>
public class CutsceneSignalReceiver : MonoBehaviour
{
    [Serializable]
    public struct SubtitleCue
    {
        public string id;
        [TextArea(2, 4)]
        public string text;
        public float duration;
    }

    [Header("References")]
    [SerializeField] private CutsceneDirector cutsceneDirector;
    [SerializeField] private SubtitleController subtitleController;
    [SerializeField] private AudioSource sfxAudioSource;

    [Header("Subtitle Cues (for Timeline Signal mapping)")]
    [SerializeField] private List<SubtitleCue> subtitleCues = new();
    [SerializeField] private string defaultSubtitleCueId;
    
    [Header("Subtitle Fallback (when no cue configured)")]
    [SerializeField] private string fallbackSubtitleText = "Hành trình bắt đầu...";
    [SerializeField] private float fallbackSubtitleDuration = 3f;

    [Header("Optional SFX Map")]
    [SerializeField] private List<string> sfxKeys = new();
    [SerializeField] private List<AudioClip> sfxClips = new();

    private readonly Dictionary<string, int> cueIndexById = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, AudioClip> sfxByKey = new(StringComparer.OrdinalIgnoreCase);

    private void Awake()
    {
        AutoResolveReferences();
        RebuildLookup();
        RebuildSfxLookup();
    }

    private void OnValidate()
    {
        EnsureSubtitleSafetyDefaults();
        RebuildLookup();
        RebuildSfxLookup();
    }
    
    private void Reset()
    {
        EnsureSubtitleSafetyDefaults();
    }

    private void AutoResolveReferences()
    {
        if (cutsceneDirector == null)
        {
            cutsceneDirector = GetComponent<CutsceneDirector>();
            if (cutsceneDirector == null)
            {
                cutsceneDirector = FindFirstObjectByType<CutsceneDirector>(FindObjectsInactive.Include);
            }
        }

        if (subtitleController == null)
        {
            subtitleController = SubtitleController.Instance;
            if (subtitleController == null)
            {
                subtitleController = FindFirstObjectByType<SubtitleController>(FindObjectsInactive.Include);
            }
        }

        if (sfxAudioSource == null)
        {
            sfxAudioSource = GetComponent<AudioSource>();
        }
    }

    private void RebuildLookup()
    {
        cueIndexById.Clear();
        if (subtitleCues == null) return;

        for (int i = 0; i < subtitleCues.Count; i++)
        {
            string id = subtitleCues[i].id;
            if (string.IsNullOrWhiteSpace(id)) continue;
            if (cueIndexById.ContainsKey(id)) continue;
            cueIndexById[id] = i;
        }
    }

    private void RebuildSfxLookup()
    {
        sfxByKey.Clear();

        int pairCount = Mathf.Min(sfxKeys?.Count ?? 0, sfxClips?.Count ?? 0);
        for (int i = 0; i < pairCount; i++)
        {
            string key = sfxKeys[i];
            AudioClip clip = sfxClips[i];
            if (string.IsNullOrWhiteSpace(key) || clip == null) continue;
            if (sfxByKey.ContainsKey(key)) continue;
            sfxByKey[key] = clip;
        }
    }

    public void SignalShowSubtitleById(string cueId)
    {
        if (string.IsNullOrWhiteSpace(cueId))
        {
            Debug.LogWarning("[CutsceneSignalReceiver] cueId rỗng.");
            return;
        }

        if (!cueIndexById.TryGetValue(cueId, out int index))
        {
            RebuildLookup();
            if (!cueIndexById.TryGetValue(cueId, out index))
            {
                Debug.LogWarning($"[CutsceneSignalReceiver] Không tìm thấy subtitle cue id='{cueId}'.");
                return;
            }
        }

        SignalShowSubtitleByIndex(index);
    }

    public void SignalShowSubtitleByIndex(int cueIndex)
    {
        if (subtitleCues == null || cueIndex < 0 || cueIndex >= subtitleCues.Count)
        {
            Debug.LogWarning($"[CutsceneSignalReceiver] cueIndex không hợp lệ: {cueIndex}.");
            return;
        }

        SubtitleCue cue = subtitleCues[cueIndex];
        ShowSubtitleInternal(cue.text, cue.duration);
    }

    public void SignalShowSubtitleDefault()
    {
        if (!string.IsNullOrWhiteSpace(defaultSubtitleCueId))
        {
            SignalShowSubtitleById(defaultSubtitleCueId);
            return;
        }

        if (subtitleCues != null && subtitleCues.Count > 0)
        {
            SignalShowSubtitleByIndex(0);
            return;
        }

        ShowSubtitleInternal(fallbackSubtitleText, fallbackSubtitleDuration);
    }

    public void SignalShowSubtitleCue0()
    {
        SignalShowSubtitleByIndex(0);
    }

    // Helper tiện test/trigger từ code
    public void SignalShowSubtitle(string text, float duration)
    {
        ShowSubtitleInternal(text, duration);
    }

    public void SignalHideSubtitle()
    {
        AutoResolveReferences();
        if (subtitleController != null)
        {
            subtitleController.HideSubtitle();
            return;
        }

        Debug.LogWarning("[CutsceneSignalReceiver] Chưa gán SubtitleController.");
    }

    public void SignalSkipCutscene()
    {
        AutoResolveReferences();
        if (cutsceneDirector != null)
        {
            cutsceneDirector.SkipCutscene();
            return;
        }

        Debug.LogWarning("[CutsceneSignalReceiver] Chưa gán CutsceneDirector.");
    }

    public void SignalEndCutscene()
    {
        AutoResolveReferences();
        if (cutsceneDirector != null)
        {
            cutsceneDirector.EndCutsceneFromSignal();
            return;
        }

        Debug.LogWarning("[CutsceneSignalReceiver] Chưa gán CutsceneDirector.");
    }

    public void SignalPlaySfx(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            Debug.LogWarning("[CutsceneSignalReceiver] SFX key rỗng.");
            return;
        }

        if (!sfxByKey.TryGetValue(key, out AudioClip clip))
        {
            RebuildSfxLookup();
            if (!sfxByKey.TryGetValue(key, out clip))
            {
                Debug.LogWarning($"[CutsceneSignalReceiver] Không tìm thấy SFX key='{key}'.");
                return;
            }
        }

        if (sfxAudioSource == null)
        {
            AutoResolveReferences();
            if (sfxAudioSource == null)
            {
                Debug.LogWarning("[CutsceneSignalReceiver] Chưa gán AudioSource để phát SFX.");
                return;
            }
        }

        sfxAudioSource.PlayOneShot(clip);
    }

    private void ShowSubtitleInternal(string text, float duration)
    {
        AutoResolveReferences();

        if (subtitleController != null)
        {
            string safeText = string.IsNullOrWhiteSpace(text) ? fallbackSubtitleText : text;
            float safeDuration = Mathf.Max(0.05f, duration);
            subtitleController.ShowSubtitle(safeText, safeDuration);
            return;
        }

        Debug.LogWarning("[CutsceneSignalReceiver] Chưa gán SubtitleController.");
    }

    private void EnsureSubtitleSafetyDefaults()
    {
        if (fallbackSubtitleDuration <= 0f)
        {
            fallbackSubtitleDuration = 0.1f;
        }

        if (!string.IsNullOrWhiteSpace(defaultSubtitleCueId) || subtitleCues == null || subtitleCues.Count == 0)
        {
            return;
        }

        for (int i = 0; i < subtitleCues.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(subtitleCues[i].id))
            {
                continue;
            }

            defaultSubtitleCueId = subtitleCues[i].id;
            return;
        }
    }
}
