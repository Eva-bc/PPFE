using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays the UV flashlight state as a fill bar.
/// - Violet and draining while UV is active.
/// - White and refilling during cooldown.
/// - Green pulse when ready.
/// - Blink warning when almost empty.
/// </summary>
public class UVGaugeUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FlashlightController flashlightController;
    [SerializeField] private Image fillImage;

    [Header("Colors")]
    [SerializeField] private Color colorActive = new Color(0.6f, 0f, 1f);
    [SerializeField] private Color colorCooldown = new Color(0.8f, 0.8f, 0.8f);
    [SerializeField] private Color colorReady = new Color(0.3f, 1f, 0.3f);

    [Header("Blink — low UV warning")]
    // Blink starts below this fill threshold while UV is active.
    [SerializeField] private float blinkThreshold = 0.25f;
    [SerializeField] private float blinkSpeed = 8f;

    [Header("Ready Pulse")]
    [SerializeField] private float pulseScale = 1.15f;
    [SerializeField] private float pulseSpeed = 8f;

    private bool wasReady;
    private float currentPulse = 1f;

    private void Awake()
    {
        if (flashlightController == null)
            Debug.LogWarning("[UVGaugeUI] FlashlightController reference is missing.");
    }

    private void Update()
    {
        if (flashlightController == null || fillImage == null) return;

        UpdateFill();
        UpdateColor();
        UpdatePulse();
    }

    /// <summary>Drives fill amount from FlashlightController.UVGaugeProgress.</summary>
    private void UpdateFill()
    {
        fillImage.fillAmount = flashlightController.UVGaugeProgress;
    }

    /// <summary>
    /// Violet while active (blinks below threshold), white while recharging, green when ready.
    /// </summary>
    private void UpdateColor()
    {
        bool uvActive = flashlightController.IsUVActive;
        bool uvReady = flashlightController.IsUVReady;
        float progress = flashlightController.UVGaugeProgress;

        Color targetColor;

        if (uvReady)
        {
            targetColor = colorReady;
        }
        else if (uvActive)
        {
            targetColor = colorActive;

            // Blink alpha when almost depleted.
            if (progress < blinkThreshold)
            {
                float alpha = Mathf.Abs(Mathf.Sin(Time.time * blinkSpeed));
                targetColor.a = alpha;
            }
        }
        else
        {
            // Recharging: lerp from active color toward ready as progress increases.
            targetColor = Color.Lerp(colorActive, colorReady, progress);
        }

        fillImage.color = targetColor;
    }

    /// <summary>One-shot scale pulse the moment the cooldown finishes.</summary>
    private void UpdatePulse()
    {
        bool isReady = flashlightController.IsUVReady;

        if (isReady && !wasReady)
            currentPulse = pulseScale;

        wasReady = isReady;

        currentPulse = Mathf.MoveTowards(currentPulse, 1f, pulseSpeed * Time.deltaTime);
        transform.localScale = Vector3.one * currentPulse * 1.72f;
    }
}
