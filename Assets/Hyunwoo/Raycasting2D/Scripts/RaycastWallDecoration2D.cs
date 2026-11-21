using System.Collections.Generic;
using UnityEngine;

namespace Hyunwoo.Raycasting2D
{
    // 창문/액자처럼 방향이 고정된 이미지. XY는 가로 중앙, Elevation은 하단 높이.
    // Width/Height는 월드 단위이며 Transform의 Scale과 독립적이다.
    public class RaycastWallDecoration2D : MonoBehaviour
    {
        [SerializeField] private Texture2D image;
        [SerializeField, Min(0.01f)] private float width = 0.8f;
        [SerializeField, Min(0.01f)] private float height = 0.5f;
        [SerializeField, Min(0f)] private float elevation = 0.3f;
        [SerializeField] private bool twoSided = true;

        private static readonly List<RaycastWallDecoration2D> ActiveDecorations = new();

        public static IReadOnlyList<RaycastWallDecoration2D> Active => ActiveDecorations;
        public Texture2D Image => image;
        public float Width => width;
        public float Height => height;
        public float Elevation => elevation;
        public bool TwoSided => twoSided;

        private void OnEnable()
        {
            if (!ActiveDecorations.Contains(this)) ActiveDecorations.Add(this);
        }

        private void OnDisable()
        {
            ActiveDecorations.Remove(this);
        }

        private void OnDrawGizmosSelected()
        {
            // Scene의 탑뷰에서 이미지가 붙는 방향과 앞면을 확인한다.
            Gizmos.color = Color.cyan;
            Vector3 half = transform.right * (width * 0.5f);
            Gizmos.DrawLine(transform.position - half, transform.position + half);
            Gizmos.DrawRay(transform.position, transform.up * 0.3f);
        }
    }
}
