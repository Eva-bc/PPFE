using System.Collections;
using UnityEngine;

/// <summary>
/// Represents a door that can be locked or unlocked.
/// When locked  : the collider blocks the passage and the visual is visible.
/// When unlocked: ALL child colliders are disabled immediately (synchronous, same frame),
///               then a smoke + fade-out plays, and the entire GameObject is destroyed.
/// </summary>
public class Door : MonoBehaviour
{
    [Header("State")]
    [SerializeField] private bool startLocked = true;

    [Header("References")]
    [Tooltip("Child GameObjects that physically block the passage. Each is SetActive(false) synchronously on Unlock.")]
    [SerializeField] private GameObject[] colliderObjects;

    [Tooltip("The child GameObject that holds the door mesh.")]
    [SerializeField] private GameObject doorVisual;

    [Header("Disappear Effect")]
    [Tooltip("Total duration of the smoke + fade-out animation in seconds.")]
    [SerializeField] private float disappearDuration = 1.0f;

    [Tooltip("Color of the smoke particles.")]
    [SerializeField] private Color smokeColor = new Color(0.8f, 0.8f, 0.8f, 1f);

    [Header("Audio (optional)")]
    [Tooltip("AudioClip played when the door disappears.")]
    [SerializeField] private AudioClip openSound;

    private bool isLocked;
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;

        isLocked = startLocked;
        ApplyStateInstant();
    }

    /// <summary>Locks the door: re-enables collider objects and shows the visual.</summary>
    public void Lock()
    {
        isLocked = true;
        ApplyStateInstant();
    }

    /// <summary>
    /// Unlocks the door. Disables ALL collision objects synchronously this frame,
    /// then plays the visual disappear effect before destroying the entire GameObject.
    /// </summary>
    public void Unlock()
    {
        if (!isLocked) return;
        isLocked = false;

        // Disable every collision immediately — synchronous, takes effect this frame.
        DisableAllCollisionsNow();

        if (openSound != null)
            audioSource.PlayOneShot(openSound);

        StartCoroutine(DisappearRoutine());
    }

    /// <summary>Whether the door is currently locked.</summary>
    public bool IsLocked => isLocked;

    // ── Private ────────────────────────────────────────────────────────────────

    private void ApplyStateInstant()
    {
        SetColliderObjectsActive(isLocked);

        if (doorVisual != null)
            doorVisual.SetActive(isLocked);
    }

    /// <summary>
    /// Synchronously disables every blocking GameObject and every Collider
    /// found anywhere in this door's hierarchy. SetActive(false) is immediate.
    /// </summary>
    private void DisableAllCollisionsNow()
    {
        // Disable explicitly referenced blocking GameObjects.
        if (colliderObjects != null)
        {
            foreach (GameObject go in colliderObjects)
            {
                if (go == null) continue;
                go.SetActive(false);
                Debug.Log($"[Door] Disabled collider object '{go.name}'");
            }
        }

        // Safety sweep: disable every Collider component on this door and all children,
        // including any that are inside the FBX sub-hierarchy or not in colliderObjects.
        foreach (Collider col in GetComponentsInChildren<Collider>(true))
        {
            col.enabled = false;
            Debug.Log($"[Door] Disabled residual collider on '{col.gameObject.name}'");
        }

        Debug.Log("[Door] All collisions disabled.");
    }

    private void SetColliderObjectsActive(bool active)
    {
        if (colliderObjects == null) return;
        foreach (GameObject go in colliderObjects)
            if (go != null) go.SetActive(active);
    }

    private IEnumerator DisappearRoutine()
    {
        // Ensure visual is visible before we start fading.
        if (doorVisual != null)
            doorVisual.SetActive(true);

        SpawnSmoke();

        // Collect all renderers once before the loop.
        Renderer[] renderers = doorVisual != null
            ? doorVisual.GetComponentsInChildren<Renderer>(true)
            : System.Array.Empty<Renderer>();

        // Create instance materials that support alpha fade.
        Material[][] fadeMaterials = new Material[renderers.Length][];
        for (int i = 0; i < renderers.Length; i++)
        {
            fadeMaterials[i] = new Material[renderers[i].sharedMaterials.Length];
            for (int j = 0; j < fadeMaterials[i].Length; j++)
                fadeMaterials[i][j] = MakeTransparentCopy(renderers[i].sharedMaterials[j]);
            renderers[i].materials = fadeMaterials[i];
        }

        float elapsed = 0f;
        while (elapsed < disappearDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.SmoothStep(1f, 0f, elapsed / disappearDuration);

            for (int i = 0; i < renderers.Length; i++)
                foreach (Material mat in renderers[i].materials)
                {
                    Color c = mat.color;
                    mat.color = new Color(c.r, c.g, c.b, alpha);
                }

            yield return null;
        }

        // Destroy the entire door GameObject — visuals, scripts, and any surviving colliders.
        Debug.Log($"[Door] Destroying door GameObject '{gameObject.name}'");
        Destroy(gameObject);
    }

    // ── Smoke VFX ─────────────────────────────────────────────────────────────

    private void SpawnSmoke()
    {
        Vector3 center = doorVisual != null
            ? doorVisual.transform.position + Vector3.up * 1.5f
            : transform.position + Vector3.up * 1.5f;

        GameObject go = new GameObject("DoorSmoke");
        go.transform.position = center;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration        = disappearDuration;
        main.loop            = false;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(disappearDuration * 0.6f, disappearDuration);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
        main.startColor      = new ParticleSystem.MinMaxGradient(
            new Color(smokeColor.r, smokeColor.g, smokeColor.b, 0.8f),
            new Color(smokeColor.r * 0.5f, smokeColor.g * 0.5f, smokeColor.b * 0.5f, 0.4f));
        main.gravityModifier = new ParticleSystem.MinMaxCurve(-0.1f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles    = 80;

        var emission = ps.emission;
        emission.enabled      = true;
        emission.rateOverTime = 60f;

        var shape = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale     = new Vector3(1.6f, 2.8f, 0.25f);

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space   = ParticleSystemSimulationSpace.World;
        vel.x       = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
        vel.y       = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
        vel.z       = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);

        var sizeOL = ps.sizeOverLifetime;
        sizeOL.enabled = true;
        sizeOL.size    = new ParticleSystem.MinMaxCurve(1f,
            new AnimationCurve(
                new Keyframe(0f,   0f),
                new Keyframe(0.2f, 1f),
                new Keyframe(1f,   0f)));

        var colorOL = ps.colorOverLifetime;
        colorOL.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.1f), new GradientAlphaKey(0f, 1f) });
        colorOL.color = new ParticleSystem.MinMaxGradient(grad);

        ps.Play();
        Destroy(go, disappearDuration + main.startLifetime.constantMax + 0.5f);
    }

    // ── Material helper ───────────────────────────────────────────────────────

    private static Material MakeTransparentCopy(Material source)
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        Material mat  = new Material(urpLit != null ? urpLit : source.shader);

        Color baseColor = Color.white;
        if (source.HasProperty("_BaseColor"))  baseColor = source.GetColor("_BaseColor");
        else if (source.HasProperty("_Color")) baseColor = source.GetColor("_Color");
        mat.SetColor("_BaseColor", baseColor);
        mat.color = baseColor;

        mat.SetFloat("_Surface",   1f);
        mat.SetFloat("_Blend",     0f);
        mat.SetFloat("_ZWrite",    0f);
        mat.SetFloat("_AlphaClip", 0f);
        mat.SetFloat("_SrcBlend",  (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend",  (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

        return mat;
    }
}
