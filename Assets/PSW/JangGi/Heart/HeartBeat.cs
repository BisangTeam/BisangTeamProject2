using UnityEngine;
using DG.Tweening;

public class Heartbeat : MonoBehaviour
{
    [Header("첫 번째 박동 (작게 쿵)")]
    [SerializeField] private float firstScale = 1.08f;
    [SerializeField] private float firstDuration = 0.07f;

    [Header("두 번째 박동 (크게 쾅)")]
    [SerializeField] private float secondScale = 1.15f;
    [SerializeField] private float secondDuration = 0.10f;

    [Header("쉬는 시간 (초)")]
    [SerializeField] private float restTime = 0.8f;

    private void Start()
    {
        Vector3 baseScale = transform.localScale;
        Sequence sequence = DOTween.Sequence();

        sequence.Append(transform.DOScale(baseScale * firstScale, firstDuration));
        sequence.Append(transform.DOScale(baseScale, firstDuration));
        sequence.Append(transform.DOScale(baseScale * secondScale, secondDuration));
        sequence.Append(transform.DOScale(baseScale, secondDuration));
        sequence.AppendInterval(restTime);

        sequence.SetLoops(-1);
        sequence.SetLink(gameObject);
    }
}