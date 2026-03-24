using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// AOE salt attack triggered by right click.
/// Damages all ghosts within a radius. GhostGreen takes increased damage via saltVulnerability.
/// Includes cooldown and a Gizmo to visualize the radius in the editor.
/// </summary>
public class SaltAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private float radius = 4f;
    [SerializeField] private float damageAmount = 50f;
    [SerializeField] private float cooldown = 3f;
    [SerializeField] private LayerMask ghostLayerMask;

    [Header("Knockback")]
    [SerializeField] private float knockbackForce = 12f;

    [Header("Visual Effects")]
    [SerializeField] private SaltParticleEffect saltParticleEffect;

    // Normalized value 0-1 readable by a future UI gauge.
    public float CooldownProgress => 1f - Mathf.Clamp01(cooldownRemaining / cooldown);
    public bool IsReady => cooldownRemaining <= 0f;

    private const int MaxOverlapResults = 16;
    private readonly Collider[] overlapResults = new Collider[MaxOverlapResults];

    private float cooldownRemaining;

    private void Update()
    {
        if (cooldownRemaining > 0f)
            cooldownRemaining -= Time.deltaTime;

        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.rightButton.wasPressedThisFrame)
            TryAttack();
    }

    private void TryAttack()
    {
        if (!IsReady)
        {
            Debug.Log($"[Salt] On cooldown � {cooldownRemaining:F1}s remaining.");
            return;
        }

        PerformAttack();
        cooldownRemaining = cooldown;
    }

    private void PerformAttack()
    {
        saltParticleEffect?.Play();

        int count = Physics.OverlapSphereNonAlloc(
            transform.position, radius, overlapResults, ghostLayerMask);

        int hits = 0;
        for (int i = 0; i < count; i++)
        {
            if (!overlapResults[i].TryGetComponent(out Ghost ghost)) continue;

            ghost.TakeDamage(damageAmount, DamageSource.Salt);
            ApplyKnockback(overlapResults[i]);
            hits++;
        }

        Debug.Log($"[Salt] Attack hit {hits} ghost(s).");
    }

    /// <summary>
    /// Propels the ghost away from the player.
    /// Force is scaled by proximity: full force at contact, half force at max radius.
    /// </summary>
    private void ApplyKnockback(Collider ghostCollider)
    {
        if (!ghostCollider.TryGetComponent(out Rigidbody ghostRb)) return;

        Vector3 direction = ghostCollider.transform.position - transform.position;
        direction.y = 0f;

        // Fallback direction if the ghost is exactly on the player.
        if (direction.sqrMagnitude < 0.001f)
            direction = ghostCollider.transform.forward;

        direction.Normalize();

        // Scale force by inverse proximity: closer = stronger knockback.
        float distance = Vector3.Distance(transform.position, ghostCollider.transform.position);
        float proximityScale = 1f - Mathf.Clamp01(distance / radius);
        float scaledForce = Mathf.Lerp(knockbackForce * 0.5f, knockbackForce, proximityScale);

        ghostRb.AddForce(direction * scaledForce, ForceMode.Impulse);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 1f, 0.25f);
        Gizmos.DrawSphere(transform.position, radius);

        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
#endif
}
