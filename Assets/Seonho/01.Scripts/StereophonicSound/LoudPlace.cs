using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.PlayerLoop;
using Vector2 = UnityEngine.Vector2;

namespace Seonho._01.Scripts.StereophonicSound
{
    [RequireComponent(typeof(AudioSource))]
    public class LoudPlace : MonoBehaviour
    {
        private Rigidbody2D _rigid; // 임시 딱까리
        [SerializeField] private GameObject _player;
        
        [SerializeField] private AudioSource _audioCompo;
        [SerializeField] private AudioLowPassFilter _lowFilter;
        
        [SerializeField] private float _audioMinDistance; // 음향 최대 치 범위
        [SerializeField] private float _audioMaxDistance; // 음향 감소 시작 범위

        [SerializeField] private float _originFilterValue;

        [SerializeField] private LayerMask _whatIsWall;

        private void Awake()
        {
            Initialize();
            
            _rigid = GetComponent<Rigidbody2D>(); // 임시 딱까리 불러오기
        }

        void Initialize()
        {
            InitializeCompo();
            InitializeValue();
        }

        void InitializeCompo() // 컴포너트 초기화
        {
            _audioCompo = GetComponent<AudioSource>();
            _lowFilter = GetComponent<AudioLowPassFilter>();
        }

        void InitializeValue() // 값 초기화
        {
            _originFilterValue = 5000;
        }


        private void Update()
        {
            ProtoType();
            //===================================
            CheckWall();
        }

        void ProtoType()
        {
            if (Keyboard.current.aKey.isPressed)
            {
                _rigid.linearVelocityX = -10f;
            }
            else if (Keyboard.current.dKey.isPressed)
            {
                _rigid.linearVelocityX = 10;
            }
            else if (Keyboard.current.wKey.isPressed)
            {
                _rigid.linearVelocityY = 10f;
            }
            else if (Keyboard.current.sKey.isPressed)
            {
                _rigid.linearVelocityY = -10f;
            }
            else
                _rigid.linearVelocity = Vector2.zero;
            
        }
        
        void CheckWall() // 플레이어와 자신 사이에 벽 확인
        {
            RaycastHit2D check = Physics2D.Raycast(transform.position, GetDirection(), GetDistance(), _whatIsWall);
            Debug.DrawRay(transform.position, GetDirection() * GetDistance(), Color.red);
            
            if (check.collider != null)
            {
                LowFilter();
            }
            else
            {
                RollBackLowFilter();
            }
        }

        Vector2 GetDirection()
        {
            return (_player.transform.position - transform.position).normalized;
        } // 플레이어 방향
        
        float GetDistance()
        {
            return Vector2.Distance(transform.position, _player.transform.position);
        } // 플레이어 거리

        void RollBackLowFilter()
        {
            _lowFilter.cutoffFrequency = _originFilterValue;
        }
        
        void LowFilter()
        {
            _lowFilter.cutoffFrequency = _originFilterValue / 4;
        }
    }
}
