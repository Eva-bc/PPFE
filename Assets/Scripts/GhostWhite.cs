using UnityEngine;

/// <summary>Standard ghost. Balanced stats.</summary>
public class GhostWhite : Ghost
{
    /// <summary>Spawns the white plasma puddle when this ghost dies.</summary>
    protected override void OnDeath()
    {
        base.OnDeath();
        if (TryGetComponent(out GhostDeathVFX deathVFX))
            deathVFX.SpawnPuddle();
    }
}
