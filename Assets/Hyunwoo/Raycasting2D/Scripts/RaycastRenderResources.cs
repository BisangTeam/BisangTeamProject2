using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Hyunwoo.Raycasting2D
{
    // 화면 Texture2D, 픽셀·깊이 배열과 원본 이미지 캐시의 생성·재사용·해제.
    internal sealed class RaycastRenderResources
    {
        private readonly Dictionary<Texture2D, Color32[]> _texturePixels = new();

        public Texture2D Texture { get; private set; }
        public Color32[] Pixels { get; private set; }
        public float[] Depths { get; private set; }
        public int Height { get; private set; }

        public bool Resize(RawImage view, int width)
        {
            Rect rect = view.rectTransform.rect;
            if (rect.width <= 0f || rect.height <= 0f) return false;
            
            int height = Mathf.Max(1, Mathf.RoundToInt(width / rect.width * rect.height));

            if (Height != height || Texture == null)
            {
                Dispose(view);
                Height = height;
                Pixels = new Color32[width * height];
                Depths = new float[Pixels.Length];
                Texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                Texture.filterMode = FilterMode.Point;
                Texture.wrapMode = TextureWrapMode.Clamp;
                view.texture = Texture;
            }
            return true;
        }

        public void Clear(Color32 floor, Color32 ceiling)
        {
            Array.Fill(Depths, float.MaxValue);
            int width = Texture.width;
            for (int y = 0; y < Height; y++)
            {
                Color32 color = y < Height / 2 ? floor : ceiling;
                for (int x = 0; x < width; x++) Pixels[y * width + x] = color;
            }
        }

        public Color32[] GetTexturePixels(Texture2D source)
        {
            if (source == null) return null;
            if (_texturePixels.TryGetValue(source, out Color32[] colors)) return colors;

            if (source.isReadable) colors = source.GetPixels32();
            else Debug.LogWarning($"{source.name}: 텍스처 Import Settings에서 Read/Write를 켜 주세요.", source);
            _texturePixels[source] = colors;
            return colors;
        }

        public void ClearTextureCache()
        {
            _texturePixels.Clear();
        }

        public void Present()
        {
            Texture.SetPixels32(Pixels);
            Texture.Apply(false);
        }

        public void Dispose(RawImage view)
        {
            if (view != null && view.texture == Texture) view.texture = null;
            if (Texture == null) return;
            if (Application.isPlaying) Object.Destroy(Texture);
            else Object.DestroyImmediate(Texture);
            Texture = null;
        }
    }
}
