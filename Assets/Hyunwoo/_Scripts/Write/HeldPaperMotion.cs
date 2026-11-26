using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Hyunwoo.Raycasting2D.Scripts
{
    [RequireComponent(typeof(Image), typeof(RectMask2D))]
    public sealed class HeldPaperMotion : MonoBehaviour
    {
        [SerializeField] private RectTransform sheet;
        [SerializeField] private CanvasGroup transcription;
        [SerializeField, Min(0.01f)] private float riseDuration = 0.55f;
        [SerializeField, Min(0.01f)] private float fallDuration = 0.32f;
        [Tooltip("화면 아래로 종이가 완전히 빠진 위치에 더하는 거리 (Canvas 단위)")]
        [SerializeField, Min(0f)] private float extraTravel = 24f;
        [SerializeField, Range(-15f, 15f)] private float pickupAngle = -4f;
        [SerializeField, Range(-20f, 20f)] private float readingAngle = 7f;
        [SerializeField, Range(0.4f, 0.9f)] private float backdropFill = 0.68f;
        [SerializeField, Range(0.5f, 0.95f)] private float pageFill = 0.88f;
        [SerializeField, Range(0f, 1f)] private float backgroundDim = 0.5f;

        private readonly Vector3[] _corners = new Vector3[4];
        private RectTransform _viewport;
        private RectTransform _overlay;
        private RectTransform _readingArea;
        private Camera _areaCamera;
        private Image _shade;
        private Tween _slide;
        private float _lift;
        private bool _hasTranscription;
        public bool IsRaised => _lift >= 1f;

        private void Awake()
        {
            _viewport = (RectTransform)transform;
            _overlay = (RectTransform)_viewport.parent;
            _shade = GetComponent<Image>();
        }

        public void FitPage(Sprite sprite, bool hasTranscription)
        {
            sheet.sizeDelta = sprite.rect.size;
            _hasTranscription = hasTranscription;
            if (_readingArea != null) PositionPaper();
        }

        public void Raise(RectTransform readingArea, Camera areaCamera)
        {
            _readingArea = readingArea;
            _areaCamera = areaCamera;
            _lift = 0f;
            Canvas.ForceUpdateCanvases();
            PositionPaper();
            SlideTo(1f, riseDuration, Ease.OutCubic);
        }

        public void Lower(TweenCallback onHidden)
        {
            if (_lift <= 0f) onHidden();
            else SlideTo(0f, fallDuration * _lift, Ease.InQuad).OnComplete(onHidden);
        }

        private Tween SlideTo(float target, float duration, Ease ease)
        {
            _slide?.Kill();
            _slide = DOTween.To(() => _lift, value => _lift = value, target, duration)
                .SetEase(ease).SetUpdate(true).SetRecyclable(false);
            return _slide;
        }

        private void LateUpdate()
        {
            if (_readingArea != null) PositionPaper();
        }

        private void PositionPaper()
        {
            // 씬의 카메라 Canvas 영역을 오버레이 좌표로 옮겨 오른쪽 신체 UI와 분리한다.
            _readingArea.GetWorldCorners(_corners);
            Vector2 lower = Vector2.Max(_overlay.rect.min, OverlayPoint(_corners[0]));
            Vector2 upper = Vector2.Min(_overlay.rect.max, OverlayPoint(_corners[2]));
            Vector2 areaSize = Vector2.Max(Vector2.zero, upper - lower);
            Vector2 center = (upper + lower) * 0.5f - _overlay.rect.center;
            if (_viewport.sizeDelta != areaSize) _viewport.sizeDelta = areaSize;
            if (_viewport.anchoredPosition != center) _viewport.anchoredPosition = center;

            Vector2 size = sheet.sizeDelta;
            float angle = Mathf.Lerp(pickupAngle, _hasTranscription ? readingAngle : 0f, _lift);
            float radians = angle * Mathf.Deg2Rad;
            float sin = Mathf.Abs(Mathf.Sin(radians));
            float cos = Mathf.Abs(Mathf.Cos(radians));
            Vector2 tiltedSize = new Vector2(size.x * cos + size.y * sin, size.x * sin + size.y * cos);
            float fill = _hasTranscription ? backdropFill : pageFill;
            float scale = Mathf.Min(areaSize.x / tiltedSize.x, areaSize.y / tiltedSize.y) * fill;
            Vector3 localScale = Vector3.one * scale;
            if (sheet.localScale != localScale) sheet.localScale = localScale;

            float travel = (areaSize.y + tiltedSize.y * scale) * 0.5f + extraTravel;
            Vector2 position = Vector2.down * (travel * (1f - _lift));
            Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
            if (sheet.anchoredPosition != position) sheet.anchoredPosition = position;
            if (!sheet.localRotation.Equals(rotation)) sheet.localRotation = rotation;

            Color tint = new Color(0f, 0f, 0f, backgroundDim * _lift);
            if (_shade.color != tint) _shade.color = tint;
            float opacity = _hasTranscription ? Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.4f, 1f, _lift)) : 0f;
            if (transcription.alpha != opacity) transcription.alpha = opacity;
        }

        private Vector2 OverlayPoint(Vector3 worldPoint)
        {
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(_areaCamera, worldPoint);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_overlay, screen, null, out Vector2 local);
            return local;
        }

        private void OnDisable()
        {
            _slide?.Kill();
            _slide = null;
            _readingArea = null;
        }
    }
}
