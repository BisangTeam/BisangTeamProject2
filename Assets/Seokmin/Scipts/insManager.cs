using UnityEngine;

public class insManager : MonoBehaviour
{
    public static insManager Instance { get; private set; }
    //private GameObject _Canvas;
    [field:SerializeField] 
    public int InstCount {get; private set;}
    public LineTest[] LineTest{get; private set;}
    [SerializeField] private GameObject _linePrefab;
    public LineTest _nowLine { get; private set; }
    [field:SerializeField]
    public Heart _Heart { get; private set; }
    
    [field:SerializeField]
    public Sprite BloodSprite { get; private set; }
    
    [SerializeField] private GameObject _bloodCav;
    [SerializeField] private Camera _bloodCamera;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
        //_Canvas = transform.parent.gameObject;
        LineTest = new LineTest[InstCount];
    }

    private void Start()
    {
        for (int i = 0; i < LineTest.Length; i++)
        {
            if (LineTest[i] == null) LineTest[i] = CreateLine();
        }
    }

    private LineTest CreateLine()
    {
        GameObject inst = Instantiate(_linePrefab, _bloodCav.transform);
        inst.transform.position = Vector3.zero;
        if (_bloodCamera != null)
            inst.GetComponent<LineRenderer>().useWorldSpace = true;
        inst.SetActive(false);
        return inst.GetComponent<LineTest>();
    }

    public Vector3 GetBloodPoint(Camera inputCamera, Vector2 screenPoint)
    {
        if (_bloodCamera == null)
            return LegacyBloodPoint(inputCamera, screenPoint);

        // The mouse uses game window coordinates even when Main Camera renders to a smaller texture.
        Vector2 viewport = inputCamera.targetTexture == null
            ? (Vector2)inputCamera.ScreenToViewportPoint(screenPoint)
            : new Vector2(screenPoint.x / Screen.width, screenPoint.y / Screen.height);
        return BloodViewportPoint(viewport);
    }

    public Vector3 GetBloodPoint(Camera inputCamera, RectTransform anchor)
    {
        if (_bloodCamera == null)
            return LegacyBloodPoint(inputCamera, anchor.position);

        return BloodViewportPoint(inputCamera.WorldToViewportPoint(anchor.position));
    }

    private Vector3 BloodViewportPoint(Vector2 viewport)
    {
        float depth = _bloodCamera.nearClipPlane + 1f;
        return _bloodCamera.ViewportToWorldPoint(new Vector3(viewport.x, viewport.y, depth));
    }

    private static Vector3 LegacyBloodPoint(Camera inputCamera, Vector3 screenPoint)
    {
        Vector3 point = inputCamera.ScreenToWorldPoint(screenPoint);
        point.z = 0;
        return point;
    }

    public LineTest GetLine()
    {
        int available = -1;
        int inUse = 0;
        for (int i = 0; i < LineTest.Length; i++)
        {
            var line = LineTest[i];
            if (line == null || !line.gameObject.activeSelf)
            {
                if (available < 0) available = i;
            }
            else if (!line._isRest) inUse++;
        }

        if (inUse >= InstCount) return null;
        if (available < 0)
        {
            // 회수 중인 줄이 끝까지 보이도록 새 연결에는 별도 슬롯을 쓴다.
            var pool = LineTest;
            available = pool.Length;
            System.Array.Resize(ref pool, available + 1);
            LineTest = pool;
        }
        if (LineTest[available] == null) LineTest[available] = CreateLine();
        _nowLine = LineTest[available];
        _nowLine.nowCount = available;
        return _nowLine;
    }
}
