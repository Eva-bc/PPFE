using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages an exterior rain particle system with interior occlusion.
///
/// Design:
///   - A single ParticleSystem emits rain from a large box high above the scene.
///   - Particle collision (lifetimeLoss = 1) destroys drops the instant they
///     hit any physics collider (rooftops act as shields).
///   - InteriorZone trigger volumes (one per room) report when the player is
///     indoors.  While at least one zone contains the player the rain emitter
///     is disabled and the audio crossfades to a quiet muffled level.
///   - The audio fades smoothly between outdoor and indoor volumes.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class RainController : MonoBehaviour
{
    // ── Singleton ────────────────────────────────────────────────────────────
    public static RainController Instance { get; private set; }

    // ── Inspector ────────────────────────────────────────────────────────────
    [Header("Emitter Shape")]
    [Tooltip("World-space center of the rain emission box.")]
    [SerializeField] private Vector3 emitterCenter = new Vector3(0f, 15f, 25f);

    [Tooltip("Size of the emission box in world units (X, Y=thickness, Z).")]
    [SerializeField] private Vector3 emitterSize = new Vector3(80f, 1f, 100f);

    [Header("Rain Particles")]
    [SerializeField] private int   maxParticles   = 2000;
    [SerializeField] private float emissionRate   = 400f;
    [SerializeField] private float startSpeed     = 18f;
    [SerializeField] private float startLifetime  = 2.5f;

    [Tooltip("Slight angle to simulate wind-driven rain (degrees around X axis).")]
    [SerializeField] private float windAngleDeg   = 8f;

    [Header("Audio")]
    [SerializeField] private AudioClip rainAmbientClip;
    [SerializeField] [Range(0f, 1f)] private float outdoorVolume = 0.7f;
    [SerializeField] [Range(0f, 1f)] private float indoorVolume  = 0.08f;
    [SerializeField] private float audioFadeDuration = 1.5f;

    // ── Private state ─────────────────────────────────────────────────────────
    private ParticleSystem _rain;
    private AudioSource    _audio;
    private int            _interiorCount = 0;   // how many zones currently contain the player
    private bool           _isIndoors     = false;
    private Coroutine      _fadeCoroutine;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _audio = GetComponent<AudioSource>();
        BuildRainParticleSystem();
        ConfigureAudio();
    }

    private void Start()
    {
        // Start outdoors by default
        _audio.volume = outdoorVolume;
    }

    // ── Public API (called by InteriorZone) ───────────────────────────────────

    /// <summary>Called when the player enters an interior zone.</summary>
    public void OnPlayerEnteredInterior(InteriorZone zone)
    {
        _interiorCount++;
        if (_interiorCount == 1)
            SetIndoors(true);
    }

    /// <summary>Called when the player exits an interior zone.</summary>
    public void OnPlayerExitedInterior(InteriorZone zone)
    {
        _interiorCount = Mathf.Max(0, _interiorCount - 1);
        if (_interiorCount == 0)
            SetIndoors(false);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private void SetIndoors(bool indoors)
    {
        if (_isIndoors == indoors) return;
        _isIndoors = indoors;

        // Toggle emitter
        if (_rain != null)
        {
            if (indoors) _rain.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            else         _rain.Play();
        }

        // Crossfade audio
        float targetVolume = indoors ? indoorVolume : outdoorVolume;
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(FadeAudio(targetVolume));
    }

    private IEnumerator FadeAudio(float target)
    {
        float start   = _audio.volume;
        float elapsed = 0f;
        while (elapsed < audioFadeDuration)
        {
            elapsed      += Time.deltaTime;
            _audio.volume = Mathf.Lerp(start, target, elapsed / audioFadeDuration);
            yield return null;
        }
        _audio.volume = target;
    }

    private void ConfigureAudio()
    {
        _audio.clip        = rainAmbientClip;
        _audio.loop        = true;
        _audio.playOnAwake = false;
        _audio.spatialBlend = 0f;   // 2D — rain is ambient, not positional
        _audio.volume      = outdoorVolume;

        if (rainAmbientClip != null)
            _audio.Play();
    }

    private void BuildRainParticleSystem()
    {
        // Create a child GameObject to host the ParticleSystem
        var go = new GameObject("RainEmitter");
        go.transform.SetParent(transform, false);
        go.transform.position = emitterCenter;
        go.transform.rotation = Quaternion.Euler(windAngleDeg, 0f, 0f);

        _rain = go.AddComponent<ParticleSystem>();

        // ── Main module ──────────────────────────────────────────────────────
        var main          = _rain.main;
        main.maxParticles = maxParticles;
        main.startSpeed   = startSpeed;
        main.startLifetime = startLifetime;
        main.startSize    = new ParticleSystem.MinMaxCurve(0.03f, 0.06f);
        main.startColor   = new Color(0.72f, 0.82f, 0.95f, 0.55f);
        main.gravityModifier = 1f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        // ── Emission ─────────────────────────────────────────────────────────
        var emission       = _rain.emission;
        emission.enabled   = true;
        emission.rateOverTime = emissionRate;

        // ── Shape (box) ───────────────────────────────────────────────────────
        var shape          = _rain.shape;
        shape.enabled      = true;
        shape.shapeType    = ParticleSystemShapeType.Box;
        shape.scale        = emitterSize;

        // ── Renderer ──────────────────────────────────────────────────────────
        var renderer       = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.velocityScale = 0.08f;
        renderer.lengthScale   = 1.2f;

        // Use the URP default unlit particle material (alpha-blended)
        renderer.material = GetOrCreateRainMaterial();

        // ── Collision (kills drops that hit roofs/walls) ────────────────────
        var collision         = _rain.collision;
        collision.enabled     = true;
        collision.type        = ParticleSystemCollisionType.World;
        collision.mode        = ParticleSystemCollisionMode.Collision3D;
        collision.lifetimeLoss = 1f;        // particle dies on first contact
        collision.bounceMultiplier = 0f;
        collision.dampen   = 1f;
        collision.radiusScale = 0.01f;
        collision.quality  = ParticleSystemCollisionQuality.High;
        collision.enableDynamicColliders = false;

        _rain.Play();
    }

    private static Material GetOrCreateRainMaterial()
    {
        // Try to find the URP Particles/Unlit shader
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null)
            shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");

        var mat = new Material(shader != null ? shader : Shader.Find("Standard"));
        mat.SetFloat("_Surface", 1f);              // Transparent
        mat.SetFloat("_Blend", 0f);                // Alpha blend
        mat.color = new Color(0.75f, 0.85f, 1f, 0.4f);
        return mat;
    }
}
