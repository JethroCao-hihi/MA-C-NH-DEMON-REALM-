using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private string menuSceneName = "Menu";
    [SerializeField] private KeyCode pauseKey = KeyCode.Escape;

    private bool isPaused;

    private void Awake()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);

        isPaused = false;
    }

    private void Update()
    {
        if (!Input.GetKeyDown(pauseKey)) return;

        if (GameOverMenu.Instance != null && GameOverMenu.Instance.IsShown)
            return;

        if (isPaused)
            Play();
        else
            Pause();
    }

    private void Pause()
    {
        isPaused = true;

        if (pausePanel != null)
            pausePanel.SetActive(true);

        Time.timeScale = 0f;
    }

    public void Play()
    {
        isPaused = false;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        Time.timeScale = 1f;
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        isPaused = false;
        PlayerRespawn.ClearSavedCheckpoint();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void Home()
    {
        Time.timeScale = 1f;
        isPaused = false;
        PlayerRespawn.ClearSavedCheckpoint();
        SceneManager.LoadScene(menuSceneName);
    }

    private void OnDisable()
    {
        if (isPaused)
            Time.timeScale = 1f;
    }
}
