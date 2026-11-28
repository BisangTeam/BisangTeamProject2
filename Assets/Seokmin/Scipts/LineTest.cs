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
    [SerializeField, InspectorName("연결 후 흔들림 시간 (초)"), Min(0f)]
    private float _settleDuration = 0.35f;
    [SerializeField, InspectorName("심장으로 회수되는 시간 (초)"), Min(0.05f)]
    private float _retractDuration = 0.45f;
    [SerializeField, Range(0f, 0.25f), InspectorName("움직일 때 휘어짐"),
     Tooltip("심장과 기관 사이 거리에 비례하는 연결 곡선과 회수 반동의 크기")]
    private float _motionBend = 0.12f;
    private float _extensionElapsed;
    private float _settleRemaining;
    private float _retractElapsed;
    private Vector3[] _releaseShape;
    private float[] _releaseDistances;
    private Vector3 _releaseBend;

    private void Awake()
    {
        _lR = GetComponent<LineRenderer>();
    }

    private void Start()
    {
        _cam = Camera.main;
    }

    private void LateUpdate()
    {
        if (_isRest)
        {
            UpdateRetraction();
            return;
        }
        if (_isCon)
        {
            if (_settleRemaining <= 0f || _points.Count < 2 || Time.deltaTime <= 0f) return;
            SettleConnectedLine();
        }
        else if (_isClick && DragMouse != null && DragMouse.added)
        {
            _mousePos = insManager.Instance.GetBloodPoint(_cam, DragMouse.position.ReadValue());
            PullTip(_mousePos);
        }
        Render();
    }

    private void SettleConnectedLine()
    {
        int last = _points.Count - 1;
        float step = Mathf.Min(Time.deltaTime, _settleRemaining);
        float damping = step / _settleRemaining;
        _settleRemaining -= step;
        for (int i = 1; i < last; i++)
        {
            var point = _points[i];
            point._prePos = Vector3.Lerp(point._prePos, point._pos, damping);
        }
        float spacing = Vector3.Distance(_endPos, _startPos) / last;
        _restLength = Mathf.MoveTowards(_restLength, spacing, _returnSpeed * step);
        _bendRestLength = _restLength * 2f;
        ResetInteriorAcceleration();
        SimulatePoints(last);
        _points[last]._pos = _endPos;
    }

    private void UpdateRetraction()
    {
        _retractElapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_retractElapsed / Mathf.Max(0.05f, _retractDuration));
        float remaining = 1f - Mathf.SmoothStep(0f, 1f, t);
        int segment = 1;
        for (int i = 1; i < _points.Count; i++)
        {
            // 분리 직전 줄을 따라 심장 쪽으로 되감는다.
            float distance = _releaseDistances[i] * remaining;
            while (segment < i && _releaseDistances[segment] < distance) segment++;
            float along = Mathf.InverseLerp(_releaseDistances[segment - 1], _releaseDistances[segment], distance);
            Vector3 onLine = Vector3.Lerp(_releaseShape[segment - 1], _releaseShape[segment], along);

            // 끝이 먼저 풀리고, 살짝 휘는 반동이 다음 마디로 전달된다.
            float u = (float)i / (_points.Count - 1);
            float delay = 0.2f * (1f - u);
            float phase = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(delay, delay + 0.65f, t));
            float recoil = Mathf.Sin(Mathf.PI * phase) * (1f - phase);
            _points[i]._pos = onLine + _releaseBend * (u * recoil * remaining);
        }
        Render();
        if (t < 1f) return;
        _isRest = false;
        gameObject.SetActive(false);
    }

    private void PullTip(Vector3 tip)
    {
        int last = _points.Count - 1;
        if (last < 1) return;
        _restLength = Vector3.Distance(tip, _startPos) / last;
        _bendRestLength = _restLength * 2f;
        _points[last]._pos = tip;
        ResetInteriorAcceleration();
        SimulatePoints(last);
    }

    private void ResetInteriorAcceleration()
    {
        for (int i = 1; i < _pointCount - 1; i++)
            _points[i].ResetAcc();
    }

    private void SimulatePoints(int endExclusive)
    {
        SpringCon();
        DistanceCon();
        BendCon();
        for (int i = 1; i < endExclusive; i++)
            _points[i].Simulate();
    }

    public bool ExtendTowards(Vector3 target)
    {
        _extensionElapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_extensionElapsed / Mathf.Max(0.05f, _extensionDuration));
        float reach = Mathf.SmoothStep(0f, 1f, t);
        Vector3 travel = target - _startPos;
        Vector3 bend = Vector3.Cross(Vector3.forward, travel) * _motionBend;
        int last = _points.Count - 1;
        for (int i = 1; i <= last; i++)
        {
            float u = (float)i / last;
            // 끝이 먼저 나가고 가운데 마디는 잠시 뒤따라온다.
            float along = u * reach * (1f - 0.4f * (1f - u) * (1f - reach));
            float bow = 4f * along * (1f - along) * (1f - 0.55f * reach * reach);
            var point = _points[i];
            point._prePos = point._pos;
            point._pos = _startPos + travel * along + bend * bow;
        }
        if (last > 0)
            _restLength = Vector3.Distance(_startPos, _points[last]._pos) / last;
        return t >= 1f;
    }

    public void IsDrClick(Vector3 heartPos, Mouse mouse = null)
    {
        DragMouse = mouse;
        _isClick = mouse != null;
        _isRest = false;
        _isCon = false;
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
        _isRest = false;
        _isCon = true;
        _settleRemaining = _settleDuration;
        if (_points.Count > 1)
        {
            var tip = _points[_points.Count - 1];
            tip._pos = tip._prePos = endPos;
            Render();
        }
        ReleasePointer();
    }

    public void Retract()
    {
        ReleasePointer();
        _isCon = false;
        _isRest = true;
        _retractElapsed = 0f;
        if (_releaseShape == null || _releaseShape.Length != _points.Count)
        {
            _releaseShape = new Vector3[_points.Count];
            _releaseDistances = new float[_points.Count];
        }
        for (int i = 0; i < _points.Count; i++)
        {
            _releaseShape[i] = _points[i]._pos;
            if (i > 0)
                _releaseDistances[i] = _releaseDistances[i - 1] + Vector3.Distance(_releaseShape[i - 1], _releaseShape[i]);
        }
        if (_points.Count > 1)
            _releaseBend = Vector3.Cross(Vector3.forward, _releaseShape[_points.Count - 1] - _startPos) * _motionBend;
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
