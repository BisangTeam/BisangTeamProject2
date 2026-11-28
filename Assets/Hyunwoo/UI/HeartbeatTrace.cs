using UnityEngine;
using UnityEngine.UI;

namespace Hyunwoo.UI
{
    public sealed class HeartbeatTrace : MaskableGraphic
    {
        [SerializeField] private Heartbeat heart;
        // Normalized monitor coordinates, with the main spike exactly at the midpoint.
        private static readonly Vector2[] Wave =
        {
            new(0f, 0f), new(0.42f, 0f), new(0.47f, -0.15f), new(0.5f, 1f),
            new(0.535f, -0.32f), new(0.58f, 0f), new(1f, 0f)
        };
        private float _amplitude;

        private void LateUpdate()
        {
            float amplitude = heart != null ? heart.StrongPulse : 0f;
            if (Mathf.Approximately(amplitude, _amplitude)) return;
            _amplitude = amplitude;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = rectTransform.rect;
            Vector2 a = Point(rect, Wave[0]);
            for (int i = 1; i < Wave.Length; i++)
            {
                Vector2 b = Point(rect, Wave[i]);
                Vector2 normal = Vector2.Perpendicular(b - a).normalized;
                Stroke(mesh, a, b, normal * 3.3f, 0.18f);
                Stroke(mesh, a, b, normal * 1.2f, 1f);
                a = b;
            }
        }

        private Vector2 Point(Rect rect, Vector2 sample) => new Vector2(
            rect.xMin + rect.width * sample.x, rect.center.y + sample.y * _amplitude * rect.height * 0.46f);

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
