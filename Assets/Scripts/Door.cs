using System.Collections;
using UnityEngine;

/// <summary>
/// Represents a door that can be locked or unlocked.
/// When locked: the collider blocks the passage and the visual is visible.
/// When unlocked: a smoke burst plays on the door while it fades out, then disappears.
/// </summary>
public class Door : MonoBehaviour
{
    [Header("State")]
    [SerializeField] private bool startLocked = true;

    [Header("References")]
    [Tooltip("The collider that physically blocks the passage.")]
    [SerializeField] private Collider doorCollider;

    [Tooltip("The child GameObject that holds the door's mesh.")]
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

    /// <summary>Locks the door instantly: re-enables the collider and shows the visual.</summary>
    public void Lock()
    {
        isLocked = true;
        ApplyStateInstant();
    }

    /// <summary>Unlocks the door: plays smoke and fades the mesh out.</summary>
    public void Unlock()
    {
        if (!isLocked) return;
        isLocked = false;

        if (doorCollider != null)
            doorCollider.enabled = false;

        if (openSound != null)
            audioSource.PlayOneShot(openSound);

        StartCoroutine(DisappearRoutine());
    }

    /// <summary>Whether the door is currently locked.</summary>
    public bool IsLocked => isLocked;

    private void ApplyStateInstant()
    {
        if (doorCollider != null)
            doorCollider.enabled = isLocked;

        if (doorVisual != null)
            doorVisual.SetActive(isLocked);
    }

    private IEnumerator DisappearRoutine()
    {
        if (doorVisual == null) yield break;

        doorVisual.SetActive(true);

        // --- Spawn smoke ---
        SpawnSmoke();

        // --- Fade out all renderers on the door visual ---
        Renderer[] renderers = doorVisual.GetComponentsInChildren<Renderer>(true);
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
            {
                foreach (Material mat in renderers[i].materials)
                {
                    Color c = mat.color;
                    mat.color = new Color(c.r, c.g, c.b, alpha);
                }
            }

            yield return null;
        }

        Destroy(doorVisual);

        if (doorCollider != null)
            Destroy(doorCollider.gameObject);
    }

    private void SpawnSmoke()
    {
        // Center the smoke on the door visual bounds.
        Vector3 center = doorVisual.transform.position + Vector3.up * 1.5f;

        GameObject go = new GameObject("DoorSmoke");
        go.transform.position = center;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // Main
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

        // Emission: steady stream over the full duration
        var emission = ps.emission;
        emission.enabled      = true;
        emission.rateOverTime = 60f;

        // Shape: box matching door face
        var shape = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale     = new Vector3(1.6f, 2.8f, 0.25f);

        // Velocity: drift upward
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space   = ParticleSystemSimulationSpace.World;
        vel.x       = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
        vel.y       = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
        vel.z       = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);

        // Size over lifetime: fade in then out
        var sizeOL = ps.sizeOverLifetime;
        sizeOL.enabled = true;
        sizeOL.size    = new ParticleSystem.MinMaxCurve(1f,
            new AnimationCurve(
                new Keyframe(0f,   0f),
                new Keyframe(0.2f, 1f),
                new Keyframe(1f,   0f)));

        // Color over lifetime: fade alpha out
        var colorOL = ps.colorOverLifetime;
        colorOL.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.1f), new GradientAlphaKey(0f, 1f) });
        colorOL.color = new ParticleSystem.MinMaxGradient(grad);

        ps.Play();

        // Auto-destroy once particles are done.
        Destroy(go, disappearDuration + main.startLifetime.constantMax + 0.5f);
    }

    /// <summary>
    /// Returns a transparent-capable copy of a material, preserving its base color.
    /// </summary>
    private static Material MakeTransparentCopy(Material source)
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        Material mat  = new Material(urpLit != null ? urpLit : source.shader);

        // Copy base color.
        Color baseColor = Color.white;
        if (source.HasProperty("_BaseColor"))       baseColor = source.GetColor("_BaseColor");
        else if (source.HasProperty("_Color"))      baseColor = source.GetColor("_Color");
        mat.SetColor("_BaseColor", baseColor);
        mat.color = baseColor;

        // Switch URP surface to Transparent.
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
