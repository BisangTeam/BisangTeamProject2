using Hyunwoo.HumanBodySystem;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hyunwoo.Raycasting2D.Scripts
{
    public sealed class LowBloodPulse : MonoBehaviour
    {
        [SerializeField] private HeartModule heart;
        [SerializeField] private Heartbeat heartbeat;
        [SerializeField, Range(0.01f, 1f)] private float threshold = 0.3f;
        [SerializeField, Min(0f)] private float maximumHeight = 0.026f;
        [SerializeField, Min(0.05f)] private float pulseDuration = 0.26f;
        [SerializeField] private bool preview;
        [SerializeField, Range(0f, 1f)] private float previewBlood = 0.6f;

        private float _lastBeat = float.NegativeInfinity;
        private float _strength;
        private bool _draggingSlider;
        private static readonly Rect SliderTrack = new Rect(26, 42, 230, 24);
        public float HeightOffset { get; private set; }

        private void OnEnable() => heartbeat.StrongBeat += OnBeat;

        private void OnDisable()
        {
            heartbeat.StrongBeat -= OnBeat;
            _lastBeat = float.NegativeInfinity;
            _strength = 0f;
            _draggingSlider = false;
            HeightOffset = 0f;
        }

        private void OnBeat() => _lastBeat = Time.time;

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f8Key.wasPressedThisFrame) preview = !preview;
            ReadPreviewSlider();
            float blood = preview ? previewBlood : heart.BloodGaugeRatio;
            float target = Mathf.Clamp01(1f - blood / threshold);
            _strength = Mathf.MoveTowards(_strength, target, Time.deltaTime * 4f);

            float phase = Mathf.Clamp01((Time.time - _lastBeat) / pulseDuration);
            float decay = 1f - phase;
            HeightOffset = Mathf.Sin(phase * 2f * Mathf.PI) * decay * decay * _strength * maximumHeight;
        }

        private void ReadPreviewSlider()
        {
            Mouse mouse = Mouse.current;
            if (!preview || mouse == null || !mouse.leftButton.isPressed)
            {
                _draggingSlider = false;
                return;
            }
            Vector2 point = mouse.position.ReadValue();
            point.y = Screen.height - point.y;
            if (mouse.leftButton.wasPressedThisFrame) _draggingSlider = SliderTrack.Contains(point);
            if (_draggingSlider) previewBlood = Mathf.InverseLerp(SliderTrack.xMin, SliderTrack.xMax, point.x);
        }

        private void OnGUI()
        {
            if (!preview) return;
            GUI.Box(new Rect(12, 12, 260, 62), $"Heart preview  {previewBlood:P0}     [F8]");
            GUI.HorizontalSlider(new Rect(SliderTrack.x, 48, SliderTrack.width, 18), previewBlood, 0f, 1f);
        }
    }
}
