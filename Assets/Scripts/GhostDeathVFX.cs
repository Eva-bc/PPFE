using UnityEngine;

/// <summary>
/// Attach to any Ghost subclass GameObject.
/// Spawns a plasma puddle prefab at the ghost's feet when Die() calls OnDeath().
/// Call SpawnPuddle() from the Ghost subclass's OnDeath() override.
/// </summary>
public class GhostDeathVFX : MonoBehaviour
{
    [Header("Puddle Prefab")]
    [SerializeField] private PlasmaPuddle puddlePrefab;

    [Header("Color")]
    [Tooltip("Color of the plasma puddle. Match to the ghost's material color.")]
    [SerializeField] private Color puddleColor = Color.white;

    [Header("Placement")]
    [Tooltip("Y offset from the ghost's position so the puddle sits flush on the ground.")]
    [SerializeField] private float groundOffset = 0.02f;

    /// <summary>
    /// Instantiates the puddle at the ghost's feet. Call from Ghost.OnDeath().
    /// </summary>
    public void SpawnPuddle()
    {
        if (puddlePrefab == null)
        {
            Debug.LogWarning($"[GhostDeathVFX] {name}: puddlePrefab is not assigned.");
            return;
        }

        Vector3 spawnPosition = new Vector3(
            transform.position.x,
            groundOffset,
            transform.position.z);

        // Flat on the ground — Quad faces up by default in Unity when rotated -90° on X.
        Quaternion spawnRotation = Quaternion.Euler(90f, 0f, 0f);

        PlasmaPuddle puddle = Instantiate(puddlePrefab, spawnPosition, spawnRotation);
        puddle.Initialize(puddleColor);
    }
}
