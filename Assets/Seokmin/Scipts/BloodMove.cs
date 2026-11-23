using UnityEngine;

public class BloodMove : MonoBehaviour
{
    private LineRenderer _lR;
    private LineTest _test;
    private int _maxPointCount;
    private int _pointCount;
    private float _timer;
    [SerializeField,Min(0.001f)] private float _moveSpeed;

    private void Awake()
    {
        _lR = GetComponent<LineRenderer>();
        _test = GetComponent<LineTest>();
    }

    private void Start()
    {
        _maxPointCount = _test._points.Count;
    }

    private void Update()
    {
        if (_timer >= _moveSpeed)
        {
            _timer = 0; 
            _pointCount++;
        }
    }
}
