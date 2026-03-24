using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Provides visual feedback when the player is grabbed by a ghost:
/// - Camera shake retriggered continuously via CameraFollow.Shake().
/// - Red vignette overlay that pulses while grabbed.
/// </summary>
public class GrabFeedback : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerGrabState grabState;
    [SerializeField] private Image vignetteImage;
    [SerializeField] private CameraFollow cameraFollow;

    [Header("Vignette")]
    [SerializeField] private float maxVignetteAlpha = 0.55f;
    [SerializeField] private float vignettePulseSpeed = 3f;
    [SerializeField] private float vignetteFadeSpeed = 3f;

    [Header("Camera Shake")]
    [SerializeField] private float shakeIntensity = 0.25f;
    [SerializeField] private float shakeDuration = 0.35f;

    // How long before re-triggering the shake while still grabbed.
    private const float ShakeRetriggerInterval = 0.25f;
    private float shakeRetriggerTimer;

    private void Awake()
    {
        if (cameraFollow == null)
            cameraFollow = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;

        if (grabState == null)
            grabState = GetComponent<PlayerGrabState>();
    }

    private void Update()
    {
        if (grabState == null) return;

        if (grabState.IsGrabbed)
        {
            UpdateVignette();
            UpdateShake();
        }
        else
        {
            FadeVignette();
            shakeRetriggerTimer = 0f;
        }
    }

    private void UpdateVignette()
    {
        if (vignetteImage == null) return;

        float pulse = (Mathf.Sin(Time.time * vignettePulseSpeed) + 1f) * 0.5f;
        Color color = vignetteImage.color;
        color.a = pulse * maxVignetteAlpha;
        vignetteImage.color = color;
    }

    private void FadeVignette()
    {
        if (vignetteImage == null) return;

        Color color = vignetteImage.color;
        color.a = Mathf.MoveTowards(color.a, 0f, vignetteFadeSpeed * Time.deltaTime);
        vignetteImage.color = color;
    }

    private void UpdateShake()
    {
        if (cameraFollow == null) return;

        shakeRetriggerTimer -= Time.deltaTime;

        if (shakeRetriggerTimer <= 0f)
        {
            cameraFollow.Shake(shakeIntensity, shakeDuration);
            shakeRetriggerTimer = ShakeRetriggerInterval;
        }
    }
}
