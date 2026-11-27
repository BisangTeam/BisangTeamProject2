using UnityEngine;

public class BloodPulse : MonoBehaviour
{
    [Header("혈관")]
    [SerializeField] private LineRenderer _line;
    [SerializeField] private LineTest _lineTest;

    [Header("혈액 이동")]
    [SerializeField, Min(0.05f)] private float _bloodDuration = 1f;

    [Header("혈관 부풀기")]
    [SerializeField] private float _bloodWidth = 0.7f;
    [SerializeField] private float _bloodRange = 1.5f;

    private float _elapsed;
    private bool _isMoving;
    private readonly AnimationCurve _restWidth = AnimationCurve.Linear(0f, 1f, 1f, 1f);
    private readonly AnimationCurve _widthCurve = new AnimationCurve();
    private Keyframe[] _widthKeys;

    private void Awake()
    {
        _line = GetComponent<LineRenderer>();
        _lineTest = GetComponent<LineTest>();
    }

    public void Play()
    {
        _elapsed = 0f;
        _isMoving = true;
    }

    private void LateUpdate()
    {
        if (!_isMoving) return;
        if (!_lineTest._isCon || _line.positionCount < 2 || _elapsed >= _bloodDuration)
        {
            _line.widthCurve = _restWidth;
            _isMoving = false;
            return;
        }
        UpdateWidth((_line.positionCount - 1) * (_elapsed / _bloodDuration));
        _elapsed += Time.deltaTime;
    }

    private void OnDisable()
    {
        _isMoving = false;
        if (_line != null) _line.widthCurve = _restWidth;
    }

    private void UpdateWidth(float pulseIndex)
    {
        int count = _line.positionCount;
        if (_widthKeys == null || _widthKeys.Length != count) _widthKeys = new Keyframe[count];

        for (int i = 0; i < count; i++)
        {
            float distance = Mathf.Abs(i - pulseIndex);

            float influence = 1f - Mathf.Clamp01(distance / _bloodRange);
            influence = Mathf.SmoothStep(0f, 1f, influence);

            float width = 1f + influence * _bloodWidth;

            float t = (float)i / (count - 1);

            _widthKeys[i] = new Keyframe(t, width);
        }

        _widthCurve.keys = _widthKeys;
        _line.widthCurve = _widthCurve;
    }
}
