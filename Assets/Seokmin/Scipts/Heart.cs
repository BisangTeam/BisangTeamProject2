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
        var mouse = _dragged != null ? _dragged.DragMouse : Mouse.current;
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
        if (_dragged == null && mouse.leftButton.wasPressedThisFrame && !OverItem(point))
        {
            if (organ != null) organ.IsRelease();
            else if (overHeart)
            {
                _dragged = insManager.Instance.GetLine();
                if (_dragged != null)
                {
                    _dragged.gameObject.SetActive(true);
                    _dragged.IsDrClick(insManager.Instance.GetBloodPoint(_camera, _pos), mouse);
                }
            }
        }
        if (_dragged == null || mouse.leftButton.isPressed) return;
        if (organ != null && !organ.Connected) organ.Attach(_dragged, _camera);
        else _dragged.Retract(false);
        _dragged = null;
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
        if (_dragged != null) _dragged.Retract(false);
        _dragged = null;
    }
}
