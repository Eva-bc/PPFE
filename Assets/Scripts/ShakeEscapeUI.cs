using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD element that displays the shake-to-escape progress bar.
/// Shown only while the player is grabbed; hidden otherwise.
/// Attach to the root GameObject of the ShakeBar HUD element.
/// </summary>
public class ShakeEscapeUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerGrabState grabState;
    [SerializeField] private Image fillImage;
    [SerializeField] private GameObject container;

    [Header("Colors")]
    [SerializeField] private Color lowColor  = new Color(0.9f, 0.2f, 0.2f, 1f);
    [SerializeField] private Color highColor = new Color(0.2f, 0.9f, 0.2f, 1f);

    [Header("Animation")]
    // How fast the fill bar visually tracks the actual value.
    [SerializeField] private float fillSmoothSpeed = 12f;

    private float displayedFill;

    private void Awake()
    {
        if (grabState == null)
            grabState = FindFirstObjectByType<PlayerGrabState>();

        if (container == null)
            container = gameObject;
    }

    private void Start()
    {
        if (fillImage != null)
        {
            // Build a 1x1 white sprite so fillAmount renders on an image with no sprite asset.
            Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            fillImage.sprite     = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
            fillImage.type       = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = 0;
            fillImage.fillAmount = 0f;
        }

        SetVisible(false);
        displayedFill = 0f;
    }

    private void Update()
    {
        if (grabState == null) return;

        bool isGrabbed = grabState.IsGrabbed;
        SetVisible(isGrabbed);

        if (!isGrabbed)
        {
            displayedFill = 0f;
            UpdateFill(0f);
            return;
        }

        float target = grabState.ShakeProgress;

        // Smooth the displayed fill toward the real value.
        displayedFill = Mathf.Lerp(displayedFill, target, fillSmoothSpeed * Time.deltaTime);
        UpdateFill(displayedFill);
    }

    private void UpdateFill(float t)
    {
        if (fillImage == null) return;

        fillImage.fillAmount = t;
        fillImage.color      = Color.Lerp(lowColor, highColor, t);
    }

    private void SetVisible(bool visible)
    {
        if (container != null && container.activeSelf != visible)
            container.SetActive(visible);
    }
}
