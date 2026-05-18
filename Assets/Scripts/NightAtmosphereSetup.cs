using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Configures post-processing and ambient lighting for the night atmosphere
/// at startup. Operates on a dedicated Volume Profile so it never mutates
/// the designer's original SampleSceneProfile asset.
/// Attach this to any persistent GameObject in the scene (e.g. RainController).
/// </summary>
[RequireComponent(typeof(Volume))]
public class NightAtmosphereSetup : MonoBehaviour
{
    [Header("Color Adjustments")]
    [Tooltip("EV compensation — positive = brighter exposure.")]
    [SerializeField] private float postExposure = 0.55f;

    [Tooltip("Global contrast boost (0 = neutral, 100 = max).")]
    [SerializeField] private float contrast = 5f;

    [Tooltip("Overall color tint applied multiplicatively. Slightly cool but readable.")]
    [SerializeField] private Color colorFilter = new Color(0.90f, 0.94f, 1.0f, 1f);

    [Tooltip("Desaturation: negative values remove color (-100 = greyscale).")]
    [SerializeField] private float saturation = -8f;

    [Header("White Balance")]
    [Tooltip("Color temperature shift. Negative = cooler/bluer.")]
    [SerializeField] private float temperature = -12f;

    [Tooltip("Tint shift (green-magenta axis).")]
    [SerializeField] private float tint = 3f;

    [Header("Lift / Gamma / Gain")]
    [Tooltip("Shadows tint — soft blue in darks.")]
    [SerializeField] private Vector4 lift  = new Vector4(-0.01f,  0.00f,  0.02f, -0.02f);

    [Tooltip("Midtones tint — neutral midtones.")]
    [SerializeField] private Vector4 gamma = new Vector4( 1.00f,  1.00f,  1.02f,  0.02f);

    [Tooltip("Highlights tint — barely cool highlights.")]
    [SerializeField] private Vector4 gain  = new Vector4( 0.98f,  0.99f,  1.03f,  0.00f);

    [Header("Bloom")]
    [Tooltip("Lower threshold so light halos on lamp props appear.")]
    [SerializeField] private float bloomThreshold = 1.1f;

    [Tooltip("Bloom strength — keep subtle.")]
    [SerializeField] private float bloomIntensity = 0.3f;

    [Tooltip("Bloom scatter / softness.")]
    [SerializeField] private float bloomScatter = 0.65f;

    [Tooltip("Warm tint on bloom halos from indoor lamps.")]
    [SerializeField] private Color bloomTint = new Color(1.0f, 0.95f, 0.88f, 1f);

    [Header("Vignette")]
    [Tooltip("Dark vignette for atmosphere.")]
    [SerializeField] private Color vignetteColor = new Color(0.02f, 0.00f, 0.06f, 1f);

    [Tooltip("Vignette strength — reduced for readability.")]
    [SerializeField] private float vignetteIntensity = 0.22f;

    [Tooltip("Vignette edge softness.")]
    [SerializeField] private float vignetteSmoothness = 0.35f;

    [Header("Ambient Lighting")]
    [Tooltip("Sky ambient color — moonlit blue night sky.")]
    [SerializeField] private Color ambientSkyColor = new Color(0.18f, 0.22f, 0.40f, 1f);

    [Tooltip("Equator ambient color — slightly lighter horizon.")]
    [SerializeField] private Color ambientEquatorColor = new Color(0.14f, 0.18f, 0.32f, 1f);

    [Tooltip("Ground ambient — cool dark ground.")]
    [SerializeField] private Color ambientGroundColor = new Color(0.06f, 0.07f, 0.12f, 1f);

    [Header("Fog")]
    [Tooltip("Enable Unity scene fog.")]
    [SerializeField] private bool enableFog = true;

    [Tooltip("Fog color — light blue-grey, less oppressive.")]
    [SerializeField] private Color fogColor = new Color(0.22f, 0.26f, 0.40f, 1f);

    [Tooltip("Fog mode: Exponential Squared gives the best atmospheric depth.")]
    [SerializeField] private FogMode fogMode = FogMode.ExponentialSquared;

    [Tooltip("Exponential density — very light to preserve gameplay visibility.")]
    [SerializeField] private float fogDensity = 0.008f;

    private Volume nightVolume;
    private VolumeProfile nightProfile;

    private void Awake()
    {
        ConfigureAmbientAndFog();
        BuildNightVolumeProfile();
    }

    // -----------------------------------------------------------------------
    // Ambient lighting and fog (scene-level settings)
    // -----------------------------------------------------------------------

    private void ConfigureAmbientAndFog()
    {
        // Gradient ambient: sky/equator/ground give depth without baking.
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor     = ambientSkyColor;
        RenderSettings.ambientEquatorColor = ambientEquatorColor;
        RenderSettings.ambientGroundColor  = ambientGroundColor;

        // Fog
        RenderSettings.fog        = enableFog;
        RenderSettings.fogColor   = fogColor;
        RenderSettings.fogMode    = fogMode;
        RenderSettings.fogDensity = fogDensity;
    }

    // -----------------------------------------------------------------------
    // Volume Profile — built at runtime so no asset serialisation is needed
    // -----------------------------------------------------------------------

    private void BuildNightVolumeProfile()
    {
        nightVolume = GetComponent<Volume>();
        nightVolume.isGlobal = true;
        // Lower priority than the main scene volume (default priority is 0).
        // We want this to override but not conflict with SampleSceneProfile
        // which is also global at priority 0. Set ours slightly higher.
        nightVolume.priority = 1;

        nightProfile = ScriptableObject.CreateInstance<VolumeProfile>();
        nightProfile.name = "NightAtmosphereProfile_Runtime";
        nightVolume.sharedProfile = nightProfile;

        AddColorAdjustments();
        AddWhiteBalance();
        AddLiftGammaGain();
        AddBloom();
        AddVignette();
    }

    private void AddColorAdjustments()
    {
        ColorAdjustments ca = nightProfile.Add<ColorAdjustments>(true);
        ca.postExposure.Override(postExposure);
        ca.contrast.Override(contrast);
        ca.colorFilter.Override(colorFilter);
        ca.saturation.Override(saturation);
    }

    private void AddWhiteBalance()
    {
        WhiteBalance wb = nightProfile.Add<WhiteBalance>(true);
        wb.temperature.Override(temperature);
        wb.tint.Override(tint);
    }

    private void AddLiftGammaGain()
    {
        LiftGammaGain lgg = nightProfile.Add<LiftGammaGain>(true);
        lgg.lift.Override(lift);
        lgg.gamma.Override(gamma);
        lgg.gain.Override(gain);
    }

    private void AddBloom()
    {
        Bloom bloom = nightProfile.Add<Bloom>(true);
        bloom.threshold.Override(bloomThreshold);
        bloom.intensity.Override(bloomIntensity);
        bloom.scatter.Override(bloomScatter);
        bloom.tint.Override(bloomTint);
        bloom.highQualityFiltering.Override(true);
    }

    private void AddVignette()
    {
        Vignette vignette = nightProfile.Add<Vignette>(true);
        vignette.color.Override(vignetteColor);
        vignette.intensity.Override(vignetteIntensity);
        vignette.smoothness.Override(vignetteSmoothness);
        vignette.rounded.Override(true);
    }

    private void OnDestroy()
    {
        // Clean up the runtime profile to avoid memory leaks in the editor.
        if (nightProfile != null)
            Destroy(nightProfile);
    }
}
