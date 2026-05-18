using UnityEngine;

/// <summary>
/// Marks a trigger volume as an interior zone (inside a room/corridor).
/// Notifies RainController when the player enters or exits.
/// Requires a BoxCollider set to isTrigger = true on this GameObject.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class InteriorZone : MonoBehaviour
{
    private void Awake()
    {
        var col = GetComponent<BoxCollider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        RainController.Instance?.OnPlayerEnteredInterior(this);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        RainController.Instance?.OnPlayerExitedInterior(this);
    }
}
