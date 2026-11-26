using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace Hyunwoo.Raycasting2D.Scripts
{
    [Serializable]
    public struct WallTexture
    {
        public TileBase tile;
        public Texture2D texture;
    }

    public class RaycastRenderer2D : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private Transform player;
        [SerializeField] private RawImage rawImage;
        [SerializeField] private LayerMask wallMask;

        [Header("WallTextures")]
        [SerializeField] private Tilemap wallTilemap;
        [SerializeField] private Texture2D defaultWallTexture;
        [SerializeField] private WallTexture[] wallTextures;

        [Header("Floor")]
        [SerializeField] private Texture2D floorTexture;
        [SerializeField] private Texture2D ceilingTexture;
        [SerializeField, Min(0.01f)] private float surfaceTileSize = 1f;

        [Header("Settings")]
        [SerializeField, Range(10f, 150f)] private float fieldOfView = 80f;
        [SerializeField, Min(0.01f)] private float wallHeight = 1f;
        [SerializeField, Min(0.1f)] private float maxDistance = 30f;

        [Header("DistanceDarkness")]
        [field:SerializeField, Min(0f)] public float ShadowStrength { get; set; } = 1f;
        [SerializeField, Min(0f)] private float shadowStartDistance = 2f;
        [SerializeField, Min(0.01f)] private float shadowEndDistance = 8f;
        [SerializeField, Range(0f, 1f)] private float sideWallBrightness = 0.8f;

        [SerializeField, Range(0.01f, 0.3f)]
        private float peekSlitWidth = 0.08f;

        private float _activeFieldOfView;
        private float _verticalViewOffset;
        
        private RaycastDoor2D _peekingDoor;

        public void SetPeekDoor(RaycastDoor2D door)
        {
            _peekingDoor = door;
        }

        [Header("Corridor Lighting")]
        [SerializeField, Range(0f, 1f)] private float ambientBrightness = 1f;
        [SerializeField] private LowBloodPulse lowBloodPulse;
        private readonly List<RaycastLight2D.Sample> _lights = new();

        private const int ScreenWidth = 320;

        private readonly RaycastRenderResources _resources = new();
        private readonly RaycastWallRenderer2D _walls = new();
        private readonly RaycastObjectRenderer2D _objects = new();

        private void Awake()
        {
            _walls.SetTileTextures(wallTextures);
            _resources.Resize(rawImage, ScreenWidth);

            _activeFieldOfView = fieldOfView;
            _verticalViewOffset = 0f;
        }

        private void LateUpdate()
        {
            RenderFrame();
        }

        private void RenderFrame()
        {
            if (!_resources.Resize(rawImage, ScreenWidth)) return;

            var view = new RaycastView2D(player, ScreenWidth, _resources.Height,
                _activeFieldOfView, wallHeight, maxDistance, lowBloodPulse != null ? lowBloodPulse.HeightOffset : 0f);
            RaycastLight2D.Collect(gameObject.scene, view, Time.time, _lights);
            var lighting = new RaycastLighting2D(ShadowStrength, shadowStartDistance,
                shadowEndDistance, sideWallBrightness, ambientBrightness, _lights);

            RaycastFloorRenderer2D.Draw(_resources, view, lighting, floorTexture, ceilingTexture, surfaceTileSize);
            _walls.Draw(_resources, view, lighting, wallMask, wallTilemap, defaultWallTexture, _peekingDoor, peekSlitWidth);
            _objects.Draw(_resources, view, lighting);
            _resources.Present();
        }

        private void OnDestroy()
        {
            _resources.Dispose(rawImage);
        }

        public RaycastSprite2D SpriteAtScreenPoint(Vector2 screenPoint, Camera eventCamera)
        {
            Texture2D texture = _resources.Texture;
            if (!isActiveAndEnabled || texture == null || rawImage.texture != texture) return null;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rawImage.rectTransform, screenPoint,
                    eventCamera, out Vector2 localPoint)) return null;

            Rect rect = rawImage.GetPixelAdjustedRect();
            if (!rect.Contains(localPoint)) return null;
            Vector2 point = Rect.PointToNormalized(rect, localPoint);
            Vector2 uv = Rect.NormalizedToPoint(rawImage.uvRect, point);
            int x = Mathf.Clamp(Mathf.FloorToInt(uv.x * texture.width), 0, texture.width - 1);
            int y = Mathf.Clamp(Mathf.FloorToInt(uv.y * texture.height), 0, texture.height - 1);
            return _resources.Sprites[y * texture.width + x];
        }

        public void SetHideView(float fieldOfView, float verticalOffset)
        {
            _activeFieldOfView = Mathf.Clamp(fieldOfView, 10f, 150f);
            _verticalViewOffset = Mathf.Clamp(verticalOffset, -0.3f, 0.3f);
        }

        public void ResetHideView()
        {
            _activeFieldOfView = fieldOfView;
            _verticalViewOffset = 0f;
        }
    }
}
