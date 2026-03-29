using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;
    public GameObject transitionsContainer;
    public Slider progressBar;
    public float minLoadingScreenTime = 0.5f;

    private SceneTransition[] transitions;
    private bool isLoading;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (transitionsContainer != null)
            transitionsContainer.SetActive(false);

        if (progressBar != null)
            progressBar.gameObject.SetActive(false);
    }

    private void Start()
    {
        if (transitionsContainer != null)
            transitions = transitionsContainer.GetComponentsInChildren<SceneTransition>(true);
    }

    public void LoadScene(string sceneName, string transitionName)
    {
        if (isLoading) return;
        StartCoroutine(LoadSceneAsync(sceneName, transitionName));
    }

    private IEnumerator LoadSceneAsync(string sceneName, string transitionName)
    {
        isLoading = true;

        if (transitionsContainer != null)
            transitionsContainer.SetActive(true);

        SceneTransition transition = null;
        if (transitions != null && transitions.Length > 0)
            transition = transitions.FirstOrDefault(t => t.name == transitionName);

        if (transition != null)
            yield return transition.AnimateTransitionIn();
        else
            Debug.LogWarning("LevelManager: Transition not found: " + transitionName);

        if (progressBar != null)
        {
            progressBar.minValue = 0f;
            progressBar.maxValue = 1f;
            progressBar.wholeNumbers = false;
            progressBar.value = 0f;
            progressBar.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("LevelManager: ProgressBar is not assigned.");
        }

        AsyncOperation scene = SceneManager.LoadSceneAsync(sceneName);
        scene.allowSceneActivation = false;

        float shownProgress = 0f;
        float timer = 0f;

        while (!scene.isDone)
        {
            timer += Time.unscaledDeltaTime;

            float targetProgress = Mathf.Clamp01(scene.progress / 0.9f);
            shownProgress = Mathf.MoveTowards(shownProgress, targetProgress, Time.unscaledDeltaTime * 1.2f);

            if (progressBar != null)
                progressBar.value = shownProgress;

            if (scene.progress >= 0.9f && shownProgress >= 0.99f && timer >= minLoadingScreenTime)
                scene.allowSceneActivation = true;

            yield return null;
        }

        if (progressBar != null)
            progressBar.gameObject.SetActive(false);

        if (transition != null)
            yield return transition.AnimateTransitionOut();

        if (transitionsContainer != null)
            transitionsContainer.SetActive(false);

        isLoading = false;
    }
}
