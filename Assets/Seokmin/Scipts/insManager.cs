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
        for (int i = 0; i < LineTest.Length; ++i)
        {
            GameObject inst = Instantiate(_linePrefab, _bloodCav.transform);
            inst.transform.position = Vector3.zero;
            inst.SetActive(false);
            LineTest[i] = inst.GetComponent<LineTest>();
        }
    }

    public LineTest GetLine()
    {
        for (int i = 0; i < LineTest.Length; ++i)
        {
            if (!LineTest[i]._isCon) //사용 가능한 혈관 체크
            {
                LineTest[i].nowCount = i;
                _nowLine =  LineTest[i];
                return _nowLine;
            }
        }
        return null;
    }
    
    
}
