using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public void PlayGame()
    {
        SceneManager.LoadScene("MainTrack");  // The scene name for your game
    }

    public void LoadCredits()
    {
        SceneManager.LoadScene("CreditsScene");  // The scene name for the credits
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
