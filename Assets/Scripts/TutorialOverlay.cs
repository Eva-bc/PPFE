using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen (or panel) UI that displays a single tutorial image with
/// fade-in / auto-dismiss / fade-out behaviour.
/// Attach to a Canvas child that has a CanvasGroup and an Image child.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class TutorialOverlay : MonoBehaviour
{
    [SerializeField] private Image tutorialImage;

    [Tooltip("How long the image takes to fade in.")]
    [SerializeField] private float fadeInDuration  = 0.4f;

    [Tooltip("How long the image stays fully visible before fading out.")]
    [SerializeField] private float displayDuration = 4f;

    [Tooltip("How long the image takes to fade out.")]
    [SerializeField] private float fadeOutDuration = 0.6f;

    private CanvasGroup _group;
    private Coroutine   _sequence;

    private void Awake()
    {
        _group = GetComponent<CanvasGroup>();
        _group.alpha          = 0f;
        _group.interactable   = false;
        _group.blocksRaycasts = false;
        // Ne pas désactiver le GameObject : StartCoroutine ne fonctionne pas sur un GO inactif.
    }

    /// <summary>
    /// Shows the given sprite with the configured fade / display / fade timings.
    /// If a sequence is already running it is cancelled and replaced.
    /// </summary>
    public void Show(Sprite sprite)
    {
        if (sprite == null) return;

        tutorialImage.sprite = sprite;

        if (_sequence != null) StopCoroutine(_sequence);
        _sequence = StartCoroutine(RunSequence());
    }

    /// <summary>Immediately hides the overlay (no fade).</summary>
    public void HideImmediate()
    {
        if (_sequence != null) StopCoroutine(_sequence);
        _group.alpha          = 0f;
        _group.blocksRaycasts = false;
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private IEnumerator RunSequence()
    {
        _group.blocksRaycasts = false;

        yield return Fade(0f, 1f, fadeInDuration);
        yield return new WaitForSeconds(displayDuration);
        yield return Fade(1f, 0f, fadeOutDuration);

        _group.alpha = 0f;
        _sequence    = null;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0f;
        _group.alpha  = from;
        while (elapsed < duration)
        {
            elapsed      += Time.deltaTime;
            _group.alpha  = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        _group.alpha = to;
    }
}
