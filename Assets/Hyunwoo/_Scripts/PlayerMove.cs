using UnityEngine;

namespace Hyunwoo.Raycasting2D.Scripts
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMove : MonoBehaviour
    {
        [field:SerializeField, Min(0f)] public float MoveSpeed = 3f;
        [SerializeField, Min(0f)] private float turnSpeed = 120f;
        [SerializeField] private WalkFeedback Walk;
        private Vector2 _lastPosition;
        private bool _isMoving;

        public Vector2 MoveDir { get; set; }
        public float TurnDir { get; set; }
        public bool CanMove { get; set; } = true;

        private Rigidbody2D _rb;
        
        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _lastPosition = _rb.position;
        }

        private void FixedUpdate()
        {
            if (!CanMove)
            {
                _rb.linearVelocity = Vector2.zero;
                return;
            }
            
            _rb.rotation += -TurnDir * turnSpeed * Time.fixedDeltaTime;

            Vector2 direction = _rb.GetRelativeVector(MoveDir);

            bool isMoving = MoveDir != Vector2.zero &&
                            Vector2.Distance(_rb.position, _lastPosition) > 0.001f;

            _lastPosition = _rb.position;

            _rb.linearVelocity = direction * MoveSpeed;
            
            if (isMoving)
            {
                Walk.PlayStep();
            }
            else if (_isMoving)
            {
                Walk.StopStep();
            }

            _isMoving = isMoving;
        }
        
        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.CompareTag("Wall"))
            {
                Walk.PlayBump();
            }
        }
        
        private void OnDisable()
        {
            _rb.linearVelocity = Vector2.zero;
        }
    }
}
