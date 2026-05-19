using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages the main menu interactions: Play and Quit.
/// Attach this to the MainMenu scene's manager GameObject.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    private const string CinematicSceneName = "Cinematic";

    [Header("Audio")]
    [Tooltip("Sound played when any button is clicked.")]
    [SerializeField] private AudioClip buttonClickSound;

    [Tooltip("Sound played when the cursor hovers a button.")]
    [SerializeField] private AudioClip buttonHoverSound;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake  = false;
        audioSource.spatialBlend = 0f;
    }

    /// <summary>Plays the button click sound. Wire to button OnClick events in the Inspector.</summary>
    public void PlayClickSound()
    {
        if (buttonClickSound != null)
            audioSource.PlayOneShot(buttonClickSound);
    }

    /// <summary>Plays the button hover sound. Wire to EventTrigger PointerEnter events in the Inspector.</summary>
    public void PlayHoverSound()
    {
        if (buttonHoverSound != null)
            audioSource.PlayOneShot(buttonHoverSound);
    }

    /// <summary>Loads the cinematic sequence scene.</summary>
    public void OnPlayClicked()
    {
        PlayClickSound();
        SceneManager.LoadScene(CinematicSceneName);
    }

    /// <summary>Quits the application (noop in the Editor).</summary>
    public void OnQuitClicked()
    {
        PlayClickSound();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
