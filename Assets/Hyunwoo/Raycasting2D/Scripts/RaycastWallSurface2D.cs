using UnityEngine;

namespace Hyunwoo.Raycasting2D
{
    // 타일맵이 아닌 독립된 벽/문에 붙여 전용 텍스처를 지정한다.
    [RequireComponent(typeof(BoxCollider2D))]
    public class RaycastWallSurface2D : MonoBehaviour
    {
        [SerializeField] private Texture2D image;

        private BoxCollider2D _box;

        public Texture2D Image => image;

        public float GetTextureU(Vector2 hitPoint, Vector2 hitNormal)
        {
            if (_box == null) _box = GetComponent<BoxCollider2D>();

            // 월드 좌표를 문의 로컬 좌표로 되돌리므로 이동/회전/크기가 반영된다.
            Vector2 point = (Vector2)transform.InverseTransformPoint(hitPoint) - _box.offset;
            Vector2 normal = transform.InverseTransformDirection(hitNormal);
            bool side = Mathf.Abs(normal.x) > Mathf.Abs(normal.y);
            float size = side ? _box.size.y : _box.size.x;
            float u = (side ? point.y : point.x) / Mathf.Max(size, 0.0001f) + 0.5f;

            // 반대쪽에서 보더라도 같은 면의 좌우 방향이 일관되게 보인다.
            if (side ? normal.x < 0f : normal.y > 0f) u = 1f - u;
            return Mathf.Clamp01(u);
        }
    }
}
