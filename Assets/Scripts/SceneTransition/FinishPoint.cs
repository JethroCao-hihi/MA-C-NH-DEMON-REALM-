using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FinishPoint : MonoBehaviour
{
    [SerializeField] private string transitionName = "CrossFade";
    [SerializeField] private bool useNextBuildIndex = true;
    [SerializeField] private string nextSceneName;

    private bool isTriggered;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isTriggered) return;
        if (!collision.CompareTag("Player")) return;

        isTriggered = true;

        string targetScene = ResolveTargetSceneName();
        if (string.IsNullOrEmpty(targetScene))
        {
            Debug.LogWarning("FinishPoint: Target scene is empty.");
            isTriggered = false;
            return;
        }

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.LoadScene(targetScene, transitionName);
        }
        else
        {
            SceneManager.LoadScene(targetScene);
        }
    }

    private string ResolveTargetSceneName()
    {
        if (!useNextBuildIndex)
            return nextSceneName;

        int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogWarning("FinishPoint: No next scene in Build Settings.");
            return string.Empty;
        }

        string scenePath = SceneUtility.GetScenePathByBuildIndex(nextIndex);
        return Path.GetFileNameWithoutExtension(scenePath);
    }
}
