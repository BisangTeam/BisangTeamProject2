using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class WalkFeedback : MonoBehaviour
{
    [SerializeField] private RawImage view;
    [SerializeField] private float strength = 0.015f;
    [SerializeField] private float duration = 0.15f;

    private Rect _origin;
    private Tween _tween;
    private float _offset;

    private void Awake()
    {
        _origin = new Rect(0.02f, 0.02f, 0.96f, 0.96f);
        view.uvRect = _origin;
    }

    public void PlayStep()
    {
        if (_tween != null && _tween.IsActive())
            return;

        _tween = DOTween.To(
                () => _offset,
                value => SetOffset(value),
                strength,
                duration
            )
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    
    
    public void StopStep()
    {
        _tween?.Kill();

        _tween = DOTween.To(
            () => _offset,
            value => SetOffset(value),
            0f,
            duration
        ).SetEase(Ease.OutSine);
    }

    private void SetOffset(float value)
    {
        if (view == null)
        {
            _tween?.Kill();
            return;
        }

        _offset = value;

        Rect rect = _origin;
        rect.y += value;
        view.uvRect = rect;
    }

    private void OnDisable()
    {
        _tween?.Kill();
        SetOffset(0f);
    }
}