using UnityEngine;

/// <summary>
/// Controls the salt attack particle effect.
/// Call Play() when the salt attack is triggered.
/// The ParticleSystem must be set to Play On Awake = false and Loop = false.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class SaltParticleEffect : MonoBehaviour
{
    private ParticleSystem saltParticles;

    private void Awake()
    {
        saltParticles = GetComponent<ParticleSystem>();
    }

    /// <summary>Triggers the salt burst effect.</summary>
    public void Play()
    {
        if (saltParticles == null) return;

        saltParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        saltParticles.Play();
    }
}
