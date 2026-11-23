using UnityEngine;

namespace Hyunwoo.Raycasting2D.Scripts
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class RaycastWallSurface2D : MonoBehaviour
    {
        [SerializeField] private Texture2D image;

        private BoxCollider2D _box;

        public Texture2D Image => image;

        private void Awake()
        {
            _box = GetComponent<BoxCollider2D>();
        }

        public float GetTextureU(Vector2 hitPoint, Vector2 hitNormal)
        {
            Vector2 point = (Vector2)transform.InverseTransformPoint(hitPoint) - _box.offset;
            //Vector2 point = transform.InverseTransformPoint(hitPoint);
            Vector2 normal = transform.InverseTransformDirection(hitNormal);

            bool isSide = Mathf.Abs(normal.x) > Mathf.Abs(normal.y);
            float size = isSide ? _box.size.y : _box.size.x;
            
            float u = (isSide ? point.y : point.x) / Mathf.Max(size, 0.0001f) + 0.5f;

            if (isSide ? normal.x < 0f : normal.y > 0f)
                u = 1f - u;
            
            return Mathf.Clamp01(u);
        }
    }
}
