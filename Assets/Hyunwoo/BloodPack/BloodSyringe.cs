using System.Collections;
using Hyunwoo.HumanBodySystem;
using Hyunwoo.Raycasting2D.Scripts;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hyunwoo.BloodPack
{
    public sealed class BloodSyringe : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IBloodReservoir
    {
        [SerializeField] private Heart heart;
        [SerializeField] private RectTransform plunger;
        [SerializeField] private RectTransform barrel;
        [InspectorName("주사기 용량"), SerializeField, Min(1f)] private float capacity = 25f;

        // 기존 주사기 스프라이트에서 약실과 바늘 끝이 차지하는 위치.
        private const float ChamberBottom = 0.30f;
        private const float ChamberTop = 0.81f;
        private const float NeedleTip = 0.02f;
        private const float TransferSeconds = 0.55f;
        private const float EmptyTolerance = 0.0001f;

        private RectTransform _rect;
        private RectTransform _fill;
        private Vector2 _home;
        private Quaternion _homeRotation;
        private IBloodReservoir _source;
        private float _blood;
        private bool _dragging;
        private bool _busy;

        public float Capacity => capacity;
        public float BloodGauge
        {
            get => _blood;
            set
            {
                _blood = Mathf.Clamp(value, 0f, capacity);
                float level = capacity > 0f ? _blood / capacity : 0f;
                _fill.anchorMax = new Vector2(0.68f, Mathf.Lerp(ChamberBottom, ChamberTop, level));
                float surface = Mathf.Lerp(barrel.rect.yMin, barrel.rect.yMax, _fill.anchorMax.y);
                // 피스톤 고무의 아랫면은 피스톤 그림 높이의 43% 지점에 있다.
                float seal = Mathf.Lerp(plunger.rect.yMin, plunger.rect.yMax, 0.43f);
                plunger.anchoredPosition = barrel.anchoredPosition + Vector2.up * (surface - seal);
            }
        }

        private void Awake()
        {
            _rect = (RectTransform)transform;
            _home = _rect.anchoredPosition;
            _homeRotation = _rect.localRotation;
            var image = new GameObject("Blood level", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            _fill = image.rectTransform;
            _fill.SetParent(barrel, false);
            _fill.anchorMin = new Vector2(0.32f, ChamberBottom);
            _fill.anchorMax = new Vector2(0.68f, ChamberBottom);
            _fill.offsetMin = _fill.offsetMax = Vector2.zero;
            image.raycastTarget = false;
            image.color = new Color32(137, 12, 23, 214);
            BloodGauge = 0f;
        }

        public void OnBeginDrag(PointerEventData data)
        {
            _dragging = !_busy && data.button == PointerEventData.InputButton.Left &&
                Time.timeScale > 0f && !RaycastPaperView.IsReading;
            if (!_dragging) return;
            _rect.localRotation = _homeRotation * Quaternion.Euler(0f, 0f, -45f);
            OnDrag(data);
        }

        public void OnDrag(PointerEventData data)
        {
            if (!_dragging) return;
            RectTransform parent = (RectTransform)_rect.parent;
            if (!RectTransformUtility.ScreenPointToWorldPointInRectangle(parent, data.position, data.pressEventCamera, out var point)) return;
            Vector3 tip = barrel.TransformPoint(new Vector2(0f, Mathf.Lerp(barrel.rect.yMin, barrel.rect.yMax, NeedleTip)));
            _rect.position += point - tip;
        }

        public void OnEndDrag(PointerEventData data)
        {
            if (!_dragging) return;
            OnDrag(data);
            _dragging = false;
            var target = !RaycastPaperView.IsReading && Time.timeScale > 0f
                ? heart.OrganAt(data.position)?.Organ : null;
            StartCoroutine(UseOrReturn(target));
        }

        private IEnumerator UseOrReturn(IBloodReservoir target)
        {
            _busy = true;
            bool draw = _source == null;
            if (target != null && !ReferenceEquals(target, _source) &&
                (draw ? target.BloodGauge > 0f : target.BloodGauge < target.Capacity))
            {
                Vector2 start = _rect.anchoredPosition;
                Vector2 needleDirection = _rect.localRotation * Vector3.down;
                yield return Slide(start + needleDirection * 12f, 0.13f);
                yield return Transfer(target, draw);
                yield return Slide(start, 0.15f);
            }
            yield return Slide(_home, 0.20f, returnHome: true);
            _busy = false;
        }

        private IEnumerator Transfer(IBloodReservoir target, bool draw)
        {
            IBloodReservoir from = draw ? target : this;
            IBloodReservoir to = draw ? this : target;
            float budget = Mathf.Min(from.BloodGauge, to.Capacity - to.BloodGauge);
            float moved = 0f;
            for (float t = 0f; t < 1f;)
            {
                t = Mathf.Min(1f, t + Time.deltaTime / TransferSeconds);
                float due = budget * Mathf.SmoothStep(0f, 1f, t) - moved;
                moved += BloodTransfer.Move(from, to, due);
                if (draw && moved > 0f) _source = target;
                yield return null;
            }
            if (_blood <= EmptyTolerance)
            {
                BloodGauge = 0f;
                _source = null;
            }
        }

        private IEnumerator Slide(Vector2 destination, float duration, bool returnHome = false)
        {
            Vector2 start = _rect.anchoredPosition;
            Quaternion startRotation = _rect.localRotation;
            for (float t = 0f; t < 1f;)
            {
                t = Mathf.Min(1f, t + Time.unscaledDeltaTime / duration);
                float progress = Mathf.SmoothStep(0f, 1f, t);
                _rect.anchoredPosition = Vector2.Lerp(start, destination, progress);
                if (returnHome) _rect.localRotation = Quaternion.Slerp(startRotation, _homeRotation, progress);
                yield return null;
            }
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            _busy = _dragging = false;
            if (_rect != null)
            {
                _rect.anchoredPosition = _home;
                _rect.localRotation = _homeRotation;
            }
            if (_blood <= EmptyTolerance) _source = null;
        }
    }
}
