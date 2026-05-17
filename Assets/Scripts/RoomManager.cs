using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Manages a single room: detects player entry, triggers all ghost phases,
/// spawns the room key once all enemies are defeated, and opens the exit door
/// when the key is collected.
///
/// Passage is ALWAYS free by default. The only blocking element is the Door
/// GameObject itself. This script never locks or blocks the passage in any way.
/// </summary>
[RequireComponent(typeof(Collider))]
public class RoomManager : MonoBehaviour
{
    [Header("Wave")]
    [Tooltip("The RoomData asset that defines all ghost phases for this room.")]
    [SerializeField] private RoomData roomData;

    [SerializeField] private WaveManager waveManager;

    [Header("Doors")]
    [Tooltip("Door(s) that open when the key is collected.")]
    [SerializeField] private Door[] exitDoors;

    [Header("Key")]
    [Tooltip("Prefab of the RoomKey to spawn when all enemies are defeated.")]
    [SerializeField] private RoomKey keyPrefab;

    [Tooltip("World position where the key will appear. Defaults to the RoomManager's position if left empty.")]
    [SerializeField] private Transform keySpawnPoint;

    [Header("Audio (optional)")]
    [Tooltip("AudioClip played when a new ghost wave starts.")]
    [SerializeField] private AudioClip waveStartSound;

    [Header("Events (optional)")]
    [Tooltip("Fired when the player enters the room and the wave starts.")]
    public UnityEvent onWaveStarted;

    [Tooltip("Fired when all enemies are defeated (before key spawn).")]
    public UnityEvent onRoomCleared;

    [Tooltip("Fired when the player collects the key and the exit door opens.")]
    public UnityEvent onKeyCollected;

    [Header("UI (optional)")]
    [Tooltip("GameObject shown while the wave is active.")]
    [SerializeField] private GameObject waveActiveUI;

    [Tooltip("GameObject shown when the room is cleared (before key collection).")]
    [SerializeField] private GameObject roomClearUI;

    [Tooltip("How long the 'Room Clear' UI stays visible.")]
    [SerializeField] private float roomClearUIDisplayDuration = 2f;

    private bool hasBeenTriggered;
    private AudioSource audioSource;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;

        AutoResolveExitDoors();

        SetUI(waveActiveUI, false);
        SetUI(roomClearUI, false);
    }

    /// <summary>
    /// If exitDoors is empty or contains only nulls, searches the room's immediate
    /// parent group (e.g. "=== 1 Room ===") for Door components and uses them.
    /// Only searches one level up to avoid picking up doors from other rooms.
    /// </summary>
    private void AutoResolveExitDoors()
    {
        bool hasValidDoor = false;
        if (exitDoors != null)
        {
            foreach (Door d in exitDoors)
            {
                if (d != null) { hasValidDoor = true; break; }
            }
        }

        if (hasValidDoor) return;

        // RoomTrigger -> Room1 -> "=== 1 Room ===" (grandparent of RoomTrigger).
        Transform searchRoot = transform.parent?.parent;
        if (searchRoot == null)
        {
            Debug.LogWarning($"[RoomManager] '{name}': cannot auto-resolve exitDoors — grandparent not found.");
            return;
        }

        Door[] found = searchRoot.GetComponentsInChildren<Door>(true);
        if (found.Length > 0)
        {
            exitDoors = found;
            Debug.Log($"[RoomManager] '{name}': auto-resolved {found.Length} exit door(s) from '{searchRoot.name}'.");
        }
        else
        {
            Debug.LogWarning($"[RoomManager] '{name}': no Door components found in '{searchRoot.name}'. Assign exitDoors in the Inspector.");
        }
    }

    private void OnEnable()
    {
        if (waveManager != null)
        {
            waveManager.OnEnemyDied   += HandleEnemyDied;
            waveManager.OnWaveCleared += HandleWaveCleared;
        }
    }

    private void OnDisable()
    {
        if (waveManager != null)
        {
            waveManager.OnEnemyDied   -= HandleEnemyDied;
            waveManager.OnWaveCleared -= HandleWaveCleared;
        }
    }

    // --- Player Detection ---

    private void OnTriggerEnter(Collider other)
    {
        if (hasBeenTriggered) return;
        if (!other.CompareTag("Player")) return;

        hasBeenTriggered = true;
        StartRoom();
    }

    // --- Room Lifecycle ---

    private void StartRoom()
    {
        SetUI(waveActiveUI, true);

        if (waveStartSound != null)
            audioSource.PlayOneShot(waveStartSound);

        waveManager.StartWave(roomData);
        onWaveStarted?.Invoke();

        Debug.Log($"[RoomManager] Room '{name}' started.");
    }

    private void HandleEnemyDied(int remaining)
    {
        Debug.Log($"[RoomManager] Enemies remaining in '{name}': {remaining}");
    }

    private void HandleWaveCleared()
    {
        SetUI(waveActiveUI, false);
        onRoomCleared?.Invoke();

        Debug.Log($"[RoomManager] Room '{name}' cleared — spawning key.");

        if (roomClearUI != null)
            StartCoroutine(ShowRoomClearUI());

        SpawnKey();
    }

    // --- Key ---

    private void SpawnKey()
    {
        if (keyPrefab == null)
        {
            Debug.LogWarning($"[RoomManager] '{name}': keyPrefab is not assigned — opening doors directly.");
            OpenExitDoors();
            return;
        }

        Vector3 spawnPos = keySpawnPoint != null ? keySpawnPoint.position : transform.position;
        RoomKey key = Instantiate(keyPrefab, spawnPos, Quaternion.identity);
        key.OnKeyCollected += HandleKeyCollected;
    }

    private void HandleKeyCollected()
    {
        OpenExitDoors();
        onKeyCollected?.Invoke();
        Debug.Log($"[RoomManager] Key collected in '{name}' — exit door opened.");
    }

    private void OpenExitDoors()
    {
        if (exitDoors == null || exitDoors.Length == 0)
        {
            Debug.LogWarning("[RoomManager] exitDoors is empty — assign the exit Door in the Inspector.");
            return;
        }

        foreach (Door door in exitDoors)
        {
            if (door == null)
            {
                Debug.LogWarning("[RoomManager] exitDoors contains a null entry.");
                continue;
            }
            door.Unlock();
        }
    }

    // --- UI Helpers ---

    private static void SetUI(GameObject ui, bool active)
    {
        if (ui != null) ui.SetActive(active);
    }

    private IEnumerator ShowRoomClearUI()
    {
        SetUI(roomClearUI, true);
        yield return new WaitForSeconds(roomClearUIDisplayDuration);
        SetUI(roomClearUI, false);
    }
}
