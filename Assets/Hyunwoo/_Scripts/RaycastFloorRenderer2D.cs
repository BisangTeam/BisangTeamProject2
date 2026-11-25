using UnityEngine;

namespace Hyunwoo.Raycasting2D.Scripts
{
    public static class RaycastFloorRenderer2D
    {
        public static void Draw(RaycastRenderResources resources, RaycastView2D view, RaycastLighting2D lighting,
            Texture2D floorTexture, Texture2D ceilingTexture, float surfaceTileSize)
        {
            resources.Clear();

            Color32[] floorPixels = resources.GetTexturePixels(floorTexture);
            Color32[] ceilingPixels = resources.GetTexturePixels(ceilingTexture);

            float tileSize = Mathf.Max(0.01f, surfaceTileSize);

            Vector2 leftRay = view.Forward - view.Right * view.HalfPlaneWidth;
            Vector2 rayStep = view.Right * (2f * view.HalfPlaneWidth / view.Width);

            for (int y = 0; y < view.Height; y++)
            {
                if (!TryCalculateDepth(view, y, out float depth, out bool isFloor))
                    continue;

                Texture2D source = isFloor ? floorTexture : ceilingTexture;
                Color32[] colors = isFloor ? floorPixels : ceilingPixels;

                Vector2 ray = leftRay + rayStep * 0.5f;
                float brightness = lighting.DistanceShade(depth);

                float depthSquared = depth * depth;
                float maxDistanceSquared = view.MaxDistance * view.MaxDistance;

                for (int x = 0; x < view.Width; x++, ray += rayStep)
                {
                    if (depthSquared * ray.sqrMagnitude > maxDistanceSquared) 
                        continue;

                    DrawPixel(resources, view, lighting, source, colors, ray, tileSize, depth, brightness, x, y);
                }
            }
        }   

        private static bool TryCalculateDepth(RaycastView2D view, int y, out float depth, out bool isFloor)
        {
            float fromHorizon = y + 0.5f - view.Horizon;

            if (Mathf.Abs(fromHorizon) < 0.0001f)
            {
                depth = 0;
                isFloor = false;
                return false;
            }

            isFloor = fromHorizon < 0f;
            float distanceToPlane = isFloor ? view.EyeHeight : view.WallHeight - view.EyeHeight;
            depth = distanceToPlane * view.PixelLength / Mathf.Abs(fromHorizon);

            return true;
        }

        private static void DrawPixel(RaycastRenderResources resources, RaycastView2D view, RaycastLighting2D lighting, Texture2D source, Color32[] colors,
            Vector2 ray, float tileSize, float depth, float brightness, int x, int y)
        {
            int index = y * view.Width + x;
            resources.Depths[index] = depth;

            Vector2 worldPoint = view.Origin + ray * depth;

            Color32 color = colors != null ? GetTextureColor(worldPoint, tileSize, source, colors) : new Color32(0, 0, 0, 255);

            resources.Pixels[index] = RaycastLighting2D.Shade(color, brightness * lighting.PlaceBrightness(worldPoint));
        }

        private static Color32 GetTextureColor(Vector2 worldPoint, float tileSize, Texture2D source, Color32[] colors)
        {
            Vector2 texturePoint = worldPoint / tileSize;

            float u = Mathf.Repeat(texturePoint.x, 1f);
            float v = Mathf.Repeat(texturePoint.y, 1f);

            int textureX = Mathf.Min((int)(u * source.width), source.width - 1);
            int textureY = Mathf.Min((int)(v * source.height), source.height - 1);

            return colors[textureY * source.width + textureX];
        }
    }
}
