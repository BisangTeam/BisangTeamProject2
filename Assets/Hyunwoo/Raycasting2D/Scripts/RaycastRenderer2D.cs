using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace Hyunwoo.Raycasting2D
{
    public class RaycastRenderer2D : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private Transform player;
        [SerializeField] private RawImage view;
        [SerializeField] private LayerMask wallMask;

        [Header("Wall Textures")]
        [SerializeField] private Tilemap wallTilemap;
        [SerializeField] private Texture2D defaultWallTexture;
        [SerializeField] private WallTexture[] wallTextures;

        [Header("Settings")]
        [SerializeField, Range(10f, 150f)] private float fieldOfView = 80f;
        [SerializeField, Min(0.01f)] private float wallHeight = 1f;
        [SerializeField, Min(0.1f)] private float maxDistance = 30f;

        [Header("Colors")]
        [SerializeField] private Color ceilingColor = new Color(0.13f, 0.17f, 0.22f);
        [SerializeField] private Color floorColor = new Color(0.24f, 0.23f, 0.22f);
        [SerializeField] private Color wallColor = new Color(0.55f, 0.73f, 0.77f);

        [Serializable]
        private struct WallTexture
        {
            public TileBase tile;
            public Texture2D texture;
        }
    
        private struct VisibleSprite
        {
            public RaycastSprite2D sprite;
            public float side;
            public float depth;
        }
        
        private const int ScreenWidth = 320;
        private int _screenHeight;

        private Texture2D _texture;
        private Color32[] _pixels;

        private readonly float[] _wallDepth = new float[ScreenWidth];
        private readonly int[] _wallBottom = new int[ScreenWidth];
        private readonly int[] _wallTop = new int[ScreenWidth];
        
        private readonly Dictionary<Texture2D, Color32[]> _texturePixels = new();
        private readonly Dictionary<TileBase, Texture2D> _tileTextures = new();
        private readonly List<VisibleSprite> _visibleSprites = new();

        private void Awake()
        {
            CacheWallTextures();
            SetScreenHeight();
        }

        private void OnValidate()
        {
            _texturePixels.Clear();
            CacheWallTextures();
        }

        // SetTileTexturesDict
        private void CacheWallTextures()
        {
            _tileTextures.Clear();
            
            if (wallTextures == null) return;

            foreach (WallTexture entry in wallTextures) 
            {
                if (entry.tile != null && entry.texture != null)
                    _tileTextures[entry.tile] = entry.texture;
            }   
        }

        private void SetScreenHeight()
        {
            Rect rect = view.rectTransform.rect;
            int height = Mathf.RoundToInt(ScreenWidth / rect.width * rect.height);

            if (_screenHeight != height)
            {
                if (_texture != null)
                {
                    Destroy(_texture);
                    _texture = null;
                }
                _screenHeight = height;
                
                _pixels = new Color32[ScreenWidth * _screenHeight];
                CreateTexture();
            }
        }
        
        private void CreateTexture()
        {
            if (_texture == null)
            {
                _texture = new Texture2D(ScreenWidth, _screenHeight, TextureFormat.RGBA32, false);
                _texture.filterMode = FilterMode.Point;
                _texture.wrapMode = TextureWrapMode.Clamp;
            }
            
            view.texture = _texture;
        }


        private void LateUpdate()
        {
            RenderFrame();
        }

        private void RenderFrame()
        {
            FillColorFloorAndCelling();
            
            Vector2 origin = player.position;
            Vector2 forward = player.up;
            Vector2 right = player.right;
            
            float halfShownScreenWidth = Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad);
            float pixelLength = ScreenWidth / (2f * halfShownScreenWidth);

            for (int x = 0; x < ScreenWidth; x++)
            {
                ResetWallData(x);
                
                float screenX = ((x + 0.5f) / ScreenWidth * 2f - 1f) * halfShownScreenWidth;
                Vector2 direction = (forward + right * screenX).normalized;
                
                RaycastHit2D hit = Physics2D.Raycast(origin, direction, maxDistance, wallMask);
                if (!hit.collider) continue;

                // float depth = Mathf.Max(0.01f ,(hit.point - origin).magnitude); => 왜곡 효과 발생
                float depth = Mathf.Max(0.01f, Vector2.Dot(hit.point - origin, forward));
 
                float columnHeight = wallHeight * pixelLength / depth;
                // bottom은 화면에 잘리기 전 벽의 아래쪽 위치이므로 음수가 될 수도 있다.
                float bottom = (_screenHeight - columnHeight) * 0.5f;
                // 픽셀 중심이 y + 0.5이므로 경계에서 0.5를 뺀 뒤 정수로 바꾼다.
                // first는 그리기 시작할 행, end는 그리지 않는 끝 경계다. 화면 밖은 잘라낸다.
                int first = Mathf.Clamp(Mathf.CeilToInt(bottom - 0.5f), 0, _screenHeight);
                int end = Mathf.Clamp(Mathf.CeilToInt(bottom + columnHeight - 0.5f), 0, _screenHeight);
                // 3. 사물 가림 처리에 사용할 벽 정보를 이 열에 저장한다.
                _wallDepth[x] = depth;
                _wallBottom[x] = first;
                _wallTop[x] = end;

                // 4. 이 벽의 이미지와 가로 위치 u(0~1)를 구한다.
                // 예: u=0.25, 이미지 너비=64라면 이미지의 16번 세로줄을 읽는다.
                Texture2D source = GetWallTexture(hit, out float u);
                Color32[] sourcePixels = GetTexturePixels(source);
                // u가 정확히 1이면 width가 나오므로, 마지막 유효 인덱스 width-1로 제한한다.
                int textureX = sourcePixels != null ? Mathf.Min((int)(u * source.width), source.width - 1) : 0;
                // 벽 방향에 따른 음영과 거리 때문에 어두워지는 정도를 곱한다.
                float shade = (Mathf.Abs(hit.normal.x) > 0.5f ? 0.8f : 1f) / (1f + depth * 0.08f);
                for (int y = first; y < end; y++)
                {
                    Color32 color = wallColor;
                    if (sourcePixels != null)
                    {
                        // 화면 밖으로 잘리기 전의 높이를 기준으로 텍스처를 읽음.
                        // v는 벽 아래=0, 위=1. 현재 픽셀이 원본 이미지의 어느 행인지 계산한다.
                        float v = (y + 0.5f - bottom) / columnHeight;
                        int textureY = Mathf.Clamp((int)(v * source.height), 0, source.height - 1);
                        // 원본 이미지의 색을 가져와서 벽 전체를 단색으로 칠하던 부분을 대체한다.
                        color = sourcePixels[textureY * source.width + textureX];
                    }
                    _pixels[y * ScreenWidth + x] = Shade(color, shade);
                }
            }
            // 5. 같은 화면 배열 위에 사물을 그린 뒤, 완성 화면을 한 번 전송한다.
            DrawSprites(origin, forward, right, pixelLength);
            _texture.SetPixels32(_pixels);
            _texture.Apply(false);
        }

        private void ResetWallData(int x)
        {
            _wallDepth[x] = float.MaxValue;
            _wallBottom[x] = 0;
            _wallTop[x] = 0;
        }

        private Texture2D GetWallTexture(RaycastHit2D hit, out float u)
        {
            // 다른 타일맵에 맞았다면 그 타일맵을 사용한다.
            // CompositeCollider2D가 부모에 있는 경우에는 연결해 둔 자식 타일맵을 사용할 수 있다.
            Tilemap map = wallTilemap;
            if (map == null || (map.transform != hit.collider.transform &&
                                !map.transform.IsChildOf(hit.collider.transform)))
                map = hit.collider.GetComponent<Tilemap>();

            Texture2D source = defaultWallTexture;
            // 일반 Collider2D 벽은 월드 좌표 1칸마다 이미지를 반복한다.
            // 좌우를 향한 벽면은 Y를 따라, 위아래를 향한 벽면은 X를 따라 이미지가 이어진다.
            bool sideFace = Mathf.Abs(hit.normal.x) > 0.5f;
            float faceX = hit.normal.x;
            float faceY = hit.normal.y;
            u = sideFace ? hit.point.y : hit.point.x;
            if (map != null)
            {
                // 셀 경계에서 빈 칸을 고르지 않도록 벽 안쪽을 검사.
                // normal은 벽 바깥 방향이므로, 빼면 벽 안으로 아주 조금 들어간다.
                Vector3Int cell = map.WorldToCell(hit.point - hit.normal * 0.001f);
                TileBase tile = map.GetTile(cell);
                // 해당 타일의 연결이 있으면 기본 이미지 대신 그 이미지를 사용한다.
                if (tile != null && _tileTextures.TryGetValue(tile, out Texture2D tileTexture))
                    source = tileTexture;

                // 셀의 모서리와 X/Y 한 칸의 방향·길이를 구한다.
                // 월드 좌표를 바로 쓰지 않아도 되므로 타일맵의 회전과 크기도 반영할 수 있다.
                Vector2 corner = map.CellToWorld(cell);
                Vector2 axisX = (Vector2)map.CellToWorld(cell + Vector3Int.right) - corner;
                Vector2 axisY = (Vector2)map.CellToWorld(cell + Vector3Int.up) - corner;
                Vector2 offset = hit.point - corner;
                // normal이 어느 셀 축과 더 나란한지 비교해서 맞은 벽면을 구분한다.
                faceX = Vector2.Dot(hit.normal, axisX.normalized);
                faceY = Vector2.Dot(hit.normal, axisY.normalized);
                sideFace = Mathf.Abs(faceX) > Mathf.Abs(faceY);
                // offset을 벽면을 따라가는 축에 투영하고, 한 칸의 길이로 나눈 비율.
                // axis 길이의 제곱으로 나누는 이유는 Dot 안에도 axis 길이가 포함되어 있기 때문이다.
                u = sideFace
                    ? Vector2.Dot(offset, axisY) / Mathf.Max(axisY.sqrMagnitude, 0.000001f)
                    : Vector2.Dot(offset, axisX) / Mathf.Max(axisX.sqrMagnitude, 0.000001f);
            }
            // 2.3이나 -0.7 같은 값도 한 칸 안의 비율 0.3으로 바꾼다.
            u = Mathf.Repeat(u, 1f);
            // 반대쪽 벽면에서는 가로 읽기 방향을 뒤집어 면 방향에 맞춘다.
            if (sideFace ? faceX < 0f : faceY > 0f) u = 1f - u;
            return source;
        }

        private void DrawSprites(Vector2 origin, Vector2 forward, Vector2 right, float pixelLength)
        {
            // 1. 등록된 사물의 위치를 플레이어 기준 오른쪽(side), 정면(depth)으로 바꾼다.
            _visibleSprites.Clear();
            foreach (RaycastSprite2D sprite in RaycastSprite2D.Active)
            {
                if (sprite == null || !sprite.isActiveAndEnabled || sprite.Image == null) continue;
                Vector2 offset = (Vector2)sprite.transform.position - origin;
                float depth = Vector2.Dot(offset, forward);
                // 뒤쪽 사물과 너무 먼 사물은 그리지 않는다. 제곱끼리 비교해 제곱근 계산도 피한다.
                if (depth <= 0.01f || offset.sqrMagnitude > maxDistance * maxDistance) continue;
                _visibleSprites.Add(new VisibleSprite
                {
                    sprite = sprite, side = Vector2.Dot(offset, right), depth = depth
                });
            }

            // 2. 정면 깊이가 큰 사물부터 그리면 가까운 사물이 나중에 덮여 앞에 보인다.
            _visibleSprites.Sort((a, b) => b.depth.CompareTo(a.depth));
            foreach (VisibleSprite item in _visibleSprites)
            {
                Texture2D image = item.sprite.Image;
                Color32[] colors = GetTexturePixels(image);
                if (colors == null) continue;
                // 3. 깊이로 나눠 원근을 만든다. 깊이가 두 배면 화면 크기는 절반이 된다.
                float scale = pixelLength / item.depth;
                float height = Mathf.Max(0.01f, item.sprite.Height) * scale;
                // 원본 이미지의 가로세로 비율을 유지한다.
                float width = height * image.width / image.height;
                // 화면 가운데 + 오른쪽으로 떨어진 거리. 왼쪽 사물은 side가 음수다.
                float centerX = ScreenWidth * 0.5f + item.side * scale;
                float left = centerX - width * 0.5f;
                // XY는 발밑 위치이며, 눈높이는 벽 높이의 절반.
                // 화면 y는 아래=0이고 위로 커진다. 바닥은 눈높이보다 아래에 그린다.
                // Elevation을 올리면 발밑이 위로 올라가서 떠 있는 사물도 표현할 수 있다.
                float bottom = _screenHeight * 0.5f + (item.sprite.Elevation - wallHeight * 0.5f) * scale;
                // 이미지 중심이 화면 밖이어도 일부가 보일 수 있으므로 사각형 전체를 잘라 그린다.
                int firstX = Mathf.Clamp(Mathf.CeilToInt(left - 0.5f), 0, ScreenWidth);
                int endX = Mathf.Clamp(Mathf.CeilToInt(left + width - 0.5f), 0, ScreenWidth);
                int firstY = Mathf.Clamp(Mathf.CeilToInt(bottom - 0.5f), 0, _screenHeight);
                int endY = Mathf.Clamp(Mathf.CeilToInt(bottom + height - 0.5f), 0, _screenHeight);
                float shade = 1f / (1f + item.depth * 0.08f);
                // 4. 원본 PNG의 색을 결과 화면에 직접 그린다.
                // 화면에 항상 똑바로 그리므로 사물 Transform을 돌리지 않아도 정면을 보인다.
                for (int x = firstX; x < endX; x++)
                {
                    // 이미지 왼쪽부터의 비율을 원본 이미지의 가로 픽셀 번호로 바꾼다.
                    int textureX = Mathf.Clamp((int)((x + 0.5f - left) / width * image.width), 0, image.width - 1);
                    for (int y = firstY; y < endY; y++)
                    {
                        // 벽보다 뒤에 있고, 현재 픽셀도 벽이 차지한 세로 범위라면 가려진다.
                        // 열마다 검사하므로 벽 모서리에 이미지 절반만 가려지는 것도 가능하다.
                        if (item.depth >= _wallDepth[x] && y >= _wallBottom[x] && y < _wallTop[x]) continue;
                        int textureY = Mathf.Clamp((int)((y + 0.5f - bottom) / height * image.height), 0, image.height - 1);
                        Color32 color = colors[textureY * image.width + textureX];
                        // 투명한 픽셀은 건드리지 않아 이미 그린 벽이나 뒤쪽 사물이 그대로 보인다.
                        if (color.a == 0) continue;
                        // Shade는 최종 화면용으로 알파를 255로 만들므로 원본 알파를 먼저 보관한다.
                        byte alpha = color.a;
                        color = Shade(color, shade);
                        int index = y * ScreenWidth + x;
                        if (alpha < 255)
                        {
                            // 반투명 색 = 사물색 * 불투명 비율 + 기존 화면색 * 투명 비율.
                            // 예: alpha=128이면 두 색을 거의 반반씩 섞는다.
                            Color32 behind = _pixels[index];
                            color.r = (byte)((color.r * alpha + behind.r * (255 - alpha)) / 255);
                            color.g = (byte)((color.g * alpha + behind.g * (255 - alpha)) / 255);
                            color.b = (byte)((color.b * alpha + behind.b * (255 - alpha)) / 255);
                        }
                        _pixels[index] = color;
                    }
                }
            }
        }

        private Color32[] GetTexturePixels(Texture2D source)
        {
            if (source == null) return null;
            // 이미 읽은 이미지라면 보관 중인 배열을 바로 반환한다.
            if (_texturePixels.TryGetValue(source, out Color32[] colors)) return colors;
            // CPU에서 픽셀을 읽으려면 이미지 Import Settings의 Read/Write가 켜져 있어야 한다.
            // 배열은 왼쪽 아래부터 한 행씩 저장되므로 y * width + x로 색을 꺼낼 수 있다.
            if (source.isReadable)
                colors = source.GetPixels32();
            else
                Debug.LogWarning($"{source.name}: 텍스처 Import Settings에서 Read/Write를 켜 주세요.", source);
            // 읽기 불가능한 이미지도 null로 기록해 같은 경고를 매 프레임 출력하지 않는다.
            _texturePixels[source] = colors;
            return colors;
        }

        private static Color32 Shade(Color32 color, float brightness)
        {
            // RGB에 밝기를 곱한다. 완성 화면은 바닥/천장까지 채우므로 불투명하게 출력한다.
            return new Color32((byte)(color.r * brightness), (byte)(color.g * brightness),
                (byte)(color.b * brightness), 255);
        }

        private void FillColorFloorAndCelling()
        {
            Color32 floor = floorColor;
            Color32 ceiling = ceilingColor;
            for (int y = 0; y < _screenHeight; y++)
            {
                Color32 color = y < _screenHeight / 2 ? floor : ceiling;
                for (int x = 0; x < ScreenWidth; x++)
                    _pixels[y * ScreenWidth + x] = color;
            }
        }

        private void OnDestroy()
        {
            if (view != null && view.texture == _texture) view.texture = null;
            if (_texture == null) return;
            if (Application.isPlaying) Destroy(_texture);
            else DestroyImmediate(_texture);
            _texture = null;
        }
    }
}
