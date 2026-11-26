using UnityEngine;

namespace Hyunwoo.Raycasting2D.Scripts
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMove : MonoBehaviour
    {
        [field:SerializeField, Min(0f)] public float MoveSpeed = 3f;
        [SerializeField, Min(0f)] private float turnSpeed = 120f;
        [SerializeField] private WalkFeedback Walk;

        public Vector2 MoveDir { get; set; }
        public float TurnDir { get; set; }
        public bool CanMove { get; set; } = true;

        private Rigidbody2D _rb;
        private bool _wasWalking;
        
        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        private void FixedUpdate()
        {
            bool acceptsInput = CanMove && !RaycastPaperView.IsReading;
            if (acceptsInput) _rb.rotation += -TurnDir * turnSpeed * Time.fixedDeltaTime;
            Vector2 input = acceptsInput ? MoveDir : Vector2.zero;
            _rb.linearVelocity = _rb.GetRelativeVector(input) * MoveSpeed;

            bool walking = input != Vector2.zero;
            if (walking) Walk?.PlayStep();
            else if (_wasWalking) Walk?.StopStep();
            _wasWalking = walking;
        }
        
        private void OnDisable()
        {
            _rb.linearVelocity = Vector2.zero;
            if (_wasWalking && Walk != null && Walk.isActiveAndEnabled) Walk.StopStep();
            _wasWalking = false;
        }
    }
}
