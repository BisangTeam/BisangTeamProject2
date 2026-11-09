using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Seonho._01.Scripts.Eye
{
    public class Eye : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D _rigidCompo;
        
        [SerializeField] private GameObject[] _afterimage;
        
        Camera _camera;

        [SerializeField] private float _speed;
        [SerializeField] private float _guage;
        
        [SerializeField] private Vector3 _originSize;
        [SerializeField] private Vector2 _moveDir;

        private void Awake()
        {
            _rigidCompo = GetComponent<Rigidbody2D>();
            _camera = Camera.main;
            _originSize = transform.GetChild(0).localScale;
        }

        private void Update()
        {
            ComingMousePoint();
            ShakeEye(_guage);
        }

        void ComingMousePoint()
        {
            _moveDir = (_camera.ScreenToWorldPoint(Mouse.current.position.ReadValue()) - transform.parent.position).normalized;
            _rigidCompo.linearVelocity = _moveDir * _speed;
        }

        void ShakeEye(float value) // 눈동자 축소 확대
        {
            if (value <= 40) // 심박수가 40 이하일 때
            {
                transform.GetChild(0).transform.DOScale(new Vector3(_originSize.x, _originSize.y, _originSize.z),0.45f);
            }
            else if (value <= 60)
            {
                transform.GetChild(0).transform
                    .DOScale(new Vector3(_originSize.x * 0.75f, _originSize.y * 0.75f, _originSize.z), 0.45f);
            }
            else if (value <= 80)
            {
                transform.GetChild(0).transform
                    .DOScale(new Vector3(_originSize.x * 0.6f, _originSize.y * 0.6f, _originSize.z), 0.45f);
            }
            else if (value < 100)
            {
                transform.GetChild(0).transform
                    .DOScale(new Vector3(_originSize.x * 0.53f, _originSize.y * 0.53f, _originSize.z), 0.45f);
            }
            else if (value >= 100)
            {
                transform.GetChild(0).transform
                    .DOScale(new Vector3(_originSize.x * 0.4f, _originSize.y * 0.4f, _originSize.z), 0.45f);
            }
        }
    }
}
