using System.Collections.Generic;
using UnityEngine;

namespace Hyunwoo.Raycasting2D.Scripts
{
    public class RaycastWallDecoration2D : MonoBehaviour
    {
        [SerializeField] private Texture2D image;
        [SerializeField, Min(0.01f)] private float width = 0.8f;
        [SerializeField, Min(0.01f)] private float height = 0.5f;
        [SerializeField, Min(0f)] private float elevation = 0.3f;
        [SerializeField] private bool twoSided = true;
        [Header("Sway")]
        [SerializeField, Min(0f)] private float swayAmplitude;
        [SerializeField, Min(0f)] private float swayCyclesPerSecond = 0.6f;

        private static readonly List<RaycastWallDecoration2D> ActiveDecorations = new();

        public static IReadOnlyList<RaycastWallDecoration2D> Active => ActiveDecorations;
        public Texture2D Image => image;
        public float Width => width;
        public float Height => height;
        public float Elevation => elevation;
        public bool TwoSided => twoSided;
        public float SwayAmplitude => swayAmplitude;
        internal GameObject SupportingWall { get; set; }

        internal bool MatchesSurface(RaycastHit2D hit)
        {
            if (hit.collider == null || hit.collider.gameObject != SupportingWall) return false;
            Vector2 normal = transform.up;
            float planeDistance = Vector2.Dot(hit.point - (Vector2)transform.position, normal);
            return Vector2.Dot(hit.normal, normal) > 0.999f && Mathf.Abs(planeDistance) < 0.001f;
        }

        public float BottomOffset(float time) =>
            swayAmplitude * Mathf.Sin(time * swayCyclesPerSecond * 2f * Mathf.PI);

        private void OnEnable()
        {
            if (!ActiveDecorations.Contains(this)) 
                ActiveDecorations.Add(this);
        }

        private void OnDisable()
        {
            ActiveDecorations.Remove(this);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Vector3 half = transform.right * (width * 0.5f);
            Gizmos.DrawLine(transform.position - half, transform.position + half);
            Gizmos.DrawRay(transform.position, transform.up * 0.3f);
        }
    }
}
