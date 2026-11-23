using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using Hyunwoo.Raycasting2D.Scripts;

namespace Hyunwoo.Raycasting2D.Editor
{
    public static class RaycastingSceneBuilder
    {
        public const string ScenePath = "Assets/Hyunwoo/Raycasting2D/Scenes/Raycasting2D.unity";
        private const string TileDirectory = "Assets/Hyunwoo/Raycasting2D/Tiles";

        [MenuItem("Hyunwoo/2D Raycasting/Create Demo Scene")]
        public static void CreateScene()
        {
            // 이미 만든 씬을 다시 생성해서 덮어쓰지 않는다.
            if (File.Exists(ScenePath))
            {
                Debug.Log("Raycasting2D scene already exists: " + ScenePath);
                return;
            }

            // 이 프로젝트의 벽 레이어를 우선 사용한다. 생성하는 벽과 플레이어의 검사 대상도 같게 맞춘다.
            int wallLayer = LayerMask.NameToLayer("RaycastCheckWall");
            if (wallLayer < 0) wallLayer = LayerMask.NameToLayer("Wall");
            if (wallLayer < 0)
                throw new InvalidOperationException("Create a Wall layer before building this scene.");

            Tile wallTile = CreateWallTile();
            Scene previousScene = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);

            try
            {
                CreateCamera();
                // 1. XY 평면에 실제로 칠해진, 수정 가능한 벽 타일맵.
                CreateMap(wallTile, wallLayer);
                PlayerMove player = CreatePlayer(wallTile.sprite, wallLayer);
                RawImage view = CreateView();

                var renderer = new GameObject("Raycast Renderer").AddComponent<RaycastRenderer2D>();
                var settings = new SerializedObject(renderer);
                settings.FindProperty("player").objectReferenceValue = player.transform;
                settings.FindProperty("view").objectReferenceValue = view;
                settings.FindProperty("wallMask").intValue = 1 << wallLayer;
                // 해상도는 렌더러의 320x180 상수로 정하므로, 인스펙터 필드로 설정하지 않는다.
                settings.FindProperty("fieldOfView").floatValue = 80f;
                settings.FindProperty("wallHeight").floatValue = 1f;
                settings.FindProperty("maxDistance").floatValue = 30f;
                settings.ApplyModifiedPropertiesWithoutUndo();

                if (!EditorSceneManager.SaveScene(scene, ScenePath))
                    throw new IOException("Could not save " + ScenePath);
                AssetDatabase.SaveAssets();
                Debug.Log("Created 2D raycasting scene: " + ScenePath);
            }
            finally
            {
                SceneManager.SetActiveScene(previousScene);
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        // Unity -batchmode -executeMethod Hyunwoo.Raycasting2D.Editor.RaycastingSceneBuilder.CreateAndValidate
        public static void CreateAndValidate()
        {
            try
            {
                CreateScene();
                RaycastingSceneValidation.Validate();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static Tile CreateWallTile()
        {
            string imagePath = TileDirectory + "/WallSquare.png";
            if (!File.Exists(imagePath))
            {
                var texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
                var pixels = new Color32[16 * 16];
                for (int i = 0; i < pixels.Length; i++)
                    pixels[i] = new Color32(130, 150, 170, 255);
                texture.SetPixels32(pixels);
                texture.Apply();
                File.WriteAllBytes(imagePath, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(imagePath);
                var importer = (TextureImporter)AssetImporter.GetAtPath(imagePath);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 16;
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                var textureSettings = new TextureImporterSettings();
                importer.ReadTextureSettings(textureSettings);
                textureSettings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(textureSettings);
                importer.SaveAndReimport();
            }

            string tilePath = TileDirectory + "/Wall.asset";
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(imagePath);
                tile.colliderType = Tile.ColliderType.Grid;
                AssetDatabase.CreateAsset(tile, tilePath);
            }
            return tile;
        }

        private static void CreateCamera()
        {
            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener))
                .GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(9f, 6.5f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = 7.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            // 화면은 Canvas의 RawImage가 그린다. 2D 타일맵은 Scene 창에서 확인한다.
            camera.cullingMask = 0;
        }

        private static void CreateMap(Tile tile, int wallLayer)
        {
            var grid = new GameObject("Grid", typeof(Grid));
            var walls = new GameObject("Walls", typeof(Tilemap), typeof(TilemapRenderer), typeof(TilemapCollider2D));
            walls.transform.SetParent(grid.transform, false);
            walls.layer = wallLayer;
            Tilemap tilemap = walls.GetComponent<Tilemap>();
            // 위에서 내려다본 맵. #만 벽이며 빈 칸은 자유롭게 칠해서 바꿀 수 있다.
            string[] rows =
            {
                "##################",
                "#................#",
                "#..####..####....#",
                "#..#.........#...#",
                "#..#..###....#...#",
                "#.....#..........#",
                "#.##..#..###..##.#",
                "#.....#....#.....#",
                "#..####....#.....#",
                "#..........####..#",
                "#..###...........#",
                "#................#",
                "##################"
            };
            for (int row = 0; row < rows.Length; row++)
            {
                if (rows[row].Length != rows[0].Length)
                    throw new InvalidOperationException("Map rows must have the same width.");
                for (int x = 0; x < rows[row].Length; x++)
                    if (rows[row][x] == '#')
                        tilemap.SetTile(new Vector3Int(x, rows.Length - 1 - row, 0), tile);
            }
            tilemap.CompressBounds();
            walls.GetComponent<TilemapCollider2D>().ProcessTilemapChanges();
        }

        private static PlayerMove CreatePlayer(Sprite markerSprite, int wallLayer)
        {
            var player = new GameObject("Player", typeof(Rigidbody2D), typeof(CircleCollider2D));
            player.transform.position = new Vector3(2.5f, 1.5f, 0f);
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            player.GetComponent<CircleCollider2D>().radius = 0.2f;
            PlayerMove controller = player.AddComponent<PlayerMove>();
            var settings = new SerializedObject(controller);
            settings.FindProperty("wallMask").intValue = 1 << wallLayer;
            settings.ApplyModifiedPropertiesWithoutUndo();

            var marker = new GameObject("Map Marker", typeof(SpriteRenderer));
            marker.transform.SetParent(player.transform, false);
            marker.transform.localScale = Vector3.one * 0.35f;
            marker.GetComponent<SpriteRenderer>().sprite = markerSprite;
            marker.GetComponent<SpriteRenderer>().color = new Color(0.2f, 1f, 0.6f);
            marker.GetComponent<SpriteRenderer>().sortingOrder = 1;
            return controller;
        }

        private static RawImage CreateView()
        {
            var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasObject.layer = LayerMask.NameToLayer("UI");
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            var viewObject = new GameObject("View", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
            viewObject.layer = LayerMask.NameToLayer("UI");
            viewObject.transform.SetParent(canvasObject.transform, false);
            RectTransform rect = viewObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            AspectRatioFitter aspect = viewObject.GetComponent<AspectRatioFitter>();
            aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            aspect.aspectRatio = 320f / 180f;
            RawImage image = viewObject.GetComponent<RawImage>();
            image.raycastTarget = false;

            var controls = new GameObject("Controls", typeof(RectTransform), typeof(Text));
            controls.layer = LayerMask.NameToLayer("UI");
            controls.transform.SetParent(viewObject.transform, false);
            RectTransform controlsRect = controls.GetComponent<RectTransform>();
            controlsRect.anchorMin = Vector2.zero;
            controlsRect.anchorMax = new Vector2(1f, 0f);
            controlsRect.pivot = new Vector2(0.5f, 0f);
            controlsRect.sizeDelta = new Vector2(0f, 36f);
            Text text = controls.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = "WASD: Move     Q / E or Left / Right: Turn";
            text.fontSize = 18;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(1f, 1f, 1f, 0.8f);
            text.raycastTarget = false;
            return image;
        }
    }
}
