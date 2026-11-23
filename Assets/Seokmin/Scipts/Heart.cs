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

    private void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            _isCl = false;
            var manager = insManager.Instance;
            _lineTest = manager.GetLine();
            if (_lineTest == null)
                return;
            _heartPos = manager.GetBloodPoint(_cam, _pos);
            _mousePos = manager.GetBloodPoint(_cam, Mouse.current.position.ReadValue());
            _lineTest._isClick = (_heartPos - _mousePos).magnitude < _heartRange; //심장 범위 체크
            if (_lineTest._isClick)
            {
                _isCl = true;
                _lineTest.GetLineRenderer().SetActive(true);
                _lineTest.IsDrClick(_heartPos);
            }
        }
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(_heartPos, _heartRange);
    }
}
