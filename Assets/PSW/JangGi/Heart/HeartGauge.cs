using UnityEngine;

public class HeartGauge : MonoBehaviour
{
    // 기존 씬에서 심장 그림의 자식인 게이지 이미지만 숨긴다.
    [SerializeField] private RectTransform bloodRect;

    private void Awake()
    {
        if (bloodRect != null) bloodRect.gameObject.SetActive(false);
    }
}
