#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.Timeline;

/// <summary>
/// Tool tạo nhanh Timeline setup cho CutsceneDirector.
/// Menu: Tools/Cutscene/Create Timeline Setup On Selected Director
/// </summary>
public static class CutsceneTimelineSetupTool
{
    private const string TimelinesFolder = "Assets/Cutscenes/Timelines";
    private const string SignalsFolder = "Assets/Cutscenes/Timelines/Signals";

    [MenuItem("Tools/Cutscene/Create Timeline Setup On Selected Director")]
    public static void CreateTimelineSetupOnSelectedDirector()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            EditorUtility.DisplayDialog("Cutscene Timeline Setup", "Hãy chọn GameObject chứa CutsceneDirector.", "OK");
            return;
        }

        CutsceneDirector cutsceneDirector = selected.GetComponent<CutsceneDirector>();
        if (cutsceneDirector == null)
        {
            EditorUtility.DisplayDialog("Cutscene Timeline Setup", "GameObject được chọn chưa có CutsceneDirector.", "OK");
            return;
        }

        Undo.RecordObject(selected, "Setup Cutscene Timeline");

        PlayableDirector playableDirector = selected.GetComponent<PlayableDirector>();
        if (playableDirector == null)
        {
            playableDirector = Undo.AddComponent<PlayableDirector>(selected);
        }

        CutsceneSignalReceiver cutsceneSignalReceiver = selected.GetComponent<CutsceneSignalReceiver>();
        if (cutsceneSignalReceiver == null)
        {
            cutsceneSignalReceiver = Undo.AddComponent<CutsceneSignalReceiver>(selected);
        }

        SignalReceiver signalReceiver = selected.GetComponent<SignalReceiver>();
        if (signalReceiver == null)
        {
            signalReceiver = Undo.AddComponent<SignalReceiver>(selected);
        }

        EnsureFolder(TimelinesFolder);
        EnsureFolder(SignalsFolder);

        TimelineAsset timelineAsset = EnsureTimelineAsset(playableDirector, selected.name);
        EnsureBasicTracks(timelineAsset);
        EnsureSignalReactions(signalReceiver, cutsceneSignalReceiver, timelineAsset, playableDirector);

        EditorUtility.SetDirty(playableDirector);
        EditorUtility.SetDirty(signalReceiver);
        EditorUtility.SetDirty(cutsceneSignalReceiver);
        EditorUtility.SetDirty(timelineAsset);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog(
            "Cutscene Timeline Setup",
            "Đã setup Timeline cơ bản cho CutsceneDirector.\n\n" +
            "- PlayableDirector đã có TimelineAsset\n" +
            "- CutsceneSignalReceiver + SignalReceiver đã được gắn\n" +
            "- Signal track + 4 signal marker mẫu đã tạo\n\n" +
            "Bước tiếp theo: chỉnh marker time, thêm cue subtitle và bind thêm track nếu cần.",
            "OK");
    }

    private static TimelineAsset EnsureTimelineAsset(PlayableDirector director, string objectName)
    {
        if (director.playableAsset != null && director.playableAsset is TimelineAsset existingTimeline)
        {
            return existingTimeline;
        }

        string safeName = SanitizeAssetName(string.IsNullOrWhiteSpace(objectName) ? "Cutscene" : objectName);
        string path = AssetDatabase.GenerateUniqueAssetPath($"{TimelinesFolder}/{safeName}_Cutscene.playable");
        TimelineAsset createdTimeline = ScriptableObject.CreateInstance<TimelineAsset>();
        AssetDatabase.CreateAsset(createdTimeline, path);
        director.playableAsset = createdTimeline;
        return createdTimeline;
    }

    private static void EnsureBasicTracks(TimelineAsset timelineAsset)
    {
        if (timelineAsset == null) return;

        bool hasSignalTrack = false;
        bool hasActivationTrack = false;

        foreach (TrackAsset outputTrack in timelineAsset.GetOutputTracks())
        {
            if (outputTrack is SignalTrack) hasSignalTrack = true;
            if (outputTrack is ActivationTrack) hasActivationTrack = true;
        }

        if (!hasSignalTrack)
        {
            timelineAsset.CreateTrack<SignalTrack>(null, "Cutscene Signals");
        }

        if (!hasActivationTrack)
        {
            timelineAsset.CreateTrack<ActivationTrack>(null, "Cutscene Activation");
        }
    }

    private static void EnsureSignalReactions(SignalReceiver signalReceiver, CutsceneSignalReceiver cutsceneSignalReceiver, TimelineAsset timelineAsset, PlayableDirector playableDirector)
    {
        if (signalReceiver == null || cutsceneSignalReceiver == null || timelineAsset == null || playableDirector == null) return;

        SignalAsset showSubtitleSignal = EnsureSignalAsset("SIG_ShowSubtitle_Default");
        SignalAsset hideSubtitleSignal = EnsureSignalAsset("SIG_HideSubtitle");
        SignalAsset skipSignal = EnsureSignalAsset("SIG_SkipCutscene");
        SignalAsset endSignal = EnsureSignalAsset("SIG_EndCutscene");

        AddReaction(signalReceiver, showSubtitleSignal, cutsceneSignalReceiver, nameof(CutsceneSignalReceiver.SignalShowSubtitleDefault));
        AddReaction(signalReceiver, hideSubtitleSignal, cutsceneSignalReceiver, nameof(CutsceneSignalReceiver.SignalHideSubtitle));
        AddReaction(signalReceiver, skipSignal, cutsceneSignalReceiver, nameof(CutsceneSignalReceiver.SignalSkipCutscene));
        AddReaction(signalReceiver, endSignal, cutsceneSignalReceiver, nameof(CutsceneSignalReceiver.SignalEndCutscene));

        SignalTrack signalTrack = FindOrCreateSignalTrack(timelineAsset);
        playableDirector.SetGenericBinding(signalTrack, signalReceiver);
        EnsureSignalMarker(signalTrack, showSubtitleSignal, 1.0);
        EnsureSignalMarker(signalTrack, hideSubtitleSignal, 4.0);
        EnsureSignalMarker(signalTrack, skipSignal, 8.0);
        EnsureSignalMarker(signalTrack, endSignal, 10.0);
    }

    private static SignalTrack FindOrCreateSignalTrack(TimelineAsset timelineAsset)
    {
        foreach (TrackAsset outputTrack in timelineAsset.GetOutputTracks())
        {
            if (outputTrack is SignalTrack signalTrack)
                return signalTrack;
        }

        return timelineAsset.CreateTrack<SignalTrack>(null, "Cutscene Signals");
    }

    private static void EnsureSignalMarker(SignalTrack track, SignalAsset signalAsset, double time)
    {
        if (track == null || signalAsset == null) return;

        foreach (IMarker marker in track.GetMarkers())
        {
            if (marker is SignalEmitter existingEmitter && existingEmitter.asset == signalAsset)
            {
                return;
            }
        }

        SignalEmitter emitter = track.CreateMarker<SignalEmitter>(time);
        emitter.asset = signalAsset;
        emitter.emitOnce = false;
        emitter.retroactive = false;
    }

    private static void AddReaction(SignalReceiver receiver, SignalAsset signalAsset, Object target, string methodName)
    {
        if (receiver == null || signalAsset == null || target == null || string.IsNullOrWhiteSpace(methodName))
            return;

        UnityEvent existingReaction = receiver.GetReaction(signalAsset);
        if (existingReaction != null)
            return;

        UnityEvent reaction = new UnityEvent();
        UnityAction call = System.Delegate.CreateDelegate(typeof(UnityAction), target, methodName) as UnityAction;
        if (call == null)
        {
            Debug.LogWarning($"[CutsceneTimelineSetupTool] Không bind được method: {methodName}");
            return;
        }

        UnityEventTools.AddPersistentListener(reaction, call);
        receiver.AddReaction(signalAsset, reaction);
    }

    private static SignalAsset EnsureSignalAsset(string signalName)
    {
        string path = $"{SignalsFolder}/{signalName}.asset";
        SignalAsset signalAsset = AssetDatabase.LoadAssetAtPath<SignalAsset>(path);
        if (signalAsset != null)
            return signalAsset;

        signalAsset = ScriptableObject.CreateInstance<SignalAsset>();
        AssetDatabase.CreateAsset(signalAsset, path);
        return signalAsset;
    }

    private static void EnsureFolder(string assetPath)
    {
        if (AssetDatabase.IsValidFolder(assetPath))
            return;

        string[] segments = assetPath.Split('/');
        string current = segments[0];
        for (int i = 1; i < segments.Length; i++)
        {
            string next = $"{current}/{segments[i]}";
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, segments[i]);
            }
            current = next;
        }
    }

    private static string SanitizeAssetName(string value)
    {
        foreach (char invalid in Path.GetInvalidFileNameChars())
        {
            value = value.Replace(invalid, '_');
        }

        return value.Replace(" ", "_");
    }
}
#endif
