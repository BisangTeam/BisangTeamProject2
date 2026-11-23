using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
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
    // 편집기에서 임시 씬을 만들어 렌더 결과를 검사하는 도구이며, 게임 실행에는 필요하지 않다.
    public static class RaycastTextureValidation
    {
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly Vector2 Origin = new Vector2(10000f, 10000f);

        [MenuItem("Hyunwoo/2D Raycasting/Validate Textures And Sprites")]
        public static void Validate()
        {
            if (EditorApplication.isPlaying)
                throw new InvalidOperationException("Run validation outside Play Mode.");

            var report = new StringBuilder();
            Directory.CreateDirectory("Logs");
            // 열려 있던 씬은 그대로 두고, 검사에 사용할 씬을 추가로 연다.
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var resources = new List<Object>();
            RaycastRenderer2D renderer = null;
            try
            {
                int layer = LayerMask.NameToLayer("RaycastCheckWall");
                if (layer < 0) layer = LayerMask.NameToLayer("Wall");
                Require(layer >= 0, "A raycasting wall layer exists.");
                Transform player = new GameObject("Validation Player").transform;
                player.position = Origin;
                var view = new GameObject("Validation View", typeof(RectTransform), typeof(RawImage))
                    .GetComponent<RawImage>();
                // 렌더러가 RawImage 비율을 따르므로 검사 화면도 320x180으로 지정한다.
                view.rectTransform.sizeDelta = new Vector2(320f, 180f);
                var rendererObject = new GameObject("Validation Renderer");
                rendererObject.SetActive(false);
                renderer = rendererObject.AddComponent<RaycastRenderer2D>();
                Set(renderer, "player", player);
                Set(renderer, "view", view);
                Set(renderer, "wallMask", (LayerMask)(1 << layer));
                Set(renderer, "wallColor", Color.white);
                Set(renderer, "shadowStrength", 0f);
                Invoke(renderer, "Awake");

                Texture2D gradient = Texture(resources, (x, y) => new Color32((byte)(y * 30 + 15), (byte)(240 - y * 30), 60, 255));
                Texture2D gray = Texture(resources, (x, y) => new Color32(120, 120, 120, 255));
                Texture2D red = Texture(resources, (x, y) => new Color32(240, 15, 15, 255));
                Texture2D blue = Texture(resources, (x, y) => new Color32(15, 15, 240, 255));
                Texture2D ring = Texture(resources, (x, y) => x >= 2 && x <= 5 && y >= 2 && y <= 5
                    ? new Color32(0, 0, 0, 0) : new Color32(240, 15, 15, 255));
                Texture2D translucent = Texture(resources, (x, y) => new Color32(240, 15, 15, 128));
                Set(renderer, "defaultWallTexture", gradient);

                // 줄무늬 벽에 가까이 가도 세로 텍스처 좌표가 유지되는지 검사한다.
                GameObject wall = Wall("Validation Wall", new Vector2(0f, 4f), new Vector2(80f, 1f), layer);
                Color32[] pixels = Render(renderer, view);
                int farColors = ColumnColors(pixels, 160);
                Require(farColors >= 7, "Wall texture contains multiple sampled rows.");
                wall.transform.position = Origin + new Vector2(0f, 0.75f);
                pixels = Render(renderer, view);
                int nearColors = ColumnColors(pixels, 160);
                Require(nearColors >= 2 && nearColors <= 4,
                    "A clipped close wall keeps its original texture scale (no vertical stretching).");
                report.AppendLine("PASS: textured wall sampling and close-wall vertical clipping.");
                wall.SetActive(false);

                // 나란한 두 타일에 다른 이미지를 연결해, 맞은 타일에 맞는 텍스처가 나오는지 확인한다.
                var grid = new GameObject("Validation Grid", typeof(Grid));
                grid.transform.position = Origin;
                var mapObject = new GameObject("Validation Tilemap", typeof(Tilemap), typeof(TilemapCollider2D));
                mapObject.transform.SetParent(grid.transform, false);
                mapObject.layer = layer;
                Tilemap map = mapObject.GetComponent<Tilemap>();
                Tile leftTile = ScriptableObject.CreateInstance<Tile>();
                Tile rightTile = ScriptableObject.CreateInstance<Tile>();
                resources.Add(leftTile);
                resources.Add(rightTile);
                leftTile.colliderType = rightTile.colliderType = Tile.ColliderType.Grid;
                map.SetTile(new Vector3Int(-1, 3, 0), leftTile);
                map.SetTile(new Vector3Int(0, 3, 0), rightTile);
                mapObject.GetComponent<TilemapCollider2D>().ProcessTilemapChanges();
                Set(renderer, "wallTilemap", map);
                SetWallTextures(renderer, leftTile, red, rightTile, blue);
                pixels = Render(renderer, view);
                Require(IsRed(pixels[90 * 320 + 145]) && IsBlue(pixels[90 * 320 + 175]),
                    "Neighboring tiles choose their own texture at the cell boundary.");
                report.AppendLine("PASS: tile-specific textures and collision-boundary cell selection.");
                mapObject.SetActive(false);
                Set(renderer, "wallTilemap", null);
                Set(renderer, "defaultWallTexture", gray);

                // 사물이 벽 앞에서는 보이고 뒤에서는 가려지며, 벽을 없애면 다시 보이는지 검사한다.
                wall.transform.position = Origin + new Vector2(0f, 6f);
                wall.SetActive(true);
                RaycastSprite2D sprite = Sprite("Validation Sprite", red, new Vector2(0f, 2f));
                pixels = Render(renderer, view);
                Require(CountRed(pixels) > 1000, "A sprite in front of the wall is visible.");
                sprite.transform.position = Origin + new Vector2(0f, 8f);
                Require(CountRed(Render(renderer, view)) == 0, "A sprite behind the wall is hidden.");
                wall.SetActive(false);
                Require(CountRed(Render(renderer, view)) > 100,
                    "A removed wall does not leave stale depth values.");
                report.AppendLine("PASS: front/behind wall occlusion and depth reset after wall removal.");

                // 벽 모서리에서는 사물 전체가 아니라 겹친 부분만 가려져야 한다.
                sprite.transform.position = Origin + new Vector2(0f, 3f);
                int whole = CountRed(Render(renderer, view));
                GameObject blocker = Wall("Validation Corner", new Vector2(-0.5f, 1.5f), Vector2.one, layer);
                pixels = Render(renderer, view);
                int partial = CountRed(pixels);
                Require(partial > whole / 4 && partial < whole * 3 / 4,
                    "A wall corner hides part of the sprite instead of the entire image.");
                File.WriteAllBytes("Logs/RaycastTexture-validation.png", ((Texture2D)view.texture).EncodeToPNG());
                report.AppendLine("PASS: per-column partial sprite occlusion at a wall corner. Preview saved.");
                blocker.SetActive(false);

                // 앞 이미지의 투명한 구멍으로 뒤 사물이 보이는지 확인한다.
                Set(sprite, "image", blue);
                RaycastSprite2D foreground = Sprite("Validation Transparent Sprite", ring, new Vector2(0f, 2f));
                pixels = Render(renderer, view);
                Require(IsBlue(pixels[90 * 320 + 160]) && IsRed(pixels[90 * 320 + 120]),
                    "Near sprites cover far sprites while transparent pixels reveal the image behind.");
                // 반투명 픽셀은 앞의 빨강과 뒤의 파랑이 섞여야 한다.
                Set(foreground, "image", translucent);
                Color32 blended = Render(renderer, view)[90 * 320 + 160];
                Require(blended.r > 70 && blended.r < 160 && blended.b > 70 && blended.b < 160 &&
                        blended.g < 30 && blended.a == 255,
                    "A half-transparent red sprite blends with the blue sprite behind it.");
                // 비활성화한 사물은 렌더링 대상에서도 빠져야 한다.
                Disable(foreground);
                Require(CountRed(Render(renderer, view)) == 0,
                    "Disabling a sprite removes it from the rendering list.");
                report.AppendLine("PASS: sprite depth ordering, transparent holes, alpha blending and disable registration.");

                // 중심이 화면 밖이어도 이미지 일부가 화면에 걸쳐 있으면 그 부분은 보여야 한다.
                Set(sprite, "image", red);
                sprite.transform.position = Origin + new Vector2(2.7f, 3f);
                Require(CountRed(Render(renderer, view)) > 100,
                    "A sprite whose center is outside the screen can still be partially visible.");
                sprite.transform.position = Origin + new Vector2(0f, -3f);
                Require(CountRed(Render(renderer, view)) == 0, "Sprites behind the viewer are skipped.");
                // 플레이어가 회전하면 새 정면에 있는 사물이 화면 중앙에 나타나야 한다.
                player.rotation = Quaternion.Euler(0f, 0f, -90f);
                sprite.transform.position = Origin + new Vector2(3f, 0f);
                Require(IsRed(Render(renderer, view)[90 * 320 + 160]),
                    "Rotating the player projects a new forward sprite to the screen center.");
                report.AppendLine("PASS: screen-edge clipping, behind-view culling and player rotation.");

                // 통합 씬의 왼쪽 2/3 영역은 1280x1080이다. 찌그러뜨리지 않고 세로 픽셀을 늘려야 한다.
                view.rectTransform.sizeDelta = new Vector2(1280f, 1080f);
                pixels = Render(renderer, view);
                Texture2D resized = (Texture2D)view.texture;
                Require(resized.width == 320 && resized.height == 270 && IsRed(pixels[135 * 320 + 160]),
                    "The split view fills its new aspect ratio and keeps the forward sprite centered.");
                Render(renderer, view);
                Require(view.texture == resized, "An unchanged view reuses its render texture.");
                view.rectTransform.sizeDelta = new Vector2(320f, 180f);
                Render(renderer, view);
                Require(((Texture2D)view.texture).height == 180, "Resizing back restores the original view ratio.");
                report.AppendLine("PASS: split-view aspect ratio, centered sprites and render texture reuse.");
                report.AppendLine("PASS: all textured raycasting checks completed.");
                Debug.Log(report.ToString());
            }
            catch (Exception exception)
            {
                Exception cause = exception is TargetInvocationException invocation && invocation.InnerException != null
                    ? invocation.InnerException : exception;
                report.AppendLine("FAIL: " + cause);
                Debug.LogError(report.ToString());
                throw;
            }
            finally
            {
                // 검사 도중 오류가 나도 임시 씬과 이미지를 지우고 원래 씬으로 돌아간다.
                if (renderer != null) Invoke(renderer, "OnDestroy");
                foreach (GameObject root in scene.GetRootGameObjects())
                    foreach (RaycastSprite2D sprite in root.GetComponentsInChildren<RaycastSprite2D>(true))
                        Invoke(sprite, "OnDisable");
                EditorSceneManager.CloseScene(scene, true);
                foreach (Object resource in resources) Object.DestroyImmediate(resource);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                File.WriteAllText("Logs/RaycastTexture-validation.txt", report.ToString(), Encoding.UTF8);
            }
        }

        private static Texture2D Texture(List<Object> resources, Func<int, int, Color32> color)
        {
            var image = new Texture2D(8, 8, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var pixels = new Color32[64];
            for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++) pixels[y * 8 + x] = color(x, y);
            image.SetPixels32(pixels);
            image.Apply();
            resources.Add(image);
            return image;
        }

        private static GameObject Wall(string name, Vector2 offset, Vector2 size, int layer)
        {
            var wall = new GameObject(name) { layer = layer };
            wall.transform.position = Origin + offset;
            wall.AddComponent<BoxCollider2D>().size = size;
            return wall;
        }

        private static RaycastSprite2D Sprite(string name, Texture2D image, Vector2 offset)
        {
            var item = new GameObject(name);
            item.SetActive(false);
            item.transform.position = Origin + offset;
            RaycastSprite2D sprite = item.AddComponent<RaycastSprite2D>();
            Set(sprite, "image", image);
            item.SetActive(true);
            Invoke(sprite, "OnEnable");
            return sprite;
        }

        private static void Disable(RaycastSprite2D sprite)
        {
            sprite.gameObject.SetActive(false);
            Invoke(sprite, "OnDisable");
        }

        private static Color32[] Render(RaycastRenderer2D renderer, RawImage view)
        {
            Physics2D.SyncTransforms();
            Invoke(renderer, "RenderFrame");
            return ((Texture2D)view.texture).GetPixels32();
        }

        private static void SetWallTextures(RaycastRenderer2D renderer, TileBase first, Texture2D firstImage,
            TileBase second, Texture2D secondImage)
        {
            // 리플렉션으로 private 연결 목록을 설정한다. 검사를 위한 접근이며 게임 로직에서는 쓰지 않는다.
            FieldInfo field = renderer.GetType().GetField("wallTextures", Fields);
            Type entryType = field.FieldType.GetElementType();
            Array entries = Array.CreateInstance(entryType, 2);
            TileBase[] tiles = { first, second };
            Texture2D[] images = { firstImage, secondImage };
            for (int i = 0; i < 2; i++)
            {
                object entry = Activator.CreateInstance(entryType);
                entryType.GetField("tile", Fields).SetValue(entry, tiles[i]);
                entryType.GetField("texture", Fields).SetValue(entry, images[i]);
                entries.SetValue(entry, i);
            }
            field.SetValue(renderer, entries);
            Invoke(renderer, "SetTileTexturesDict");
        }

        private static int ColumnColors(Color32[] pixels, int x)
        {
            var colors = new HashSet<Color32>();
            for (int y = 0; y < 180; y++) colors.Add(pixels[y * 320 + x]);
            return colors.Count;
        }

        private static int CountRed(Color32[] pixels)
        {
            int count = 0;
            foreach (Color32 pixel in pixels) if (IsRed(pixel)) count++;
            return count;
        }

        private static bool IsRed(Color32 pixel) => pixel.r > 30 && pixel.r > pixel.g * 2 && pixel.r > pixel.b * 2;
        private static bool IsBlue(Color32 pixel) => pixel.b > 30 && pixel.b > pixel.r * 2 && pixel.b > pixel.g * 2;
        // 테스트에서만 private 필드를 설정하거나 private 메서드를 직접 호출하기 위한 헬퍼다.
        private static void Set(object target, string field, object value) => target.GetType().GetField(field, Fields).SetValue(target, value);
        private static void Invoke(object target, string method) => target.GetType().GetMethod(method, Fields)?.Invoke(target, null);
        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
