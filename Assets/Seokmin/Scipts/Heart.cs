using Hyunwoo.BloodPack;
using Hyunwoo.Raycasting2D.Scripts;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class Heart : MonoBehaviour
{
    [SerializeField] private RectTransform _pos;
    [SerializeField] private institution[] organs;
    private Camera _camera;
    private LineTest _dragged;
    private Mouse _heartPressMouse;
    private Vector2 _heartPressPoint;
    private double _lastHeartClick = double.NegativeInfinity;
    private const double DoubleClickSeconds = 0.3;
    private MedicalHover _heartHover;
    private MedicalHover _hovered;
    private readonly List<RaycastResult> _hits = new();
    public RectTransform BloodAnchor => _pos;

    private void Start()
    {
        _camera = Camera.main;
        _heartHover = GetComponent<MedicalHover>();
        if (organs == null || organs.Length == 0)
            organs = FindObjectsByType<institution>(FindObjectsSortMode.None);
    }

    public institution OrganAt(Vector2 position)
    {
        institution nearest = null;
        float best = float.PositiveInfinity;
        foreach (var organ in organs)
        {
            if (organ == null || !organ.isActiveAndEnabled || !organ.TryHit(position, _camera, out float distance) || distance >= best) continue;
            best = distance;
            nearest = organ;
        }
        return nearest;
    }

    private bool OverItem(Vector2 position)
    {
        var events = EventSystem.current;
        if (events == null) return false;
        _hits.Clear();
        events.RaycastAll(new PointerEventData(events) { position = position }, _hits);
        if (_hits.Count == 0) return false;
        var hit = _hits[0].gameObject;
        return hit.GetComponentInParent<MouseDragAndMove>() != null || hit.GetComponentInParent<BloodSyringe>() != null;
    }

    private void Update()
    {
        var mouse = _heartPressMouse ?? Mouse.current;
        if (mouse == null || !mouse.added || RaycastPaperView.IsReading || Time.timeScale <= 0f)
        {
            SetHover(null);
            CancelDrag();
            return;
        }

        Vector2 point = mouse.position.ReadValue();
        var organ = OrganAt(point);
        bool overHeart = RectTransformUtility.RectangleContainsScreenPoint(_pos, point, _camera);
        SetHover(organ != null ? organ.Hover : overHeart ? _heartHover : null);
        if (_heartPressMouse == null && mouse.leftButton.wasPressedThisFrame)
        {
            if (OverItem(point))
            {
                _lastHeartClick = double.NegativeInfinity;
                return;
            }
            if (organ == null && overHeart)
            {
                _heartPressMouse = mouse;
                _heartPressPoint = point;
            }
            else
            {
                _lastHeartClick = double.NegativeInfinity;
                if (organ == null) return;
                if (organ.HasVessel) organ.IsRelease();
                else organ.ConnectToHeart(_camera);
            }
        }
        if (_heartPressMouse != null)
            UpdateHeartPress(point, organ, overHeart);
    }

    private void UpdateHeartPress(Vector2 point, institution organ, bool overHeart)
    {
        float dragThreshold = Mathf.Max(1f, InputSystem.settings.tapRadius);
        if (_dragged == null && (point - _heartPressPoint).sqrMagnitude >= dragThreshold * dragThreshold)
        {
            _lastHeartClick = double.NegativeInfinity;
            var manager = insManager.Instance;
            _dragged = manager.GetLine();
            if (_dragged == null)
            {
                _heartPressMouse = null;
                return;
            }
            _dragged.gameObject.SetActive(true);
            _dragged.IsDrClick(manager.GetBloodPoint(_camera, _pos), _heartPressMouse);
        }

        if (_heartPressMouse.leftButton.isPressed) return;
        _heartPressMouse = null;
        if (_dragged != null)
        {
            if (organ != null && !organ.HasVessel) organ.Attach(_dragged, _camera);
            else _dragged.Retract();
            _dragged = null;
        }
        else if (overHeart && organ == null)
        {
            double now = Time.unscaledTimeAsDouble;
            if (now - _lastHeartClick <= DoubleClickSeconds)
            {
                foreach (var socket in organs)
                    if (socket != null) socket.IsRelease();
                _lastHeartClick = double.NegativeInfinity;
            }
            else _lastHeartClick = now;
        }
        else _lastHeartClick = double.NegativeInfinity;
    }

    private void SetHover(MedicalHover next)
    {
        if (_hovered == next) return;
        if (_hovered != null) _hovered.SetHovered(false);
        _hovered = next;
        if (_hovered != null) _hovered.SetHovered(true);
    }

    private void OnDisable()
    {
        SetHover(null);
        CancelDrag();
    }

    private void CancelDrag()
    {
        _lastHeartClick = double.NegativeInfinity;
        _heartPressMouse = null;
        if (_dragged != null) _dragged.Retract();
        _dragged = null;
    }
}
