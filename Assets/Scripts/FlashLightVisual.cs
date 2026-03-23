using UnityEngine;

/// <summary>
/// Drives the visual representation of the flashlight (Spot Light).
/// Reads state from FlashlightController without modifying its logic.
/// Attach this script to the Player. The Spot Light must be a child GameObject.
/// </summary>
public class FlashlightVisual : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Light spotLight;
    [SerializeField] private FlashlightController flashlightController;

    [Header("Light Settings")]
    [SerializeField] private float lightIntensity = 3f;
    [SerializeField] private Color lightColor = new Color(1f, 0.95f, 0.8f);

    // How fast the light fades in and out (higher = snappier).
    [SerializeField] private float fadeSpeed = 12f;

    private float targetIntensity;

    private void Awake()
    {
        if (spotLight != null)
        {
            spotLight.color = lightColor;
            spotLight.intensity = 0f;
        }
    }

    private void Update()
    {
        if (flashlightController == null || spotLight == null) return;

        OrientLight();
        FadeLight();
    }

    /// <summary>
    /// Rotates the Spot Light to face the mouse aim direction every frame.
    /// </summary>
    private void OrientLight()
    {
        Vector3 aim = flashlightController.AimDirection;
        if (aim.sqrMagnitude < 0.001f) return;

        // Spot lights point along their local -Z, so we use LookRotation directly.
        // We tilt slightly downward so the cone hits the ground plane.
        Vector3 downwardAim = aim + Vector3.down * 0.5f;
        spotLight.transform.rotation = Quaternion.LookRotation(downwardAim);
    }

    /// <summary>
    /// Smoothly fades intensity in when active and out when released.
    /// </summary>
    private void FadeLight()
    {
        targetIntensity = flashlightController.IsActive ? lightIntensity : 0f;
        spotLight.intensity = Mathf.Lerp(
            spotLight.intensity,
            targetIntensity,
            fadeSpeed * Time.deltaTime);
    }
}
