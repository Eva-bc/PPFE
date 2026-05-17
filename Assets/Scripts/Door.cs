using System.Collections;
using UnityEngine;

/// <summary>
/// A door that physically blocks a passage via a single BoxCollider on this GameObject.
/// The door is the ONLY blocking element. No lock system, no entry control.
///
/// When Unlock() is called:
///   1. BoxCollider is disabled immediately (passage is free this physics frame).
///   2. A smoke + fade-out effect plays over <disappearDuration> seconds.
///   3. The entire GameObject is destroyed.
///   4. The passage is scanned and the number of remaining solid colliders is logged.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class Door : MonoBehaviour
{
    [Header("Disappear Effect")]
    [Tooltip("Total duration of the smoke + fade-out animation in seconds.")]
    [SerializeField] private float disappearDuration = 1.0f;

    [Tooltip("Color of the smoke particles.")]
    [SerializeField] private Color smokeColor = new Color(0.8f, 0.8f, 0.8f, 1f);

    [Header("Audio (optional)")]
    [Tooltip("AudioClip played when the door disappears.")]
    [SerializeField] private AudioClip openSound;

    [Header("Debug — Passage Verification")]
    [Tooltip("World-space centre of the passage zone used to count colliders after destruction.")]
    [SerializeField] private Vector3 passageScanCenter;
    [Tooltip("Half-extents of the passage scan box.")]
    [SerializeField] private Vector3 passageScanHalfExtents = new Vector3(3f, 2f, 2f);

    private BoxCollider doorCollider;
    private AudioSource audioSource;
    private bool isUnlocking;

    private void Awake()
    {
        doorCollider = GetComponent<BoxCollider>();
        doorCollider.isTrigger = false;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;

        // Default scan center to the door's own position.
        if (passageScanCenter == Vector3.zero)
            passageScanCenter = transform.position;
    }

    /// <summary>
    /// Disables the BoxCollider immediately (passage is free this frame),
    /// plays the disappear effect, destroys this GameObject, then logs
    /// how many solid colliders remain in the passage.
    /// </summary>
    public void Unlock()
    {
        if (isUnlocking) return;
        isUnlocking = true;

        // Immediately free the passage — takes effect this physics step.
        doorCollider.enabled = false;

        if (openSound != null)
            audioSource.PlayOneShot(openSound);

        StartCoroutine(DisappearRoutine());
    }

    // ── Private ──────────────────────────────────────────────────────────────

    private IEnumerator DisappearRoutine()
    {
        SpawnSmoke();

        // Grab all renderers on this door and its children.
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

        // Create per-renderer instance materials set to transparent mode.
        Material[][] fadeMats = new Material[renderers.Length][];
        for (int i = 0; i < renderers.Length; i++)
        {
            fadeMats[i] = new Material[renderers[i].sharedMaterials.Length];
            for (int j = 0; j < fadeMats[i].Length; j++)
                fadeMats[i][j] = MakeTransparentCopy(renderers[i].sharedMaterials[j]);
            renderers[i].materials = fadeMats[i];
        }

        // Fade out alpha over the disappear duration.
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

        // Destroy the entire GameObject — mesh, collider, scripts, everything.
        Destroy(gameObject);
        Debug.Log("[Door] Door destroyed.");

        // Wait one frame so physics registers the destruction, then verify.
        yield return null;
        VerifyPassage();
    }

    private void VerifyPassage()
    {
        Collider[] hits = Physics.OverlapBox(
            passageScanCenter,
            passageScanHalfExtents,
            Quaternion.identity,
            Physics.AllLayers,
            QueryTriggerInteraction.Ignore);

        Debug.Log($"[Door] Colliders remaining in passage after destruction: {hits.Length}");
        foreach (Collider col in hits)
            Debug.Log($"[Door]   -> '{col.gameObject.name}' ({col.GetType().Name}) at {col.bounds.center}", col.gameObject);
    }

    // ── Smoke VFX ────────────────────────────────────────────────────────────

    private void SpawnSmoke()
    {
        GameObject go = new GameObject("DoorSmoke");
        go.transform.position = transform.position + Vector3.up * 1.5f;

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
