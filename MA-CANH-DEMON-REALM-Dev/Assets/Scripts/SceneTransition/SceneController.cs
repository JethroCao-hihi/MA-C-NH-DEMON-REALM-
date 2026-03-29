using UnityEngine;
using UnityEngine.SceneManagement;
public class SceneController : MonoBehaviour
{
    public static SceneController instance;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else 
        {
            Destroy(gameObject);
        }

    }
    public void NextLevel()
    {
        int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogWarning("SceneController: No next scene in Build Settings.");
            return;
        }

        string nextPath = SceneUtility.GetScenePathByBuildIndex(nextIndex);
        string nextSceneName = System.IO.Path.GetFileNameWithoutExtension(nextPath);
        LoadScene(nextSceneName);
    }
    public void LoadScene(string sceneName)
    {
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.LoadScene(sceneName, "CrossFade");
            return;
        }

        SceneManager.LoadSceneAsync(sceneName);
    }

}
