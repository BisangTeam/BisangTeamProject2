using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Hyunwoo.Raycasting2D.Scripts;

namespace Hyunwoo.Raycasting2D.Editor
{
    // 배치와 이미지 준비만 담당한다. 실제 게임에서는 Scripts 폴더의 컴포넌트가 동작한다.
    public static class RaycastEnvironmentDemoSetup
    {
        public const string DemoScenePath = "Assets/Hyunwoo/Raycasting2D/Scenes/RaycastingEnvironmentDemo.unity";
        private const string OriginalScenePath = "Assets/Hyunwoo/Raycasting2D/Scenes/Raycasting2D.unity";
        private const string CurrentScenePath = "Assets/Hyunwoo/Scene2/HyunwooTestScene2.unity";
        private const string TextureRoot = "Assets/Hyunwoo/Raycasting2D/Textures";
        private const string GroupName = "Raycast Environment";
        private const int Size = 64;
        private static readonly Vector3Int[] Neighbors =
        {
            Vector3Int.right, Vector3Int.up, Vector3Int.left, Vector3Int.down
        };

        [MenuItem("Hyunwoo/2D Raycasting/Apply Environment To Open Scene")]
        public static void ApplyToOpenScene()
        {
            ApplyToScene(SceneManager.GetActiveScene());
        }

        [MenuItem("Hyunwoo/2D Raycasting/Create Environment Demo Scene")]
        public static void CreateEnvironmentDemoScene()
        {
            RequireEditMode();
            // 기존 맵을 복사하므로 원래 데모와 사용자가 편집하던 씬은 덮어쓰지 않는다.
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(DemoScenePath) == null &&
                !AssetDatabase.CopyAsset(OriginalScenePath, DemoScenePath))
                throw new IOException("Could not create " + DemoScenePath);
            ApplyToSavedScene(DemoScenePath);
        }

        // 배치 자동화/일괄 검사에서도 같은 준비 과정을 사용할 수 있다.
        public static void ApplyToSavedScenes()
        {
            RequireEditMode();
            if (File.Exists(CurrentScenePath)) ApplyToSavedScene(CurrentScenePath);
            CreateEnvironmentDemoScene();
            AssetDatabase.SaveAssets();
            Debug.Log("Environment setup saved: " + CurrentScenePath + ", " + DemoScenePath);
        }

