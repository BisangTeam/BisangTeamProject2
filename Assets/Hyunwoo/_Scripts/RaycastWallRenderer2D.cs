using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Hyunwoo.Raycasting2D.Scripts
{
    public sealed class RaycastWallRenderer2D
    {
        private readonly Dictionary<TileBase, Texture2D> _tileTextures = new();
        private readonly RaycastHit2D[] _hits = new RaycastHit2D[32];

        private ContactFilter2D _filter;

        private readonly Color32 _defaultColor = new Color32(0, 0, 0, 255); 

        public void SetTileTextures(WallTexture[] wallTextures)
        {
            _tileTextures.Clear();

            foreach (WallTexture wallTexture in wallTextures)
            {
                if (wallTexture.tile != null && wallTexture.texture != null)
                    _tileTextures[wallTexture.tile] = wallTexture.texture;
            }
        }

        public void Draw(RaycastRenderResources resources, RaycastView2D view, RaycastLighting2D lighting,
            LayerMask wallMask, Tilemap wallTilemap, Texture2D defaultWallTexture, RaycastDoor2D peekDoor, float slitWidth)
        {
            Physics2D.SyncTransforms();

            _filter.SetLayerMask(wallMask);
            _filter.useTriggers = false;

            for (int x = 0; x < view.Width; x++)
            {
                Vector2 direction = view.GetDirection(x);

                bool throughSlit = peekDoor != null &&
                                   Mathf.Abs((x + 0.5f) / view.Width - 0.5f)
                                   <= slitWidth * 0.5f;

                int hitCount = Physics2D.Raycast(
                    view.Origin, direction, _filter, _hits, view.MaxDistance);

                int nearestIndex = -1;
                float nearestDistance = float.MaxValue;

                for (int i = 0; i < hitCount; i++)
                {
                    Collider2D hitCollider = _hits[i].collider;
                    if (hitCollider == null)
                        continue;

                    if (throughSlit &&
                        hitCollider.GetComponentInParent<RaycastDoor2D>() == peekDoor)
                        continue;

                    if (_hits[i].distance < nearestDistance)
                    {
                        nearestDistance = _hits[i].distance;
                        nearestIndex = i;
                    }
                }

                if (nearestIndex >= 0)
                {
                    DrawColumn(
                        x, _hits[nearestIndex], resources, view, lighting,
                        wallTilemap, defaultWallTexture);
                }
            }
        }

        private void DrawColumn(int x, RaycastHit2D hit, RaycastRenderResources resources, RaycastView2D view,
            RaycastLighting2D lighting, Tilemap wallTilemap, Texture2D defaultWallTexture)
        {
            DrawStruct drawStruct = DrawStruct.CalculateDrawStruct(hit, view);

            Texture2D source = GetWallTexture(hit, wallTilemap, defaultWallTexture, out float u);
            Color32[] sourcePixels = resources.GetTexturePixels(source);

            float shade = lighting.WallShade(hit.normal, drawStruct.Depth);

            DrawColumnPixels(resources, view, drawStruct, source, sourcePixels, x, u, shade);
        }
        
        private void DrawColumnPixels(RaycastRenderResources resources, RaycastView2D view, DrawStruct drawStruct,
            Texture2D source, Color32[] sourcePixels, int x, float u, float shade)
        {
            int textureX = sourcePixels != null ? Mathf.Min((int)(u * source.width), source.width - 1) : 0;

            for (int y = drawStruct.First; y < drawStruct.End; y++)
            {
                Color32 color = GetColor(drawStruct, source, sourcePixels, textureX, y);

                int index = y * view.Width + x;

                resources.Pixels[index] = RaycastLighting2D.Shade(color, shade);
                resources.Depths[index] = drawStruct.Depth;
            }
        }

        private Color32 GetColor(DrawStruct drawStruct, Texture2D source, Color32[] sourcePixels, int textureX, int y)      
        {
            Color32 color = _defaultColor;

            if (sourcePixels != null)
            {
                float v = (y + 0.5f - drawStruct.Bottom) / drawStruct.Height;
                int textureY = Mathf.Clamp((int)(v * source.height), 0, source.height - 1);

                color = sourcePixels[textureY * source.width + textureX];
            }

            return color;
        }

        private Texture2D GetWallTexture(RaycastHit2D hit, Tilemap tilemap, Texture2D defaultWallTexture, out float u)
        {
            if (IsDetectDoor(hit, defaultWallTexture, out u, out Texture2D texture))
                return texture;

            Vector3Int cell = tilemap.WorldToCell(hit.point - hit.normal * 0.001f);

            Texture2D source = CalculateWallTexture(tilemap, defaultWallTexture, cell);

            u = CalculateU(hit, tilemap, cell);

            return source;
        }

        private bool IsDetectDoor(RaycastHit2D hit, Texture2D defaultWallTexture, out float u, out Texture2D texture)
        {
            if (!hit.collider.TryGetComponent(out RaycastWallSurface2D surface))
            {
                u = 0;
                texture = defaultWallTexture;
                return false;
            }

            u = surface.GetTextureU(hit.point, hit.normal);
            texture = surface.Image != null ? surface.Image : defaultWallTexture;
            return true;
        }

        private Texture2D CalculateWallTexture(Tilemap tilemap, Texture2D defaultWallTexture, Vector3Int cell)
        {
            TileBase tile = tilemap.GetTile(cell);

            Texture2D source = defaultWallTexture;

            if (tile != null && _tileTextures.TryGetValue(tile, out Texture2D tileTexture))
                source = tileTexture;

            return source;
        }

        private float CalculateU(RaycastHit2D hit, Tilemap tilemap, Vector3Int cell)
        {
            Vector2 corner = tilemap.CellToWorld(cell);

            Vector2 axisX = (Vector2)tilemap.CellToWorld(cell + Vector3Int.right) - corner;
            Vector2 axisY = (Vector2)tilemap.CellToWorld(cell + Vector3Int.up) - corner;

            Vector2 offset = hit.point - corner;

            float faceX = Vector2.Dot(hit.normal, axisX.normalized);
            float faceY = Vector2.Dot(hit.normal, axisY.normalized);
            bool isSide = Mathf.Abs(faceX) > Mathf.Abs(faceY);

            Vector2 axis = isSide ? axisY : axisX;

            float distance = Vector2.Dot(offset, axis.normalized);
            float u = distance / Mathf.Max(axis.magnitude, 0.000001f);

            u = Mathf.Repeat(u, 1f);
            if (isSide ? faceX < 0f : faceY > 0f) u = 1f - u;

            return u;
        }
    }
}
