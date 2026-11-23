using UnityEngine;

namespace Hyunwoo.Raycasting2D.Scripts
{
    public readonly struct RaycastLighting2D
    {
        private readonly float _strength;
        private readonly float _startDistance;
        private readonly float _endDistance;
        private readonly float _sideBrightness;

        public bool DistanceDarknessEnabled => _strength > 0f;

        public RaycastLighting2D(float strength, float startDistance, float endDistance, float sideBrightness)
        {
            _strength = strength;
            _startDistance = startDistance;
            _endDistance = endDistance;
            _sideBrightness = sideBrightness;
        }

        public float DistanceShade(float depth)
        {
            if (!DistanceDarknessEnabled) return 1f;

            float start = Mathf.Max(0f, _startDistance) / _strength;
            float end = Mathf.Max(start + 0.01f, _endDistance) / _strength;
            float t = Mathf.InverseLerp(start, end, depth);

            float smoothStep = 1f - t * t * (3f - 2f * t);
            return smoothStep;
        }

        public float WallShade(Vector2 normal, float depth)
        {
            bool isSide = Mathf.Abs(normal.x) > 0.5f;
            float directionShade = isSide ? _sideBrightness : 1f;

            return directionShade * DistanceShade(depth);
        }

        public static Color32 Shade(Color32 color, float brightness)
        {
            return new Color32(
                (byte)(color.r * brightness),
                (byte)(color.g * brightness),
                (byte)(color.b * brightness),
                255
            );
        }
    }
}
