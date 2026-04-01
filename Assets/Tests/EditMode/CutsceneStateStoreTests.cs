using NUnit.Framework;
using UnityEngine;

public class CutsceneStateStoreTests
{
    private string _testCutsceneId;
    private string _watchedKey;

    [SetUp]
    public void SetUp()
    {
        _testCutsceneId = $"cutscene_test_{System.Guid.NewGuid():N}";
        _watchedKey = CutsceneStateStore.BuildWatchedKey(_testCutsceneId);
        if (!string.IsNullOrEmpty(_watchedKey))
        {
            PlayerPrefs.DeleteKey(_watchedKey);
            PlayerPrefs.Save();
        }
    }

    [TearDown]
    public void TearDown()
    {
        if (!string.IsNullOrEmpty(_watchedKey))
        {
            PlayerPrefs.DeleteKey(_watchedKey);
            PlayerPrefs.Save();
        }
    }

    [Test]
    public void IsWatched_DefaultFalse_ForRandomId()
    {
        Assert.IsFalse(CutsceneStateStore.IsWatched(_testCutsceneId));
    }

    [Test]
    public void MarkWatched_ThenIsWatched_True()
    {
        CutsceneStateStore.MarkWatched(_testCutsceneId);

        Assert.IsTrue(CutsceneStateStore.IsWatched(_testCutsceneId));
    }

    [Test]
    public void ClearWatched_ResetsToFalse()
    {
        CutsceneStateStore.MarkWatched(_testCutsceneId);
        CutsceneStateStore.ClearWatched(_testCutsceneId);

        Assert.IsFalse(CutsceneStateStore.IsWatched(_testCutsceneId));
    }

    [Test]
    public void EmptyOrNullId_HandledSafely()
    {
        Assert.DoesNotThrow(() => CutsceneStateStore.MarkWatched(string.Empty));
        Assert.DoesNotThrow(() => CutsceneStateStore.MarkWatched(null));
        Assert.DoesNotThrow(() => CutsceneStateStore.ClearWatched(string.Empty));
        Assert.DoesNotThrow(() => CutsceneStateStore.ClearWatched(null));
        Assert.IsFalse(CutsceneStateStore.IsWatched(string.Empty));
        Assert.IsFalse(CutsceneStateStore.IsWatched(null));
    }
}
