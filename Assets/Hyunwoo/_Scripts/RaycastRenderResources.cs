using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Hyunwoo.Raycasting2D.Scripts
{
    public sealed class RaycastRenderResources
    {
        private readonly Dictionary<Texture2D, Color32[]> _texturePixels = new();

        public Texture2D Texture { get; private set; }
        public Color32[] Pixels { get; private set; }
        public float[] Depths { get; private set; }
        public int Height { get; private set; }

        public bool Resize(RawImage rawImage, int width)
        {
            Rect rect = rawImage.rectTransform.rect;
            if (rect.width <= 0f || rect.height <= 0f) return false;
            
            int height = Mathf.Max(1, Mathf.RoundToInt(width * rect.height / rect.width));

            if (Height != height || Texture == null)
            {
                Dispose(rawImage);
                Height = height;

                Pixels = new Color32[width * height];
                Depths = new float[Pixels.Length];

                Texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };

                rawImage.texture = Texture;
            }
            return true;
        }

        public void Clear()
        {
            Array.Fill(Pixels, new Color32(0, 0, 0, 255));
            Array.Fill(Depths, float.MaxValue);
        }

        public Color32[] GetTexturePixels(Texture2D source)
        {
            if (source == null) return null;

            if (_texturePixels.TryGetValue(source, out Color32[] colors)) return colors;

            if (source.isReadable)
            {
                colors = source.GetPixels32();
            }
            else
            {
                Debug.LogWarning($"{source.name}: 텍스처 Import Settings에서 Read/Write를 켜야만 해", source);
                return null;
            }

            _texturePixels[source] = colors;
            return colors;
        }

        public void Present()
        {
            Texture.SetPixels32(Pixels);
            Texture.Apply(false);
        }

        public void Dispose(RawImage rawImage)
        {
            if (rawImage != null && rawImage.texture == Texture) 
                rawImage.texture = null;

            if (Texture == null) return;
            
            Object.Destroy(Texture);
            Texture = null;
        }
    }
}
