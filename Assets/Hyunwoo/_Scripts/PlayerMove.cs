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

        public Vector2 MoveDir { get; set; }
        public float TurnDir { get; set; }
        public bool CanMove { get; set; } = true;
        public bool SprintHeld { get; set; }
        public float SprintSpeed { get; set; }
        public bool IsSprinting { get; private set; }

        private Rigidbody2D _rb;
        private bool _wasWalking;
        private bool _lastStepWasSprint;
        
        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _lastPosition = _rb.position;
        }

        private void FixedUpdate()
        {
            bool acceptsInput = CanMove && !RaycastPaperView.IsReading;
            if (acceptsInput) _rb.rotation += -TurnDir * turnSpeed * Time.fixedDeltaTime;
            Vector2 input = acceptsInput ? MoveDir : Vector2.zero;
            bool moved = Vector2.Distance(_rb.position, _lastPosition) > 0.001f;
            IsSprinting = _lastStepWasSprint && moved;
            _lastPosition = _rb.position;

            bool sprint = input != Vector2.zero && SprintHeld && SprintSpeed > 0f;
            _rb.linearVelocity = _rb.GetRelativeVector(input) * (sprint ? SprintSpeed : MoveSpeed);
            _lastStepWasSprint = sprint;

            bool walking = input != Vector2.zero && moved;
            if (walking) Walk?.PlayStep();
            else if (_wasWalking) Walk?.StopStep();
            _wasWalking = walking;
        }
        
        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.gameObject.CompareTag("Wall"))
            {
                Walk?.PlayBump();
            }
        }

        private void OnDisable()
        {
            _rb.linearVelocity = Vector2.zero;
            if (_wasWalking && Walk != null && Walk.isActiveAndEnabled) Walk.StopStep();
            _wasWalking = false;
            _lastStepWasSprint = false;
            IsSprinting = false;
        }
    }
}
