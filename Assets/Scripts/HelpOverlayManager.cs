using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the in-game help overlay that cycles through Control, Control 2 and Ghosts images.
/// Attach to the HelpOverlay root GameObject.
/// </summary>
public class HelpOverlayManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject overlayRoot;
    [SerializeField] private Image displayImage;
    [SerializeField] private Button helpButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button closeButton;

    [Header("Help Images (in order)")]
    [Tooltip("Assign: Control, Control 2, Ghosts")]
    [SerializeField] private Sprite[] helpSprites;

    private int currentIndex;
    private bool isOpen;

    private void Start()
    {
        overlayRoot.SetActive(false);
    }

    /// <summary>Opens the overlay, pauses the game and shows the first help image.</summary>
    public void OpenOverlay()
    {
        if (helpSprites == null || helpSprites.Length == 0) return;

        for (int i = 0; i < helpSprites.Length; i++)
            Debug.Log($"[HelpOverlay] helpSprites[{i}] = {(helpSprites[i] != null ? helpSprites[i].name : "NULL")}");

        currentIndex = 0;
        ShowCurrentImage();
        overlayRoot.SetActive(true);
        isOpen = true;
        RefreshButtons();
        Time.timeScale = 0f;
    }

    /// <summary>Advances to the next help image, or closes the overlay on the last one.</summary>
    public void ShowNextImage()
    {
        currentIndex++;

        if (currentIndex >= helpSprites.Length)
        {
            CloseOverlay();
            return;
        }

        ShowCurrentImage();
        RefreshButtons();
    }

    /// <summary>Closes the help overlay and resumes the game.</summary>
    public void CloseOverlay()
    {
        overlayRoot.SetActive(false);
        isOpen = false;
        Time.timeScale = 1f;
    }

    private void ShowCurrentImage()
    {
        if (displayImage != null && currentIndex < helpSprites.Length)
            displayImage.sprite = helpSprites[currentIndex];
    }

    // Shows "Next" only when there are more images; last image shows only "Close".
    private void RefreshButtons()
    {
        bool hasNext = currentIndex < helpSprites.Length - 1;
        nextButton.gameObject.SetActive(hasNext);
        closeButton.gameObject.SetActive(true);
    }
}
