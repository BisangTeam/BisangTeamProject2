using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Hyunwoo.Raycasting2D
{
    // 벽을 찾고, 맞은 벽면의 텍스처를 화면의 세로줄에 그린다.
    internal sealed class RaycastWallRenderer2D
    {
        private readonly Dictionary<TileBase, Texture2D> _tileTextures = new();
        private readonly RaycastHit2D[] _hits = new RaycastHit2D[1];
        private ContactFilter2D _filter;

        public void SetTileTextures(WallTexture[] entries)
        {
            _tileTextures.Clear();
            if (entries == null) return;

            foreach (WallTexture entry in entries)
            {
                if (entry.tile != null && entry.texture != null)
                    _tileTextures[entry.tile] = entry.texture;
            }
        }

        public void Draw(RaycastRenderResources resources, RaycastView2D camera, RaycastLighting2D lighting,
            LayerMask wallMask, Tilemap wallTilemap, Texture2D defaultWallTexture, Color32 wallColor)
        {
            // 같은 프레임에 움직인 문도 반영하고, 상호작용 Trigger는 제외한다.
            Physics2D.SyncTransforms();
            _filter.SetLayerMask(wallMask);
            _filter.useTriggers = false;

            for (int x = 0; x < camera.Width; x++)
            {
                Vector2 direction = camera.GetDirection(x);
                int hitCount = Physics2D.Raycast(camera.Origin, direction, _filter, _hits, camera.MaxDistance);
                RaycastHit2D hit = hitCount > 0 ? _hits[0] : default;
                if (!hit.collider) continue;

                // 직선거리가 아닌 정면 깊이를 사용해서 벽의 어안 왜곡을 막는다.
                float depth = Mathf.Max(0.01f, Vector2.Dot(hit.point - camera.Origin, camera.Forward));
                float columnHeight = camera.WallHeight * camera.PixelLength / depth;
                float bottom = (camera.Height - columnHeight) * 0.5f;
                int first = Mathf.Clamp(Mathf.CeilToInt(bottom - 0.5f), 0, camera.Height);
                int end = Mathf.Clamp(Mathf.CeilToInt(bottom + columnHeight - 0.5f), 0, camera.Height);

                Texture2D source = GetWallTexture(hit, wallTilemap, defaultWallTexture, out float u);
                Color32[] sourcePixels = resources.GetTexturePixels(source);
                int textureX = sourcePixels != null ? Mathf.Min((int)(u * source.width), source.width - 1) : 0;
                float shade = lighting.WallShade(hit.normal, depth);

                for (int y = first; y < end; y++)
                {
                    Color32 color = wallColor;
                    if (sourcePixels != null)
                    {
                        // 화면에 잘리기 전의 벽 높이를 기준으로 원본 이미지의 행을 읽는다.
                        float v = (y + 0.5f - bottom) / columnHeight;
                        int textureY = Mathf.Clamp((int)(v * source.height), 0, source.height - 1);
                        color = sourcePixels[textureY * source.width + textureX];
                    }
                    int index = y * camera.Width + x;
                    resources.Pixels[index] = RaycastLighting2D.Shade(color, shade);
                    resources.Depths[index] = depth;
                }
            }
        }

        private Texture2D GetWallTexture(RaycastHit2D hit, Tilemap wallTilemap,
            Texture2D defaultWallTexture, out float u)
        {
            // 움직이는 문은 자신의 로컬 좌표에 붙은 이미지를 사용한다.
            RaycastWallSurface2D surface = hit.collider.GetComponent<RaycastWallSurface2D>();
            if (surface != null)
            {
                u = surface.GetTextureU(hit.point, hit.normal);
                return surface.Image != null ? surface.Image : defaultWallTexture;
            }

            Tilemap map = wallTilemap;
            if (map == null || (map.transform != hit.collider.transform &&
                                !map.transform.IsChildOf(hit.collider.transform)))
                map = hit.collider.GetComponent<Tilemap>();
            Texture2D source = defaultWallTexture;

            bool isSide = Mathf.Abs(hit.normal.x) > 0.5f;
            float faceX = hit.normal.x;
            float faceY = hit.normal.y;
            u = isSide ? hit.point.y : hit.point.x;

            if (map != null)
            {
                // 셀 경계의 빈 칸을 고르지 않도록 벽 안쪽으로 조금 들어가 검사한다.
                Vector3Int cell = map.WorldToCell(hit.point - hit.normal * 0.001f);
                TileBase tile = map.GetTile(cell);
                if (tile != null && _tileTextures.TryGetValue(tile, out Texture2D tileTexture))
                    source = tileTexture;

                Vector2 corner = map.CellToWorld(cell);
                Vector2 axisX = (Vector2)map.CellToWorld(cell + Vector3Int.right) - corner;
                Vector2 axisY = (Vector2)map.CellToWorld(cell + Vector3Int.up) - corner;
                Vector2 offset = hit.point - corner;
                faceX = Vector2.Dot(hit.normal, axisX.normalized);
                faceY = Vector2.Dot(hit.normal, axisY.normalized);
                isSide = Mathf.Abs(faceX) > Mathf.Abs(faceY);
                // 타일맵의 회전과 크기가 바뀌어도 벽면을 따라 이미지를 읽는다.
                u = isSide
                    ? Vector2.Dot(offset, axisY) / Mathf.Max(axisY.sqrMagnitude, 0.000001f)
                    : Vector2.Dot(offset, axisX) / Mathf.Max(axisX.sqrMagnitude, 0.000001f);
            }
            u = Mathf.Repeat(u, 1f);
            if (isSide ? faceX < 0f : faceY > 0f) u = 1f - u;
            return source;
        }
    }
}
