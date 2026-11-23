using System.Collections.Generic;
using UnityEngine;

namespace Hyunwoo.Raycasting2D.Scripts
{
    public class RaycastSprite2D : MonoBehaviour
    {
        [SerializeField] private Texture2D image;
        [SerializeField, Min(0.01f)] private float height = 1f;
        [SerializeField, Min(0f)] private float elevation = 0f;

        private static readonly List<RaycastSprite2D> ActiveSprites = new();

        public static IReadOnlyList<RaycastSprite2D> Active => ActiveSprites;
        public Texture2D Image => image;
        public float Height => height;
        public float Elevation => elevation;

        private void OnEnable()
        {
            if (!ActiveSprites.Contains(this))
                ActiveSprites.Add(this);
        }

        private void OnDisable()
        {
            ActiveSprites.Remove(this);
        }
    }
}
