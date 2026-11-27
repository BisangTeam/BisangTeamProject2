using UnityEngine;

public class MineMap : MineMapEvent
{
    private Camera _camera;
    public float ViewSize { get; private set; } = 2f;
    
    private void Awake()
    {
        OnMineMapSize += MakeMineMap;
        _camera = GetComponent<Camera>();
    }

    private void OnDestroy()
    {
        OnMineMapSize -=  MakeMineMap;
    }

    public override void MakeMineMap(float x)
    {
        ViewSize = Mathf.Max(0f, x);
        if (_camera.enabled) _camera.orthographicSize = Mathf.Max(0.01f, ViewSize);
    }
}
