using System.Collections;
using UnityEngine;

/// <summary>
/// Manages the exterior rain particle system and associated audio.
/// The rain GameObject follows the player horizontally so particles are always
/// generated just above the player's visible area without wasting budget on
/// off-screen zones.
///
/// Interior detection relies on InteriorZone triggers: each room should have
/// one. A counter tracks how many interior zones the player is currently inside
/// so that overlapping zones are handled correctly.
/// </summary>
public class RainController : MonoBehaviour
{
    // -----------------------------------------------------------------------
    // Singleton
    // -----------------------------------------------------------------------

    public static RainController Instance { get; private set; }

    // -----------------------------------------------------------------------
    // Inspector
    // -----------------------------------------------------------------------

    [Header("References")]
    [Tooltip("The Player Transform the rain follows.")]
    [SerializeField] private Transform playerTransform;

    [Tooltip("The rain ParticleSystem. Created procedurally if left empty.")]
    [SerializeField] private ParticleSystem rainParticleSystem;

    [Tooltip("Optional splash ParticleSystem that plays at ground level.")]
    [SerializeField] private ParticleSystem splashParticleSystem;

    [Header("Follow Settings")]
    [Tooltip("Height above the scene's ground at which rain particles are emitted.")]
    [SerializeField] private float emitterHeight = 12f;

    [Tooltip("Half-width (X and Z) of the emitter box. Keep it just larger than the camera view.")]
    [SerializeField] private float emitterHalfExtent = 14f;

    [Header("Rain Particles")]
    [Tooltip("Maximum number of simultaneous rain particles.")]
    [SerializeField] private int maxParticles = 600;

    [Tooltip("How many rain drops are emitted per second.")]
    [SerializeField] private float emissionRate = 280f;

    [Tooltip("Rain drop fall speed (units/second).")]
    [SerializeField] private float rainSpeed = 18f;

    [Tooltip("Lifetime of each rain particle (seconds). Should cover emitterHeight / rainSpeed.")]
    [SerializeField] private float particleLifetime = 1.2f;

    [Tooltip("Scale of each rain streak particle.")]
    [SerializeField] private float particleSize = 0.08f;

    [Header("Audio")]
    [Tooltip("AudioClip of looping rain sound.")]
    [SerializeField] private AudioClip rainAudioClip;

    [Tooltip("Volume when fully outside.")]
    [SerializeField] private float exteriorVolume = 0.55f;

    [Tooltip("Volume when inside a room (muffled effect, set to 0 for silence).")]
    [SerializeField] private float interiorVolume = 0.06f;

    [Tooltip("How many seconds the audio fade takes when entering/leaving a room.")]
    [SerializeField] private float audioFadeDuration = 1.2f;

    // -----------------------------------------------------------------------
    // Runtime state
    // -----------------------------------------------------------------------

    private int interiorZoneCount;   // number of interior zones the player is inside
    private bool isInsideInterior;

    private AudioSource audioSource;
    private Coroutine audioFadeCoroutine;

    // -----------------------------------------------------------------------
    // Unity lifecycle
    // -----------------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        SetupAudioSource();

        if (rainParticleSystem == null)
            rainParticleSystem = BuildRainParticleSystem();

        if (splashParticleSystem != null)
            ConfigureSplashSystem();

