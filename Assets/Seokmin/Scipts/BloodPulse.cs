using Hyunwoo.HumanBodySystem;
using UnityEngine;

public class BloodPulse : MonoBehaviour
{
    [Header("혈관")]
    [SerializeField] private LineRenderer _line;
    [SerializeField] private LineTest _lineTest;

    [Header("혈액 이동")]
    [SerializeField, Min(0.05f), Tooltip("혈관 끝까지 이동하는 데 걸리는 박자 수")] private float _bloodDuration = 1f;

    [Header("혈관 부풀기")]
    [SerializeField] private float _bloodWidth = 0.7f;
    [SerializeField] private float _bloodRange = 1.5f;

    private AbstractViscera _organ;
    private double _startedAt;
    private bool _isMoving;
    private double PulseTime => _organ != null ? _organ.HeartBeatsElapsed : Time.timeAsDouble;
    private readonly AnimationCurve _restWidth = AnimationCurve.Linear(0f, 1f, 1f, 1f);
    private readonly AnimationCurve _widthCurve = new AnimationCurve();
    private Keyframe[] _widthKeys;

    private void Awake()
    {
        _line = GetComponent<LineRenderer>();
        _lineTest = GetComponent<LineTest>();
    }

    public void Play() => Play(null);

    internal void Play(AbstractViscera organ)
    {
        _organ = organ;
        _startedAt = organ != null ? organ.LastPulseBeat : Time.timeAsDouble;
        _isMoving = true;
    }

    private void LateUpdate()
    {
        if (!_isMoving) return;
        float elapsed = (float)(PulseTime - _startedAt);
        if (!_lineTest._isCon || _line.positionCount < 2 || elapsed >= _bloodDuration)
        {
            _line.widthCurve = _restWidth;
            _isMoving = false;
            return;
        }
        UpdateWidth((_line.positionCount - 1) * (elapsed / _bloodDuration));
    }

    private void OnDisable()
    {
        _organ = null;
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
