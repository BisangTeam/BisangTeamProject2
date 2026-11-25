using UnityEngine;

namespace Hyunwoo.Raycasting2D.Scripts
{
    public readonly struct DrawStruct
    {
        public readonly float Depth;
        public readonly float Height;
        public readonly float Bottom;
        public readonly int First;
        public readonly int End;

        public DrawStruct(float depth, float height, float bottom, int first, int end)
        {
            Depth = depth;
            Height = height;
            Bottom = bottom;
            First = first;
            End = end;
        }

        public static DrawStruct CalculateDrawStruct(RaycastHit2D hit, RaycastView2D view)
        {
            float depth = Mathf.Max(0.01f, Vector2.Dot(hit.point - view.Origin, view.Forward));

            float height = view.WallHeight * view.PixelLength / depth;
            float bottom = view.Horizon - height * 0.5f;

            int first = Mathf.Clamp(Mathf.CeilToInt(bottom - 0.5f), 0, view.Height);
            int end = Mathf.Clamp(Mathf.CeilToInt(bottom + height - 0.5f), 0, view.Height);

            return new DrawStruct(depth, height, bottom, first, end);
        }
    }
}
