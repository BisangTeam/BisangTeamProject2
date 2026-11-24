using UnityEngine;

public class MineMap : MineMapEvent
{
    private Camera _camera;
    
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
        _camera.orthographicSize = x;
    }
}