        private static void ApplyToSavedScene(string path)
        {
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = SceneManager.GetSceneByPath(path);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (!opened && scene.isDirty)
                throw new InvalidOperationException("Save the open scene before applying saved setup: " + path);
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                ApplyToScene(scene);
                if (!EditorSceneManager.SaveScene(scene, path))
                    throw new IOException("Could not save " + path);
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                if (opened) EditorSceneManager.CloseScene(scene, true);
            }
        }

        public static void ApplyToScene(Scene scene)
        {
            RequireEditMode();
            RaycastRenderer2D renderer = FindInScene<RaycastRenderer2D>(scene).FirstOrDefault();
            if (renderer == null) throw new InvalidOperationException("This scene has no RaycastRenderer2D.");
            var settings = new SerializedObject(renderer);
            Transform player = settings.FindProperty("player").objectReferenceValue as Transform;
            Tilemap map = settings.FindProperty("wallTilemap").objectReferenceValue as Tilemap;
            int wallMask = settings.FindProperty("wallMask").intValue;
            if (player == null || map == null || wallMask == 0)
                throw new InvalidOperationException("Assign the renderer's Player, Wall Tilemap and Wall Mask first.");

            Texture2D floor = CreateImage("FloorTiles", FloorPixel);
            Texture2D ceiling = CreateImage("CeilingPanels", CeilingPixel);
            Texture2D window = CreateImage("Window", WindowPixel);
            Texture2D door = CreateImage("Door", DoorPixel);
            Texture2D lamp = CreateImage("CeilingLamp", LampPixel);
            float wallHeight = settings.FindProperty("wallHeight").floatValue;

            Undo.SetCurrentGroupName("Set Up Raycast Environment");
            int undoGroup = Undo.GetCurrentGroup();
            Undo.RecordObject(renderer, "Assign floor and ceiling textures");
            settings.FindProperty("floorTexture").objectReferenceValue = floor;
            settings.FindProperty("ceilingTexture").objectReferenceValue = ceiling;
            settings.FindProperty("surfaceTileSize").floatValue = 1f;
            settings.ApplyModifiedProperties();

            RaycastDoorInteractor2D interactor = player.GetComponent<RaycastDoorInteractor2D>();
            if (interactor == null) interactor = Undo.AddComponent<RaycastDoorInteractor2D>(player.gameObject);
            var interaction = new SerializedObject(interactor);
            Undo.RecordObject(interactor, "Set door interaction distance");
            interaction.FindProperty("wallMask").intValue = wallMask;
            interaction.FindProperty("interactionDistance").floatValue = 1.6f;
            interaction.ApplyModifiedProperties();

            Transform group = scene.GetRootGameObjects().FirstOrDefault(item => item.name == GroupName)?.transform;
            if (group == null)
            {
                var container = new GameObject(GroupName);
                SceneManager.MoveGameObjectToScene(container, scene);
                Undo.RegisterCreatedObjectUndo(container, "Create environment group");
                group = container.transform;
            }
            map.GetComponent<TilemapCollider2D>()?.ProcessTilemapChanges();
            Physics2D.SyncTransforms();
            List<Vector3Int> reachable = ReachableCells(map, player.position);
            if (reachable.Count == 0) throw new InvalidOperationException("No empty map cell is reachable from the player.");

            // 같은 이름이 있으면 보존한다. 다시 실행해도 직접 옮겨 둔 샘플을 되돌리지 않는다.
            if (group.Find("Wall Window") == null)
                CreateWindow(group, player, map, reachable, window, wallHeight, wallMask);
            if (group.Find("Sliding Door") == null)
                CreateDoor(group, player, map, reachable, door, wallMask);
            if (group.Find("Ceiling Lamp") == null)
                CreateLamp(group, player, map, reachable, lamp, wallHeight);

            RawImage view = settings.FindProperty("view").objectReferenceValue as RawImage;
            if (view != null)
            {
                foreach (Text text in view.GetComponentsInChildren<Text>(true))
                {
                    if (!text.text.Contains("WASD") || text.text.Contains("F: Door")) continue;
                    Undo.RecordObject(text, "Add door control hint");
                    text.text += "     F: Door";
                }
            }
            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("Ready: floor/ceiling textures, wall window, sliding door (F), ceiling lamp. " + scene.path, renderer);
        }

        private static IEnumerable<T> FindInScene<T>(Scene scene) where T : Component
        {
            return scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true));
        }

        private static List<Vector3Int> ReachableCells(Tilemap map, Vector3 position)
        {
            Vector3Int start = map.WorldToCell(position);
            BoundsInt bounds = map.cellBounds;
            if (!bounds.Contains(start) || map.HasTile(start))
            {
                var empty = new List<Vector3Int>();
                foreach (Vector3Int cell in bounds.allPositionsWithin)
                    if (!map.HasTile(cell)) empty.Add(cell);
                if (empty.Count == 0) return new List<Vector3Int>();
                start = empty.OrderBy(cell => (map.GetCellCenterWorld(cell) - position).sqrMagnitude).First();
            }
            var result = new List<Vector3Int>();
            var queue = new Queue<Vector3Int>();
            var visited = new HashSet<Vector3Int> { start };
            queue.Enqueue(start);
            while (queue.Count > 0 && result.Count < 20000)
            {
                Vector3Int cell = queue.Dequeue();
                result.Add(cell);
                foreach (Vector3Int direction in Neighbors)
                {
                    Vector3Int next = cell + direction;
                    if (!bounds.Contains(next) || map.HasTile(next) || !visited.Add(next)) continue;
                    queue.Enqueue(next);
                }
            }
            return result;
        }

        private static void CreateWindow(Transform group, Transform player, Tilemap map,
            List<Vector3Int> reachable, Texture2D image, float wallHeight, int mask)
        {
            Vector3 bestPosition = default;
            Vector2 bestNormal = Vector2.up;
            float bestWidth = 0.8f, bestScore = float.MaxValue;
            foreach (Vector3Int cell in reachable)
            {
                Vector3 center = map.GetCellCenterWorld(cell);
                foreach (Vector3Int direction in Neighbors)
                {
                    Vector3Int wall = cell + direction;
                    if (!map.HasTile(wall)) continue;
                    Vector3 wallCenter = map.GetCellCenterWorld(wall);
                    Vector2 normal = (center - wallCenter).normalized;
                    Vector3 position = (center + wallCenter) * 0.5f + (Vector3)normal * 0.01f;
                    Vector2 offset = position - player.position;
                    if (offset.magnitude < 1f) continue;
                    float score = offset.magnitude + (Vector2.Dot(offset.normalized, player.up) < 0.2f ? 6f : 0f);
                    // 시작점에서 실제로 보이는 벽면을 우선한다. 다른 벽 뒤의 창문은 피한다.
                    if (Physics2D.Raycast(player.position, offset.normalized, Mathf.Max(0f, offset.magnitude - 0.03f), mask).collider != null)
                        score += 12f;
                    if (score >= bestScore) continue;
                    bestScore = score;
                    bestPosition = position;
                    bestNormal = normal;
                    Vector3Int across = direction.x != 0 ? Vector3Int.up : Vector3Int.right;
                    bestWidth = (map.GetCellCenterWorld(cell + across) - center).magnitude * 0.82f;
                }
            }
            if (bestScore == float.MaxValue) return;
            GameObject item = CreateItem(group, "Wall Window", bestPosition);
            item.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(bestNormal.y, bestNormal.x) * Mathf.Rad2Deg - 90f);
            RaycastWallDecoration2D decoration = Undo.AddComponent<RaycastWallDecoration2D>(item);
            var settings = new SerializedObject(decoration);
            settings.FindProperty("image").objectReferenceValue = image;
            settings.FindProperty("width").floatValue = bestWidth;
            settings.FindProperty("height").floatValue = wallHeight * 0.5f;
            settings.FindProperty("elevation").floatValue = wallHeight * 0.3f;
            settings.FindProperty("twoSided").boolValue = false;
            settings.ApplyModifiedProperties();
            AddMapMarker(item, image, new Vector2(bestWidth, 0.06f));
        }

        private static void CreateDoor(Transform group, Transform player, Tilemap map,
            List<Vector3Int> reachable, Texture2D image, int wallMask)
        {
            Vector3Int bestCell = default;
            Vector3Int bestAcross = Vector3Int.right;
            float bestScore = float.MaxValue;
            foreach (Vector3Int cell in reachable)
            {
                Vector3 center = map.GetCellCenterWorld(cell);
                Vector2 offset = center - player.position;
                if (offset.magnitude < 1.6f) continue;
                foreach (Vector3Int across in new[] { Vector3Int.right, Vector3Int.up })
                {
                    Vector3Int along = across.x != 0 ? Vector3Int.up : Vector3Int.right;
                    if (map.HasTile(cell + along) || map.HasTile(cell - along)) continue;
                    bool corridor = map.HasTile(cell + across) && map.HasTile(cell - across);
                    // 막힌 옆 칸은 문이 숨는 주머니가 된다. 넓은 공간에서는 옆 칸을 비워 둔다.
                    if (!corridor && (map.HasTile(cell + across) || map.HasTile(cell + across + across))) continue;
                    float score = offset.magnitude + (corridor ? 0f : 15f) +
                                  (Vector2.Dot(offset.normalized, player.up) < 0.1f ? 4f : 0f);
                    if (score >= bestScore) continue;
                    bestScore = score;
                    bestCell = cell;
                    bestAcross = across;
                }
            }
            if (bestScore == float.MaxValue)
            {
                Debug.LogWarning("No safe door placement was found; place a door manually in an empty corridor.");
                return;
            }
            Vector3 position = map.GetCellCenterWorld(bestCell);
            Vector3 acrossAxis = map.GetCellCenterWorld(bestCell + bestAcross) - position;
            float width = acrossAxis.magnitude;
            GameObject item = CreateItem(group, "Sliding Door", position);
            item.layer = Enumerable.Range(0, 32).First(layer => (wallMask & (1 << layer)) != 0);
            item.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(acrossAxis.y, acrossAxis.x) * Mathf.Rad2Deg);
            BoxCollider2D box = Undo.AddComponent<BoxCollider2D>(item);
            box.size = new Vector2(width, 0.12f);
            var surface = new SerializedObject(Undo.AddComponent<RaycastWallSurface2D>(item));
            surface.FindProperty("image").objectReferenceValue = image;
            surface.ApplyModifiedProperties();
            var door = new SerializedObject(Undo.AddComponent<RaycastDoor2D>(item));
            door.FindProperty("slideOffset").vector2Value = new Vector2(width * 1.05f, 0f);
            door.FindProperty("openSpeed").floatValue = 1.5f;
            door.FindProperty("startOpen").boolValue = false;
            door.ApplyModifiedProperties();
            AddMapMarker(item, image, box.size);
            Debug.Log("Sliding door placed at " + position.ToString("F2") + "; approach it and press F.", item);
        }

        private static void CreateLamp(Transform group, Transform player, Tilemap map,
            List<Vector3Int> reachable, Texture2D image, float wallHeight)
        {
            Vector3 desired = player.position + player.up * 2f;
            Vector3 position = reachable.Select(map.GetCellCenterWorld)
                .Where(point => Vector2.Distance(point, player.position) > 0.8f)
                .OrderBy(point => (point - desired).sqrMagnitude).FirstOrDefault();
            GameObject item = CreateItem(group, "Ceiling Lamp", position);
            var settings = new SerializedObject(Undo.AddComponent<RaycastSprite2D>(item));
            settings.FindProperty("image").objectReferenceValue = image;
            settings.FindProperty("height").floatValue = wallHeight * 0.32f;
            settings.FindProperty("elevation").floatValue = wallHeight * 0.68f;
            settings.ApplyModifiedProperties();
            AddMapMarker(item, image, Vector2.one * 0.22f);
        }

        private static GameObject CreateItem(Transform group, string name, Vector3 position)
        {
            var item = new GameObject(name);
            item.transform.SetParent(group, false);
            item.transform.position = position;
            Undo.RegisterCreatedObjectUndo(item, "Create " + name);
            return item;
        }

        private static void AddMapMarker(GameObject item, Texture2D image, Vector2 size)
        {
            SpriteRenderer marker = Undo.AddComponent<SpriteRenderer>(item);
            marker.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GetAssetPath(image));
            marker.drawMode = SpriteDrawMode.Sliced;
            marker.size = size;
            marker.sortingOrder = 4;
        }

        private static Texture2D CreateImage(string name, Func<int, int, Color32> pixel)
        {
            string path = TextureRoot + "/" + name + ".png";
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
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            }
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
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static int Grain(int x, int y) => ((x * 17 + y * 23 + x * y) & 7) - 3;

        private static Color32 FloorPixel(int x, int y)
        {
            if (x % 32 < 2 || y % 32 < 2) return new Color32(35, 41, 45, 255);
            int variation = Grain(x, y) + ((x / 32 + y / 32) % 2) * 12;
            if (x % 32 == 2 || y % 32 == 31) variation += 14;
            return Shade(91, 103, 108, variation);
        }

        private static Color32 CeilingPixel(int x, int y)
        {
            if (x < 2 || y < 2 || x > 61 || y > 61) return new Color32(35, 47, 53, 255);
            if (x >= 17 && x <= 46 && y >= 22 && y <= 41)
                return y % 4 < 2 ? new Color32(39, 51, 59, 255) : new Color32(74, 88, 94, 255);
            return Shade(103, 117, 123, Grain(x, y));
        }

        private static Color32 WindowPixel(int x, int y)
        {
            if (x < 3 || x > 60 || y < 3 || y > 60) return default;
            if (x < 7 || x > 56 || y < 7 || y > 56) return Shade(57, 46, 35, Grain(x, y));
            if (x >= 30 && x <= 33 || y >= 30 && y <= 33) return new Color32(51, 42, 32, 255);
            if (Mathf.Abs((x + y) % 36 - 18) < 2) return new Color32(146, 191, 202, 255);
            return Shade(57, 103, 125, (y / 8) * 3 + Grain(x, y));
        }

        private static Color32 DoorPixel(int x, int y)
        {
            if (x < 3 || x > 60 || y < 3 || y > 60) return new Color32(33, 43, 47, 255);
            if (x < 6 || x > 57 || y < 6 || y > 57) return new Color32(106, 124, 126, 255);
            if (y >= 26 && y <= 37 && x >= 9 && x <= 54)
                return ((x + y) / 7) % 2 == 0 ? new Color32(177, 145, 58, 255) : new Color32(38, 44, 41, 255);
            if (x >= 47 && x <= 50 && y >= 16 && y <= 23) return new Color32(183, 191, 167, 255);
            if (x % 16 == 0) return new Color32(42, 61, 67, 255);
            return Shade(65, 90, 99, Grain(x, y));
        }

        private static Color32 LampPixel(int x, int y)
        {
            if (y >= 39 && x >= 30 && x <= 33) return new Color32(62, 65, 61, 255);
            if (y < 16 || y > 39) return default;
            float halfWidth = 10f + (39 - y) * 0.75f;
            if (Mathf.Abs(x - 31.5f) > halfWidth) return default;
            if (y <= 19) return new Color32(252, 227, 157, 255);
            if (y <= 22) return new Color32(66, 70, 60, 255);
            return Shade(92, 112, 103, (x < 31 ? 16 : -7) + Grain(x, y));
        }

        private static Color32 Shade(int r, int g, int b, int amount)
        {
            return new Color32((byte)Mathf.Clamp(r + amount, 0, 255),
                (byte)Mathf.Clamp(g + amount, 0, 255), (byte)Mathf.Clamp(b + amount, 0, 255), 255);
        }

        private static void RequireEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before setting up the environment.");
        }
    }
}
