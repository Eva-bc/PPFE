using UnityEngine;

/// <summary>Tank ghost. High HP, low vulnerability to light.</summary>
public class GhostPurple : Ghost
{
    protected override void OnDeath()
    {
        Debug.Log($"[GhostPurple] {name} shattered.");
        base.OnDeath();
    }
}
