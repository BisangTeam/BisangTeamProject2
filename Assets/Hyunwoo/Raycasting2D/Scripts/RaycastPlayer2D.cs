using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hyunwoo.Raycasting2D
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public class RaycastPlayer2D : MonoBehaviour
    {
        [SerializeField] private LayerMask wallMask;
        [SerializeField, Min(0f)] private float moveSpeed = 3f;
        [SerializeField, Min(0f)] private float turnSpeed = 120f;

        public Vector2 MoveDir { get; set; }

        private const float Skin = 0.02f;
        private readonly RaycastHit2D[] _hits = new RaycastHit2D[16];
        private Rigidbody2D _body;
        private CircleCollider2D _circle;
        private ContactFilter2D _wallFilter;
        private Vector2 _input;
        private float _turnInput;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _circle = GetComponent<CircleCollider2D>();
            _body.bodyType = RigidbodyType2D.Kinematic;
            _body.gravityScale = 0f;
            _wallFilter.SetLayerMask(wallMask);
            _wallFilter.useTriggers = false;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            _input = Vector2.zero;
            _turnInput = 0f;
            if (keyboard == null)
                return;
           
            _input += MoveDir;

            // 2D에서는 +Z 회전이 왼쪽 회전이다. 플레이어의 위쪽이 정면이다.
            if (keyboard.qKey.isPressed || keyboard.leftArrowKey.isPressed) _turnInput += 1f;
            if (keyboard.eKey.isPressed || keyboard.rightArrowKey.isPressed) _turnInput -= 1f;
        }
        
        private void FixedUpdate()
        {
            _body.rotation += _turnInput * turnSpeed * Time.fixedDeltaTime;
            float angle = _body.rotation * Mathf.Deg2Rad;
            Vector2 forward = new Vector2(-Mathf.Sin(angle), Mathf.Cos(angle));
            Vector2 right = new Vector2(forward.y, -forward.x);
            Move((forward * _input.y + right * _input.x) * (moveSpeed * Time.fixedDeltaTime));
        }

        public void Move(Vector2 displacement)
        {
            Vector2 position = _body.position;
            Vector2 offset = transform.TransformVector(_circle.offset);
            float radius = _circle.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));

            // 이동할 거리 전체를 원으로 검사하므로 빠르게 이동해도 벽을 뚫지 않는다.
            for (int step = 0; step < 3 && displacement.sqrMagnitude > 0.000001f; step++)
            {
                float distance = displacement.magnitude;
                Vector2 direction = displacement / distance;
                int count = Physics2D.CircleCast(position + offset, radius, direction,
                    _wallFilter, _hits, distance + Skin);

                float allowedDistance = distance;
                Vector2 wallNormal = Vector2.zero;
                for (int i = 0; i < count; i++)
                {
                    // 벽과 나란히 가거나 멀어지는 이동은 막지 않는다.
                    if (Vector2.Dot(direction, _hits[i].normal) >= -0.0001f)
                        continue;

                    float beforeWall = Mathf.Max(0f, _hits[i].distance - Skin);
                    if (beforeWall < allowedDistance)
                    {
                        allowedDistance = beforeWall;
                        wallNormal = _hits[i].normal;
                    }
                }

                Vector2 movement = direction * allowedDistance;
                position += movement;
                displacement -= movement;
                if (wallNormal == Vector2.zero)
                    break;

                // 벽을 향하는 성분만 빼면 남은 이동은 벽을 따라 미끄러진다.
                displacement -= wallNormal * Mathf.Min(0f, Vector2.Dot(displacement, wallNormal));
            }

            _body.position = position;
        }

        private void OnDisable()
        {
            _input = Vector2.zero;
            _turnInput = 0f;
        }
    }
}
