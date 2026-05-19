using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages the main menu interactions: Play and Quit.
/// Attach this to the MainMenu scene's manager GameObject.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    private const string CinematicSceneName = "Cinematic";

    /// <summary>Loads the cinematic sequence scene.</summary>
    public void OnPlayClicked()
    {
        SceneManager.LoadScene(CinematicSceneName);
    }

    /// <summary>Quits the application (noop in the Editor).</summary>
    public void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
