using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns ghosts according to a RoomData asset (multi-phase).
/// Reports active enemy count to the RoomManager via events.
/// OnWaveCleared fires only when all phases are done AND all ghosts are dead.
/// </summary>
public class WaveManager : MonoBehaviour
{
    [Header("Spawn Points")]
    [Tooltip("World positions where ghosts can appear. Assigned round-robin.")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Audio (optional)")]
    [Tooltip("AudioClip played at each spawn point when a ghost appears.")]
    [SerializeField] private AudioClip ghostSpawnSound;

    [Header("VFX (optional)")]
    [Tooltip("Particle system played at each spawn point when a ghost appears.")]
    [SerializeField] private ParticleSystem spawnVFX;

    /// <summary>Fired when a ghost dies, passing the remaining active count.</summary>
    public event Action<int> OnEnemyDied;

    /// <summary>Fired once when all phases are complete and all ghosts are dead.</summary>
    public event Action OnWaveCleared;

    private readonly List<Ghost> activeGhosts = new();
    private int spawnIndex;
    private bool allPhasesSpawned;
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
    }

    /// <summary>Starts spawning all phases of the given room. Safe to call from RoomManager.</summary>
    public void StartWave(RoomData roomData)
    {
        if (roomData == null)
        {
            Debug.LogError("[WaveManager] RoomData is null — cannot start wave.");
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("[WaveManager] No spawn points assigned.");
            return;
        }

        activeGhosts.Clear();
        allPhasesSpawned = false;
        spawnIndex = 0;

        StartCoroutine(SpawnRoutine(roomData));
    }

    // --- Spawning ---

    private IEnumerator SpawnRoutine(RoomData roomData)
    {
        foreach (RoomData.Phase phase in roomData.phases)
        {
            yield return new WaitForSeconds(phase.delayBefore);

            if (phase.prefab == null)
            {
                Debug.LogWarning("[WaveManager] Un prefab est null dans RoomData — phase ignorée.");
                continue;
            }

            if (phase.prefab.GetComponent<Ghost>() == null)
            {
                Debug.LogWarning($"[WaveManager] Le prefab '{phase.prefab.name}' n'a pas de composant Ghost — phase ignorée.");
                continue;
            }

            for (int i = 0; i < phase.count; i++)
            {
                SpawnGhost(phase.prefab);
                if (i < phase.count - 1)
                    yield return new WaitForSeconds(roomData.spawnInterval);
            }
        }

        allPhasesSpawned = true;

        // All phases done: if no ghost survived until now, fire immediately.
        if (activeGhosts.Count == 0)
            OnWaveCleared?.Invoke();
    }

    private void SpawnGhost(GameObject prefab)
    {
        Transform point = spawnPoints[spawnIndex % spawnPoints.Length];
        spawnIndex++;

        GameObject instance = Instantiate(prefab, point.position, point.rotation);
        Ghost ghost = instance.GetComponent<Ghost>();
        activeGhosts.Add(ghost);

        ghost.OnHealthChanged += (current, _) =>
        {
            if (current <= 0f)
                HandleGhostDeath(ghost);
        };

        if (ghostSpawnSound != null)
            audioSource.PlayOneShot(ghostSpawnSound);

        if (spawnVFX != null)
            Instantiate(spawnVFX, point.position, Quaternion.identity);
    }

    // --- Death Tracking ---

    private void HandleGhostDeath(Ghost ghost)
    {
        if (!activeGhosts.Contains(ghost)) return;

        activeGhosts.Remove(ghost);
        OnEnemyDied?.Invoke(activeGhosts.Count);

        // Fire only when all phases have been sent AND no ghost remains.
        if (allPhasesSpawned && activeGhosts.Count == 0)
            OnWaveCleared?.Invoke();
    }
}
