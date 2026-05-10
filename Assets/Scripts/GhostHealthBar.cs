using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays a world-space health bar above a ghost.
/// Reads ghost health every LateUpdate so it never misses an update,
/// regardless of event subscription order.
/// Attach to the HealthBarCanvas child of the ghost prefab.
/// </summary>
public class GhostHealthBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Ghost ghost;
    [SerializeField] private Image fillImage;
    [SerializeField] private Image backgroundImage;

    [Header("Settings")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2.2f, 0f);

    private const float BarWidth  = 1.2f;
    private const float BarHeight = 0.12f;

    private Camera    mainCamera;
    private Transform ghostTransform;
    private float     lastKnownFill = 1f;

    private void Awake()
    {
        mainCamera = Camera.main;

        if (ghost == null)
            ghost = GetComponentInParent<Ghost>();

        if (ghost == null)
        {
            Debug.LogWarning("[GhostHealthBar] No Ghost component found in parent hierarchy.");
            enabled = false;
            return;
        }

        ghostTransform = ghost.transform;
    }

    private void Start()
    {
        if (ghost == null) return;

        Sprite whiteSprite = BuildWhiteSprite();

        // --- Background ---
        if (backgroundImage != null)
        {
            backgroundImage.sprite    = whiteSprite;
            backgroundImage.type      = Image.Type.Simple;
            backgroundImage.color     = new Color(0.1f, 0.1f, 0.1f, 0.85f);
            backgroundImage.raycastTarget = false;

            RectTransform bgRect = backgroundImage.rectTransform;
            bgRect.anchorMin      = Vector2.zero;
            bgRect.anchorMax      = Vector2.one;
            bgRect.sizeDelta      = Vector2.zero;
            bgRect.anchoredPosition = Vector2.zero;
        }

        // --- Fill ---
        if (fillImage != null)
        {
            fillImage.sprite      = whiteSprite;
            fillImage.type        = Image.Type.Filled;
            fillImage.fillMethod  = Image.FillMethod.Horizontal;
            fillImage.fillOrigin  = 0;   // Left to right
            fillImage.fillAmount  = 1f;
            fillImage.color       = new Color(0.9f, 0.1f, 0.1f, 1f);
            fillImage.raycastTarget = false;

            RectTransform fillRect = fillImage.rectTransform;
            fillRect.anchorMin       = Vector2.zero;
            fillRect.anchorMax       = Vector2.one;
            fillRect.sizeDelta       = Vector2.zero;
            fillRect.anchoredPosition = Vector2.zero;
        }

        lastKnownFill = 1f;

        // Force canvas size
        RectTransform canvasRect = GetComponent<RectTransform>();
        if (canvasRect != null)
            canvasRect.sizeDelta = new Vector2(BarWidth, BarHeight);
    }

    private void LateUpdate()
    {
        if (ghost == null || ghostTransform == null || fillImage == null) return;

        // Position the canvas above the ghost and billboard toward the camera.
        transform.position = ghostTransform.position + worldOffset;

        if (mainCamera != null)
            transform.rotation = Quaternion.LookRotation(
                transform.position - mainCamera.transform.position);

        // Poll health directly — no event dependency.
        float targetFill = ghost.MaxHealth > 0f
            ? ghost.CurrentHealth / ghost.MaxHealth
            : 0f;

        // Only update the Image when the value has changed.
        if (!Mathf.Approximately(targetFill, lastKnownFill))
        {
            lastKnownFill    = targetFill;
            fillImage.fillAmount = targetFill;
        }
    }

    /// <summary>Creates a 1x1 white sprite usable as fill for UI Image.</summary>
    private static Sprite BuildWhiteSprite()
    {
        Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
    }
}
