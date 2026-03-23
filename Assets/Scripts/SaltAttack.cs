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

    // Normalized value 0–1 readable by a future UI gauge.
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
            Debug.Log($"[Salt] On cooldown — {cooldownRemaining:F1}s remaining.");
            return;
        }

        PerformAttack();
        cooldownRemaining = cooldown;
    }

    private void PerformAttack()
    {
        int count = Physics.OverlapSphereNonAlloc(
            transform.position, radius, overlapResults, ghostLayerMask);

        int hits = 0;
        for (int i = 0; i < count; i++)
        {
            if (overlapResults[i].TryGetComponent(out Ghost ghost))
            {
                ghost.TakeDamage(damageAmount, DamageSource.Salt);
                hits++;
            }
        }

        Debug.Log($"[Salt] Attack hit {hits} ghost(s).");
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
