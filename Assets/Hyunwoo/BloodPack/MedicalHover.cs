using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hyunwoo.BloodPack
{
    public sealed class MedicalHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Graphic[] images;
        [SerializeField] private bool organ;
        private const float TrembleSeconds = 0.18f;
        private Outline[] _outlines;
        private Quaternion _restRotation;
        private float _remaining;
        private bool _hovered;

        private void Awake()
        {
            _outlines = new Outline[images.Length];
            for (int i = 0; i < images.Length; i++)
            {
                var outline = images[i].gameObject.AddComponent<Outline>();
                outline.effectDistance = new Vector2(1f, -1f);
                outline.effectColor = Tint(false);
                _outlines[i] = outline;
            }
        }

        public void SetHovered(bool hovered)
        {
            if (_hovered == hovered) return;
            _hovered = hovered;
            foreach (Outline outline in _outlines) outline.effectColor = Tint(hovered);

            if (!organ || !hovered) return;
            if (_remaining <= 0f) _restRotation = transform.localRotation;
            _remaining = TrembleSeconds;
        }

        private static Color Tint(bool hovered) => new Color(1f, 1f, 1f, hovered ? 0.33f : 0.14f);

        private void Update()
        {
            if (_remaining <= 0f) return;
            _remaining = Mathf.Max(0f, _remaining - Time.deltaTime);
            float t = 1f - _remaining / TrembleSeconds;
            float angle = Mathf.Sin(t * Mathf.PI * 4f) * (1f - t) * 1.65f;
            transform.localRotation = _restRotation * Quaternion.Euler(0f, 0f, angle);
        }

        public void OnPointerEnter(PointerEventData data) { if (!organ) SetHovered(true); }
        public void OnPointerExit(PointerEventData data) { if (!organ) SetHovered(false); }

        private void OnDisable()
        {
            if (_remaining > 0f) transform.localRotation = _restRotation;
            _remaining = 0f;
            SetHovered(false);
        }
    }
}
