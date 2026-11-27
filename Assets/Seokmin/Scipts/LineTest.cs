using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class LineTest : MonoBehaviour
{
    private LineRenderer _lR;
    public bool _isClick = false;
    public Mouse DragMouse { get; private set; }
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
    private Vector3[] _releaseShape;
    private float _releaseTime;
    private bool _rebound;

    private void Awake()
    {
        _lR = GetComponent<LineRenderer>();
    }

    private void Start()
    {
        _cam = Camera.main;
    }

    private void Update()
    {
        if (_isClick && DragMouse != null && DragMouse.added)
        {
            _timer = 0f;
            _mousePos = insManager.Instance.GetBloodPoint(_cam, DragMouse.position.ReadValue());
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
            _points[_pointCount - 1]._pos = _mousePos;
        }
        else if (_isRest && !_isCon)
        {
            _timer = 0f;
            _releaseTime += Time.deltaTime;
            const float duration = 0.32f;
            float t = Mathf.Clamp01(_releaseTime / duration);
            float length = 1f - Mathf.SmoothStep(0f, 1f, t);
            if (_rebound) length += Mathf.Sin(t * Mathf.PI * 4f) * Mathf.Exp(-12f * t) * 0.09f;
            for (int i = 1; i < _points.Count; i++)
                _points[i]._pos = Vector3.LerpUnclamped(_startPos, _releaseShape[i], length);
            if (t >= 1f)
            {
                _isRest = false;
                gameObject.SetActive(false);
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
            for (int i = 1; i < _pointCount - 1; i++)
                _points[i].ResetAcc();
            SpringCon();
            DistanceCon();
            BendCon();
            _points[_pointCount - 1]._pos = _endPos;
            for (int i = 1; i < _pointCount - 1; ++i)
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

    public void IsDrClick(Vector3 heartPos, Mouse mouse = null)
    {
        DragMouse = mouse;
        _isClick = mouse != null;
        _isRest = false;
        _isCon = false;
        _timer = 0f;
        _restLength = 0f;
        _lR.positionCount = _pointCount;
        _startPos = heartPos;
        _points.Clear();
        for (int i = 0; i < _pointCount; ++i)
        {
            _points.Add(new VerletIntegrationTest(heartPos, _damping, _springStrength));
        }

        Render();
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
        
    public void ReleasePointer()
    {
        DragMouse = null;
        _isClick = false;
    }

    public void Retract(bool rebound)
    {
        ReleasePointer();
        _isCon = false;
        _isRest = true;
        _rebound = rebound;
        _releaseTime = 0f;
        if (_releaseShape == null || _releaseShape.Length != _points.Count)
            _releaseShape = new Vector3[_points.Count];
        for (int i = 0; i < _points.Count; i++)
            _releaseShape[i] = _points[i]._pos;
        _lR.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
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
