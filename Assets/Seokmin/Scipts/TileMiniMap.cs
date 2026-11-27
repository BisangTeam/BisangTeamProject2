using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public sealed class TileMiniMap : MaskableGraphic
{
    [SerializeField] private MiniMapFog fog;
    [SerializeField] private Tilemap walls;
    [SerializeField] private MineMap zoom;
    private int _revision = -1;
    private int _radius = -1;
    private float _fadeUntil;
    private bool _fading;
    private Vector3Int _centre;
    private Vector2 _facing;

    private void LateUpdate()
    {
        if (fog == null || walls == null || zoom == null) return;
        int radius = Mathf.Clamp(Mathf.CeilToInt(zoom.ViewSize * 2f), 0, 12);
        bool tilesChanged = _revision != fog.Revision || _centre != fog.PlayerCell || radius != _radius;
        if (tilesChanged)
        {
            _revision = fog.Revision;
            _centre = fog.PlayerCell;
            _radius = radius;
            _fadeUntil = Time.time + fog.FadeTime;
            _fading = true;
        }
        Vector2 facing = fog.PlayerFacing;
        if (!_fading && (facing - _facing).sqrMagnitude < 0.0001f) return;
        _facing = facing;
        _fading = Time.time < _fadeUntil;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        if (_radius <= 0 || fog == null) return;
        Rect rect = rectTransform.rect;
        float size = Mathf.Min(rect.width, rect.height) / (2 * _radius + 1);
        for (int y = -_radius; y <= _radius; y++)
        for (int x = -_radius; x <= _radius; x++)
        {
            Vector3Int cell = _centre + new Vector3Int(x, y, 0);
            if (!fog.TryGetBrightness(cell, out float light)) continue;
            Color tint = walls.HasTile(cell) ? new Color32(97, 113, 114, 235) : new Color32(185, 199, 196, 235);
            if (x == 0 && y == 0) tint = new Color32(105, 255, 157, 255);
            tint.a *= light;
            Vector2 mid = rect.center + new Vector2(x, y) * size;
            float half = size * 0.46f;
            int first = mesh.currentVertCount;
            mesh.AddVert(mid + new Vector2(-half, -half), tint, Vector2.zero);
            mesh.AddVert(mid + new Vector2(-half, half), tint, Vector2.zero);
            mesh.AddVert(mid + new Vector2(half, half), tint, Vector2.zero);
            mesh.AddVert(mid + new Vector2(half, -half), tint, Vector2.zero);
            mesh.AddTriangle(first, first + 1, first + 2);
            mesh.AddTriangle(first, first + 2, first + 3);
        }
        if (fog.TryGetBrightness(_centre, out float brightness))
            DrawHeading(mesh, rect.center, size, brightness);
    }

    private void DrawHeading(VertexHelper mesh, Vector2 centre, float size, float brightness)
    {
        Color tint = new Color32(22, 62, 45, 255);
        tint.a *= brightness;
        Vector2 nose = centre + _facing * (size * 0.36f);
        Vector2 tail = centre - _facing * (size * 0.23f);
        Vector2 wing = Vector2.Perpendicular(_facing) * (size * 0.22f);
        int first = mesh.currentVertCount;
        mesh.AddVert(nose, tint, Vector2.zero);
        mesh.AddVert(tail - wing, tint, Vector2.zero);
        mesh.AddVert(tail + wing, tint, Vector2.zero);
        mesh.AddTriangle(first, first + 1, first + 2);
    }
}
