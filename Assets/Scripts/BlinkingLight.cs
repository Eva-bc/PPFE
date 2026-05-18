using UnityEngine;

/// <summary>
/// Makes an attached Light blink with configurable on/off intervals and optional random jitter.
/// </summary>
[RequireComponent(typeof(Light))]
public class BlinkingLight : MonoBehaviour
{
    [SerializeField] private float onDuration = 0.12f;
    [SerializeField] private float offDuration = 0.08f;
    [SerializeField] private float jitter = 0.04f;

    private Light _light;
    private float _timer;
    private bool _isOn = true;

    private void Awake()
    {
        _light = GetComponent<Light>();
        _timer = GetNextOnDuration();
    }

    private void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer > 0f) return;

        _isOn = !_isOn;
        _light.enabled = _isOn;
        _timer = _isOn ? GetNextOnDuration() : GetNextOffDuration();
    }

    private float GetNextOnDuration() => onDuration + Random.Range(-jitter, jitter);
    private float GetNextOffDuration() => offDuration + Random.Range(-jitter, jitter);
}
