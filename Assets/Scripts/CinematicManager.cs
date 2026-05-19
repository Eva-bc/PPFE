using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Displays a sequence of full-screen images one by one.
/// - Images before <see cref="manualSkipStartIndex"/> advance automatically with a fade.
/// - Images from <see cref="manualSkipStartIndex"/> onward stay visible until
///   the player clicks the skip button.
/// </summary>
public class CinematicManager : MonoBehaviour
{
    private const string GameSceneName = "SampleScene";

    [Header("Cinematic Images (in order)")]
    [Tooltip("Assign: Cinematic1, Cinematic2, Cinematic3, Control, Control2, Ghosts")]
    [SerializeField] private Sprite[] cinematicSprites;

    [Header("Sequence Settings")]
    [Tooltip("Index from which images require a manual skip (skip button click). " +
             "Images before this index advance automatically.")]
    [SerializeField] private int manualSkipStartIndex = 3;

    [Tooltip("How long auto images stay fully visible (seconds).")]
    [SerializeField] private float displayDuration = 3f;

    [Tooltip("Duration of fade-in and fade-out transitions (seconds).")]
    [SerializeField] private float fadeDuration = 0.8f;

    [Header("References")]
    [SerializeField] private Image displayImage;
    [SerializeField] private Button skipButton;

    // Set to true by the skip button click during a manual image.
    private bool playerClickedSkip;

    private void Start()
    {
        if (skipButton != null)
            skipButton.onClick.AddListener(OnSkipClicked);

        SetSkipButtonVisible(false);
        SetImageAlpha(0f);

        StartCoroutine(PlaySequence());
    }

    private IEnumerator PlaySequence()
    {
        for (int i = 0; i < cinematicSprites.Length; i++)
        {
            displayImage.sprite = cinematicSprites[i];
            bool isManual = i >= manualSkipStartIndex;

            // Fade in.
            yield return StartCoroutine(Fade(0f, 1f));

            if (isManual)
            {
                // Show skip button and wait for the player to click it.
                SetSkipButtonVisible(true);
                playerClickedSkip = false;
                yield return new WaitUntil(() => playerClickedSkip);
                SetSkipButtonVisible(false);
            }
            else
            {
                // Auto: stay visible for the set duration then fade out.
                yield return new WaitForSeconds(displayDuration);
            }

            // Fade out before the next image (skip last fade-out).
            if (i < cinematicSprites.Length - 1)
                yield return StartCoroutine(Fade(1f, 0f));
        }

        LoadGame();
    }

    private IEnumerator Fade(float fromAlpha, float toAlpha)
    {
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            SetImageAlpha(Mathf.Lerp(fromAlpha, toAlpha, elapsed / fadeDuration));
            yield return null;
        }

        SetImageAlpha(toAlpha);
    }

    /// <summary>Called by the skip button click during a manual image.</summary>
    public void OnSkipClicked()
    {
        playerClickedSkip = true;
    }

    private void SetSkipButtonVisible(bool visible)
    {
        if (skipButton != null)
            skipButton.gameObject.SetActive(visible);
    }

    private void SetImageAlpha(float alpha)
    {
        if (displayImage == null) return;
        Color c = displayImage.color;
        c.a = alpha;
        displayImage.color = c;
    }

    private void LoadGame()
    {
        SceneManager.LoadScene(GameSceneName);
    }
}
