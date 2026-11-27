using UnityEngine;
using DG.Tweening;
using Hyunwoo.HumanBodySystem;

public class Heartbeat : MonoBehaviour
{
    public event System.Action StrongBeat;
    [Header("첫 번째 박동 (작게 쿵)")]
    [SerializeField] private float firstScale = 1.08f;
    [SerializeField] private float firstDuration = 0.07f;

    [Header("두 번째 박동 (크게 쾅)")]
    [SerializeField] private float secondScale = 1.15f;
    [SerializeField] private float secondDuration = 0.10f;

    [Header("쉬는 시간 (초)")]
    [SerializeField] private float restTime = 0.8f;

    private HeartModule _circulation;
    private Vector3 _baseScale;
    private Sequence _sequence;

    private void Awake()
    {
        _circulation = GetComponent<HeartModule>();
        _baseScale = transform.localScale;
    }

    private void OnEnable()
    {
        _sequence = DOTween.Sequence()
            .Append(transform.DOScale(_baseScale * firstScale, firstDuration))
            .Append(transform.DOScale(_baseScale, firstDuration))
            .Append(transform.DOScale(_baseScale * secondScale, secondDuration))
            .AppendCallback(() => StrongBeat?.Invoke())
            .Append(transform.DOScale(_baseScale, secondDuration));

        if (_circulation == null)
            _sequence.AppendInterval(restTime).SetLoops(-1);
        else
        {
            _sequence.SetAutoKill(false).Pause();
            _circulation.Beat += PlayBeat;
        }
    }

    private void PlayBeat()
    {
        if (!_sequence.IsPlaying()) _sequence.Restart();
    }

    private void OnDisable()
    {
        if (_circulation != null) _circulation.Beat -= PlayBeat;
        _sequence?.Kill();
        transform.localScale = _baseScale;
    }
}
