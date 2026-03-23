using UnityEngine;

/// <summary>Fast ghost. Low HP, highly vulnerable to salt.</summary>
public class GhostGreen : Ghost
{
    protected override void OnDamageReceived(float amount, DamageSource source)
    {
        Debug.Log($"[GhostGreen] {name} flinches — {source} hit for {amount:F1}.");
    }
}
