using UnityEngine;

/// <summary>Tank ghost. Only damaged by UV light — immune to normal flashlight.</summary>
public class GhostPurple : Ghost
{
    public override bool IsUVOnly => true;

    /// <summary>Spawns the purple plasma puddle when this ghost dies.</summary>
    protected override void OnDeath()
    {
        base.OnDeath();
        if (TryGetComponent(out GhostDeathVFX deathVFX))
            deathVFX.SpawnPuddle();
    }
}
