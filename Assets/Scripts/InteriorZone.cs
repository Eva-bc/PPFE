using UnityEngine;

/// <summary>
/// Place this component on a trigger collider that covers an interior room.
/// When the player enters, it notifies RainController to hide the rain.
/// When the player exits, the rain is restored.
/// </summary>
[RequireComponent(typeof(Collider))]
public class InteriorZone : MonoBehaviour
{
    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        RainController.Instance?.OnPlayerEnterInterior();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        RainController.Instance?.OnPlayerExitInterior();
    }
}
