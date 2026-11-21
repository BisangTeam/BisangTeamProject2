using System.Collections.Generic;
using UnityEngine;

namespace Hyunwoo.Raycasting2D
{
    // 일반 사물과 벽 장식을 같은 깊이 순서로 그린다.
    internal sealed class RaycastObjectRenderer2D
    {
        private struct VisibleSprite
        {
            public RaycastSprite2D sprite;
            public RaycastWallDecoration2D decoration;
            public float side;
            public float depth;
        }

        private readonly List<VisibleSprite> _visibleSprites = new();

        public void Draw(RaycastRenderResources resources, RaycastView2D camera, RaycastLighting2D lighting)
        {
            _visibleSprites.Clear();
            foreach (RaycastSprite2D sprite in RaycastSprite2D.Active)
            {
                if (sprite == null || !sprite.isActiveAndEnabled || sprite.Image == null) continue;
                Vector2 offset = (Vector2)sprite.transform.position - camera.Origin;
                float depth = Vector2.Dot(offset, camera.Forward);
                if (depth <= 0.01f || offset.sqrMagnitude > camera.MaxDistance * camera.MaxDistance) continue;
                _visibleSprites.Add(new VisibleSprite
                {
                    sprite = sprite, side = Vector2.Dot(offset, camera.Right), depth = depth
                });
            }

            foreach (RaycastWallDecoration2D decoration in RaycastWallDecoration2D.Active)
            {
                if (decoration == null || !decoration.isActiveAndEnabled || decoration.Image == null) continue;
                Vector2 offset = (Vector2)decoration.transform.position - camera.Origin;
                float depth = Vector2.Dot(offset, camera.Forward);
                // 중심이 뒤에 있어도 장식의 끝부분은 화면 앞에 있을 수 있다.
                float halfDepth = Mathf.Abs(Vector2.Dot(decoration.transform.right, camera.Forward)) * decoration.Width * 0.5f;
                if (depth + halfDepth <= 0.01f || offset.magnitude - decoration.Width * 0.5f > camera.MaxDistance) continue;
                _visibleSprites.Add(new VisibleSprite { decoration = decoration, depth = depth });
            }

            // 먼 것부터 그리되, 각 픽셀의 깊이로 앞뒤 관계를 다시 확인한다.
            _visibleSprites.Sort((a, b) => b.depth.CompareTo(a.depth));
            foreach (VisibleSprite item in _visibleSprites)
            {
                if (item.decoration != null)
                    DrawWallDecoration(item.decoration, resources, camera, lighting);
                else
                    DrawBillboard(item, resources, camera, lighting);
            }
        }

        private static void DrawBillboard(VisibleSprite item, RaycastRenderResources resources,
            RaycastView2D camera, RaycastLighting2D lighting)
        {
            Texture2D image = item.sprite.Image;
            Color32[] colors = resources.GetTexturePixels(image);
            if (colors == null) return;

            float scale = camera.PixelLength / item.depth;
            float height = Mathf.Max(0.01f, item.sprite.Height) * scale;
            float width = height * image.width / image.height;
            float centerX = camera.Width * 0.5f + item.side * scale;
            float left = centerX - width * 0.5f;
            float bottom = camera.Height * 0.5f + (item.sprite.Elevation - camera.WallHeight * 0.5f) * scale;

            int firstX = Mathf.Clamp(Mathf.CeilToInt(left - 0.5f), 0, camera.Width);
            int endX = Mathf.Clamp(Mathf.CeilToInt(left + width - 0.5f), 0, camera.Width);
            int firstY = Mathf.Clamp(Mathf.CeilToInt(bottom - 0.5f), 0, camera.Height);
            int endY = Mathf.Clamp(Mathf.CeilToInt(bottom + height - 0.5f), 0, camera.Height);
            float shade = lighting.DistanceShade(item.depth);

            // 항상 플레이어를 향하는 사물을 사각형 이미지로 그린다.
            for (int x = firstX; x < endX; x++)
            {
                int textureX = Mathf.Clamp((int)((x + 0.5f - left) / width * image.width), 0, image.width - 1);
                for (int y = firstY; y < endY; y++)
                {
                    int index = y * camera.Width + x;
                    if (item.depth >= resources.Depths[index]) continue;
                    int textureY = Mathf.Clamp((int)((y + 0.5f - bottom) / height * image.height), 0, image.height - 1);
                    DrawPixel(resources, index, colors[textureY * image.width + textureX], shade, item.depth);
                }
            }
        }

