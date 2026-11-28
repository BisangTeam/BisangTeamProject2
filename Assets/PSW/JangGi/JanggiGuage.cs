using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(100)] // Draw after the heart has distributed this frame's blood.
public class JanggiGuage : MonoBehaviour
{
    [SerializeField] private RectTransform bloodRect;
    [SerializeField] private Graphic emptyOverlay;

    private float _gauge;
    public float Gauge 
    { 
        get => _gauge;
        set
        {
            value = Mathf.Clamp(value, 0f, 100f);
            if (_gauge == value) return;
            _gauge = value;
            UpdateGauge();
        } 
    }

    [SerializeField] private float emptyPosY = -200f;
    [SerializeField] private float fullPosY = 0f;

    [SerializeField] private float fillDuration = 0.4f;
    [SerializeField] private Ease fillEase = Ease.OutQuad;

    private Quaternion _levelRotation;
    private float _shown;
    private float _from;
    private float _elapsed;
    private float _tilt;
    private float _tiltVelocity;

    private void Awake()
    {
        _levelRotation = bloodRect.localRotation;
        ApplyLevel();
    }

    public void UpdateGauge()
    {
        _from = _shown;
        _elapsed = 0f;
    }

    private void LateUpdate()
    {
        if (Time.deltaTime <= 0f) return;

        float lastLevel = _shown;
        if (_shown != _gauge)
        {
            // Small flow increments reach their level this frame. A large injection still eases in.
            float duration = fillDuration * Mathf.Abs(_gauge - _from) / 100f;
            _elapsed += Time.deltaTime;
            _shown = _elapsed >= duration ? _gauge : DOVirtual.EasedValue(_from, _gauge, _elapsed / duration, fillEase);
            ApplyLevel();
        }
        float fillSpeed = (_shown - lastLevel) / Time.deltaTime;
        float targetTilt = Mathf.Clamp(fillSpeed * 0.08f, -4f, 4f);
        _tilt = Mathf.SmoothDamp(_tilt, targetTilt, ref _tiltVelocity, 0.12f);
        bloodRect.localRotation = _levelRotation * Quaternion.Euler(0f, 0f, _tilt);
    }

    private void ApplyLevel()
    {
        float level = _shown / 100f;
        Vector2 position = bloodRect.anchoredPosition;
        position.y = Mathf.Lerp(emptyPosY, fullPosY, level);
        bloodRect.anchoredPosition = position;
        if (emptyOverlay != null)
        {
            // The first drops lighten the veil even inside the sprite's transparent bottom margin.
            float firstDrops = Mathf.Sqrt(Mathf.Clamp01(level * 8f));
            emptyOverlay.canvasRenderer.SetAlpha(1f - 0.24f * firstDrops);
        }
    }
}
