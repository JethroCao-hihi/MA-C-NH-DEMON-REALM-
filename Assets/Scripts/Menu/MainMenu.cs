using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void Play()
    {
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.LoadScene("Game1", "CrossFade");
        }
        else
        {
            SceneManager.LoadScene("Game1");
        }
    }

    public void Quit()
    {
        Application.Quit();
    }
}
