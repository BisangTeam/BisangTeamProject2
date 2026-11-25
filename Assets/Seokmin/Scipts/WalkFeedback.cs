using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class WalkFeedback : MonoBehaviour
{
    [SerializeField] private RawImage view;
    [SerializeField] private float strength = 0.015f;
    [SerializeField] private float duration = 0.15f;
    [SerializeField] private float bumpStrength = 0.06f;
    [SerializeField] private float bumpDuration = 0.2f;

    private Tween _bumpTween;
    private float _bump;

    private Rect _origin;
    private Tween _tween;
    private float _offset;

    private void Awake()
    {
        _origin = new Rect(0.02f, 0.02f, 0.96f, 0.96f);
        view.uvRect = _origin;
    }
    
    public void PlayBump()
    {
        if (view == null) return;

        _tween?.Kill();
        _tween = null;

        _bumpTween?.Kill();

        SetOffset(0f);
        SetBump(0f);

        _bumpTween = DOTween.To(
                () => _bump,
                SetBump,
                bumpStrength,
                bumpDuration / 2f
            )
            .SetEase(Ease.OutQuad)
            .SetLoops(2, LoopType.Yoyo)
            .SetLink(view.gameObject)
            .OnComplete(() => SetBump(0f));
    }

    private void SetBump(float value)
    {
        if (view == null) return;

        _bump = value;

        Rect rect = _origin;

        rect.x += value / 2f;
        rect.y += value / 2f;

        rect.width -= value;
        rect.height -= value;

        view.uvRect = rect;
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
            .SetLoops(-1, LoopType.Yoyo)
            .SetLink(view.gameObject);
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
        _tween = null;

        if (view != null)
            SetOffset(0f);
    }
}