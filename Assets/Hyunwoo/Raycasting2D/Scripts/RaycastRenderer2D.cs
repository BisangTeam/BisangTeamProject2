using System;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace Hyunwoo.Raycasting2D
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
        [SerializeField] private RawImage view;
        [SerializeField] private LayerMask wallMask;

        [Header("Wall Textures")]
        [SerializeField] private Tilemap wallTilemap;
        [SerializeField] private Texture2D defaultWallTexture;
        [SerializeField] private WallTexture[] wallTextures;

        [Header("Floor And Ceiling Textures")]
        [SerializeField] private Texture2D floorTexture;
        [SerializeField] private Texture2D ceilingTexture;
        [SerializeField, Min(0.01f)] private float surfaceTileSize = 1f;

        [Header("Settings")]
        [SerializeField, Range(10f, 150f)] private float fieldOfView = 80f;
        [SerializeField, Min(0.01f)] private float wallHeight = 1f;
        [SerializeField, Min(0.1f)] private float maxDistance = 30f;

        [Header("Distance Darkness")]
        [SerializeField, Min(0f)] private float shadowStrength = 1f;
        [SerializeField, Min(0f)] private float shadowStartDistance = 2f;
        [SerializeField, Min(0.01f)] private float shadowEndDistance = 8f;
        [SerializeField, Range(0f, 1f)] private float sideWallBrightness = 0.8f;

        [Header("Colors")]
        [SerializeField] private Color ceilingColor = new Color(0.13f, 0.17f, 0.22f);
        [SerializeField] private Color floorColor = new Color(0.24f, 0.23f, 0.22f);
        [SerializeField] private Color wallColor = new Color(0.55f, 0.73f, 0.77f);

        private const int ScreenWidth = 320;
        private readonly RaycastRenderResources _resources = new();
        private readonly RaycastWallRenderer2D _walls = new();
        private readonly RaycastObjectRenderer2D _objects = new();

        private void Awake()
        {
            SetTileTexturesDict();
            _resources.Resize(view, ScreenWidth);
        }

        private void OnValidate()
        {
            _resources.ClearTextureCache();
            SetTileTexturesDict();
        }

        private void SetTileTexturesDict()
        {
            _walls.SetTileTextures(wallTextures);
        }

        private void LateUpdate()
        {
            RenderFrame();
        }

        private void RenderFrame()
        {
            if (!_resources.Resize(view, ScreenWidth)) return;

            var camera = new RaycastView2D(player, ScreenWidth, _resources.Height,
                fieldOfView, wallHeight, maxDistance);
            var lighting = new RaycastLighting2D(shadowStrength, shadowStartDistance,
                shadowEndDistance, sideWallBrightness);

            RaycastFloorRenderer2D.Draw(_resources, camera, lighting,
                floorTexture, ceilingTexture, surfaceTileSize, floorColor, ceilingColor);
            _walls.Draw(_resources, camera, lighting, wallMask, wallTilemap, defaultWallTexture, wallColor);
            _objects.Draw(_resources, camera, lighting);
            _resources.Present();
        }

        private void OnDestroy()
        {
            _resources.Dispose(view);
        }
    }
}
