using System.Collections.Generic;
using UnityEngine;

namespace Hyunwoo.Raycasting2D.Scripts
{
    [RequireComponent(typeof(BoxCollider2D), typeof(RaycastWallSurface2D))]
    public class RaycastDoor2D : MonoBehaviour
    {
        [SerializeField] private Vector2 slideOffset = new Vector2(1.05f, 0f);
        [SerializeField, Min(0.01f)] private float openSpeed = 1.5f;
        [SerializeField] private bool startOpen;
        [SerializeField] private bool canPeek;
        [SerializeField] private bool canToggle = true;
        
        private bool permanentlyLocked;
        public bool CanPeek => canPeek;
        public bool IsClosed => !IsMoving && _openAmount <= 0.0001f;

        private static readonly List<RaycastDoor2D> ActiveDoors = new();
        private readonly RaycastHit2D[] _castHits = new RaycastHit2D[32];
        private readonly Collider2D[] _overlaps = new Collider2D[32];
        private BoxCollider2D _box;
        private Vector3 _closedPosition;
        private Vector3 _slide;
        private ContactFilter2D _actorFilter;
        private bool _initialized;
        private bool _targetOpen;
        private float _openAmount;

        public static IReadOnlyList<RaycastDoor2D> Active => ActiveDoors;
        public bool IsOpen => _openAmount >= 0.9999f;
        public bool IsMoving => !Mathf.Approximately(_openAmount, _targetOpen ? 1f : 0f);
        public float OpenAmount => _openAmount;

        // 문이 벽 안으로 들어가도 원래 입구를 바라보며 다시 닫을 수 있다.
        public Vector2 InteractionPosition
        {
            get
            {
                Initialize();
                Vector3 closedWorld = transform.parent != null
                    ? transform.parent.TransformPoint(_closedPosition)
                    : _closedPosition;
                return closedWorld + transform.TransformVector(_box.offset);
            }
        }

        private void OnEnable()
        {
            if (!ActiveDoors.Contains(this)) ActiveDoors.Add(this);
        }

        private void OnDisable()
        {
            ActiveDoors.Remove(this);
        }

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            _box = GetComponent<BoxCollider2D>();
            _closedPosition = transform.localPosition;
            // 회전된 문도 자기 로컬 축을 따라 미끄러지도록 한다.
            _slide = transform.localRotation * new Vector3(slideOffset.x, slideOffset.y, 0f);
            _actorFilter = new ContactFilter2D { useTriggers = false, useLayerMask = false };
            _targetOpen = startOpen;
            _openAmount = startOpen ? 1f : 0f;
            transform.localPosition = _closedPosition + _slide * _openAmount;
        }

        private void Update()
        {
            Step(Time.deltaTime);
        }

        public void Toggle()
        {
            Initialize();

            if (permanentlyLocked || !canToggle)
                return;

            _targetOpen = !_targetOpen;
        }

        public void SetOpen(bool open)
        {
            Initialize();

            if (permanentlyLocked && open)
                return;

            _targetOpen = open;
        }
        
        public void CloseAndLock()
        {
            Initialize();
            permanentlyLocked = true;
            _targetOpen = false;
        }

        // 시간을 인자로 받아 에디터 검증에서도 같은 이동을 확인할 수 있다.
        public void Step(float deltaTime)
        {
            Initialize();
            if (deltaTime <= 0f || !IsMoving) return;

            float distance = Mathf.Max(slideOffset.magnitude, 0.0001f);
            float next = Mathf.MoveTowards(_openAmount, _targetOpen ? 1f : 0f,
                openSpeed * deltaTime / distance);
            Vector3 nextLocal = _closedPosition + _slide * next;
            Vector2 nextWorld = transform.parent != null
                ? transform.parent.TransformPoint(nextLocal)
                : nextLocal;
            Vector2 displacement = nextWorld - (Vector2)transform.position;

            // 닫히는 경로에 플레이어가 있으면 멈춘다. 벽 안쪽으로 수납되는 동작은 허용한다.
            if (!_targetOpen && ClosingHitsActor(displacement)) return;

            _openAmount = next;
            transform.localPosition = nextLocal;
        }

        private bool ClosingHitsActor(Vector2 displacement)
        {
            Physics2D.SyncTransforms();
            int count = _box.Overlap(_actorFilter, _overlaps);
            for (int i = 0; i < count; i++)
                if (IsActor(_overlaps[i])) return true;

            float distance = displacement.magnitude;
            if (distance <= 0.00001f) return false;
            count = _box.Cast(displacement / distance, _actorFilter, _castHits, distance + 0.01f);
            for (int i = 0; i < count; i++)
                if (IsActor(_castHits[i].collider)) return true;
            return false;
        }

        private bool IsActor(Collider2D other)
        {
            if (other == null || other.isTrigger || other.transform.IsChildOf(transform)) return false;
            Rigidbody2D body = other.attachedRigidbody;
            // 현재 플레이어는 Kinematic Rigidbody2D이므로 함께 검사한다.
            return body != null && body != _box.attachedRigidbody && body.bodyType != RigidbodyType2D.Static;
        }
    }
}
