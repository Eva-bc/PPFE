using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles the grab mechanic:
/// - A ghost calls Grab() on contact.
/// - The player accumulates struggle by shaking the mouse.
/// - On release, nearby grabbing ghosts are pushed back.
/// - Damage per second escalates the longer the grab lasts.
/// </summary>
[RequireComponent(typeof(PlayerHealth))]
public class PlayerGrabState : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private float baseDamagePerSecond = 10f;
    // Damage increases by this amount every second of grab.
    [SerializeField] private float damageEscalationPerSec = 2f;

    [Header("Struggle")]
    [SerializeField] private float struggleThreshold = 300f;
    [SerializeField] private float mouseDeltaScale = 1f;

    [Header("Repulsion on Release")]
    [SerializeField] private float repulsionRadius = 3f;
    [SerializeField] private float repulsionForce = 6f;
    [SerializeField] private LayerMask ghostLayerMask;

    public bool IsGrabbed { get; private set; }

    // The ghost currently grabbing the player (only one at a time).
    private Ghost grabbingGhost;

    private PlayerHealth playerHealth;
    private float struggleValue;
    private float grabDuration;

    // Accumulated damage escalation.
    private float currentDamageRate;

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
    }

    private void Update()
    {
        if (!IsGrabbed) return;

        ApplyGrabDamage();
        ReadStruggle();
    }

    // --- Grab / Release ---

    /// <summary>Called by a Ghost when it reaches the player.</summary>
    /// <param name="ghost">The ghost initiating the grab.</param>
    public void Grab(Ghost ghost)
    {
        if (IsGrabbed) return;

        IsGrabbed = true;
        grabbingGhost = ghost;
        struggleValue = 0f;
        grabDuration = 0f;
        currentDamageRate = baseDamagePerSecond;

        Debug.Log($"[PlayerGrabState] Grabbed by {ghost.name}.");
    }

    /// <summary>Releases the player, repulses nearby ghosts.</summary>
    public void Release()
    {
        if (!IsGrabbed) return;

        IsGrabbed = false;
        grabbingGhost = null;
        struggleValue = 0f;

        RepulseGhosts();

        Debug.Log("[PlayerGrabState] Escaped!");
    }

    // --- Damage ---

    private void ApplyGrabDamage()
    {
        grabDuration += Time.deltaTime;
        currentDamageRate = baseDamagePerSecond + damageEscalationPerSec * grabDuration;

        playerHealth.TakeDamage(currentDamageRate * Time.deltaTime);

        if (playerHealth.IsDead)
            Release();
    }

    // --- Struggle ---

    private void ReadStruggle()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        Vector2 delta = mouse.delta.ReadValue();
        struggleValue += delta.magnitude * mouseDeltaScale;

        if (struggleValue >= struggleThreshold)
            Release();
    }

    // --- Repulsion ---

    private void RepulseGhosts()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, repulsionRadius, ghostLayerMask);

        foreach (Collider hit in hits)
        {
            if (!hit.TryGetComponent(out Rigidbody ghostRb)) continue;

            Vector3 direction = hit.transform.position - transform.position;
            direction.y = 0f;
            direction = direction.normalized;

            ghostRb.AddForce(direction * repulsionForce, ForceMode.Impulse);
        }
    }
}
