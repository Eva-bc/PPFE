using UnityEngine;

/// <summary>
/// Manages player HP. Exposes TakeDamage() and an OnDeath event.
/// Other systems (UI, grab) subscribe to OnHealthChanged and OnDeath.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;

    // NonSerialized prevents Unity from persisting this value across editor sessions.
    [System.NonSerialized]
    private float currentHealth;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => currentHealth <= 0f;

    // Fired whenever HP changes � subscribe for UI updates.
    public event System.Action<float, float> OnHealthChanged;

    // Fired once when the player dies.
    public event System.Action OnDeath;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    private void Start()
    {
        // Broadcast initial value so any UI that subscribed during Start()
        // receives the correct fill amount after all Awake() calls have run.
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>Applies damage to the player. Ignored if already dead.</summary>
    /// <param name="amount">Raw damage amount (must be positive).</param>
    public void TakeDamage(float amount)
    {
        if (IsDead) return;

        currentHealth = Mathf.Max(currentHealth - amount, 0f);

        Debug.Log($"[PlayerHealth] TakeDamage({amount:F1}) -> {currentHealth:F1}/{maxHealth:F1}");

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (IsDead)
            Die();
    }

    private void Die()
    {
        Debug.Log("[PlayerHealth] Player died.");
        OnDeath?.Invoke();
        gameObject.SetActive(false);
    }
}
