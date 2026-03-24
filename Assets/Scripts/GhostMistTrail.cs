using UnityEngine;

/// <summary>
/// Adds a mist trail particle effect to a ghost.
/// Reads the ghost's color from its SkinnedMeshRenderer and applies it to the ParticleSystem.
/// Attach this script to a dedicated child GameObject (e.g. "MistTrail") with a ParticleSystem.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class GhostMistTrail : MonoBehaviour
{
    [Header("Color Source")]
    [Tooltip("SkinnedMeshRenderer on the ghost mesh (ChamferBox001). Auto-detected if left empty.")]
    [SerializeField] private SkinnedMeshRenderer ghostRenderer;

    [Header("Trail Settings")]
    [SerializeField] private float colorAlpha = 0.45f;

    private ParticleSystem mistParticles;
    private ParticleSystem.MainModule mainModule;

    private void Awake()
    {
        mistParticles = GetComponent<ParticleSystem>();
        mainModule = mistParticles.main;

        if (ghostRenderer == null)
            ghostRenderer = GetComponentInParent<SkinnedMeshRenderer>();

        ApplyGhostColor();
    }

    /// <summary>Reads the ghost material's main color and applies it to the particle system.</summary>
    private void ApplyGhostColor()
    {
        if (ghostRenderer == null || ghostRenderer.sharedMaterial == null) return;

        Color ghostColor = ghostRenderer.sharedMaterial.HasProperty("_BaseColor")
            ? ghostRenderer.sharedMaterial.GetColor("_BaseColor")
            : ghostRenderer.sharedMaterial.color;

        Color startColor = ghostColor;
        startColor.a = colorAlpha;

        Color endColor = ghostColor;
        endColor.a = 0f;

        mainModule.startColor = new ParticleSystem.MinMaxGradient(
            new Gradient
            {
                colorKeys = new[]
                {
                    new GradientColorKey(ghostColor, 0f),
                    new GradientColorKey(ghostColor, 1f)
                },
                alphaKeys = new[]
                {
                    new GradientAlphaKey(colorAlpha, 0f),
                    new GradientAlphaKey(0f, 1f)
                }
            }
        );
    }
}
