using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Manages a single room: detects player entry, triggers the wave,
/// locks/unlocks doors, and tracks wave completion.
/// Attach to a GameObject with a trigger Collider covering the room entrance.
/// </summary>
[RequireComponent(typeof(Collider))]
public class RoomManager : MonoBehaviour
{
    [Header("Wave")]
    [Tooltip("The WaveData asset that defines ghosts for this room.")]
    [SerializeField] private WaveData waveData;

    [SerializeField] private WaveManager waveManager;

    [Header("Doors")]
    [Tooltip("Door(s) that lock when the player enters the room.")]
    [SerializeField] private Door[] entryDoors;

    [Tooltip("Door(s) that unlock when the wave is cleared.")]
    [SerializeField] private Door[] exitDoors;

    [Header("Events (optional)")]
    [Tooltip("Fired when the player enters the room and the wave starts.")]
    public UnityEvent onWaveStarted;

    [Tooltip("Fired when all enemies are defeated.")]
    public UnityEvent onRoomCleared;

    [Header("UI (optional)")]
    [Tooltip("GameObject shown while the wave is active (e.g. 'Wave en cours' panel).")]
    [SerializeField] private GameObject waveActiveUI;

    [Tooltip("GameObject shown when the room is cleared (e.g. 'Room Clear' panel).")]
    [SerializeField] private GameObject roomClearUI;

    [Tooltip("How long the 'Room Clear' UI stays visible before hiding.")]
    [SerializeField] private float roomClearUIDisplayDuration = 2f;

    private bool hasBeenTriggered;

    private void Awake()
    {
        // Ensure the entry collider is a trigger.
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;

        SetUI(waveActiveUI, false);
        SetUI(roomClearUI, false);
    }

    private void OnEnable()
    {
        if (waveManager != null)
        {
            waveManager.OnEnemyDied += HandleEnemyDied;
            waveManager.OnWaveCleared += HandleWaveCleared;
        }
    }

    private void OnDisable()
    {
        if (waveManager != null)
        {
            waveManager.OnEnemyDied -= HandleEnemyDied;
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
        LockDoors(entryDoors);

        SetUI(waveActiveUI, true);

        waveManager.StartWave(waveData);
        onWaveStarted?.Invoke();

        Debug.Log($"[RoomManager] Room '{name}' started.");
    }

    private void HandleEnemyDied(int remaining)
    {
        Debug.Log($"[RoomManager] Enemies remaining in '{name}': {remaining}");
    }

    private void HandleWaveCleared()
    {
        UnlockDoors(exitDoors);

        SetUI(waveActiveUI, false);

        onRoomCleared?.Invoke();

        Debug.Log($"[RoomManager] Room '{name}' cleared!");

        if (roomClearUI != null)
            StartCoroutine(ShowRoomClearUI());
    }

    // --- Door Helpers ---

    private static void LockDoors(Door[] doors)
    {
        if (doors == null) return;
        foreach (Door door in doors)
            door?.Lock();
    }

    private static void UnlockDoors(Door[] doors)
    {
        if (doors == null) return;
        foreach (Door door in doors)
            door?.Unlock();
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