        // Start outside by default
        isInsideInterior = false;
        SetRainActive(true);
        audioSource.volume = exteriorVolume;
    }

    private void LateUpdate()
    {
        if (playerTransform == null) return;

        // Keep the emitter directly above the player at the configured height.
        Vector3 pos = playerTransform.position;
        pos.y = emitterHeight;
        transform.position = pos;

        // Keep splash system at ground level
        if (splashParticleSystem != null)
        {
            Vector3 splashPos = playerTransform.position;
            splashPos.y = 0f;
            splashParticleSystem.transform.position = splashPos;
        }
    }

    // -----------------------------------------------------------------------
    // Interior zone callbacks (called by InteriorZone)
    // -----------------------------------------------------------------------

    /// <summary>Called by InteriorZone when the player enters an interior trigger.</summary>
    public void OnPlayerEnterInterior()
    {
        interiorZoneCount++;
        if (interiorZoneCount == 1 && !isInsideInterior)
        {
            isInsideInterior = true;
            SetRainActive(false);
            FadeAudio(interiorVolume);
        }
    }

    /// <summary>Called by InteriorZone when the player exits an interior trigger.</summary>
    public void OnPlayerExitInterior()
    {
        interiorZoneCount = Mathf.Max(0, interiorZoneCount - 1);
        if (interiorZoneCount == 0 && isInsideInterior)
        {
            isInsideInterior = false;
            SetRainActive(true);
            FadeAudio(exteriorVolume);
        }
    }

    // -----------------------------------------------------------------------
    // Rain visibility
    // -----------------------------------------------------------------------

    private void SetRainActive(bool active)
    {
        if (rainParticleSystem == null) return;

        var emission = rainParticleSystem.emission;
        emission.enabled = active;

        if (!active)
            rainParticleSystem.Clear();
    }

    // -----------------------------------------------------------------------
    // Audio
    // -----------------------------------------------------------------------

    private void SetupAudioSource()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = rainAudioClip;
        audioSource.loop = true;
        audioSource.spatialBlend = 0f;   // 2D — rain is an ambient layer
        audioSource.playOnAwake = false;
        audioSource.volume = 0f;

        if (rainAudioClip != null)
            audioSource.Play();
    }

    private void FadeAudio(float targetVolume)
    {
        if (audioFadeCoroutine != null)
            StopCoroutine(audioFadeCoroutine);
        audioFadeCoroutine = StartCoroutine(FadeAudioCoroutine(targetVolume));
    }

    private IEnumerator FadeAudioCoroutine(float targetVolume)
    {
        float startVolume = audioSource.volume;
        float elapsed = 0f;

        while (elapsed < audioFadeDuration)
        {
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / audioFadeDuration);
            yield return null;
        }

        audioSource.volume = targetVolume;
        audioFadeCoroutine = null;
    }

    // -----------------------------------------------------------------------
    // Procedural ParticleSystem construction
    // -----------------------------------------------------------------------

    private ParticleSystem BuildRainParticleSystem()
    {
        GameObject rainGO = new GameObject("RainParticles");
        rainGO.transform.SetParent(transform);
        rainGO.transform.localPosition = Vector3.zero;

        ParticleSystem ps = rainGO.AddComponent<ParticleSystem>();

        // Stop the system while we configure it
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // --- Main module ---
        var main = ps.main;
        main.loop = true;
        main.startLifetime = particleLifetime;
        main.startSpeed = rainSpeed;
        main.startSize = particleSize;
        main.maxParticles = maxParticles;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = new Color(0.72f, 0.82f, 0.95f, 0.55f); // cold blue-white, semi-transparent

        // Gravity multiplier — rain falls straight down
        main.gravityModifier = 0.15f;

        // --- Emission module ---
        var emission = ps.emission;
        emission.rateOverTime = emissionRate;

        // --- Shape module: flat box emitter just above the scene ---
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(emitterHalfExtent * 2f, 0.1f, emitterHalfExtent * 2f);

        // --- Velocity over lifetime: add slight horizontal drift for realism ---
        var velocityOverLifetime = ps.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-0.4f, 0.4f);
        velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);

        // --- Renderer ---
        ParticleSystemRenderer renderer = rainGO.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.velocityScale = 0.04f;
        renderer.lengthScale = 2.5f;
        renderer.sortingOrder = 1;

        // Use a default particle material if no custom one is assigned
        Material rainMat = new Material(Shader.Find("Particles/Standard Unlit"));
        if (rainMat.shader.name == "Hidden/InternalErrorShader")
        {
            // Fallback for URP / HDRP projects
            rainMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        }
        rainMat.SetFloat("_Mode", 2f);       // Fade mode
        rainMat.EnableKeyword("_ALPHABLEND_ON");
        rainMat.renderQueue = 3000;
        renderer.material = rainMat;

        ps.Play();
        return ps;
    }

    private void ConfigureSplashSystem()
    {
        var main = splashParticleSystem.main;
        main.startLifetime = 0.3f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
        main.startColor = new Color(0.7f, 0.8f, 0.95f, 0.4f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = splashParticleSystem.emission;
        emission.rateOverTime = 60f;

        var shape = splashParticleSystem.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(emitterHalfExtent * 2f, 0.05f, emitterHalfExtent * 2f);
    }

    // -----------------------------------------------------------------------
    // Gizmos (editor only)
    // -----------------------------------------------------------------------

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.4f, 0.7f, 1f, 0.3f);
        Gizmos.DrawCube(transform.position, new Vector3(emitterHalfExtent * 2f, 0.2f, emitterHalfExtent * 2f));
    }
#endif
}
