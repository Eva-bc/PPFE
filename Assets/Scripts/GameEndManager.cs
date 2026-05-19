using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Singleton that handles both the victory sequence (final key collected) and the
/// death sequence (player health reaches zero).
///
/// Usage:
///   - Place this component once in the scene on the "GameManager" GameObject.
///   - Wire RoomManager.onKeyCollected → TriggerVictory() for the final room.
///   - Wire PlayerHealth.OnDeath → TriggerDeath() (done automatically via FindFirstObjectByType).
///   - Assign thanksScreen, deathScreen and their text labels in the Inspector.
/// </summary>
public class GameEndManager : MonoBehaviour
{
    private static GameEndManager instance;

    private const string MainMenuSceneName = "MainMenu";
    private const string CurrentSceneName   = "SampleScene";

    // ── Victory UI ────────────────────────────────────────────────────────────
    [Header("Victory UI")]
    [Tooltip("Root GameObject of the black thank-you screen.")]
    [SerializeField] private GameObject thanksScreen;

    [Tooltip("TextMeshPro label on the thank-you screen.")]
    [SerializeField] private TextMeshProUGUI thanksText;

    // ── Death UI ──────────────────────────────────────────────────────────────
    [Header("Death UI")]
    [Tooltip("Root GameObject of the black death screen.")]
    [SerializeField] private GameObject deathScreen;

    [Tooltip("TextMeshPro label on the death screen.")]
    [SerializeField] private TextMeshProUGUI deathText;

    // ── Timing ────────────────────────────────────────────────────────────────
    [Header("Timing")]
    [Tooltip("Delay (real seconds) before the victory screen appears.")]
    [SerializeField] private float victoryDelay = 1.5f;

    [Tooltip("How long the victory screen is shown before loading the main menu.")]
    [SerializeField] private float thanksDuration = 5f;

    [Tooltip("Delay (real seconds) before the death screen appears.")]
    [SerializeField] private float deathDelay = 0.5f;

    [Tooltip("How long the death screen is shown before restarting.")]
    [SerializeField] private float deathDuration = 3f;

    // ── Fade ──────────────────────────────────────────────────────────────────
    [Header("Fade")]
    [Tooltip("Duration of fade-in and fade-out animations.")]
    [SerializeField] private float fadeDuration = 1f;

    // ── Audio ─────────────────────────────────────────────────────────────────
    [Header("Audio")]
    [Tooltip("AudioClip played on victory.")]
    [SerializeField] private AudioClip victoryClip;

    [Tooltip("AudioClip played on death.")]
    [SerializeField] private AudioClip deathClip;

    // ── Input ─────────────────────────────────────────────────────────────────
    [Header("Input (new Input System)")]
    [Tooltip("Action that skips the victory screen.")]
    [SerializeField] private InputActionReference skipAction;

    // ── Private state ─────────────────────────────────────────────────────────
    private AudioSource audioSource;
    private bool gameEnded;
    private bool skipRequested;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

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
        audioSource.spatialBlend = 0f;

        SetScreen(thanksScreen, false);
        SetScreen(deathScreen,  false);
    }

    private void Start()
    {
        // Auto-subscribe to PlayerHealth.OnDeath so the Inspector wiring is optional.
        PlayerHealth ph = FindFirstObjectByType<PlayerHealth>();
        if (ph != null)
            ph.OnDeath += TriggerDeath;
        else
            Debug.LogWarning("[GameEndManager] No PlayerHealth found in scene — death screen won't trigger.");
    }

    private void OnEnable()
    {
        if (skipAction != null)
            skipAction.action.performed += OnSkipPerformed;
    }

    private void OnDisable()
    {
        if (skipAction != null)
            skipAction.action.performed -= OnSkipPerformed;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Starts the victory sequence.
    /// Wire to RoomManager.onKeyCollected via the Inspector on the final room.
    /// </summary>
    public void TriggerVictory()
    {
        if (gameEnded) return;
        gameEnded = true;

        Debug.Log("[GameEndManager] Victory triggered.");
        StartCoroutine(VictoryRoutine());
    }

    /// <summary>
    /// Starts the death sequence.
    /// Called automatically when PlayerHealth.OnDeath fires.
    /// </summary>
    public void TriggerDeath()
    {
        if (gameEnded) return;
        gameEnded = true;

        Debug.Log("[GameEndManager] Death triggered.");
        StartCoroutine(DeathRoutine());
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

    // ── Coroutines ────────────────────────────────────────────────────────────

    private IEnumerator VictoryRoutine()
    {
        yield return new WaitForSecondsRealtime(victoryDelay);

        Time.timeScale = 0f;

        if (victoryClip != null)
            audioSource.PlayOneShot(victoryClip);

        SetScreen(thanksScreen, true);
        yield return StartCoroutine(FadeScreen(thanksScreen, 0f, 1f));

        float elapsed = 0f;
        while (elapsed < thanksDuration && !skipRequested)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        yield return StartCoroutine(FadeScreen(thanksScreen, 1f, 0f));

        Time.timeScale = 1f;
        SceneManager.LoadScene(MainMenuSceneName);
    }

    private IEnumerator DeathRoutine()
    {
        yield return new WaitForSecondsRealtime(deathDelay);

        Time.timeScale = 0f;

        if (deathClip != null)
            audioSource.PlayOneShot(deathClip);

        SetScreen(deathScreen, true);
        yield return StartCoroutine(FadeScreen(deathScreen, 0f, 1f));

        yield return new WaitForSecondsRealtime(deathDuration);

        yield return StartCoroutine(FadeScreen(deathScreen, 1f, 0f));

        Time.timeScale = 1f;
        SceneManager.LoadScene(CurrentSceneName);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private IEnumerator FadeScreen(GameObject screen, float from, float to)
    {
        if (screen == null) yield break;

        CanvasGroup cg = screen.GetComponent<CanvasGroup>();
        if (cg == null) cg = screen.AddComponent<CanvasGroup>();

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            yield return null;
        }
        cg.alpha = to;
    }

    private void SetScreen(GameObject screen, bool active)
    {
        if (screen != null)
            screen.SetActive(active);
    }

    private void OnSkipPerformed(InputAction.CallbackContext ctx)
    {
        if (!gameEnded) return;
        skipRequested = true;
    }
}
