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
        
        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
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

            _rb.linearVelocity = direction * MoveSpeed;

            if (MoveDir != Vector2.zero)
            {
                Walk.PlayStep();
            }
            else
            {
                Walk.StopStep();
            }
        }
        
        private void OnDisable()
        {
            _rb.linearVelocity = Vector2.zero;
        }
    }
}
