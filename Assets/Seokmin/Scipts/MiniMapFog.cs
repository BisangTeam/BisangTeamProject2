using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;

public class MiniMapFog : MonoBehaviour
{
    [SerializeField] private Tilemap _fogTilemap;
    [SerializeField] private Transform _player;
    [SerializeField] private int _range = 1;

    private HashSet<Vector3Int> _visitedTiles = new();
    private string _path;
    
    [SerializeField] private LayerMask _wallMask;
    [SerializeField] private float _fadeTime = 0.5f;
    [SerializeField] private TileBase _fogTile;
    
    private void Awake()
    {
        _path = Path.Combine(
            Application.persistentDataPath,
            "VisitedMap.txt"
        );

        Debug.Log(_path);
        LoadMap();
    }
    
    private bool IsBlocked(Vector3Int pos)
    {
        Vector2 start = _player.position;
        Vector2 end = _fogTilemap.GetCellCenterWorld(pos);

        RaycastHit2D hit = Physics2D.Linecast(
            start, end, _wallMask
        );

        if (hit.collider == null) return false;

        Vector3Int hitCell = _fogTilemap.WorldToCell(
            hit.point + (end - start).normalized * 0.01f
        );

        return hitCell != pos;
    }
    
    private void LoadMap()
    {
        if (!File.Exists(_path)) return;

        string[] lines = File.ReadAllLines(_path);

        foreach (string line in lines)
        {
            string[] data = line.Split(',');

            if (data.Length != 3) continue;

            Vector3Int pos = new Vector3Int(
                int.Parse(data[0]),
                int.Parse(data[1]),
                int.Parse(data[2])
            );

            _visitedTiles.Add(pos);
            _fogTilemap.SetTile(pos, null);
        }
    }
    
    private void Reveal(Vector3Int pos)
    {
        if (!_fogTilemap.HasTile(pos)) return;

        if (_visitedTiles.Add(pos))
        {
            StartCoroutine(FadeTile(pos));

            File.AppendAllText(
                _path, $"{pos.x},{pos.y},{pos.z}\n"
            );
        }
    }
    
    private IEnumerator FadeTile(Vector3Int pos)
    {
        _fogTilemap.SetTileFlags(pos, TileFlags.None);

        Color color = _fogTilemap.GetColor(pos);
        float startAlpha = color.a;
        float timer = 0f;

        while (timer < _fadeTime)
        {
            timer += Time.deltaTime;

            color.a = Mathf.Lerp(startAlpha, 0f, timer / _fadeTime);

            _fogTilemap.SetColor(pos, color);

            yield return null;
        }

        _fogTilemap.SetTile(pos, null);
    }
    
    public void ResetMap()//초기화
    {
        StopAllCoroutines();

        foreach (Vector3Int pos in _visitedTiles)
        {
            _fogTilemap.SetTile(pos, _fogTile);
            _fogTilemap.SetTileFlags(pos, TileFlags.None);
            _fogTilemap.SetColor(pos, Color.white);
        }

        _visitedTiles.Clear();

        if (File.Exists(_path))
        {
            File.Delete(_path);
        }
    }
    
    private void Update()
    {
        Vector3Int cell = _fogTilemap.WorldToCell(_player.position);

        for (int x = -_range; x <= _range; x++)
        {
            for (int y = -_range; y <= _range; y++)
            {
                if (x * x + y * y > _range * _range)
                    continue;

                Vector3Int pos = cell + new Vector3Int(x, y, 0);
                if (IsBlocked(pos)) continue;
                
                Reveal(pos);
            }
        }
    }
}
