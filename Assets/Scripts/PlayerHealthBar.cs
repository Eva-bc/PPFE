using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Subscribes to PlayerHealth events and updates the fill Image accordingly.
/// Assign the player's PlayerHealth component and the fill Image in the Inspector.
/// </summary>
public class PlayerHealthBar : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Image fillImage;

    private void Awake()
    {
        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();

        if (playerHealth == null)
            Debug.LogError("[PlayerHealthBar] No PlayerHealth found in the scene.");

        if (fillImage == null)
            Debug.LogError("[PlayerHealthBar] Fill Image is not assigned in the Inspector.");
    }

    private void Start()
    {
        // Subscribe here (not OnEnable) so PlayerHealth.Awake() has already run.
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += UpdateHealthUI;
            UpdateHealthUI(playerHealth.CurrentHealth, playerHealth.MaxHealth);
        }
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged -= UpdateHealthUI;
    }

    /// <summary>Updates the fill amount of the health bar image.</summary>
    private void UpdateHealthUI(float current, float max)
    {
        if (fillImage == null) return;

        fillImage.fillAmount = max > 0f ? current / max : 0f;

        Debug.Log($"[PlayerHealthBar] UI updated → {current:F1}/{max:F1} ({fillImage.fillAmount:P0})");
    }
}
