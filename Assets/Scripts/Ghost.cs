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

    // Multiplier applied to incoming normal flashlight damage.
    [SerializeField] private float lightVulnerability = 1f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;

    private float currentHealth;
    private Rigidbody rigidBody;
    private Transform playerTransform;

    public bool IsDead => currentHealth <= 0f;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    // Fired whenever health changes — GhostHealthBar subscribes to this.
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
    /// Applies flashlight damage based on the light type.
    /// UV-only ghosts ignore normal light. Normal ghosts ignore UV light.
    /// </summary>
    /// <param name="amount">Raw damage per second.</param>
    /// <param name="isUV">True if the damage source is UV light.</param>
    public void TakeDamage(float amount, bool isUV = false)
    {
        if (IsDead) return;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        // UV-only ghost hit by normal light → no damage.
        // Normal ghost hit by UV light → no damage.
        if (IsUVOnly != isUV) return;

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
