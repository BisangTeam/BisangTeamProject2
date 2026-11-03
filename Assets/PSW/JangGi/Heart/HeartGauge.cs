using DG.Tweening;
using UnityEngine;

public class HeartGauge : MonoBehaviour
{
    // Transform 대신 RectTransform 사용
    [SerializeField] private RectTransform bloodRect;

    [Range(0f, 100f)] [SerializeField] private float gauge;

    // UI 캔버스 픽셀 단위 (예: 높이가 200px라면 -200f에서 0f 등)
    [SerializeField] private float emptyPosY = -200f;
    [SerializeField] private float fullPosY = 0f;

    [SerializeField] private float fillDuration = 0.4f;
    [SerializeField] private Ease fillEase = Ease.OutQuad;

    private void Start()
    {
        UpdateGauge(gauge);
    }

    private void OnValidate()
    {
        if (bloodRect != null && Application.isPlaying)
        {
            UpdateGauge(gauge);
        }
    }

    public void UpdateGauge(float newGauge)
    {
        gauge = Mathf.Clamp(newGauge, 0f, 100f);
        float chg = gauge / 100f;
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