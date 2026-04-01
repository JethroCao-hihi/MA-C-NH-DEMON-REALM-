using UnityEngine;

/// <summary>
/// Lưu trạng thái đã xem cutscene bằng PlayerPrefs.
/// </summary>
public static class CutsceneStateStore
{
    private const string WatchedPrefix = "cutscene_watched_";

    public static string BuildWatchedKey(string cutsceneId)
    {
        if (!IsValidCutsceneId(cutsceneId))
        {
            Debug.LogWarning("[CutsceneStateStore] cutsceneId không hợp lệ khi build key.");
            return string.Empty;
        }

        return WatchedPrefix + cutsceneId.Trim();
    }

    public static bool IsWatched(string cutsceneId)
    {
        string key = BuildWatchedKey(cutsceneId);
        if (string.IsNullOrEmpty(key)) return false;

        return PlayerPrefs.GetInt(key, 0) == 1;
    }

    public static void MarkWatched(string cutsceneId)
    {
        string key = BuildWatchedKey(cutsceneId);
        if (string.IsNullOrEmpty(key)) return;

        PlayerPrefs.SetInt(key, 1);
        PlayerPrefs.Save();
    }

    public static void ClearWatched(string cutsceneId)
    {
        string key = BuildWatchedKey(cutsceneId);
        if (string.IsNullOrEmpty(key)) return;

        PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
    }

    private static bool IsValidCutsceneId(string cutsceneId)
    {
        return !string.IsNullOrWhiteSpace(cutsceneId);
    }
}
