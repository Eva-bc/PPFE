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

    private float currentHealth;
    private Rigidbody rigidBody;
    private Transform playerTransform;

    public bool IsDead => currentHealth <= 0f;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    // Fired after health changes — GhostHealthBar subscribes to this.
    public event System.Action<float, float> OnHealthChanged;

    // Override to true in subclasses that are only hurt by UV light.
    public virtual bool IsUVOnly => false;

    protected virtual void Awake()
    {
        currentHealth = maxHealth;
        rigidBody = GetComponent<Rigidbody>();

        rigidBody.constraints = RigidbodyConstraints.FreezeRotation
                              | RigidbodyConstraints.FreezePositionY;

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
            playerTransform = player.transform;
        else
            Debug.LogWarning($"[Ghost] No GameObject tagged 'Player' found. {name} won't move.");
    }

    protected virtual void FixedUpdate()
    {
        MoveTowardPlayer();
    }

    // --- Movement ---

    private void MoveTowardPlayer()
    {
        if (playerTransform == null || IsDead) return;

        Vector3 direction = playerTransform.position - transform.position;
        direction.y = 0f;
        direction = direction.normalized;

        rigidBody.MovePosition(rigidBody.position + direction * moveSpeed * Time.fixedDeltaTime);
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
