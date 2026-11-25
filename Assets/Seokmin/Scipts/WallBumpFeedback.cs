using UnityEngine;
using DG.Tweening;

public class WallBumpFeedback : MonoBehaviour
{
    [SerializeField] private Transform renderPivot;
    [SerializeField] private float strength = 0.1f;
    [SerializeField] private float duration = 0.2f;

    private Vector3 _origin;
    private Tween _tween;

    private void Awake()
    {
        _origin = renderPivot.localPosition;
    }

    public void PlayBump(Vector2 direction)
    {
        if (_tween != null && _tween.IsActive())
            return;

        Vector3 localDir = renderPivot.parent
            .InverseTransformDirection(direction.normalized);

        Sequence sequence = DOTween.Sequence();

        sequence.Append(
            renderPivot.DOLocalMove(
                    _origin + localDir * strength,
                    duration * 0.15f)
                .SetEase(Ease.InQuad)
        );

        sequence.Append(
            renderPivot.DOLocalMove(
                    _origin - localDir * strength * 0.6f,
                    duration * 0.25f)
                .SetEase(Ease.OutExpo)
        );

        sequence.Append(
            renderPivot.DOLocalMove(
                    _origin,
                    duration * 0.6f)
                .SetEase(Ease.OutSine)
        );

        _tween = sequence.SetLink(renderPivot.gameObject);
    }

    private void OnDisable()
    {
        _tween?.Kill();

        if (renderPivot != null)
            renderPivot.localPosition = _origin;
    }
}
