using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays the salt attack cooldown as a fill bar.
/// Changes color and triggers a pulse animation when the attack is ready.
/// </summary>
public class SaltCooldownUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SaltAttack saltAttack;
    [SerializeField] private Image fillImage;
    [SerializeField] private Image backgroundImage;

    [Header("Colors")]
    [SerializeField] private Color colorReady = new Color(0.3f, 1f, 0.3f);
    [SerializeField] private Color colorCooldown = new Color(0.9f, 0.6f, 0.1f);

    [Header("Ready Pulse")]
    [SerializeField] private float pulseScale = 1.15f;
    [SerializeField] private float pulseSpeed = 8f;

    private bool wasReady;
    private float currentPulse = 1f;

    private void Awake()
    {
        if (saltAttack == null)
            Debug.LogWarning("[SaltCooldownUI] SaltAttack reference is missing.");
    }

    private void Update()
    {
        if (saltAttack == null || fillImage == null) return;

        UpdateFill();
        UpdateColor();
        UpdatePulse();
    }

    /// <summary>Drives the fill amount directly from SaltAttack.CooldownProgress.</summary>
    private void UpdateFill()
    {
        fillImage.fillAmount = saltAttack.CooldownProgress;
    }

    /// <summary>Lerps color between cooldown (orange) and ready (green).</summary>
    private void UpdateColor()
    {
        fillImage.color = Color.Lerp(colorCooldown, colorReady, saltAttack.CooldownProgress);
    }

    /// <summary>Triggers a one-shot scale pulse when the cooldown completes.</summary>
    private void UpdatePulse()
    {
        bool isReady = saltAttack.IsReady;

        // Detect the moment the cooldown finishes.
        if (isReady && !wasReady)
            currentPulse = pulseScale;

        wasReady = isReady;

        // Animate scale back to 1 using MoveTowards for a guaranteed landing.
        currentPulse = Mathf.MoveTowards(currentPulse, 1f, pulseSpeed * Time.deltaTime);
        transform.localScale = Vector3.one * currentPulse *1.72f;
    }
}
