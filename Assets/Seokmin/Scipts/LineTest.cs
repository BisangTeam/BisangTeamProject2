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
    [SerializeField, Min(0.05f), Tooltip("클릭한 장기에 혈관 끝이 닿기까지 걸리는 시간(초)")]
    private float _extensionDuration = 0.45f;
    private float _extensionElapsed;
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
            MoveTip(_mousePos);
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
            MoveTip(_endPos);
            _timer += Time.deltaTime;
        }
    }

    private void MoveTip(Vector3 tip)
    {
        int last = _points.Count - 1;
        if (last < 1) return;
        Vector3 span = tip - _startPos;
        float length = span.magnitude;
        float spacing = length / last;
        _restLength = _isCon
            ? Mathf.MoveTowards(_restLength, spacing, _returnSpeed * Time.deltaTime)
            : spacing;
        _bendRestLength = _restLength * 2f;
        _points[last]._pos = _points[last]._prePos = tip;
        for (int i = 1; i < last; i++)
            _points[i].ResetAcc();

        SpringCon();
        DistanceCon();
        BendCon();
        _points[last]._pos = tip;

        Vector3 direction = length > 0f ? span / length : Vector3.zero;
        for (int i = 1; i < last; i++)
        {
            var point = _points[i];
            point.Simulate();
            float t = (float)i / last;
            Vector3 centre = _startPos + span * t;
            float maxBend = length * 0.5f * t * (1f - t);
            // 각 점의 진행 방향 간격은 고정하고, 현재 위치와 이전 위치 모두 옆으로만 휘게 한다.
            point._pos = KeepLateralBend(point._pos, centre, direction, maxBend);
            point._prePos = KeepLateralBend(point._prePos, centre, direction, maxBend);
        }
    }

    private static Vector3 KeepLateralBend(Vector3 position, Vector3 centre, Vector3 direction, float maxBend)
    {
        Vector3 lateral = Vector3.ProjectOnPlane(position - centre, direction);
        return centre + Vector3.ClampMagnitude(lateral, maxBend);
    }

    public bool ExtendTowards(Vector3 target)
    {
        _extensionElapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_extensionElapsed / Mathf.Max(0.05f, _extensionDuration));
        float progress = Mathf.SmoothStep(0f, 1f, t);
        Vector3 travel = target - _startPos;
        Vector3 sideways = Vector3.Cross(Vector3.forward, travel);
        // 손으로 끌 때처럼 중간에서만 살짝 휘고 양 끝에서는 정확히 붙는다.
        float bend = 0.24f * progress * (1f - progress);
        MoveTip(_startPos + travel * progress + sideways * bend);
        return t >= 1f;
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
        _extensionElapsed = 0f;
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

    public void PlaceStraight(Vector3 endPos)
    {
        int last = _points.Count - 1;
        if (last < 1) return;
        _restLength = Vector3.Distance(_startPos, endPos) / last;
        _bendRestLength = _restLength * 2f;
        for (int i = 1; i <= last; i++)
        {
            Vector3 point = Vector3.Lerp(_startPos, endPos, (float)i / last);
            _points[i]._pos = _points[i]._prePos = point;
        }
        Render();
    }

    public void AttachTo(Vector3 endPos)
    {
        _endPos = endPos;
        if (_points.Count > 1 && _points[_points.Count - 1]._pos != endPos)
            MoveTip(endPos);
        _isRest = false;
        _isCon = true;
        ReleasePointer();
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
