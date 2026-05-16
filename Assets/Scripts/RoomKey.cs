using System;
using UnityEngine;

/// <summary>
/// Represents the key that spawns at the center of a room when all enemies are defeated.
/// The player collects it by walking into its trigger collider.
/// Once collected, fires <see cref="OnKeyCollected"/> so the RoomManager can open the exit door.
/// </summary>
[RequireComponent(typeof(Collider))]
public class RoomKey : MonoBehaviour
{
    [Header("Feedback")]
    [Tooltip("AudioClip played when the player picks up the key.")]
    [SerializeField] private AudioClip collectSound;

    [Tooltip("Optional particle effect played at the key's position when collected.")]
    [SerializeField] private ParticleSystem collectVFX;

    [Tooltip("Rotation speed in degrees per second for the idle spin.")]
    [SerializeField] private float rotationSpeed = 90f;

    [Tooltip("Vertical bob amplitude in world units.")]
    [SerializeField] private float bobAmplitude = 0.15f;

    [Tooltip("Vertical bob frequency in Hz.")]
    [SerializeField] private float bobFrequency = 1f;

    /// <summary>Fired once when the player collects the key.</summary>
    public event Action OnKeyCollected;

    private AudioSource audioSource;
    private Vector3 startPosition;
    private bool collected;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;

        startPosition = transform.position;
    }

    private void Update()
    {
        if (collected) return;

        // Idle animation: spin + vertical bob.
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        float newY = startPosition.y + Mathf.Sin(Time.time * bobFrequency * Mathf.PI * 2f) * bobAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected) return;
        if (!other.CompareTag("Player")) return;

        Collect();
    }

    private void Collect()
    {
        collected = true;

        if (collectVFX != null)
        {
            ParticleSystem vfx = Instantiate(collectVFX, transform.position, Quaternion.identity);
            vfx.Play();
            Destroy(vfx.gameObject, vfx.main.duration + vfx.main.startLifetime.constantMax);
        }

        if (collectSound != null)
            audioSource.PlayOneShot(collectSound);

        OnKeyCollected?.Invoke();

        // Disable visual immediately; destroy after the sound finishes.
        DisableVisuals();
        float destroyDelay = collectSound != null ? collectSound.length : 0f;
        Destroy(gameObject, destroyDelay + 0.1f);
    }

    private void DisableVisuals()
    {
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
            r.enabled = false;

        GetComponent<Collider>().enabled = false;
    }
}
