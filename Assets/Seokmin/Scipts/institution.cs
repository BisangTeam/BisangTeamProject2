using Hyunwoo.BloodPack;
using Hyunwoo.HumanBodySystem;
using UnityEngine;

public class institution : MonoBehaviour
{
    [SerializeField] private RectTransform _pos;
    [SerializeField] private bool _isCon;
    [SerializeField] private AbstractViscera viscera;
    private LineTest _nowLine;
    private Camera _connectCamera;
    private BloodPulse _pulse;
    private RectTransform _hitArea;

    public AbstractViscera Organ => viscera;
    public bool Connected => _isCon;
    public bool HasVessel => _nowLine != null || _isCon;

    public MedicalHover Hover { get; private set; }

    private void Awake()
    {
        Hover = GetComponent<MedicalHover>();
        _hitArea = _pos;
        if (_hitArea == null || _hitArea.rect.width <= 0f || _hitArea.rect.height <= 0f)
        {
            _hitArea = transform.Find("Frame") as RectTransform ?? (RectTransform)transform;
            if (_pos == null) _pos = _hitArea;
        }
    }

    public bool TryHit(Vector2 pointer, Camera camera, out float distance)
    {
        distance = float.PositiveInfinity;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_hitArea, pointer, camera, out var local)) return false;
        Rect rect = _hitArea.rect;
        if (!rect.Contains(local)) return false;
        Vector2 centred = Rect.PointToNormalized(rect, local) - Vector2.one * 0.5f;
        distance = centred.sqrMagnitude;
        return true;
    }

    public void Attach(LineTest line, Camera camera)
    {
        if (_isCon || line == null || line._isCon) return;
        if (_nowLine != null && _nowLine != line) return;
        if (viscera != null)
        {
            viscera.ConnectionBlood();
            if (!viscera.ConnectedToHeart)
            {
                _nowLine = null;
                line.Retract(false);
                return;
            }
            _pulse = line.GetComponent<BloodPulse>();
            if (_pulse != null) viscera.BloodSupplied += _pulse.Play;
        }
        _nowLine = line;
        line.AttachTo(insManager.Instance.GetBloodPoint(camera, _pos));
        _isCon = true;
    }

    public void RestoreConnection() => BeginConnection(Camera.main, animate: false);

    public void ConnectToHeart(Camera camera) => BeginConnection(camera, animate: true);

    private void BeginConnection(Camera camera, bool animate)
    {
        if (HasVessel) return;
        var manager = insManager.Instance;
        var line = manager.GetLine();
        if (line == null) return;
        line.gameObject.SetActive(true);
        line.IsDrClick(manager.GetBloodPoint(camera, manager._Heart.BloodAnchor));
        _nowLine = line;
        _connectCamera = camera;
        if (!animate)
        {
            line.PlaceStraight(manager.GetBloodPoint(camera, _pos));
            Attach(line, camera);
        }
    }

    private void Update()
    {
        if (_nowLine == null || _isCon || Time.deltaTime <= 0f) return;
        Vector3 target = insManager.Instance.GetBloodPoint(_connectCamera, _pos);
        if (_nowLine.ExtendTowards(target))
            Attach(_nowLine, _connectCamera);
    }

    public void IsRelease()
    {
        if (!HasVessel) return;
        if (_isCon && viscera != null)
        {
            if (_pulse != null) viscera.BloodSupplied -= _pulse.Play;
            viscera.DisConnectionBlood();
        }
        if (_nowLine != null && _nowLine.isActiveAndEnabled) _nowLine.Retract(true);
        _isCon = false;
        _nowLine = null;
        _pulse = null;
    }

    private void OnDisable() => IsRelease();
}
