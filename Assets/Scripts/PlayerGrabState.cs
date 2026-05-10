using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles the grab mechanic:
/// - Detects nearby ghosts each frame via OverlapSphere — no dependency on the
///   Physics Layer Collision Matrix or trigger events.
/// - Once grabbed, the player must shake the mouse LEFT / RIGHT rapidly to escape.
/// - Shakes are detected by accumulating horizontal mouse delta in short time windows
///   and counting direction reversals between consecutive windows.
/// - On release, nearby ghosts are pushed back with an impulse.
/// - Damage per second escalates the longer the grab lasts.
/// </summary>
[RequireComponent(typeof(PlayerHealth))]
public class PlayerGrabState : MonoBehaviour
{
    [Header("Grab Detection")]
    [SerializeField] private float grabRadius = 0.7f;
    [SerializeField] private LayerMask ghostLayerMask;

    [Header("Damage")]
    [SerializeField] private float baseDamagePerSecond    = 10f;
    [SerializeField] private float damageEscalationPerSec =  2f;

    [Header("Shake Detection")]
    // How many full left/right reversals are needed to escape.
    [SerializeField] private int   shakesRequired  = 6;
    // Horizontal delta is accumulated over this window before checking for a reversal.
    [SerializeField] private float windowDuration  = 0.08f;
    // Accumulated delta in the window must exceed this to count as a directional move.
    [SerializeField] private float windowThreshold = 5f;
    // Shake count decays at this rate per second — keeps pressure on the player.
    [SerializeField] private float shakeDecayRate  = 0.8f;

    [Header("Repulsion on Release")]
    [SerializeField] private float repulsionRadius = 3f;
    [SerializeField] private float repulsionForce  = 6f;

    // --- Public state ---
    public bool  IsGrabbed     { get; private set; }
    public float ShakeProgress => shakesRequired > 0
        ? Mathf.Clamp01(shakeCount / shakesRequired)
        : 0f;

    private Ghost        grabbingGhost;
    private PlayerHealth playerHealth;
    private float        grabDuration;
    private float        currentDamageRate;

    // Shake window state
    private float windowAccum;
    private float windowTimer;
    private int   lastSign;
    private float shakeCount;

    // OverlapSphere buffer
    private readonly Collider[] overlapBuffer = new Collider[8];

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
    }

    private void Update()
    {
        if (IsGrabbed)
        {
            ApplyGrabDamage();
            ReadShake();
        }
        else
        {
            CheckForGrab();
        }
    }

    // -------------------------------------------------------- Grab Detection

    /// <summary>Polls for nearby ghost colliders every frame while not grabbed.</summary>
    private void CheckForGrab()
    {
        int count = Physics.OverlapSphereNonAlloc(
            transform.position, grabRadius, overlapBuffer,
            ghostLayerMask, QueryTriggerInteraction.Collide);

        for (int i = 0; i < count; i++)
        {
            if (overlapBuffer[i].TryGetComponent(out Ghost ghost) && !ghost.IsDead)
            {
                Grab(ghost);
                return;
            }
        }
    }

    // ----------------------------------------------------------- Grab / Release

    /// <summary>Initiates the grab state.</summary>
    public void Grab(Ghost ghost)
    {
        if (IsGrabbed) return;

        IsGrabbed         = true;
        grabbingGhost     = ghost;
        grabDuration      = 0f;
        currentDamageRate = baseDamagePerSecond;

        ResetShake();
        Debug.Log($"[PlayerGrabState] Grabbed by {ghost.name}.");
    }

    /// <summary>Releases the player and repulses nearby ghosts.</summary>
    public void Release()
    {
        if (!IsGrabbed) return;

        IsGrabbed     = false;
        grabbingGhost = null;

        ResetShake();
        RepulseGhosts();
        Debug.Log("[PlayerGrabState] Escaped!");
    }

    // -------------------------------------------------------------- Damage

    private void ApplyGrabDamage()
    {
        grabDuration      += Time.deltaTime;
        currentDamageRate  = baseDamagePerSecond + damageEscalationPerSec * grabDuration;

        playerHealth.TakeDamage(currentDamageRate * Time.deltaTime);

        if (playerHealth.IsDead)
            Release();
    }

    // --------------------------------------------------- Shake Detection

    private void ReadShake()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        float dx = mouse.delta.ReadValue().x;

        windowAccum += dx;
        windowTimer += Time.deltaTime;

        if (windowTimer >= windowDuration)
        {
            Debug.Log($"[ShakeDebug] windowAccum={windowAccum:F2}  threshold=±{windowThreshold}  lastSign={lastSign}  shakeCount={shakeCount:F2}");

            int currentSign = 0;
            if (windowAccum >  windowThreshold) currentSign =  1;
            if (windowAccum < -windowThreshold) currentSign = -1;

            // Reversal = direction flipped from last non-zero window.
            if (currentSign != 0 && lastSign != 0 && currentSign != lastSign)
            {
                shakeCount += 1f;
                Debug.Log($"[PlayerGrabState] Shake {shakeCount}/{shakesRequired}");

                if (shakeCount >= shakesRequired)
                {
                    Release();
                    return;
                }
            }

            if (currentSign != 0)
                lastSign = currentSign;

            windowAccum = 0f;
            windowTimer = 0f;
        }

        // Decay — must keep shaking.
        shakeCount = Mathf.Max(0f, shakeCount - shakeDecayRate * Time.deltaTime);
    }

    private void ResetShake()
    {
        shakeCount  = 0f;
        windowAccum = 0f;
        windowTimer = 0f;
        lastSign    = 0;
    }

    // ----------------------------------------------------------- Repulsion

    private void RepulseGhosts()
    {
        int count = Physics.OverlapSphereNonAlloc(
            transform.position, repulsionRadius, overlapBuffer,
            ghostLayerMask, QueryTriggerInteraction.Collide);

        for (int i = 0; i < count; i++)
        {
            if (overlapBuffer[i].TryGetComponent(out Rigidbody ghostRb))
            {
                Vector3 dir = overlapBuffer[i].transform.position - transform.position;
                dir.y = 0f;
                dir   = dir.normalized;
                ghostRb.AddForce(dir * repulsionForce, ForceMode.Impulse);
            }
        }
    }
}
