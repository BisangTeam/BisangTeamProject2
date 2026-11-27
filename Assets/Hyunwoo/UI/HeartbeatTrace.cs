using UnityEngine;
using UnityEngine.UI;

namespace Hyunwoo.UI
{
    public sealed class HeartbeatTrace : MaskableGraphic
    {
        [SerializeField] private Heartbeat heart;
        [SerializeField, Min(1f)] private float sweepSeconds = 3.4f;
        [SerializeField, Min(0.1f)] private float trailSeconds = 1.45f;
        private const int Samples = 192;
        private readonly float[] _height = new float[Samples];
        private readonly float[] _timestamp = new float[Samples];
        private int _step;
        private float _start;
        private float _beat;

        protected override void OnEnable()
        {
            base.OnEnable();
            _start = Time.time;
            _beat = float.NegativeInfinity;
            _step = -1;
            System.Array.Fill(_timestamp, float.NegativeInfinity);
            if (heart != null) heart.StrongBeat += Beat;
        }

        protected override void OnDisable()
        {
            if (heart != null) heart.StrongBeat -= Beat;
            base.OnDisable();
        }

        private void Beat() => _beat = Time.time;

        private static float Peak(float t, float centre, float width) =>
            Mathf.Max(0f, 1f - Mathf.Abs(t - centre) / width);

        private void Update()
        {
            float interval = sweepSeconds / Samples;
            int current = Mathf.FloorToInt((Time.time - _start) / interval);
            if (_step == current) return;
            for (int n = Mathf.Max(_step + 1, current - Samples + 1); n <= current; n++)
            {
                int index = n % Samples;
                float time = _start + n * interval;
                float t = time - _beat;
                _height[index] = 0.12f * Peak(t, 0.08f, 0.05f) - 0.18f * Peak(t, 0.20f, 0.03f)
                    + Peak(t, 0.25f, 0.044f) - 0.38f * Peak(t, 0.31f, 0.04f) + 0.19f * Peak(t, 0.52f, 0.10f);
                _timestamp[index] = time;
            }
            _step = current;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = rectTransform.rect;
            for (int i = 1; i < Samples; i++)
            {
                float age = Time.time - _timestamp[i - 1];
                if (age >= trailSeconds || _timestamp[i] < _timestamp[i - 1]) continue;
                Vector2 a = Point(rect, i - 1);
                Vector2 b = Point(rect, i);
                Vector2 normal = Vector2.Perpendicular(b - a).normalized;
                float alpha = 1f - age / trailSeconds;
                Stroke(mesh, a, b, normal * 3.3f, alpha * 0.18f);
                Stroke(mesh, a, b, normal * 1.2f, alpha);
            }
        }

        private Vector2 Point(Rect rect, int i) => new Vector2(
            rect.xMin + rect.width * i / (Samples - 1), rect.center.y + _height[i] * rect.height * 0.42f);

        private void Stroke(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 radius, float alpha)
        {
            Color tint = color;
            tint.a *= alpha;
            int first = mesh.currentVertCount;
            mesh.AddVert(a - radius, tint, Vector2.zero);
            mesh.AddVert(a + radius, tint, Vector2.zero);
            mesh.AddVert(b + radius, tint, Vector2.zero);
            mesh.AddVert(b - radius, tint, Vector2.zero);
            mesh.AddTriangle(first, first + 1, first + 2);
            mesh.AddTriangle(first, first + 2, first + 3);
        }
    }
}
