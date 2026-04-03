using UnityEngine;

/// <summary>
/// Represents a door that can be locked or unlocked.
/// Optionally animates via an Animator with an "IsOpen" bool parameter.
/// </summary>
public class Door : MonoBehaviour
{
    [Header("State")]
    [SerializeField] private bool startLocked = true;

    [Header("Collider")]
    [SerializeField] private Collider doorCollider;

    [Header("Animation (optional)")]
    [SerializeField] private Animator animator;

    private static readonly int AnimatorIsOpen = Animator.StringToHash("IsOpen");

    private bool isLocked;

    private void Awake()
    {
        isLocked = startLocked;
        ApplyState();
    }

    /// <summary>Locks the door: enables the collider and closes the animation.</summary>
    public void Lock()
    {
        isLocked = true;
        ApplyState();
    }

    /// <summary>Unlocks the door: disables the collider and opens the animation.</summary>
    public void Unlock()
    {
        isLocked = false;
        ApplyState();
    }

    public bool IsLocked => isLocked;

    private void ApplyState()
    {
        if (doorCollider != null)
            doorCollider.enabled = isLocked;

        if (animator != null)
            animator.SetBool(AnimatorIsOpen, !isLocked);
    }
}
