using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class LineTest : MonoBehaviour
{
    private LineRenderer _lR;
    public bool _isClick = false;
    private Camera _cam;
    public Vector3 _mousePos;
    public List<VerletIntegrationTest> _points = new List<VerletIntegrationTest>();
    [SerializeField] private float _damping; //감쇠 계수 => 관성을 얼마나 유지할지
    [SerializeField] private float _springStrength;
    [SerializeField] private int _pointCount;
    private float _dis;
    private Vector3 _dir;
    private float _error;
    private float _restLength;
    private float _bendRestLength;
    private Vector3 _correction;
    [SerializeField, Range(0f, 1f)] private float _stiffness; //오차거리 보정 강도
    [SerializeField, Range(0f, 1f)] private float _bendStiffness;
    [SerializeField] private float _constraintIterations; //계산 반복 정도 사용은 안하고 있음
    private Vector3 _startPos;
    [SerializeField, Range(0f, 100f)] private float _segSpringStrength = 20f; //당기는 강도
    private float _returnSegSpr;
    private Vector3 _spring;
    [SerializeField, Range(0.01f, 4f)] private float _returnSpeed; //돌아가는 힘
    public bool _isRest;
    public int nowCount;
    public Vector3 _endPos;
    public bool _isCon = false;
    [SerializeField, Range(0.01f, 1f)] private float _instSpeed;
    private float _timer = 0f;

    private void Awake()
    {
        _lR = GetComponent<LineRenderer>();
    }

    private void Start()
    {
        _cam = Camera.main;
        _isRest = false;
    }

    private void Update()
    {
        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            if (_points.Count > 1)
            {
                _points[_points.Count - 1]._prePos = _points[_points.Count - 2]._pos;
            }
        }
        if (_isClick)
        {
            _timer = 0f;
            _mousePos = insManager.Instance.GetBloodPoint(_cam, Mouse.current.position.ReadValue());
            _restLength = (_mousePos - _points[0]._pos).magnitude / (_points.Count - 1); //오차 거리식
            _bendRestLength = _restLength * 2;
            _points[_points.Count - 1]._pos = _mousePos;
            for (int i = 1; i < _pointCount - 1; ++i)
            {
                _points[i].ResetAcc();
            }

            SpringCon();
            DistanceCon();
            BendCon(); //더 자연스러워지기 위한 추가 계산
            for (int i = 1; i < _pointCount - 1; ++i)
            {
                _points[i].Simulate();
            }

            _isRest = true;
        }
        else if (_isRest && !_isCon)
        {
            _timer = 0f;
            _restLength = Mathf.MoveTowards(_restLength, 0.01f, _returnSpeed * Time.deltaTime);
            _bendRestLength = _restLength * 2;
            Reset();
            if (_restLength <= 0.01f)
            {
                _isRest = false;
                for (int i = 0; i < _points.Count; i++)
                {
                    _lR.SetPosition(i, _startPos);
                    gameObject.SetActive(false);
                }
            }
        }
        else if (_isCon)
        {
            if (_points.Count > 1)
            {
                _points[_points.Count - 1]._pos = _endPos;
                _points[_points.Count - 1]._prePos = _points[_points.Count - 1]._pos;
            }
            _restLength = Mathf.MoveTowards(_restLength, (_endPos - _points[0]._pos).magnitude / (_points.Count - 1), _returnSpeed * Time.deltaTime);
            _bendRestLength = _restLength * 2;
            SpringCon();
            DistanceCon();
            BendCon();
            for (int i = 1; i < _pointCount; ++i)
            {
                _points[i].Simulate();
            }
            _timer += Time.deltaTime;
        }
    }

    private void LateUpdate()
    {
        if (_timer <= 5f)
        {
            Render();
        }
    }

    public void IsDrClick(Vector3 heartPos)
    {
        _startPos = heartPos;
        _points.Clear();
        for (int i = 0; i < _pointCount; ++i)
        {
            _points.Add(new VerletIntegrationTest(heartPos, _damping, _springStrength));
        }

        _lR.SetPosition(0, _startPos);
    }
    
    public GameObject GetLineRenderer()
    {
        return gameObject;
    }
    
    private void Render()
    {
        for (int i = 0; i < _points.Count; ++i)
        {
            _lR.SetPosition(i, _points[i]._pos);
        }
    }
        
    private void Reset()
    {
        for (int i = 1; i < _pointCount - 1; ++i)
        {
            _points[i].ResetAcc(); //누적된 탄성의 정도를 초기화
        }
        SpringCon();
        DistanceCon();
        BendCon();
        for (int i = 1; i < _pointCount; ++i)
        {
            _points[i].Simulate(); //Verlet를 사용한 자연스러운 움직임
        }
    }
    
    private void SpringCon() //탄성 구현
    {
        for (int i = 1; i < _pointCount - 1; ++i)
        {
            _dir = _points[i - 1]._pos -  _points[i]._pos;
            _dis = _dir.magnitude;
            if (_dis < 0.000001f) 
                continue;
    
            _error = _dis - _restLength;
            _spring = _dir / _dis * _error * _segSpringStrength;
            _points[i].AddAcc(_spring); //탄성의 힘 추가
        }
        for (int i = 1; i < _pointCount - 1; ++i)
        {
            _dir = _points[i + 1]._pos -  _points[i]._pos;
            _dis = _dir.magnitude;
            if (_dis < 0.000001f)
                continue;
                
            _error = _dis - _restLength;
            _spring = _dir / _dis * _error * _segSpringStrength;
            _points[i].AddAcc(_spring);
        }
    }
    
    private void DistanceCon() //자연스러운 혈관 움직임 구현이 되기 위한 계산하는 곳
    { 
        for (int i = 1; i < _points.Count; ++i)
        {
            _dir = _points[i]._pos - _points[i - 1]._pos;
            _dis = _dir.magnitude; // magnitude = Vector길이 구함 피타고라스 사용
            if (_dis < 0.000001f)
                continue;
                    
            _error = _dis - _restLength;
            _correction = _dir.normalized * _error * _stiffness; //보정값
            if (i == 1)
            {
                _points[i]._pos -= _correction;
            }
            else
            {
                _points[i - 1]._pos += _correction * 0.5f;
                _points[i]._pos -= _correction * 0.5f;
            }
        }
    }
    
    private void BendCon() //전에가 다음 노드랑 비교 했다면 이번에 그 다음 노드랑 비교 
    {
        for (int i = 0; i < _points.Count - 2; ++i)
        {
            _dir = _points[i + 2]._pos - _points[i]._pos;
            _dis = _dir.magnitude;
            if (_dis < 0.000001f)
                continue;
                
            _error = _dis - _bendRestLength;
            _correction = (_dir / _dis) * _error * _bendStiffness; // 보정값
            if (i == 0)
            {
                _points[i + 2]._pos -= _correction;
            }
            else
            {
                _points[i]._pos += _correction * 0.5f;
                _points[i + 2]._pos -= _correction * 0.5f;
            }
        }
    }
    
}
