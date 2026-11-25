using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class WalkFeedback : MonoBehaviour
{
    [SerializeField] private RawImage view;
    [SerializeField] private float strength = 0.015f;
    [SerializeField] private float duration = 0.15f;
    [SerializeField] private float bumpStrength = 0.2f;
    [SerializeField] private float bumpDuration = 0.35f;
    [SerializeField] private float bumpDarkness = 0.2f;

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

        if (_bumpTween != null && _bumpTween.IsActive())
            return;

        _tween?.Kill();
        _tween = null;

        SetOffset(0f);
        SetBump(0f);

        Sequence sequence = DOTween.Sequence();

        // 벽을 향해 빠르게 쏠림
        sequence.Append(
            DOTween.To(() => _bump, SetBump,
                    bumpStrength, bumpDuration * 0.18f)
                .SetEase(Ease.InQuad)
        );

        // 충돌 순간 정지
        sequence.AppendInterval(bumpDuration * 0.08f);

        // 충격으로 강하게 뒤로 밀림
        sequence.Append(
            DOTween.To(() => _bump, SetBump,
                    -0.035f, bumpDuration * 0.2f)
                .SetEase(Ease.OutExpo)
        );

        // 원래 시야로 복귀
        sequence.Append(
            DOTween.To(() => _bump, SetBump,
                    0f, bumpDuration * 0.54f)
                .SetEase(Ease.OutSine)
        );

        _bumpTween = sequence
            .SetLink(view.gameObject)
            .OnComplete(() =>
            {
                SetBump(0f);
                _bumpTween = null;
            });
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

        // 충돌 순간 화면 어두워짐
        float impact = Mathf.Clamp01(
            Mathf.Max(0f, value) /
            Mathf.Max(bumpStrength, 0.0001f)
        );

        float brightness = 1f - impact * bumpDarkness;

        view.color = new Color(
            brightness,
            brightness,
            brightness,
            view.color.a
        );
    }

    public void PlayStep()
    {
        if (_tween != null && _tween.IsActive())
            return;
        
        if (_bumpTween != null && _bumpTween.IsActive())
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
        if (_bumpTween != null && _bumpTween.IsActive())
            return;
        
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
        
        _bumpTween?.Kill();
        _bumpTween = null;
    }
}