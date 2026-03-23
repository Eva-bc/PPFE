using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays a world-space health bar above a ghost.
/// Attach this script to the HealthBarCanvas — NOT to the ghost root.
/// The canvas moves itself above the ghost every LateUpdate.
/// </summary>
public class GhostHealthBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Ghost ghost;
    [SerializeField] private Image fillImage;

    [Header("Settings")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.5f, 0f);

    private Camera mainCamera;
    private Transform ghostTransform;

    private void Awake()
    {
        mainCamera = Camera.main;

        if (ghost == null)
            ghost = GetComponentInParent<Ghost>();

        if (ghost != null)
        {
            ghostTransform = ghost.transform;
            ghost.OnHealthChanged += UpdateBar;
            UpdateBar(ghost.CurrentHealth, ghost.MaxHealth);
        }
        else
        {
            Debug.LogWarning("[GhostHealthBar] No Ghost component found.");
        }
    }

    private void OnDestroy()
    {
        if (ghost != null)
            ghost.OnHealthChanged -= UpdateBar;
    }

    private void LateUpdate()
    {
        if (ghostTransform == null) return;

        // Move the Canvas above the ghost — not the ghost itself.
        transform.position = ghostTransform.position + worldOffset;

        // Billboard: always face the camera.
        if (mainCamera != null)
            transform.rotation = Quaternion.LookRotation(
                transform.position - mainCamera.transform.position);
    }

    /// <summary>Updates the fill amount of the health bar image.</summary>
    private void UpdateBar(float current, float max)
    {
        if (fillImage == null) return;
        fillImage.fillAmount = max > 0f ? current / max : 0f;
    }
}

