using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverMenu : MonoBehaviour
{
    public static GameOverMenu Instance;

    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private string menuSceneName = "Menu";

    public bool IsShown { get; private set; }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        IsShown = false;
    }

    public void ShowGameOver()
    {
        if (IsShown) return;

        IsShown = true;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        Time.timeScale = 0f;
    }

    public void PlayAgain()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void Home()
    {
        Time.timeScale = 1f;
        PlayerRespawn.ClearSavedCheckpoint();
        SceneManager.LoadScene(menuSceneName);
    }
}
