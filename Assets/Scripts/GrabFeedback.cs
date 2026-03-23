using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Provides visual feedback when the player is grabbed:
/// - Red vignette overlay that pulses.
/// - Camera shake that ramps up the longer the grab lasts.
/// Assign a full-screen UI Image (red, alpha 0 at rest) and the player camera in the Inspector.
/// </summary>
public class GrabFeedback : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerGrabState grabState;
    [SerializeField] private Image vignetteImage;
    [SerializeField] private Camera playerCamera;

    [Header("Vignette")]
    [SerializeField] private float maxVignetteAlpha = 0.55f;
    [SerializeField] private float vignettePulseSpeed = 3f;
    [SerializeField] private float vignetteFadeSpeed = 3f;

    [Header("Camera Shake")]
    [SerializeField] private float maxShakeIntensity = 0.08f;
    [SerializeField] private float shakeRampSpeed = 0.5f;

    private Vector3 cameraOriginalLocalPos;
    private float currentShakeIntensity;

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (playerCamera != null)
            cameraOriginalLocalPos = playerCamera.transform.localPosition;

        if (grabState == null)
            grabState = GetComponent<PlayerGrabState>();
    }

    private void Update()
    {
        if (grabState == null) return;

        if (grabState.IsGrabbed)
        {
            UpdateVignette();
            UpdateCameraShake();
        }
        else
        {
            ResetFeedback();
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

    private void UpdateCameraShake()
    {
        if (playerCamera == null) return;

        currentShakeIntensity = Mathf.MoveTowards(
            currentShakeIntensity,
            maxShakeIntensity,
            shakeRampSpeed * Time.deltaTime);

        Vector3 shakeOffset = new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f),
            0f) * currentShakeIntensity;

        playerCamera.transform.localPosition = cameraOriginalLocalPos + shakeOffset;
    }

    private void ResetFeedback()
    {
        if (vignetteImage != null)
        {
            Color color = vignetteImage.color;
            color.a = Mathf.MoveTowards(color.a, 0f, vignetteFadeSpeed * Time.deltaTime);
            vignetteImage.color = color;
        }

        if (playerCamera != null)
        {
            currentShakeIntensity = 0f;
            playerCamera.transform.localPosition = Vector3.Lerp(
                playerCamera.transform.localPosition,
                cameraOriginalLocalPos,
                Time.deltaTime * 10f);
        }
    }
}
