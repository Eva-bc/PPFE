using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD element that displays the click-to-escape progress bar.
/// Uses a CanvasGroup to show/hide so the script keeps running regardless
/// of whether the bar is visible.
/// Attach to the root GameObject of the ShakeBar HUD element.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class ShakeEscapeUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerGrabState grabState;
    [SerializeField] private Image           fillImage;

    [Header("Colors")]
    [SerializeField] private Color lowColor  = new Color(0.9f, 0.2f, 0.2f, 1f);
    [SerializeField] private Color highColor = new Color(0.2f, 0.9f, 0.2f, 1f);

    [Header("Animation")]
    [SerializeField] private float fillSmoothSpeed = 12f;
    [SerializeField] private float fadeSmoothSpeed = 8f;

    private CanvasGroup canvasGroup;
    private float       displayedFill;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        if (grabState == null)
            grabState = FindFirstObjectByType<PlayerGrabState>();
    }

    private void Start()
    {
        if (fillImage != null)
        {
            Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            fillImage.sprite     = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
            fillImage.type       = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = 0;
            fillImage.fillAmount = 0f;
        }

        // Start fully hidden.
        canvasGroup.alpha          = 0f;
        canvasGroup.interactable   = false;
        canvasGroup.blocksRaycasts = false;
        displayedFill              = 0f;
    }

    private void Update()
    {
        if (grabState == null) return;

        bool  isGrabbed   = grabState.IsGrabbed;
        float targetAlpha = isGrabbed ? 1f : 0f;

        // Fade in/out smoothly.
        canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, fadeSmoothSpeed * Time.deltaTime);

        if (!isGrabbed)
        {
            displayedFill = 0f;
            UpdateFill(0f);
            return;
        }

        float target  = grabState.ShakeProgress;
        displayedFill = Mathf.Lerp(displayedFill, target, fillSmoothSpeed * Time.deltaTime);
        UpdateFill(displayedFill);
    }

    private void UpdateFill(float t)
    {
        if (fillImage == null) return;

        fillImage.fillAmount = t;
        fillImage.color      = Color.Lerp(lowColor, highColor, t);
    }
}
