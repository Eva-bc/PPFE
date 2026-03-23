using UnityEngine;

/// <summary>Fast ghost, low HP. Flashes on damage.</summary>
public class GhostGreen : Ghost
{
    protected override void OnDamageReceived(float amount)
    {
        Debug.Log($"[GhostGreen] {name} flinches — took {amount:F1} damage.");
    }
}
