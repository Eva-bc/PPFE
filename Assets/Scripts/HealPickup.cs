using UnityEngine;

/// <summary>
/// Collectible that fully restores the player's HP when picked up.
/// Plays optional VFX and audio on collection, then destroys itself.
/// </summary>
[RequireComponent(typeof(Collider))]
public class HealPickup : MonoBehaviour
{
    [Header("Idle Animation")]
    [Tooltip("Rotation speed in degrees per second.")]
    [SerializeField] private float rotationSpeed = 90f;

    [Tooltip("Vertical bob amplitude in world units.")]
    [SerializeField] private float bobAmplitude = 0.2f;

    [Tooltip("Vertical bob frequency in Hz.")]
    [SerializeField] private float bobFrequency = 1f;

    [Header("Feedback")]
    [Tooltip("Optional particle effect played at collection.")]
    [SerializeField] private ParticleSystem collectVFX;

    [Tooltip("Optional audio clip played at collection.")]
    [SerializeField] private AudioClip collectSound;

    private AudioSource audioSource;
    private Vector3 startPosition;
    private bool collected;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;

        startPosition = transform.position;
    }

    private void Update()
    {
        if (collected) return;

        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        float newY = startPosition.y + Mathf.Sin(Time.time * bobFrequency * Mathf.PI * 2f) * bobAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected) return;
        if (!other.CompareTag("Player")) return;

        PlayerHealth health = other.GetComponentInParent<PlayerHealth>()
                           ?? other.GetComponent<PlayerHealth>();

        if (health == null)
        {
            Debug.LogWarning("[HealPickup] Player entered trigger but no PlayerHealth found.");
            return;
        }

        Collect(health);
    }

    private void Collect(PlayerHealth health)
    {
        collected = true;

        health.Heal(Mathf.Infinity);

        if (collectVFX != null)
        {
            ParticleSystem vfx = Instantiate(collectVFX, transform.position, Quaternion.identity);
            vfx.Play();
            Destroy(vfx.gameObject, vfx.main.duration + vfx.main.startLifetime.constantMax);
        }

        if (collectSound != null)
            audioSource.PlayOneShot(collectSound);

        DisableVisuals();
        float destroyDelay = collectSound != null ? collectSound.length : 0f;
        Destroy(gameObject, destroyDelay + 0.1f);

        Debug.Log("[HealPickup] Player fully healed.");
    }

    private void DisableVisuals()
    {
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
            r.enabled = false;

        GetComponent<Collider>().enabled = false;
    }
}
