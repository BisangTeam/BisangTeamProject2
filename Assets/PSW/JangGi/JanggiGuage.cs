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

    private void OnDestroy()
    {
        if (bloodRect != null)
        {
            bloodRect.DOKill();
        }
    }
}