using Hyunwoo.HumanBodySystem;
using UnityEngine;
using UnityEngine.InputSystem;

public class institution : MonoBehaviour
{
    private Vector3 _instpos;
    private Camera _cam;
    private LineTest _nowLine;
    [SerializeField] private float _instRange;
    [SerializeField]private RectTransform _pos;
    [SerializeField]private bool _isCon = false; //Line에서 조건은 채웠는지
    [SerializeField]private bool _isCon2 = false; //지금 연결된 혈관이 있는지
    private Vector3 _mouse;

    [SerializeField] private AbstractViscera viscera;
    
    private void Start()
    {
        _cam = Camera.main;
    }

    private void Update()
    {
        //해제
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            _mouse = insManager.Instance.GetBloodPoint(_cam, Mouse.current.position.ReadValue());
            if (_nowLine != null)
            {
                if (_isCon2)
                {
                    IsRelease();
                    if (!_isCon)
                    {
                        _isCon2 = false;
                    }
                }
            }
        }
        
        //연결
        if (Mouse.current.leftButton.wasReleasedThisFrame && insManager.Instance._Heart._isCl)
        {
           if (!_isCon)
           {
                _nowLine = insManager.Instance._Heart._lineTest;
                IsConnection();
                if (_isCon)
                {
                    _isCon2 = true;
                }
           }
           _nowLine._isClick = false;
        }
        
    }
    public void IsConnection()
    {
        var manager = insManager.Instance;
        _instpos = manager.GetBloodPoint(_cam, _pos);
        Vector3 dropPoint = manager.GetBloodPoint(_cam, Mouse.current.position.ReadValue());
        if (!_nowLine._isCon && (_instpos - dropPoint).magnitude < _instRange)
        {
            viscera?.ConnectionBlood();
            _isCon = true;
            _nowLine._endPos = _instpos;
            _nowLine._isRest = false;
            _nowLine._isCon = true;
        }
    }

    public void IsRelease()
    {
        _instpos = insManager.Instance.GetBloodPoint(_cam, _pos);
        if ((_instpos - _mouse).magnitude < _instRange)
        {
            if (_nowLine._points.Count > 1)
            {
                _nowLine._points[_nowLine._points.Count - 1]._prePos = _nowLine._points[_nowLine._points.Count - 2]._pos;
            }
            viscera?.DisConnectionBlood();
            _nowLine._endPos = _instpos;
            _nowLine._isRest = true;
            _nowLine._isCon = false;
            _isCon = false;
        }
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(_instpos, _instRange);
    }
}