        private static void DrawWallDecoration(RaycastWallDecoration2D decoration, RaycastRenderResources resources,
            RaycastView2D camera, RaycastLighting2D lighting)
        {
            Texture2D image = decoration.Image;
            Color32[] colors = resources.GetTexturePixels(image);
            if (colors == null) return;

            Vector2 center = decoration.transform.position;
            Vector2 normal = decoration.transform.up;
            Vector2 tangent = decoration.transform.right;
            float planeDistance = Vector2.Dot(center - camera.Origin, normal);
            float halfPlaneWidth = camera.Width / (2f * camera.PixelLength);
            float width = Mathf.Max(0.01f, decoration.Width);

            for (int x = 0; x < camera.Width; x++)
            {
                float screenX = ((x + 0.5f) / camera.Width * 2f - 1f) * halfPlaneWidth;
                Vector2 direction = (camera.Forward + camera.Right * screenX).normalized;
                float facing = Vector2.Dot(direction, normal);
                if (Mathf.Abs(facing) < 0.00001f || (!decoration.TwoSided && facing >= 0f)) continue;

                // 고정된 장식의 면과 검사선이 만나는 위치로 이미지 좌표를 구한다.
                float distance = planeDistance / facing;
                if (distance <= 0f || distance > camera.MaxDistance) continue;
                Vector2 offset = camera.Origin + direction * distance - center;
                float u = Vector2.Dot(offset, tangent) / width + 0.5f;
                if (u < 0f || u > 1f) continue;
                if (facing > 0f) u = 1f - u;

                float depth = distance * Vector2.Dot(direction, camera.Forward);
                if (depth <= 0.01f) continue;
                float scale = camera.PixelLength / depth;
                float height = Mathf.Max(0.01f, decoration.Height) * scale;
                float bottom = camera.Height * 0.5f + (decoration.Elevation - camera.WallHeight * 0.5f) * scale;
                int first = Mathf.Clamp(Mathf.CeilToInt(bottom - 0.5f), 0, camera.Height);
                int end = Mathf.Clamp(Mathf.CeilToInt(bottom + height - 0.5f), 0, camera.Height);
                int textureX = Mathf.Clamp((int)(u * image.width), 0, image.width - 1);
                float brightness = lighting.WallShade(normal, depth);

                for (int y = first; y < end; y++)
                {
                    int index = y * camera.Width + x;
                    if (depth >= resources.Depths[index]) continue;
                    int textureY = Mathf.Clamp((int)((y + 0.5f - bottom) / height * image.height), 0, image.height - 1);
                    DrawPixel(resources, index, colors[textureY * image.width + textureX], brightness, depth);
                }
            }
        }

        private static void DrawPixel(RaycastRenderResources resources, int index, Color32 color, float brightness, float depth)
        {
            if (color.a == 0) return;
            byte alpha = color.a;
            color = RaycastLighting2D.Shade(color, brightness);
            if (alpha < 255)
            {
                // 반투명 픽셀은 이미 그려진 색과 섞고, 불투명 픽셀만 깊이를 기록한다.
                Color32 behind = resources.Pixels[index];
                color.r = (byte)((color.r * alpha + behind.r * (255 - alpha)) / 255);
                color.g = (byte)((color.g * alpha + behind.g * (255 - alpha)) / 255);
                color.b = (byte)((color.b * alpha + behind.b * (255 - alpha)) / 255);
            }
            resources.Pixels[index] = color;
            if (alpha == 255) resources.Depths[index] = depth;
        }
    }
}
