using UnityEngine;

/// <summary>Tank ghost. Only damaged by UV light — immune to normal flashlight.</summary>
public class GhostPurple : Ghost
{
    public override bool IsUVOnly => true;

    protected override void Awake()
    {
        base.Awake();
        // Notify the tutorial that a UV-type ghost has appeared (fires at most once).
        TutorialManager.Instance?.NotifyUVGhostSpawned();
    }

    /// <summary>Spawns the purple plasma puddle when this ghost dies.</summary>
    protected override void OnDeath()
    {
        base.OnDeath();
        if (TryGetComponent(out GhostDeathVFX deathVFX))
            deathVFX.SpawnPuddle();
    }
}
