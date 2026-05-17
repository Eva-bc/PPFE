using System;
using UnityEngine;

/// <summary>
/// Describes the full ghost sequence for a single room.
/// A room is composed of several phases, each spawning a group of ghosts.
/// Create instances via Assets > Create > Room System > Room Data.
/// </summary>
[CreateAssetMenu(fileName = "RoomData", menuName = "Room System/Room Data")]
public class RoomData : ScriptableObject
{
    [Serializable]
    public struct Phase
    {
        [Tooltip("Ghost prefab to spawn during this phase.")]
        public GameObject prefab;

        [Tooltip("Number of ghosts of this type to spawn.")]
        [Min(1)] public int count;

        [Tooltip("Delay in seconds before this phase starts (after the previous phase's last spawn).")]
        [Min(0f)] public float delayBefore;

        [Tooltip("If true, this phase will not start until every ghost from the previous phase is dead. The delayBefore still applies as an additional wait after the last enemy dies.")]
        public bool waitForPreviousPhase;
    }

    [Header("Room Phases")]
    [Tooltip("Ordered list of ghost spawn phases for this room.")]
    public Phase[] phases;

    [Tooltip("Interval in seconds between each individual ghost spawn within a phase.")]
    [Min(0f)] public float spawnInterval = 0.4f;
}
