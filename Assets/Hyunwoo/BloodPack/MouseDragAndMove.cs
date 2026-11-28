using DG.Tweening;
using Hyunwoo.HumanBodySystem;
using Hyunwoo.Raycasting2D.Scripts;
using Hyunwoo.Save;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Hyunwoo.BloodPack
{
    public class MouseDragAndMove : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField, Min(0f)] private float insertBloodAmount = 50f;
        [SerializeField] private HeartModule heartModule;
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private RectTransform canvasRect;
        [SerializeField, Range(1f, 1.4f)] private float hoverScale = 1.1f;
        [SerializeField] private BloodPackLiquid liquid;

        private RectTransform _rect;
        private Vector2 _home;
        private Vector3 _normalScale;
        private bool _dragging;
        private bool _overHeart;
        private Tween _scaleTween;
        private Tween _returnTween;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            _home = _rect.anchoredPosition;
            _normalScale = _rect.localScale;
        }

        private void OnEnable()
        {
            if (liquid != null) liquid.ResetMotion(_rect.anchoredPosition);
        }

        private void LateUpdate()
        {
            if (liquid != null) liquid.FollowBag(_rect.anchoredPosition, Time.unscaledDeltaTime);
        }

        private bool CanUse(PointerEventData data) =>
            !RaycastPaperView.IsReading && Time.timeScale > 0f && insertBloodAmount > 0f && heartModule.CanReceiveBloodPack &&
            RectTransformUtility.RectangleContainsScreenPoint((RectTransform)heartModule.transform, data.position, data.pressEventCamera);

        public void OnBeginDrag(PointerEventData data)
        {
            _dragging = data.button == PointerEventData.InputButton.Left && Time.timeScale > 0f && !RaycastPaperView.IsReading;
            if (_dragging) _returnTween?.Kill();
        }

        public void OnDrag(PointerEventData data)
        {
            if (!_dragging) return;
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(canvasRect, data.position, data.pressEventCamera, out var point))
                _rect.position = point;
            bool overHeart = CanUse(data);
            if (_overHeart == overHeart) return;
            _overHeart = overHeart;
            _scaleTween?.Kill();
            _scaleTween = _rect.DOScale(_normalScale * (_overHeart ? hoverScale : 1f), 0.10f).SetEase(Ease.OutQuad);
        }

        public void OnEndDrag(PointerEventData data)
        {
            if (!_dragging) return;
            _dragging = _overHeart = false;
            _scaleTween?.Kill();
            _rect.localScale = _normalScale;
            if (CanUse(data) && (inventory == null || inventory.TryConsume("blood-pack")))
            {
                heartModule.DistributeBlood(insertBloodAmount);
                _rect.anchoredPosition = _home;
                if (liquid != null) liquid.ResetMotion(_home);
                if (inventory == null) gameObject.SetActive(false);
                return;
            }
            _returnTween = _rect.DOAnchorPos(_home, 0.18f).SetEase(Ease.OutCubic).SetUpdate(true);
        }

        private void OnDisable()
        {
            _returnTween?.Kill();
            _scaleTween?.Kill();
            _dragging = _overHeart = false;
            if (_rect == null) return;
            _rect.anchoredPosition = _home;
            _rect.localScale = _normalScale;
        }
    }
}
