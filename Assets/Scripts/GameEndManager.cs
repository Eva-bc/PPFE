using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton that handles the victory sequence when the player collects the final key.
///
/// Usage:
///   - Place this component once in the scene (e.g. on a "GameManager" GameObject).
///   - Wire RoomManager.onKeyCollected → GameEndManager.TriggerVictory() in the Inspector
///     on the final room's RoomTrigger.
///   - Optionally assign a <see cref="victoryScreen"/> UI root to show/hide.
/// </summary>
public class GameEndManager : MonoBehaviour
{
    private static GameEndManager instance;

    [Header("Victory UI")]
    [Tooltip("Root GameObject of the victory screen UI. Enabled when the game ends.")]
    [SerializeField] private GameObject victoryScreen;

    [Header("Timing")]
    [Tooltip("Delay in seconds before the victory screen appears (e.g. let a fanfare play).")]
    [SerializeField] private float victoryDelay = 1.5f;

    [Header("Audio")]
    [Tooltip("AudioClip played on victory.")]
    [SerializeField] private AudioClip victoryClip;

    [Header("Input (new Input System)")]
    [Tooltip("Action used to restart the game (e.g. any key / Submit).")]
    [SerializeField] private InputActionReference restartAction;

    private AudioSource audioSource;
    private bool gameEnded;

    // -------------------------------------------------------------------------

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2-D — plays centred in the mix.

        SetVictoryScreen(false);
    }

    private void OnEnable()
    {
        if (restartAction != null)
            restartAction.action.performed += OnRestartPerformed;
    }

    private void OnDisable()
    {
        if (restartAction != null)
            restartAction.action.performed -= OnRestartPerformed;
    }

    // -------------------------------------------------------------------------

    /// <summary>
    /// Call this to start the end-of-game sequence.
    /// Wire to RoomManager.onKeyCollected via the Inspector.
    /// </summary>
    public void TriggerVictory()
    {
        if (gameEnded) return;
        gameEnded = true;

        Debug.Log("[GameEndManager] Victory triggered — starting end sequence.");
        StartCoroutine(VictoryRoutine());
    }

    // -------------------------------------------------------------------------

    private IEnumerator VictoryRoutine()
    {
        // Wait a beat so any final effects can play out.
        yield return new WaitForSecondsRealtime(victoryDelay);

        // Freeze gameplay.
        Time.timeScale = 0f;

        if (victoryClip != null)
            audioSource.PlayOneShot(victoryClip);

        SetVictoryScreen(true);

        Debug.Log("[GameEndManager] Victory screen shown. Press restart to reload.");
    }

    // -------------------------------------------------------------------------

    /// <summary>Restarts the current scene.</summary>
    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>Quits the application (noop in the Editor).</summary>
    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // -------------------------------------------------------------------------

    private void OnRestartPerformed(InputAction.CallbackContext ctx)
    {
        if (!gameEnded) return;
        Restart();
    }

    private void SetVictoryScreen(bool active)
    {
        if (victoryScreen != null)
            victoryScreen.SetActive(active);
    }
}
