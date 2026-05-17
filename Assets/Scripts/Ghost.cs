using UnityEngine;

/// <summary>
/// Identifies the source of damage applied to a ghost.
/// Each ghost type can define its own vulnerability per source.
/// </summary>
public enum DamageSource { Light, UVLight, Salt }

/// <summary>
/// Abstract base class for all ghost types.
/// Handles HP, damage from any source, movement toward the player, and death.
/// </summary>
[RequireComponent(typeof(Rigidbody))]

public abstract class Ghost : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;

    [Header("Vulnerabilities")]
    [SerializeField] private float lightVulnerability = 1f;
    [SerializeField] private float saltVulnerability = 1f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;

    [Header("Grab Damage")]
    [SerializeField] private float grabDamageMultiplier = 1f;

    [Header("Separation")]
    [SerializeField] private float separationRadius = 1.5f;
    [SerializeField] private float separationForce  = 2f;

    private float currentHealth;
    private Rigidbody rigidBody;
    private Transform playerTransform;

    // Reusable buffer for separation overlap checks — avoids per-frame allocations.
    private readonly Collider[] separationBuffer = new Collider[8];

    public bool IsDead => currentHealth <= 0f;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    /// <summary>Multiplier applied to grab damage dealt to the player by this ghost type.</summary>
    public float GrabDamageMultiplier => grabDamageMultiplier;

    // Repulsion state — suspends autonomous movement briefly after being pushed.
    private bool  isRepulsed;
    private float repulsionTimer;

    /// <summary>
    /// Applies an instant impulse and suspends movement for the given duration.
    /// Called by PlayerGrabState on escape.
    /// </summary>
    public void Repulse(Vector3 force, float duration)
    {
        rigidBody.AddForce(force, ForceMode.Impulse);
        isRepulsed    = true;
        repulsionTimer = duration;
    }

    // Fired after health changes — GhostHealthBar subscribes to this.
    public event System.Action<float, float> OnHealthChanged;

    // Override to true in subclasses that are only hurt by UV light.
    public virtual bool IsUVOnly => false;

    protected virtual void Awake()
    {
        currentHealth = maxHealth;
        rigidBody = GetComponent<Rigidbody>();

        rigidBody.constraints = RigidbodyConstraints.FreezeRotationX
                              | RigidbodyConstraints.FreezeRotationZ
                              | RigidbodyConstraints.FreezePositionY;

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
            playerTransform = player.transform;
        else
            Debug.LogWarning($"[Ghost] No GameObject tagged 'Player' found. {name} won't move.");
    }

    protected virtual void FixedUpdate()
    {
        if (isRepulsed)
        {
            repulsionTimer -= Time.fixedDeltaTime;
            if (repulsionTimer <= 0f)
                isRepulsed = false;
            return; // skip autonomous movement while repulsed
        }

        MoveTowardPlayer();
        RotateTowardPlayer();
    }

    // --- Movement ---

    private void MoveTowardPlayer()
    {
        if (playerTransform == null || IsDead) return;

        Vector3 toPlayer = playerTransform.position - transform.position;
        toPlayer.y = 0f;
        Vector3 moveDir = toPlayer.normalized;

        // Steering separation: accumulate a push vector away from every nearby ghost.
        Vector3 separation = ComputeSeparation();

        Vector3 finalDir = (moveDir + separation).normalized;

        rigidBody.linearVelocity = new Vector3(
            finalDir.x * moveSpeed,
            rigidBody.linearVelocity.y,
            finalDir.z * moveSpeed);
    }

    /// <summary>
    /// Computes a lateral separation vector to keep this ghost from overlapping its neighbours.
    /// </summary>
    private Vector3 ComputeSeparation()
    {
        int count = Physics.OverlapSphereNonAlloc(
            transform.position, separationRadius, separationBuffer);

        Vector3 push = Vector3.zero;

        for (int i = 0; i < count; i++)
        {
            Collider neighbour = separationBuffer[i];

            // Ignore self and non-ghost colliders.
            if (neighbour.gameObject == gameObject) continue;
            if (!neighbour.TryGetComponent(out Ghost _)) continue;

            Vector3 away = transform.position - neighbour.transform.position;
            away.y = 0f;

            float distance = away.magnitude;
            if (distance < 0.001f)
            {
                // Perfectly overlapping: push in a pseudo-random but stable direction.
                away = new Vector3(transform.position.x + 0.01f, 0f, transform.position.z);
                distance = 0.01f;
            }

            // Closer neighbours exert a stronger push.
            float weight = 1f - Mathf.Clamp01(distance / separationRadius);
            push += away.normalized * (weight * separationForce);
        }

        return push;
    }

    private void RotateTowardPlayer()
    {
        if (playerTransform == null || IsDead) return;

        Vector3 direction = playerTransform.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        rigidBody.MoveRotation(Quaternion.Slerp(rigidBody.rotation, targetRotation, 10f * Time.fixedDeltaTime));
    }

    // --- Damage ---

    /// <summary>
    /// Applies damage from a given source, scaled by this ghost's vulnerability.
    /// UV-only ghosts ignore Light. Salt bypasses the light system entirely.
    /// </summary>
    /// <param name="amount">Raw damage amount.</param>
    /// <param name="source">Source of the damage.</param>
    public void TakeDamage(float amount, DamageSource source = DamageSource.Light)
    {
        if (IsDead) return;

        // Light immunity: UV-only ghosts ignore normal light and vice versa.
        if (source == DamageSource.Light && IsUVOnly) return;
        if (source == DamageSource.UVLight && !IsUVOnly) return;

        float vulnerability = source == DamageSource.Salt ? saltVulnerability : lightVulnerability;
        float scaledDamage = amount * vulnerability;

        currentHealth -= scaledDamage;
        currentHealth = Mathf.Max(currentHealth, 0f);

        // Fire event AFTER updating health so the bar reflects the new value.
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        OnDamageReceived(scaledDamage, source);

        if (IsDead)
            Die();
    }

    // --- Death ---

    private void Die()
    {
        // OnDeath() is called first so subclasses can still access GhostDeathVFX
        // before the GameObject is destroyed.
        OnDeath();
        Destroy(gameObject);
    }

    // --- Overridable Hooks ---

    /// <summary>Called each time this ghost takes damage. Override to add reactions.</summary>
    protected virtual void OnDamageReceived(float amount, DamageSource source) { }

    /// <summary>Called just before the ghost is destroyed. Override for VFX, events, etc.</summary>
    protected virtual void OnDeath()
    {
        Debug.Log($"[Ghost] {name} died.");
    }
}
