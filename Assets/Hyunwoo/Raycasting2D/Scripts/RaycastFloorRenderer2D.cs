using UnityEngine;

namespace Hyunwoo.Raycasting2D
{
    // 화면의 각 픽셀을 바닥·천장의 맵 좌표로 되돌려 텍스처를 읽는다.
    internal static class RaycastFloorRenderer2D
    {
        public static void Draw(RaycastRenderResources resources, RaycastView2D camera, RaycastLighting2D lighting,
            Texture2D floorTexture, Texture2D ceilingTexture, float surfaceTileSize, Color32 floorColor, Color32 ceilingColor)
        {
            // 수평선과 렌더 거리 밖에도 밝은 배경이 새지 않도록 먼저 채운다.
            Color32 backgroundFloor = lighting.DistanceDarknessEnabled ? (Color32)Color.black : floorColor;
            Color32 backgroundCeiling = lighting.DistanceDarknessEnabled ? (Color32)Color.black : ceilingColor;
            resources.Clear(backgroundFloor, backgroundCeiling);

            Color32[] floorPixels = resources.GetTexturePixels(floorTexture);
            Color32[] ceilingPixels = resources.GetTexturePixels(ceilingTexture);
            float tileSize = Mathf.Max(0.01f, surfaceTileSize);
            Vector2 leftRay = camera.Forward - camera.Right * camera.HalfPlaneWidth;
            Vector2 rayStep = camera.Right * (2f * camera.HalfPlaneWidth / camera.Width);

            for (int y = 0; y < camera.Height; y++)
            {
                float fromHorizon = y + 0.5f - camera.Height * 0.5f;
                // 정확한 수평선은 바닥·천장과 만나지 않으므로 0으로 나누지 않는다.
                if (Mathf.Abs(fromHorizon) < 0.0001f) continue;
                float depth = camera.WallHeight * 0.5f * camera.PixelLength / Mathf.Abs(fromHorizon);
                Texture2D source = fromHorizon < 0f ? floorTexture : ceilingTexture;
                Color32[] colors = fromHorizon < 0f ? floorPixels : ceilingPixels;
                Vector2 ray = leftRay + rayStep * 0.5f;
                float brightness = lighting.DistanceShade(depth);
                Color32 fallback = fromHorizon < 0f ? floorColor : ceilingColor;

                for (int x = 0; x < camera.Width; x++, ray += rayStep)
                {
                    if (depth * depth * ray.sqrMagnitude > camera.MaxDistance * camera.MaxDistance) continue;
                    int index = y * camera.Width + x;
                    resources.Depths[index] = depth;
                    if (colors == null)
                    {
                        resources.Pixels[index] = RaycastLighting2D.Shade(fallback, brightness);
                        continue;
                    }
                    Vector2 point = (camera.Origin + ray * depth) / tileSize;
                    int textureX = Mathf.Min((int)(Mathf.Repeat(point.x, 1f) * source.width), source.width - 1);
                    int textureY = Mathf.Min((int)(Mathf.Repeat(point.y, 1f) * source.height), source.height - 1);
                    resources.Pixels[index] = RaycastLighting2D.Shade(colors[textureY * source.width + textureX], brightness);
                }
            }
        }
    }
}
