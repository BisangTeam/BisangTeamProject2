using UnityEngine;

namespace Hyunwoo.Raycasting2D
{
    public readonly struct RaycastView2D
    {
        public readonly int Width;
        public readonly int Height;
        public readonly Vector2 Origin;
        public readonly Vector2 Forward;
        public readonly Vector2 Right;
        public readonly float HalfPlaneWidth;
        public readonly float PixelLength;
        public readonly float WallHeight;
        public readonly float MaxDistance;

        public RaycastView2D(Transform player, int width, int height, float fieldOfView,
            float wallHeight, float maxDistance)
        {
            Width = width;
            Height = height;
            Origin = player.position;
            Forward = player.up;
            Right = player.right;
            HalfPlaneWidth = Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad);
            PixelLength = width / (2f * HalfPlaneWidth);
            WallHeight = wallHeight;
            MaxDistance = maxDistance;
        }

        public Vector2 GetDirection(int column)
        {
            float screenX = ((column + 0.5f) / Width * 2f - 1f) * HalfPlaneWidth;
            return (Forward + Right * screenX).normalized;
        }
    }
}
