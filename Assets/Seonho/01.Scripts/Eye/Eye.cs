using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Seonho._01.Scripts.Eye
{
    public class Eye : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D _rigidCompo;
        
        Camera _camera;

        [SerializeField] private float _speed;

        [SerializeField] private Vector2 _moveDir;

        private void Awake()
        {
            _rigidCompo = GetComponent<Rigidbody2D>();
            _camera = Camera.main;
        }

        private void Update()
        {
            ComingMousePoint();
        }

        void ComingMousePoint()
        {
            _moveDir = (_camera.ScreenToWorldPoint(Mouse.current.position.ReadValue()) - transform.parent.position).normalized;
            _rigidCompo.linearVelocity = _moveDir * _speed;
        }
    }
}
