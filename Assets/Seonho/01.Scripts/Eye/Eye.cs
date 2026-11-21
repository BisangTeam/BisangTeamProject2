using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Seonho._01.Scripts.Eye
{
    public class Eye : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D _rigidCompo;
        [SerializeField] private RectTransform _rect;
        
        private Vector3 _originSize;
        private Vector2 _moveDir;

        Camera _camera;

        [SerializeField] private float _speed;
        [SerializeField] private float _guage;

        private void Awake()
        {
            _rigidCompo = GetComponent<Rigidbody2D>();
            _rect = GetComponent<RectTransform>();
            _originSize = _rect.transform.localScale;
            _camera = Camera.main;
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
                _rect.transform.DOScale(new Vector3(_originSize.x, _originSize.y, _originSize.z),0.45f);
            }
            else if (value <= 60)
            {
                _rect.transform.DOScale(new Vector3(_originSize.x * 0.75f, _originSize.y * 0.75f, _originSize.z), 0.45f);
            }
            else if (value <= 80)
            {
                _rect.transform.DOScale(new Vector3(_originSize.x * 0.6f, _originSize.y * 0.6f, _originSize.z), 0.45f);
            }
            else if (value < 100)
            {
                _rect.transform.DOScale(new Vector3(_originSize.x * 0.53f, _originSize.y * 0.53f, _originSize.z), 0.45f);
            }
            else if (value >= 100)
            {
                _rect.transform.DOScale(new Vector3(_originSize.x * 0.4f, _originSize.y * 0.4f, _originSize.z), 0.45f);
            }
        }
    }
}
