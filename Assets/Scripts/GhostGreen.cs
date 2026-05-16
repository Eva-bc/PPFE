using UnityEngine;

/// <summary>Fast ghost. Low HP, highly vulnerable to salt.</summary>
public class GhostGreen : Ghost
{
    protected override void OnDamageReceived(float amount, DamageSource source)
    {
        Debug.Log($"[GhostGreen] {name} flinches — {source} hit for {amount:F1}.");
    }

    /// <summary>Spawns the green plasma puddle when this ghost dies.</summary>
    protected override void OnDeath()
    {
        base.OnDeath();
        if (TryGetComponent(out GhostDeathVFX deathVFX))
            deathVFX.SpawnPuddle();
    }
}
