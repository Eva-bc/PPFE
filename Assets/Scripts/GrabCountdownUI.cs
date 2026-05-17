using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays a countdown image feedback when the player is grabbed.
/// Assign one sprite per step in <see cref="countdownSprites"/>:
///   index 0 → shown on grab (e.g. image of "5")
///   index 1 → shown after 1st click (e.g. image of "4")
///   ...
///   index 4 → shown after 4th click (e.g. image of "1")
/// The panel hides automatically on release or when not grabbed.
/// Requires a CanvasGroup on this GameObject for fade in/out.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class GrabCountdownUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerGrabState grabState;
    [SerializeField] private Image           displayImage;

    [Header("Countdown Sprites")]
    [Tooltip("One sprite per step. Index 0 = shown on grab (number 5), index 1 = after 1st click (number 4), etc.")]
    [SerializeField] private Sprite[] countdownSprites;

    [Header("Animation")]
    [SerializeField] private float fadeSmoothSpeed  = 10f;
    [SerializeField] private float popScale         = 1.25f;  // scale burst on each click
    [SerializeField] private float popSmoothSpeed   = 14f;

    private CanvasGroup canvasGroup;
    private float       currentScale = 1f;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        if (grabState == null)
            grabState = FindFirstObjectByType<PlayerGrabState>();
    }

    private void OnEnable()
    {
        if (grabState == null) return;

        grabState.OnGrabbed         += HandleGrabbed;
        grabState.OnClickRegistered += HandleClick;
        grabState.OnReleased        += HandleReleased;
    }

    private void OnDisable()
    {
        if (grabState == null) return;

        grabState.OnGrabbed         -= HandleGrabbed;
        grabState.OnClickRegistered -= HandleClick;
        grabState.OnReleased        -= HandleReleased;
    }

    private void Start()
    {
        canvasGroup.alpha          = 0f;
        canvasGroup.interactable   = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void Update()
    {
        // Fade toward target alpha.
        float targetAlpha = (grabState != null && grabState.IsGrabbed) ? 1f : 0f;
        canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, fadeSmoothSpeed * Time.deltaTime);

        // Animate pop scale back to 1.
        currentScale = Mathf.Lerp(currentScale, 1f, popSmoothSpeed * Time.deltaTime);
        if (displayImage != null)
            displayImage.transform.localScale = Vector3.one * currentScale;
    }

    // ----------------------------------------------------------- Event Handlers

    /// <summary>Called when the player is grabbed. Shows the first countdown image.</summary>
    private void HandleGrabbed(int clicksRequired)
    {
        ShowSprite(0);
    }

    /// <summary>Called on each valid click. Advances to the next countdown image.</summary>
    private void HandleClick(int clickCount)
    {
        ShowSprite(clickCount);
        currentScale = popScale; // trigger pop animation
    }

    /// <summary>Called when the player escapes. Hides the panel.</summary>
    private void HandleReleased()
    {
        if (displayImage != null)
        {
            displayImage.sprite  = null;
            displayImage.enabled = false;
        }
    }

    // ----------------------------------------------------------- Helpers

    /// <summary>Sets the displayed sprite by index, clamped to the available array.</summary>
    private void ShowSprite(int index)
    {
        if (displayImage == null || countdownSprites == null || countdownSprites.Length == 0)
            return;

        int safeIndex = Mathf.Clamp(index, 0, countdownSprites.Length - 1);
        displayImage.sprite  = countdownSprites[safeIndex];
        displayImage.enabled = countdownSprites[safeIndex] != null;
    }
}
