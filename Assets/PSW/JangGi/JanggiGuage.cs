using DG.Tweening;
using UnityEngine;

public class JanggiGuage : MonoBehaviour
{
    [SerializeField] private RectTransform bloodRect;

    private float _gauge;
    public float Gauge 
    { 
        get => _gauge;
        set
        {
            _gauge = Mathf.Clamp(value, 0f, 100f);
            UpdateGauge();
        } 
    }

    [SerializeField] private float emptyPosY = -200f;
    [SerializeField] private float fullPosY = 0f;

    [SerializeField] private float fillDuration = 0.4f;
    [SerializeField] private Ease fillEase = Ease.OutQuad;

    private Quaternion _levelRotation;
    private float _tilt;
    private float _tiltVelocity;

    private void Awake()
    {
        _levelRotation = bloodRect.localRotation;
    }

    private void Start()
    {
        UpdateGauge();
    }

    public void UpdateGauge()
    {
        float chg = Gauge / 100f;
        float targetY = Mathf.Lerp(emptyPosY, fullPosY, chg);

        bloodRect.DOKill();
        bloodRect.DOAnchorPosY(targetY, fillDuration)
            .SetEase(fillEase)
            .SetLink(bloodRect.gameObject);
    }

    private void LateUpdate()
    {
        if (Time.deltaTime <= 0f) return;

        float shown = Mathf.InverseLerp(emptyPosY, fullPosY, bloodRect.anchoredPosition.y);
        float incoming = Gauge / 100f - shown;
        float targetTilt = Mathf.Clamp(incoming * 24f, -4f, 4f);
        _tilt = Mathf.SmoothDamp(_tilt, targetTilt, ref _tiltVelocity, 0.12f);
        bloodRect.localRotation = _levelRotation * Quaternion.Euler(0f, 0f, _tilt);
    }

    private void OnDestroy()
    {
        if (bloodRect != null)
        {
            bloodRect.DOKill();
        }
    }
}
