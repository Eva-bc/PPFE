using UnityEngine;

/// <summary>
/// Placed on the plasma puddle prefab.
/// Scales the puddle up on spawn then fades it out and destroys it.
/// The puddle color is set at runtime by GhostDeathVFX via Initialize().
/// </summary>
[RequireComponent(typeof(Renderer))]
public class PlasmaPuddle : MonoBehaviour
{
    [Header("Grow")]
    [SerializeField] private float growDuration = 0.35f;
    [SerializeField] private AnimationCurve growCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Lifetime")]
    [SerializeField] private float lifetime = 4f;
    [SerializeField] private float fadeDuration = 1f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private Renderer puddleRenderer;
    private MaterialPropertyBlock propertyBlock;
    private Vector3 targetScale;
    private float timer;

    private Color puddleColor;
    private bool initialized;

    private void Awake()
    {
        puddleRenderer = GetComponent<Renderer>();
        propertyBlock  = new MaterialPropertyBlock();
        targetScale    = transform.localScale;
        transform.localScale = Vector3.zero;
    }

    /// <summary>
    /// Called by GhostDeathVFX right after instantiation to set the puddle color.
    /// </summary>
    public void Initialize(Color color)
    {
        puddleColor = color;
        initialized = true;
        ApplyColor(1f);
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer <= growDuration)
        {
            float t = growCurve.Evaluate(timer / growDuration);
            transform.localScale = Vector3.LerpUnclamped(Vector3.zero, targetScale, t);
            return;
        }

        float fadeStart = lifetime - fadeDuration;
        if (timer >= fadeStart)
        {
            float fadeAlpha = 1f - Mathf.Clamp01((timer - fadeStart) / fadeDuration);
            ApplyColor(fadeAlpha);
        }

        if (timer >= lifetime)
            Destroy(gameObject);
    }

    private void ApplyColor(float alpha)
    {
        if (!initialized) return;

        Color tinted = puddleColor;
        tinted.a = alpha;

        puddleRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(BaseColorId, tinted);
        puddleRenderer.SetPropertyBlock(propertyBlock);
    }
}
