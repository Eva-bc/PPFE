using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles the grab mechanic:
/// - Detects nearby ghosts each frame via OverlapSphere.
/// - Once grabbed, the player is immobilised and must click the left mouse
///   button <see cref="clicksRequired"/> times within <see cref="clickWindow"/>
///   seconds to escape.
/// - On release, nearby ghosts are pushed back with an impulse.
/// - Damage is applied per second while grabbed.
/// </summary>
[RequireComponent(typeof(PlayerHealth))]
public class PlayerGrabState : MonoBehaviour
{
    [Header("Grab Detection")]
    [SerializeField] private float grabRadius = 0.7f;
    [SerializeField] private LayerMask ghostLayerMask;

    [Header("Escape — Click")]
    /// <summary>Number of left-clicks required to escape.</summary>
    [SerializeField] private int   clicksRequired = 5;
    /// <summary>Time window (seconds) in which all clicks must occur. Resets on first click.</summary>
    [SerializeField] private float clickWindow    = 3f;

    [Header("Damage")]
    [SerializeField] private float baseDamagePerSecond    = 5f;
    [SerializeField] private float damageEscalationPerSec = 1f;

    [Header("Repulsion on Release")]
    [SerializeField] private float repulsionRadius   = 3f;
    [SerializeField] private float repulsionForce    = 10f;
    [SerializeField] private float repulsionDuration = 1.2f; // seconds ghosts can't move after repulsion
    [SerializeField] private float regrabCooldown    = 2f;   // seconds before a ghost can grab again

    [Header("Audio")]
    [Tooltip("Sound played when the player is grabbed by a ghost.")]
    [SerializeField] private AudioClip grabSound;

    private AudioSource audioSource;

    // --- Events ---
    /// <summary>Fired when the player is grabbed. Passes the total clicks required.</summary>
    public event Action<int> OnGrabbed;
    /// <summary>Fired on each valid click while grabbed. Passes the current click count.</summary>
    public event Action<int> OnClickRegistered;
    /// <summary>Fired when the player successfully escapes.</summary>
    public event Action OnReleased;

    // --- Public state ---
    public bool IsGrabbed { get; private set; }

    /// <summary>0–1 progress toward escape, used by the HUD.</summary>
    public float ShakeProgress => clicksRequired > 0
        ? Mathf.Clamp01((float)clickCount / clicksRequired)
        : 0f;

    private Ghost        grabbingGhost;
    private PlayerHealth playerHealth;
    private float        grabDuration;

    // Click state
    private int   clickCount;
    private float clickTimer;
    private bool  windowOpen;
    private float regrabTimer; // counts down after release

    // OverlapSphere buffer
    private readonly Collider[] overlapBuffer = new Collider[8];

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake  = false;
        audioSource.spatialBlend = 0f;
    }

    private void Update()
    {
        if (IsGrabbed)
        {
            ReadClicks();
            if (IsGrabbed) // ReadClicks may call Release()
                ApplyGrabDamage();
        }
        else
        {
            if (regrabTimer > 0f)
            {
                regrabTimer -= Time.deltaTime;
                return; // invincibility window after escape
            }
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

        IsGrabbed     = true;
        grabbingGhost = ghost;
        grabDuration  = 0f;

        ResetClicks();

        if (grabSound != null)
            audioSource.PlayOneShot(grabSound);

        Debug.Log($"[PlayerGrabState] Grabbed by {ghost.name}.");
        OnGrabbed?.Invoke(clicksRequired);
    }

    /// <summary>Releases the player and repulses nearby ghosts.</summary>
    public void Release()
    {
        if (!IsGrabbed) return;

        IsGrabbed     = false;
        grabbingGhost = null;

        ResetClicks();
        RepulseGhosts();
        regrabTimer = regrabCooldown;
        Debug.Log("[PlayerGrabState] Escaped!");
        OnReleased?.Invoke();
    }

    // -------------------------------------------------------------- Damage

    private void ApplyGrabDamage()
    {
        grabDuration += Time.deltaTime;
        float rate    = baseDamagePerSecond + damageEscalationPerSec * grabDuration;
        float multiplier = grabbingGhost != null ? grabbingGhost.GrabDamageMultiplier : 1f;
        playerHealth.TakeDamage(rate * multiplier * Time.deltaTime);

        if (playerHealth.IsDead)
            Release();
    }

    // --------------------------------------------------- Click Detection

    private void ReadClicks()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        // Tick down the active click window.
        if (windowOpen)
        {
            clickTimer -= Time.deltaTime;
            if (clickTimer <= 0f)
                ResetClicks(); // window expired — reset progress
        }

        // Detect a fresh left-click (pressed this frame only).
        if (!mouse.leftButton.wasPressedThisFrame) return;

        if (!windowOpen)
        {
            // First click — open the time window.
            windowOpen = true;
            clickTimer = clickWindow;
        }

        clickCount++;
        Debug.Log($"[PlayerGrabState] Click {clickCount}/{clicksRequired}");
        OnClickRegistered?.Invoke(clickCount);

        if (clickCount >= clicksRequired)
            Release();
    }

    private void ResetClicks()
    {
        clickCount = 0;
        clickTimer = 0f;
        windowOpen = false;
    }

    // ----------------------------------------------------------- Repulsion

    private void RepulseGhosts()
    {
        int count = Physics.OverlapSphereNonAlloc(
            transform.position, repulsionRadius, overlapBuffer,
            ghostLayerMask, QueryTriggerInteraction.Collide);

        for (int i = 0; i < count; i++)
        {
            if (overlapBuffer[i].TryGetComponent(out Ghost ghost))
            {
                Vector3 dir = (overlapBuffer[i].transform.position - transform.position);
                dir.y = 0f;
                dir   = dir.normalized;
                ghost.Repulse(dir * repulsionForce, repulsionDuration);
            }
        }
    }
}
