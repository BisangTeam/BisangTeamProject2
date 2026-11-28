using System.IO;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;

public class MiniMapFog : MonoBehaviour
{
    [SerializeField] private Tilemap _fogTilemap;
    [SerializeField] private Transform _player;
    [SerializeField] private int _range = 1;

    private readonly HashSet<Vector3Int> _visitedTiles = new();
    [SerializeField] private Tilemap floor;
    [SerializeField] private Tilemap walls;
    private readonly Dictionary<Vector3Int, float> _revealedAt = new();
    private readonly StringBuilder _newVisits = new();
    private Vector3Int? _lastCell;
    public Vector3Int PlayerCell => _lastCell ?? Vector3Int.zero;
    public Vector2 PlayerFacing => ((Vector2)floor.transform.InverseTransformDirection(_player.up)).normalized;
    public float FadeTime => _fadeTime;
    public int Revision { get; private set; }
    private string _path;

    public bool TryGetBrightness(Vector3Int cell, out float light)
    {
        light = 0f;
        if (!_visitedTiles.Contains(cell)) return false;
        light = _revealedAt.TryGetValue(cell, out float time) && _fadeTime > 0f
            ? Mathf.Clamp01((Time.time - time) / _fadeTime) : 1f;
        return true;
    }
    
    [SerializeField] private LayerMask _wallMask;
    [SerializeField] private float _fadeTime = 0.5f;
    [SerializeField] private TileBase _fogTile;
    
    private void Awake()
    {
        _path = Path.Combine(
            Application.persistentDataPath,
            "VisitedMap.txt"
        );

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

            if (data.Length != 3 || !int.TryParse(data[0], out int x) ||
                !int.TryParse(data[1], out int y) || !int.TryParse(data[2], out int z)) continue;
            Vector3Int pos = new Vector3Int(x, y, z);
            _visitedTiles.Add(pos);
            if (floor == null) _fogTilemap.SetTile(pos, null);
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

        if (floor == null)
        {
            foreach (Vector3Int pos in _visitedTiles)
            {
                _fogTilemap.SetTile(pos, _fogTile);
                _fogTilemap.SetTileFlags(pos, TileFlags.None);
                _fogTilemap.SetColor(pos, Color.white);
            }
        }

        _visitedTiles.Clear();
        _lastCell = null;
        _revealedAt.Clear();
        Revision++;

        if (File.Exists(_path))
        {
            File.Delete(_path);
        }
    }
    
    private void Update()
    {
        if (floor != null)
        {
            Vector3Int centre = floor.WorldToCell(_player.position);
            centre.z = 0;
            if (_lastCell == centre) return;
            _lastCell = centre;
            _newVisits.Clear();
            RevealCell(centre);
            RevealCell(centre + Vector3Int.up);
            RevealCell(centre + Vector3Int.down);
            RevealCell(centre + Vector3Int.left);
            RevealCell(centre + Vector3Int.right);
            if (_newVisits.Length > 0)
            {
                Revision++;
                File.AppendAllText(_path, _newVisits.ToString());
            }
            return;
        }
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
    private void RevealCell(Vector3Int cell)
    {
        if ((!floor.HasTile(cell) && !walls.HasTile(cell)) || !_visitedTiles.Add(cell)) return;
        _revealedAt[cell] = Time.time;
        _newVisits.Append(cell.x).Append(',').Append(cell.y).Append(',').Append(cell.z).Append('\n');
    }
}
