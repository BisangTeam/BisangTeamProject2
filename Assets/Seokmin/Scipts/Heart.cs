using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class Heart : MonoBehaviour
{
    private Vector3 _heartPos;
    [SerializeField] private float _heartRange;
    private Camera _cam;
    private Vector3 _mousePos;
    public LineTest _lineTest;
    [SerializeField]private RectTransform _pos;
    public bool _isCl;

    private void Awake()
    {
        _cam = Camera.main;
    }

    private void Start()
    {
        _heartPos = _cam.ScreenToWorldPoint(_pos.position);
        _heartPos.z = 0;
    }

    private void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            _lineTest = insManager.Instance.GetLine();
            if (_lineTest == null)
                return;
            _mousePos = _cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            _mousePos.z = 0;
            _lineTest._isClick = (_heartPos - _mousePos).magnitude < _heartRange; //심장 범위 체크
            if (_lineTest._isClick)
            {
                _isCl = true;
                _lineTest.GetLineRenderer().SetActive(true);
                _lineTest.IsDrClick(_heartPos);
            }
            else
            {
                _isCl = false;
            }
        }
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(_heartPos, _heartRange);
    }
}
