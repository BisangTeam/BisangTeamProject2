using System;
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

        private const int ScreenWidth = 320;

        private readonly RaycastRenderResources _resources = new();
        private readonly RaycastWallRenderer2D _walls = new();
        private readonly RaycastObjectRenderer2D _objects = new();

        private void Awake()
        {
            _walls.SetTileTextures(wallTextures);
            _resources.Resize(rawImage, ScreenWidth);
        }

        private void LateUpdate()
        {
            RenderFrame();
        }

        private void RenderFrame()
        {
            if (!_resources.Resize(rawImage, ScreenWidth)) return;

            var view = new RaycastView2D(player, ScreenWidth, _resources.Height,
                fieldOfView, wallHeight, maxDistance);
            var lighting = new RaycastLighting2D(shadowStrength, shadowStartDistance,
                shadowEndDistance, sideWallBrightness);

            RaycastFloorRenderer2D.Draw(_resources, view, lighting, floorTexture, ceilingTexture, surfaceTileSize);
            _walls.Draw(_resources, view, lighting, wallMask, wallTilemap, defaultWallTexture);
            _objects.Draw(_resources, view, lighting);
            _resources.Present();
        }

        private void OnDestroy()
        {
            _resources.Dispose(rawImage);
        }
    }
}
