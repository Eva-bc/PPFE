using UnityEngine;

/// <summary>
/// Drives the visual representation of the flashlight (Spot Light).
/// Switches color between normal and UV mode based on FlashlightController state.
/// </summary>
public class FlashlightVisual : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Light spotLight;
    [SerializeField] private FlashlightController flashlightController;

    [Header("Normal Light")]
    [SerializeField] private float lightIntensity = 3f;
    [SerializeField] private Color lightColor = new Color(1f, 0.95f, 0.8f);

    [Header("UV Light")]
    [SerializeField] private float uvIntensity = 4f;
    [SerializeField] private Color uvColor = new Color(0.6f, 0f, 1f);

    [SerializeField] private float fadeSpeed = 12f;

    private void Awake()
    {
        if (spotLight != null)
            spotLight.intensity = 0f;
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

        Vector3 downwardAim = aim + Vector3.down * 0.5f;
        spotLight.transform.rotation = Quaternion.LookRotation(downwardAim);
    }
    /// <summary>
    /// Fades intensity and switches color depending on whether UV mode is active.
    /// Uses MoveTowards instead of Lerp to guarantee reaching the target value.
    /// </summary>
    private void FadeLight()
    {
        bool uvActive = flashlightController.IsUVActive;
        bool normalActive = flashlightController.IsActive;

        float targetIntensity = uvActive ? uvIntensity : normalActive ? lightIntensity : 0f;
        Color targetColor = uvActive ? uvColor : lightColor;

        float step = fadeSpeed * Time.deltaTime;
        spotLight.intensity = Mathf.MoveTowards(spotLight.intensity, targetIntensity, step);
        spotLight.color = Color.Lerp(spotLight.color, targetColor, step);
    }

}
