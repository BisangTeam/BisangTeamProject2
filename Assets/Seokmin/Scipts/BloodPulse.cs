using UnityEngine;

public class BloodPulse : MonoBehaviour
{
    [Header("혈관")]
    [SerializeField] private LineRenderer _line;
    [SerializeField] private LineTest _lineTest;

    [Header("혈액 이동")]
    [SerializeField] private float _bloodDuration = 2f; //이동 시간
    [SerializeField] private float _bloodInterval = 1f; //다시 시작 시간 이건 그 심장에서 피를 만들어 내는 시간으로 하든가 하삼 알아서

    [Header("혈관 부풀기")]
    [SerializeField] private float _bloodWidth = 0.7f;
    [SerializeField] private float _bloodRange = 1.5f;

    private float _bloodIndex;
    private float _timer;
    private bool _isMoving = true;
    
    private void Awake()
    {
        _line = GetComponent<LineRenderer>();
        _lineTest = GetComponent<LineTest>();
    }

    private void LateUpdate()
    {
        if (_line == null || _line.positionCount < 2 || _lineTest._isCon == false)
            return;

        UpdateBlood();
        UpdateWidth();
    }

    private void UpdateBlood()
    {
        if (_isMoving)
        {
            _bloodIndex = Mathf.MoveTowards(
                _bloodIndex,
                _line.positionCount - 1,
                (_line.positionCount - 1) * Time.deltaTime / _bloodDuration
            );

            if (_bloodIndex >= _line.positionCount - 1)
            {
                _isMoving = false;
                _timer = 0f;
            }
        }
        else
        {
            _timer += Time.deltaTime;

            if (_timer >= _bloodInterval)
            {
                _timer = 0f;
                _bloodIndex = 0f;
                _isMoving = true;
            }
        }
    }

    private void UpdateWidth()
    {
        AnimationCurve curve = new AnimationCurve();

        int count = _line.positionCount;

        for (int i = 0; i < count; i++)
        {
            float distance = Mathf.Abs(i - _bloodIndex);

            float influence = 1f - Mathf.Clamp01(distance / _bloodRange);
            influence = Mathf.SmoothStep(0f, 1f, influence);

            float width = 1f + influence * _bloodWidth;

            float t = (float)i / (count - 1);

            curve.AddKey(t, width);
        }

        _line.widthCurve = curve;
    }
}
