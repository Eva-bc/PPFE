using System;
using UnityEngine;

/// <summary>
/// Describes a single wave: which ghost prefabs to spawn, how many, and where.
/// Create instances via Assets > Create > Room System > Wave Data.
/// </summary>
[CreateAssetMenu(fileName = "WaveData", menuName = "Room System/Wave Data")]
public class WaveData : ScriptableObject
{
    [Serializable]
    public struct GhostSpawnEntry
    {
        [Tooltip("Ghost prefab to spawn (GhostWhite, GhostGreen, GhostPurple�)")]
        public GameObject prefab;

        [Tooltip("Number of this ghost type to spawn.")]
        [Min(1)] public int count;
    }

    [Header("Spawn Configuration")]
    [Tooltip("List of ghost types and their counts for this wave.")]
    public GhostSpawnEntry[] ghostEntries;

    [Tooltip("Delay in seconds before the first ghost appears.")]
    [Min(0f)] public float spawnDelay = 1.5f;

    [Tooltip("Interval in seconds between each individual ghost spawn.")]
    [Min(0f)] public float spawnInterval = 0.4f;
}
