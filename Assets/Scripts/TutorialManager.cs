using UnityEngine;

/// <summary>
/// Drives the progressive in-game tutorial for Room 1.
///
/// Trigger rules (Room 1 only, each fires at most once):
///   Step 1 — 1st ghost spawned  → show "how to use the flashlight" image.
///   Step 2 — 2nd ghost spawned  → show "how to throw salt" image.
///   Step 3 — 1st UV-vulnerable ghost (GhostPurple) spawned
///             → show "how to use the UV lamp" image.
///
/// The UV step is tracked separately (any spawn index) because the purple
/// ghost may appear in phase 3 of Room 1 while being the Nth ghost overall.
/// </summary>
public class TutorialManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────
    public static TutorialManager Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Room 1 WaveManager")]
    [Tooltip("WaveManager component of Room 1's RoomTrigger. Assign in the Inspector.")]
    [SerializeField] private WaveManager room1WaveManager;

    [Header("Overlay")]
    [SerializeField] private TutorialOverlay overlay;

    [Header("Tutorial Images")]
    [Tooltip("Displayed when the 1st ghost spawns (flashlight usage).")]
    [SerializeField] private Texture2D flashlightTutorial;

    [Tooltip("Displayed when the 2nd ghost spawns (salt throw usage).")]
    [SerializeField] private Texture2D saltTutorial;

    [Tooltip("Displayed when the 1st UV-type ghost spawns (UV lamp usage).")]
    [SerializeField] private Texture2D uvLampTutorial;

    [Header("UV Ghost Type")]
    [Tooltip("Prefab or type name prefix used to identify UV-vulnerable ghosts. " +
             "Leave empty to use the default 'GhostPurple' type name check.")]
    [SerializeField] private string uvGhostTypeName = "GhostPurple";

    // ── State ─────────────────────────────────────────────────────────────────
    private bool _step1Done;
    private bool _step2Done;
    private bool _step3Done;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        if (room1WaveManager != null)
            room1WaveManager.OnGhostSpawned += HandleGhostSpawned;
    }

    private void OnDisable()
    {
        if (room1WaveManager != null)
            room1WaveManager.OnGhostSpawned -= HandleGhostSpawned;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by WaveManager (Room 1) each time a ghost is spawned.
    /// spawnIndex is 1-based.
    /// </summary>
    private void HandleGhostSpawned(int spawnIndex)
    {
        if (spawnIndex == 1 && !_step1Done)
        {
            _step1Done = true;
            ShowStep(flashlightTutorial);
            return;
        }

        if (spawnIndex == 2 && !_step2Done)
        {
            _step2Done = true;
            ShowStep(saltTutorial);
        }
    }

    /// <summary>
    /// Call this from GhostPurple's Awake to trigger the UV tutorial step.
    /// </summary>
    public void NotifyUVGhostSpawned()
    {
        if (_step3Done) return;
        _step3Done = true;
        ShowStep(uvLampTutorial);
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private void ShowStep(Texture2D texture)
    {
        if (overlay == null)
        {
            Debug.LogWarning("[TutorialManager] TutorialOverlay is not assigned.");
            return;
        }

        if (texture == null)
        {
            Debug.LogWarning("[TutorialManager] Tutorial texture is null — step skipped.");
            return;
        }

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f)
        );
        overlay.Show(sprite);
    }
}
