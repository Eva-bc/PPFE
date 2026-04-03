using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns ghosts from a WaveData asset at the assigned spawn points.
/// Reports active enemy count to the RoomManager via events.
/// </summary>
public class WaveManager : MonoBehaviour
{
    [Header("Spawn Points")]
    [Tooltip("World positions where ghosts can appear. Assigned round-robin if fewer than the total ghost count.")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("VFX (optional)")]
    [Tooltip("Particle system played at each spawn point when a ghost appears.")]
    [SerializeField] private ParticleSystem spawnVFX;

    // Fired when a ghost dies, passing the remaining active count.
    public event Action<int> OnEnemyDied;

    // Fired once when all ghosts in the wave are dead.
    public event Action OnWaveCleared;

    private readonly List<Ghost> activeGhosts = new();
    private int spawnIndex;

    /// <summary>Starts spawning the given wave. Safe to call from RoomManager.</summary>
    public void StartWave(WaveData waveData)
    {
        if (waveData == null)
        {
            Debug.LogError("[WaveManager] WaveData is null � cannot start wave.");
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("[WaveManager] No spawn points assigned.");
            return;
        }

        StartCoroutine(SpawnRoutine(waveData));
    }

    // --- Spawning ---

    private IEnumerator SpawnRoutine(WaveData waveData)
    {
        yield return new WaitForSeconds(waveData.spawnDelay);

        foreach (WaveData.GhostSpawnEntry entry in waveData.ghostEntries)
        {
            if (entry.prefab == null)
            {
                Debug.LogWarning("[WaveManager] Un prefab est null dans WaveData — entrée ignorée.");
                continue;
            }

            if (entry.prefab.GetComponent<Ghost>() == null)
            {
                Debug.LogWarning($"[WaveManager] Le prefab '{entry.prefab.name}' n'a pas de composant Ghost — entrée ignorée.");
                continue;
            }

            for (int i = 0; i < entry.count; i++)
            {
                SpawnGhost(entry.prefab);
                yield return new WaitForSeconds(waveData.spawnInterval);
            }
        }
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

        if (spawnVFX != null)
            Instantiate(spawnVFX, point.position, Quaternion.identity);
    }

    // --- Death Tracking ---

    private void HandleGhostDeath(Ghost ghost)
    {
        // Guard: ghost may already have been removed (event fires before Destroy completes).
        if (!activeGhosts.Contains(ghost)) return;

        activeGhosts.Remove(ghost);
        OnEnemyDied?.Invoke(activeGhosts.Count);

        if (activeGhosts.Count == 0)
            OnWaveCleared?.Invoke();
    }
}
