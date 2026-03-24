using UnityEngine;

/// <summary>
/// Smoothly follows the target (player) in top-down view.
/// Camera shake is applied directly on the final position each LateUpdate,
/// bypassing the follow Lerp so even small intensities are fully visible.
/// Call Shake() from any script to trigger a timed shake.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Follow Settings")]
    [SerializeField] private float smoothSpeed = 8f;
    [SerializeField] private Vector3 offset = new Vector3(0f, 15f, 0f);

    [Header("Shake Settings")]
    [SerializeField] private float shakeFrequency = 18f;

    private float shakeIntensity;
    private float shakeDuration;
    private float shakeTimer;

    // Perlin noise seeds — randomized at startup so shakes never look identical.
    private float seedX;
    private float seedZ;

    private void Awake()
    {
        seedX = Random.Range(0f, 100f);
        seedZ = Random.Range(0f, 100f);
    }

    /// <summary>
    /// Triggers a camera shake. If a shake is already running,
    /// takes the max intensity and resets the duration.
    /// </summary>
    /// <param name="intensity">World-space displacement amplitude.</param>
    /// <param name="duration">How long the shake lasts in seconds.</param>
    public void Shake(float intensity, float duration)
    {
        shakeIntensity = Mathf.Max(shakeIntensity, intensity);
        shakeDuration = duration;
        shakeTimer = 0f;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Follow — smooth lerp toward target.
        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // Shake — applied directly on top of the lerped position, unaffected by smooth speed.
        if (shakeTimer < shakeDuration)
        {
            shakeTimer += Time.deltaTime;

            float progress = shakeTimer / shakeDuration;
            float envelope = 1f - progress; // fade out linearly

            float t = shakeTimer * shakeFrequency;
            float dx = (Mathf.PerlinNoise(seedX + t, 0f) - 0.5f) * 2f;
            float dz = (Mathf.PerlinNoise(0f, seedZ + t) - 0.5f) * 2f;

            transform.position += new Vector3(dx, 0f, dz) * shakeIntensity * envelope;
        }
        else
        {
            shakeIntensity = 0f;
        }
    }
}