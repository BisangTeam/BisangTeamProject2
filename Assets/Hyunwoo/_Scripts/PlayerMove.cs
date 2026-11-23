using UnityEngine;

namespace Hyunwoo.Raycasting2D.Scripts
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMove : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float moveSpeed = 3f;
        [SerializeField, Min(0f)] private float turnSpeed = 120f;

        public Vector2 MoveDir { get; set; }
        public float TurnDir { get; set; }

        private Rigidbody2D _rb;
        
        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        private void FixedUpdate()
        {
            _rb.rotation += -TurnDir * turnSpeed * Time.fixedDeltaTime;

            Vector2 direction = _rb.GetRelativeVector(MoveDir);

            _rb.linearVelocity = direction * moveSpeed;
        }
        
        private void OnDisable()
        {
            _rb.linearVelocity = Vector2.zero;
        }
    }
}
