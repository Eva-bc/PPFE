using UnityEngine;

/// <summary>
/// Tank ghost. Only damaged by UV light — immune to normal flashlight.
/// </summary>
public class GhostPurple : Ghost
{
    public override bool IsUVOnly => true;

    protected override void OnDeath()
    {
        Debug.Log($"[GhostPurple] {name} shattered.");
        base.OnDeath();
    }
}
