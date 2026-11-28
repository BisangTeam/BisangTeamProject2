using UnityEngine;
using UnityEngine.UI;

namespace Hyunwoo.BloodPack
{
    public sealed class BloodPackLiquid : MaskableGraphic
    {
        [Header("Blood")]
        [SerializeField] private Color deepColor = new Color32(57, 8, 25, 255);
        [SerializeField] private Color litColor = new Color32(185, 39, 52, 255);
        [SerializeField] private Color rimColor = new Color32(232, 107, 94, 255);

        [Header("Surface motion")]
        [SerializeField, Range(0.45f, 0.86f)] private float fillLevel = 0.79f;
        [SerializeField, Range(0f, 0.25f)] private float sloshHeight = 0.19f;
        [SerializeField, Range(4f, 16f)] private float damping = 9f;

        private const int Columns = 24;
        private const int BodySegments = 7;
        private Vector2 _previousBagPosition;
        private Vector2 _slosh;
        private Vector2 _sloshVelocity;

        public void ResetMotion(Vector2 bagPosition)
        {
            _previousBagPosition = bagPosition;
            _slosh = _sloshVelocity = Vector2.zero;
            SetVerticesDirty();
        }

        public void FollowBag(Vector2 bagPosition, float deltaTime)
        {
            Vector2 travel = bagPosition - _previousBagPosition;
            _previousBagPosition = bagPosition;
            if (deltaTime <= 0f) return;
            if (travel == Vector2.zero && _slosh == Vector2.zero && _sloshVelocity == Vector2.zero) return;

            // Measure movement in bag widths/heights so the response scales with the UI.
            Rect rect = rectTransform.rect;
            Vector2 speed = new Vector2(travel.x / Mathf.Max(1f, rect.width),
                travel.y / Mathf.Max(1f, rect.height)) / deltaTime;
            Vector2 target = Vector2.ClampMagnitude(-speed * 0.20f, 1f);

            // Small spring steps keep the rebound stable even on a long frame.
            float elapsed = Mathf.Min(deltaTime, 0.15f);
            int steps = Mathf.CeilToInt(elapsed * 90f);
            float step = elapsed / steps;
            for (int i = 0; i < steps; i++)
            {
                _sloshVelocity += ((target - _slosh) * 155f - _sloshVelocity * damping) * step;
                _slosh += _sloshVelocity * step;
            }
            if (travel == Vector2.zero && _slosh.sqrMagnitude < 0.00001f && _sloshVelocity.sqrMagnitude < 0.0004f)
                _slosh = _sloshVelocity = Vector2.zero;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = GetPixelAdjustedRect();
            if (rect.width <= 0f || rect.height <= 0f) return;

            const int rows = BodySegments + 2; // Body samples plus the thin reflective surface.
            for (int col = 0; col <= Columns; col++)
            {
                float u = (float)col / Columns;
                float side = 2f * u - 1f;
                float corner = Mathf.Pow(Mathf.Abs(side), 8f);
                float bottom = 0.15f * corner;
                // Both wave shapes displace liquid from one part of the pouch to another.
                float wave = _slosh.x * side - 0.65f * _slosh.y * Mathf.Cos(2f * Mathf.PI * u);
                float surface = Mathf.Clamp(fillLevel + 0.018f * corner + sloshHeight * wave,
                    bottom + 0.08f, 1f - 0.10f * corner);
                float x = rect.xMin + rect.width * u;
                float rimThickness = 0.028f + 0.012f * (1f - side * side);

                for (int row = 0; row <= BodySegments; row++)
                {
                    float depth = (float)row / BodySegments;
                    float height = Mathf.Lerp(bottom, surface - rimThickness, depth);
                    mesh.AddVert(new Vector2(x, rect.yMin + rect.height * height),
                        BodyColor(u, depth) * color, Vector2.zero);
                }
                float reflection = Mathf.Clamp01(1f - Mathf.Abs(u - 0.32f) * 1.6f);
                Color rim = Color.Lerp(litColor, rimColor, 0.32f + 0.68f * reflection);
                mesh.AddVert(new Vector2(x, rect.yMin + rect.height * surface), rim * color, Vector2.zero);

                if (col == 0) continue;
                int previous = (col - 1) * rows;
                for (int row = 0; row < rows - 1; row++)
                {
                    int left = previous + row;
                    int right = left + rows;
                    mesh.AddTriangle(left, left + 1, right + 1);
                    mesh.AddTriangle(left, right + 1, right);
                }
            }
        }

        private Color BodyColor(float u, float depth)
        {
            float litCenter = 0.30f + 0.12f * depth + 0.10f * _slosh.x * depth;
            float glow = Mathf.Clamp01(1f - Mathf.Abs(u - litCenter) * 2.4f);
            float fold = Mathf.Sin((2.4f * u + 1.3f * depth + 0.18f * _slosh.y) * Mathf.PI);
            float light = 0.04f + 0.61f * depth + 0.32f * glow * glow
                + 0.09f * fold * Mathf.Sin(depth * Mathf.PI);
            return Color.Lerp(deepColor, litColor, light);
        }
    }
}
