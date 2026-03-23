using UnityEngine;

/// <summary>
/// Abstract base class for all ghost types.
/// Handles HP, damage from the flashlight, movement toward the player, and death.
/// Subclasses configure stats and can override behavior hooks.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public abstract class Ghost : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;

    // Multiplier applied to incoming flashlight damage (higher = more vulnerable).
    [SerializeField] private float lightVulnerability = 1f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;

    private float currentHealth;
    private Rigidbody rigidBody;
    private Transform playerTransform;

    public bool IsDead => currentHealth <= 0f;

    protected virtual void Awake()
    {
        currentHealth = maxHealth;
        rigidBody = GetComponent<Rigidbody>();

        rigidBody.constraints = RigidbodyConstraints.FreezeRotation
                              | RigidbodyConstraints.FreezePositionY;

        // The Player GameObject must be tagged "Player".
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
    /// Applies flashlight damage, scaled by this ghost's light vulnerability.
    /// </summary>
    /// <param name="amount">Raw damage per second from the flashlight.</param>
    public void TakeDamage(float amount)
    {
        if (IsDead) return;

        float scaledDamage = amount * lightVulnerability;
        currentHealth -= scaledDamage;
        currentHealth = Mathf.Max(currentHealth, 0f);

        OnDamageReceived(scaledDamage);

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
    protected virtual void OnDamageReceived(float amount) { }

    /// <summary>Called just before the ghost is destroyed. Override to spawn VFX, notify GameManager, etc.</summary>
    protected virtual void OnDeath()
    {
        Debug.Log($"[Ghost] {name} died.");
    }
}
