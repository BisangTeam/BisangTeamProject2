using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;
using Hyunwoo.Raycasting2D.Scripts;

namespace Hyunwoo.Raycasting2D.Editor
{
    // Editor에서 예제 이미지와 씬 배치를 준비하는 도구다. 게임 실행 중의 렌더링에는 필요하지 않다.
    public static class RaycastTextureDemoSetup
    {
        private const string Root = "Assets/Hyunwoo/Raycasting2D";
        private const int Size = 64;

        // Unity 위쪽 메뉴에서 실행한다. 현재 선택된 씬에만 예제 설정을 적용한다.
        [MenuItem("Hyunwoo/2D Raycasting/Apply Textures And Sprites To Open Scene")]
        public static void ApplyToOpenScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Stop Play Mode before setting up the raycasting demo.");
                return;
            }
            ApplyToScene(SceneManager.GetActiveScene());
        }

        // 씬 안의 렌더러와 벽 Tilemap을 찾아 텍스처를 연결하고 예제 사물을 만든다.
        public static void ApplyToScene(Scene scene)
        {
            // 최상위 오브젝트뿐 아니라 그 아래 자식까지 찾아 첫 번째 렌더러를 사용한다.
            RaycastRenderer2D renderer = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<RaycastRenderer2D>(true)).FirstOrDefault();
            if (renderer == null)
            {
                Debug.LogWarning("The active scene has no RaycastRenderer2D. Open a raycasting scene first.");
                return;
            }

            // SerializedObject는 인스펙터에 보이는 값을 Editor 코드에서 읽고 바꾸는 도구다.
            // wallTilemap이 아직 비어 있으면 Wall Mask에 포함된 레이어의 Tilemap을 찾는다.
            var settings = new SerializedObject(renderer);
            int wallMask = settings.FindProperty("wallMask").intValue;
            Tilemap map = settings.FindProperty("wallTilemap").objectReferenceValue as Tilemap;
            if (map == null)
                map = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Tilemap>(true))
                    .FirstOrDefault(item => (wallMask & (1 << item.gameObject.layer)) != 0);
            if (map == null)
            {
                Debug.LogWarning("No wall Tilemap matches the renderer's Wall Mask.");
                return;
            }

            // 각 Pixel 함수가 좌표마다 반환한 색으로 64×64 예제 이미지를 준비한다.
            EnsureFolder(Root + "/Textures");
            Texture2D brick = CreateImage("Brick", BrickPixel);
            Texture2D stone = CreateImage("Stone", StonePixel);
            Texture2D metal = CreateImage("Metal", MetalPixel);
            Texture2D barrel = CreateImage("Barrel", BarrelPixel);
            Texture2D torch = CreateImage("Torch", TorchPixel);

            // 원본 타일 에셋은 그대로 두고, 복제본 세 개에 서로 다른 벽 이미지를 지정한다.
            Tile source = map.GetUsedTilesCount() == 0 ? null : FindGridTile(map);
            if (source == null)
                throw new InvalidOperationException("The demo needs at least one square Grid-collider Tile.");
            Tile[] tiles = { CreateTile("BrickWall", source, brick), CreateTile("StoneWall", source, stone),
                CreateTile("MetalWall", source, metal) };

            // 여러 오브젝트의 변경을 하나로 묶어 Ctrl+Z 한 번으로 되돌릴 수 있게 한다.
            Undo.SetCurrentGroupName("Set Up Raycast Textures And Sprites");
            int undoGroup = Undo.GetCurrentGroup();
            Undo.RegisterCompleteObjectUndo(map, "Assign wall textures");
            // 벽이 있는 위치는 유지하고 종류만 교체한다. 4칸 단위로 재질을 달리해 비교하기 쉽게 한다.
            foreach (Vector3Int cell in map.cellBounds.allPositionsWithin)
            {
                Tile tile = map.GetTile<Tile>(cell);
                if (tile != source && !tiles.Contains(tile)) continue;
                int section = Mathf.Abs(Mathf.FloorToInt(cell.x / 4f) + Mathf.FloorToInt(cell.y / 4f)) % tiles.Length;
                map.SetTile(cell, tiles[section]);
            }
            map.GetComponent<TilemapCollider2D>()?.ProcessTilemapChanges();

            // 인스펙터에서 참조와 배열을 채우는 작업을 대신한다. 마지막 Apply로 변경 내용을 반영한다.
            Undo.RecordObject(renderer, "Assign raycast textures");
            settings.Update();
            settings.FindProperty("wallTilemap").objectReferenceValue = map;
            settings.FindProperty("defaultWallTexture").objectReferenceValue = brick;
            SerializedProperty textures = settings.FindProperty("wallTextures");
            textures.arraySize = tiles.Length;
            Texture2D[] images = { brick, stone, metal };
            for (int i = 0; i < tiles.Length; i++)
            {
                textures.GetArrayElementAtIndex(i).FindPropertyRelative("tile").objectReferenceValue = tiles[i];
                textures.GetArrayElementAtIndex(i).FindPropertyRelative("texture").objectReferenceValue = images[i];
            }
            settings.ApplyModifiedProperties();

            // 통과 횃불을 한 부모 아래 모은다. 같은 이름이 이미 있으면 중복으로 만들지 않는다.
            Transform player = settings.FindProperty("player").objectReferenceValue as Transform;
            if (player != null)
            {
                Transform group = scene.GetRootGameObjects().FirstOrDefault(item => item.name == "Raycast Demo Sprites")?.transform;
                if (group == null)
                {
                    var container = new GameObject("Raycast Demo Sprites");
                    SceneManager.MoveGameObjectToScene(container, scene);
                    Undo.RegisterCreatedObjectUndo(container, "Create demo sprites");
                    group = container.transform;
                }
                CreateDemoSprite(group, "Barrel", barrel, 0.65f, player, map, 2f, 0.45f);
                CreateDemoSprite(group, "Torch", torch, 0.9f, player, map, 3.2f, -0.55f);
            }
            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Raycast wall textures and two billboard sprites are ready. Save the scene to keep the setup.", renderer);
        }

        // 기본 데모 씬을 열어 설정한 뒤 저장한다. 다른 곳에서 이 함수를 호출할 때 사용한다.
        // 이미 열린 씬에 저장하지 않은 변경이 있으면 중단해 작업 내용을 보호한다.
        public static void ApplyToSavedDemoScene()
        {
            string path = Root + "/Scenes/Raycasting2D.unity";
            Scene scene = SceneManager.GetSceneByPath(path);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (!opened && scene.isDirty)
                throw new InvalidOperationException("Save the demo scene before applying its texture setup.");
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                ApplyToScene(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                if (opened) EditorSceneManager.CloseScene(scene, true);
            }
        }

        // 정사각형 한 칸을 충돌 영역으로 사용하는 타일을 복제의 기준으로 고른다.
        private static Tile FindGridTile(Tilemap map)
        {
            var used = new TileBase[map.GetUsedTilesCount()];
            map.GetUsedTilesNonAlloc(used);
            return used.OfType<Tile>().FirstOrDefault(tile => tile.colliderType == Tile.ColliderType.Grid);
        }

        // 기존 에셋은 재사용하고, 없을 때만 원본 Tile을 복제하여 새 에셋으로 저장한다.
        private static Tile CreateTile(string name, Tile source, Texture2D image)
        {
            string path = Root + "/Tiles/" + name + ".asset";
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile != null) return tile;
            tile = Object.Instantiate(source);
            tile.name = name;
            tile.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GetAssetPath(image));
            tile.color = Color.white;
            tile.colliderType = Tile.ColliderType.Grid;
            AssetDatabase.CreateAsset(tile, path);
            return tile;
        }

        // 플레이어의 앞쪽·오른쪽 방향을 기준으로 사물을 배치하고 이미지와 실제 높이를 연결한다.
        private static void CreateDemoSprite(Transform group, string name, Texture2D image, float height,
            Transform player, Tilemap map, float forward, float side)
        {
            if (group.Find(name) != null) return;
            Vector3 position = player.position + player.up * forward + player.right * side;
            // 벽 안에 사물을 만들지 않도록 가까운 통로를 찾는다.
            // 처음 고른 위치가 벽이면 조금씩 플레이어 쪽으로 당기며 빈 셀을 찾는다.
            bool found = false;
            for (int step = 0; step < 12; step++)
            {
                Vector3 candidate = position - player.up * (step * 0.2f);
                if (Vector2.Distance(candidate, player.position) < 0.6f) continue;
                if (map.HasTile(map.WorldToCell(candidate))) continue;
                position = candidate;
                found = true;
                break;
            }
            if (!found) return;
            var item = new GameObject(name);
            item.transform.SetParent(group, false);
            item.transform.position = position;
            Undo.RegisterCreatedObjectUndo(item, "Create billboard sprite");
            RaycastSprite2D sprite = Undo.AddComponent<RaycastSprite2D>(item);
            var settings = new SerializedObject(sprite);
            settings.FindProperty("image").objectReferenceValue = image;
            settings.FindProperty("height").floatValue = height;
            settings.FindProperty("elevation").floatValue = 0f;
            settings.ApplyModifiedProperties();
        }

        // 좌표별 색을 모아 PNG 파일을 만들고, Unity가 픽셀을 읽을 수 있게 Import Settings를 맞춘다.
        // 파일이 이미 있으면 그림을 덮어쓰지 않는다.
        private static Texture2D CreateImage(string name, Func<int, int, Color32> pixel)
        {
            string path = Root + "/Textures/" + name + ".png";
            if (!File.Exists(path))
            {
                var image = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
                var colors = new Color32[Size * Size];
                for (int y = 0; y < Size; y++)
                    for (int x = 0; x < Size; x++) colors[y * Size + x] = pixel(x, y);
                image.SetPixels32(colors);
                image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
                Object.DestroyImmediate(image);
                AssetDatabase.ImportAsset(path);
            }
            // Read/Write는 CPU에서 색을 읽기 위해, Point와 압축 끄기는 픽셀 무늬를 보존하기 위해 필요하다.
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (!importer.isReadable || importer.textureType != TextureImporterType.Sprite ||
                importer.filterMode != FilterMode.Point || importer.mipmapEnabled ||
                importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = Size;
                importer.isReadable = true;
                importer.alphaIsTransparency = true;
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                var spriteSettings = new TextureImporterSettings();
                importer.ReadTextureSettings(spriteSettings);
                spriteSettings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(spriteSettings);
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // 상위 폴더부터 만들며 Unity에도 폴더 에셋이 생겼음을 알린다.
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        // 좌표에 따라 작은 밝기 차이를 준다. 매번 같은 무늬가 나와 단색 표면에 질감을 더할 수 있다.
        private static int Grain(int x, int y) => ((x * 17 + y * 31 + x * y * 3) & 15) - 7;

        // 일정 간격의 줄눈과 번갈아 어긋난 가로 위치로 벽돌 무늬를 만든다.
        private static Color32 BrickPixel(int x, int y)
        {
            int row = y / 16;
            int offset = (x + (row % 2) * 16) % 32;
            if (y % 16 < 2 || offset < 2) return new Color32(53, 44, 43, 255);
            int shade = Grain(x, y) + (y % 16 == 2 ? 18 : 0) - (offset == 31 ? 18 : 0);
            return Shade(139, 70, 49, shade);
        }

        // 벽돌보다 큰 블록과 회색 계열 색을 사용해 돌벽을 만든다.
        private static Color32 StonePixel(int x, int y)
        {
            int row = y / 32;
            int offset = (x + row * 16) % 32;
            if (y % 32 < 2 || offset < 2) return new Color32(43, 49, 57, 255);
            int shade = Grain(x, y) + ((x / 32 + row) % 2) * 10 + (y % 32 == 2 ? 16 : 0);
            return Shade(113, 124, 132, shade);
        }

        // 가장자리, 고정 나사, 가운데의 경고 줄무늬를 좌표 범위로 구분한다.
        private static Color32 MetalPixel(int x, int y)
        {
            if (x < 2 || y < 2 || x > 61 || y > 61) return new Color32(34, 44, 49, 255);
            if ((x - 7) % 48 >= 0 && (x - 7) % 48 < 3 && (y - 7) % 48 >= 0 && (y - 7) % 48 < 3)
                return new Color32(180, 188, 172, 255);
            if (y >= 25 && y <= 38 && x >= 8 && x <= 55)
                return ((x + y) / 6) % 2 == 0 ? new Color32(174, 138, 53, 255) : new Color32(40, 44, 40, 255);
            return Shade(70, 100, 100, Grain(x, y) / 2 + (x < 5 || y > 59 ? 16 : 0));
        }

        // 통의 윤곽 안에 나무판과 금속 띠를 그린다. default는 알파가 0인 투명 픽셀이다.
        private static Color32 BarrelPixel(int x, int y)
        {
            if (y > 57) return default;
            float edge = 11f + Mathf.Abs(y - 29f) * 0.13f;
            if (x < edge || x >= 64f - edge) return default;
            if (y < 3 || y > 54) return new Color32(45, 29, 22, 255);
            int light = 20 - Mathf.Abs(x - 26) * 2;
            if (y >= 10 && y <= 15 || y >= 40 && y <= 45)
                return Shade(83, 94, 91, light / 2);
            if (x % 8 == 0) return new Color32(57, 35, 22, 255);
            return Shade(124, 79, 39, light + Grain(x, y) / 2);
        }

        // 아래는 손잡이, 위는 불꽃으로 나누고 바깥쪽은 투명하게 남긴다.
        private static Color32 TorchPixel(int x, int y)
        {
            if (y < 35 && x >= 28 && x <= 35)
                return Shade(90, 55, 29, x < 31 ? 15 : -10);
            if (y >= 30 && y <= 38 && x >= 24 && x <= 39)
                return new Color32(75, 80, 72, 255);
            float width = y < 45 ? (y - 33) * 0.8f : (63 - y) * 0.6f;
            float center = 32 + Mathf.Sin(y * 0.32f) * 3;
            if (y > 35 && Mathf.Abs(x - center) < width)
                return Mathf.Abs(x - 31) < width * 0.4f && y < 52
                    ? new Color32(255, 224, 118, 255) : new Color32(229, (byte)(83 + y), 38, 255);
            return default;
        }

        // RGB에 밝기를 더하되, 색상 값이 유효한 0~255 범위를 벗어나지 않게 제한한다.
        private static Color32 Shade(int r, int g, int b, int offset)
        {
            return new Color32((byte)Mathf.Clamp(r + offset, 0, 255),
                (byte)Mathf.Clamp(g + offset, 0, 255), (byte)Mathf.Clamp(b + offset, 0, 255), 255);
        }
    }
}
