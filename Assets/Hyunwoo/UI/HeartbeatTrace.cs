using UnityEngine;
using UnityEngine.UI;

namespace Hyunwoo.UI
{
    public sealed class HeartbeatTrace : MaskableGraphic
    {
        [SerializeField] private Heartbeat heart;
        [SerializeField, Range(0.1f, 0.9f)] private float trailLength = 0.43f;
        [SerializeField, Min(0f), InspectorName("상하 파형 높이 배율")] private float peakHeightMultiplier = 1f;

        // X is measured from the strong beat in fractions of the trace width.
        private static readonly Vector2[] Wave =
        {
            new(-1f, 0f), new(-0.16f, 0f),
            new(-0.14f, 0.04f), new(-0.122f, 0.10f), new(-0.104f, 0.05f), new(-0.086f, 0f),
            new(-0.032f, 0f), new(-0.018f, -0.13f), new(0f, 1f), new(0.025f, -0.90f),
            new(0.043f, -0.10f), new(0.055f, 0f), new(0.083f, 0f),
            new(0.103f, 0.05f), new(0.128f, 0.16f), new(0.145f, 0.18f),
            new(0.165f, 0.13f), new(0.185f, 0.04f), new(0.205f, 0f), new(1f, 0f)
        };
        private float _head;
        private float _peakX = 0.5f;
        private bool _hasHeartbeat;

        private void LateUpdate()
        {
            bool hasHeartbeat = heart != null && heart.isActiveAndEnabled;
            float peakX = 0.5f;
            if (hasHeartbeat && heart.transform is RectTransform heartRect)
            {
                Rect rect = rectTransform.rect;
                Vector3 heartCenter = heartRect.TransformPoint(heartRect.rect.center);
                float localX = rectTransform.InverseTransformPoint(heartCenter).x;
                peakX = Mathf.InverseLerp(rect.xMin, rect.xMax, localX);
            }

            float head = hasHeartbeat ? Mathf.Repeat(heart.PhaseSinceStrongBeat + peakX, 1f)
                : Mathf.Repeat(Time.time / 3.4f, 1f);
            if (head == _head && peakX == _peakX && hasHeartbeat == _hasHeartbeat) return;
            _head = head;
            _peakX = peakX;
            _hasHeartbeat = hasHeartbeat;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            DrawTrail(mesh, _head);
            if (_head < trailLength) DrawTrail(mesh, _head + 1f);
        }

        private void DrawTrail(VertexHelper mesh, float head)
        {
            Rect rect = rectTransform.rect;
            float tail = head - trailLength;
            float visibleStart = Mathf.Max(0f, tail);
            float visibleEnd = Mathf.Min(1f, head);
            Vector2 offset = Vector2.right * _peakX;
            for (int i = 1; i < Wave.Length; i++)
            {
                Vector2 start = Wave[i - 1] + offset;
                Vector2 end = Wave[i] + offset;
                float left = Mathf.Max(start.x, visibleStart);
                float right = Mathf.Min(end.x, visibleEnd);
                if (right <= left) continue;

                Vector2 a = Point(rect, start, end, left);
                Vector2 b = Point(rect, start, end, right);
                Vector2 normal = Vector2.Perpendicular(b - a).normalized;
                float alphaA = Mathf.InverseLerp(tail, head, left);
                float alphaB = Mathf.InverseLerp(tail, head, right);
                Stroke(mesh, a, b, normal * 3.3f, alphaA * 0.18f, alphaB * 0.18f);
                Stroke(mesh, a, b, normal * 1.2f, alphaA, alphaB);
            }
        }

        private Vector2 Point(Rect rect, Vector2 start, Vector2 end, float x)
        {
            float height = _hasHeartbeat
                ? Mathf.Lerp(start.y, end.y, Mathf.InverseLerp(start.x, end.x, x)) * peakHeightMultiplier
                : 0f;
            return new Vector2(rect.xMin + rect.width * x, rect.center.y + height * rect.height * 0.46f);
        }

        private void Stroke(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 radius, float alphaA, float alphaB)
        {
            Color start = color;
            Color end = color;
            start.a *= alphaA;
            end.a *= alphaB;
            int first = mesh.currentVertCount;
            mesh.AddVert(a - radius, start, Vector2.zero);
            mesh.AddVert(a + radius, start, Vector2.zero);
            mesh.AddVert(b + radius, end, Vector2.zero);
            mesh.AddVert(b - radius, end, Vector2.zero);
            mesh.AddTriangle(first, first + 1, first + 2);
            mesh.AddTriangle(first, first + 2, first + 3);
        }
    }
}
