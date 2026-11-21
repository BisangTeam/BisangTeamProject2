using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Hyunwoo.Raycasting2D.Editor
{
    // 실제 씬을 건드리지 않고 임시 씬에서 렌더링과 문 충돌을 검사한다.
    public static class RaycastEnvironmentValidation
    {
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly Vector2 Origin = new Vector2(20000f, 20000f);

        [MenuItem("Hyunwoo/2D Raycasting/Validate Environment")]
        public static void Validate()
        {
            if (EditorApplication.isPlaying)
                throw new InvalidOperationException("Run validation outside Play Mode.");

            Directory.CreateDirectory("Logs");
            var report = new StringBuilder();
            var resources = new List<Object>();
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            RaycastRenderer2D renderer = null;
            try
            {
                int layer = LayerMask.NameToLayer("RaycastCheckWall");
                Require(layer >= 0, "The RaycastCheckWall layer exists.");
                Transform player = new GameObject("Environment Validation Player").transform;
                player.position = Origin;
                RawImage view = new GameObject("Environment Validation View", typeof(RectTransform), typeof(RawImage))
                    .GetComponent<RawImage>();
                view.rectTransform.sizeDelta = new Vector2(320f, 180f);
                var rendererObject = new GameObject("Environment Validation Renderer");
                rendererObject.SetActive(false);
                renderer = rendererObject.AddComponent<RaycastRenderer2D>();
                Set(renderer, "player", player);
                Set(renderer, "view", view);
                Set(renderer, "wallMask", (LayerMask)(1 << layer));
                Set(renderer, "shadowStrength", 0f);
                Invoke(renderer, "Awake");

                Texture2D red = Texture(resources, (x, y) => new Color32(240, 15, 15, 255));
                Texture2D blue = Texture(resources, (x, y) => new Color32(15, 15, 240, 255));
                Texture2D gray = Texture(resources, (x, y) => new Color32(120, 120, 120, 255));
                Texture2D stripes = Texture(resources, (x, y) => x < 4
                    ? new Color32(240, 15, 15, 255) : new Color32(15, 240, 15, 255));
                Texture2D frame = Texture(resources, (x, y) => x >= 2 && x < 6 && y >= 2 && y < 6
                    ? new Color32(0, 0, 0, 0) : new Color32(240, 15, 15, 255));

                ValidateDistanceDarkness(renderer, view, gray, layer, report);

                // 텍스처가 없을 때는 기존 단색 바닥/천장을 그대로 사용한다.
                Color32[] pixels = Render(renderer, view);
                Require(Same(pixels[160], (Color32)Read<Color>(renderer, "floorColor")) &&
                        Same(pixels[179 * 320 + 160], (Color32)Read<Color>(renderer, "ceilingColor")),
                    "Missing floor/ceiling textures preserve the configured background colors.");
                Set(renderer, "floorTexture", red);
                Set(renderer, "ceilingTexture", blue);
                pixels = Render(renderer, view);
                Require(IsRed(pixels[160]) && IsBlue(pixels[179 * 320 + 160]),
                    "Floor and ceiling sample their own textures, including the screen edges.");
                Require(pixels[89 * 320 + 160].a == 255 && pixels[90 * 320 + 160].a == 255,
                    "The two rows next to the horizon remain valid opaque pixels.");

                Set(renderer, "floorTexture", stripes);
                Set(renderer, "surfaceTileSize", 1f);
                Color32[] beforeMove = Render(renderer, view);
                player.position = Origin + new Vector2(0.375f, 0f);
                Color32[] afterMove = Render(renderer, view);
                int changedSamples = 0;
                for (int y = 0; y < 70; y++)
                    for (int x = 0; x < 320; x++)
                        if (IsRed(beforeMove[y * 320 + x]) != IsRed(afterMove[y * 320 + x])) changedSamples++;
                Require(changedSamples > 2000,
                    "Floor markings are anchored in the world and move across the view when the player translates.");
                player.position = Origin;
                view.rectTransform.sizeDelta = new Vector2(320f, 181f);
                pixels = Render(renderer, view);
                Texture2D oddView = (Texture2D)view.texture;
                Require(oddView.height == 181 && pixels[90 * 320 + 160].a == 255,
                    "An odd-height view renders its exact horizon without dividing by zero.");
                Render(renderer, view);
                Require(view.texture == oddView, "An unchanged view reuses its allocated render texture.");
                view.rectTransform.sizeDelta = new Vector2(320f, 180f);
                Render(renderer, view);
                Require(((Texture2D)view.texture).height == 180,
                    "Resizing the view back restores its original aspect ratio.");
                report.AppendLine("PASS: optional floor/ceiling textures, horizon handling and world-anchored floor sampling.");

                // 벽 부착물은 일반 사물처럼 카메라를 따라 회전하지 않아야 한다.
                Set(renderer, "floorTexture", null);
                Set(renderer, "ceilingTexture", null);
                Set(renderer, "defaultWallTexture", gray);
                GameObject wall = Wall("Environment Validation Back Wall", new Vector2(0f, 4.2f),
                    new Vector2(20f, 0.2f), layer);
                Require(IsGray(Render(renderer, view)[90 * 320 + 160]),
                    "A standalone wall collider renders without a Tilemap reference.");
                RaycastWallDecoration2D decoration = Decoration(frame, new Vector2(0f, 3f));
                Set(decoration, "width", 1.2f);
                Set(decoration, "height", 0.6f);
                Set(decoration, "elevation", 0.2f);
                pixels = Render(renderer, view);
                Require(Count(pixels, IsRed) > 100 && IsGray(pixels[90 * 320 + 160]),
                    "A wall decoration draws its opaque frame and reveals the wall through transparent pixels.");
                Set(decoration, "image", red);
                int facingArea = Count(Render(renderer, view), IsRed);
                decoration.transform.rotation = Quaternion.Euler(0f, 0f, 60f);
                int angledArea = Count(Render(renderer, view), IsRed);
                Require(angledArea > facingArea * 0.2f && angledArea < facingArea * 0.85f,
                    "An angled decoration narrows with perspective instead of behaving as a billboard.");
                decoration.transform.rotation = Quaternion.identity;
                wall.transform.position = Origin + new Vector2(0f, 2f);
                Require(Count(Render(renderer, view), IsRed) == 0,
                    "A nearer wall completely hides a decoration behind it.");
                wall.transform.position = Origin + new Vector2(0f, 4.2f);

                RaycastSprite2D sprite = Sprite(blue, new Vector2(0f, 2f), 0.6f, 0.2f);
                Require(IsBlue(Render(renderer, view)[90 * 320 + 160]),
                    "A foreground billboard covers the wall decoration.");
                sprite.transform.position = Origin + new Vector2(0f, 3.6f);
                Require(IsRed(Render(renderer, view)[90 * 320 + 160]),
                    "An opaque wall decoration covers a billboard behind it.");
                decoration.transform.position = Origin + new Vector2(0f, -2f);
                sprite.gameObject.SetActive(false);
                Invoke(sprite, "OnDisable");
                Require(Count(Render(renderer, view), IsRed) == 0,
                    "A decoration behind the viewer is skipped safely.");
                report.AppendLine("PASS: wall attachment alpha holes, oriented perspective and wall/sprite depth ordering.");

                // 높이를 올린 이미지는 천장 가까이에 보이며 바닥 쪽에는 나타나지 않는다.
                decoration.gameObject.SetActive(false);
                Invoke(decoration, "OnDisable");
                wall.SetActive(false);
                RaycastSprite2D lamp = Sprite(red, new Vector2(0f, 1.5f), 0.3f, 0.7f);
                pixels = Render(renderer, view);
                int above = 0;
                int below = 0;
                for (int y = 0; y < 180; y++)
                    for (int x = 0; x < 320; x++)
                        if (IsRed(pixels[y * 320 + x]))
                        {
                            if (y >= 90) above++;
                            else below++;
                        }
                Require(above > 100 && below == 0,
                    "A height-0.3 object at elevation 0.7 hangs at the ceiling above the horizon.");
                File.WriteAllBytes("Logs/RaycastEnvironment-validation.png", ((Texture2D)view.texture).EncodeToPNG());
                lamp.gameObject.SetActive(false);
                Invoke(lamp, "OnDisable");
                report.AppendLine("PASS: elevated ceiling object placement.");

                ValidateDoor(renderer, view, player, blue, gray, layer, report);
                report.AppendLine("PASS: all environment checks completed.");
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
                if (renderer != null) Invoke(renderer, "OnDestroy");
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (RaycastSprite2D sprite in root.GetComponentsInChildren<RaycastSprite2D>(true))
                        Invoke(sprite, "OnDisable");
                    foreach (RaycastWallDecoration2D decoration in root.GetComponentsInChildren<RaycastWallDecoration2D>(true))
                        Invoke(decoration, "OnDisable");
                    foreach (RaycastDoor2D door in root.GetComponentsInChildren<RaycastDoor2D>(true))
                        Invoke(door, "OnDisable");
                }
                EditorSceneManager.CloseScene(scene, true);
                foreach (Object resource in resources) Object.DestroyImmediate(resource);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                File.WriteAllText("Logs/RaycastEnvironment-validation.txt", report.ToString(), Encoding.UTF8);
            }
        }

        private static void ValidateDistanceDarkness(RaycastRenderer2D renderer, RawImage view,
            Texture2D gray, int layer, StringBuilder report)
        {
            const int center = 90 * 320 + 160;
            Color32 basePixel = gray.GetPixels32()[0];
            Color floor = Read<Color>(renderer, "floorColor");
            Color ceiling = Read<Color>(renderer, "ceilingColor");
            float range = Read<float>(renderer, "maxDistance");
            float start = Read<float>(renderer, "shadowStartDistance");
            float end = Read<float>(renderer, "shadowEndDistance");
            Texture2D wallImage = Read<Texture2D>(renderer, "defaultWallTexture");
            var group = new GameObject("Distance Darkness Validation");
            try
            {
                Set(renderer, "shadowStrength", 1f);
                Set(renderer, "shadowStartDistance", 2f);
                Set(renderer, "shadowEndDistance", 8f);
                Set(renderer, "defaultWallTexture", gray);
                GameObject wall = Wall("Darkness Validation Wall", new Vector2(0f, 1.5f),
                    new Vector2(20f, 0.2f), layer);
                wall.transform.SetParent(group.transform, true);
                Require(Same(Render(renderer, view)[center], basePixel),
                    "A wall before the darkness start retains its original color.");
                wall.transform.position = Origin + new Vector2(0f, 9f);
                Require(IsBlack(Render(renderer, view)[center]),
                    "A wall past the darkness end renders exact opaque black.");
                wall.transform.position = Origin + new Vector2(0f, 4f);
                Color32 middle = Render(renderer, view)[center];
                Require(middle.r > 0 && middle.r < basePixel.r,
                    "A wall between the start and end fades gradually.");
                Set(renderer, "shadowStrength", 3f);
                Require(IsBlack(Render(renderer, view)[center]),
                    "Increasing strength brings full darkness closer to the same wall.");
                Set(renderer, "shadowStrength", 0f);
                wall.transform.position = Origin + new Vector2(0f, 9f);
                Require(Same(Render(renderer, view)[center], basePixel),
                    "Zero strength restores a distant wall's original color.");
                wall.transform.rotation = Quaternion.Euler(0f, 0f, 45f);
                byte side = (byte)(basePixel.r * Read<float>(renderer, "sideWallBrightness"));
                Require(Same(Render(renderer, view)[center], new Color32(side, side, side, 255)),
                    "Zero distance strength preserves the wall's direction shading.");
                wall.SetActive(false);

                Set(renderer, "shadowStrength", 1f);
                RaycastSprite2D sprite = Sprite(gray, new Vector2(0f, 1.5f), 0.6f, 0.2f);
                sprite.transform.SetParent(group.transform, true);
                Require(Same(Render(renderer, view)[center], basePixel),
                    "A nearby billboard retains its original color.");
                sprite.transform.position = Origin + new Vector2(0f, 9f);
                Require(IsBlack(Render(renderer, view)[center]),
                    "A distant billboard fades to exact opaque black.");
                Set(renderer, "shadowStrength", 0f);
                Require(Same(Render(renderer, view)[center], basePixel),
                    "Zero strength restores the distant billboard.");
                sprite.gameObject.SetActive(false);
                Invoke(sprite, "OnDisable");

                Set(renderer, "shadowStrength", 1f);
                RaycastWallDecoration2D decoration = Decoration(gray, new Vector2(0f, 1.5f));
                decoration.transform.SetParent(group.transform, true);
                Require(Same(Render(renderer, view)[center], basePixel),
                    "A nearby wall decoration retains its original color.");
                decoration.transform.position = Origin + new Vector2(0f, 9f);
                Require(IsBlack(Render(renderer, view)[center]),
                    "A distant wall decoration fades to exact opaque black.");
                Set(renderer, "shadowStrength", 0f);
                Require(Same(Render(renderer, view)[center], basePixel),
                    "Zero strength restores the distant wall decoration.");
                decoration.gameObject.SetActive(false);
                Invoke(decoration, "OnDisable");

                Set(renderer, "shadowStrength", 1f);
                Set(renderer, "floorColor", new Color(0.8f, 0.6f, 0.4f, 1f));
                Set(renderer, "ceilingColor", new Color(0.4f, 0.6f, 0.8f, 1f));
                Color32[] pixels = Render(renderer, view);
                Require(Same(pixels[160], (Color32)Read<Color>(renderer, "floorColor")) &&
                        Same(pixels[179 * 320 + 160], (Color32)Read<Color>(renderer, "ceilingColor")),
                    "Nearby untextured floor and ceiling retain their configured colors.");
                Require(IsBlack(pixels[78 * 320 + 160]) && IsBlack(pixels[101 * 320 + 160]) &&
                        IsBlack(pixels[89 * 320 + 160]) && IsBlack(pixels[center]),
                    "Untextured floor, ceiling and horizon fade to exact opaque black.");
                Set(renderer, "floorTexture", gray);
                Set(renderer, "ceilingTexture", gray);
                pixels = Render(renderer, view);
                Require(Same(pixels[160], basePixel) && Same(pixels[179 * 320 + 160], basePixel) &&
                        IsBlack(pixels[78 * 320 + 160]) && IsBlack(pixels[101 * 320 + 160]),
                    "Textured floor and ceiling obey the same near/far darkness limits.");
                Set(renderer, "floorTexture", null);
                Set(renderer, "ceilingTexture", null);
                view.rectTransform.sizeDelta = new Vector2(320f, 181f);
                Require(IsBlack(Render(renderer, view)[center]),
                    "The exact horizon of an odd-height view remains opaque black.");
                view.rectTransform.sizeDelta = new Vector2(320f, 180f);
                Set(renderer, "maxDistance", 0.5f);
                pixels = Render(renderer, view);
                Require(Count(pixels, IsBlack) == pixels.Length,
                    "Pixels beyond the render range stay black without leaking fallback colors.");
                report.AppendLine("PASS: near/far distance darkness, strength controls, all surface types and black horizon/range cutoff.");
            }
            finally
            {
                foreach (RaycastSprite2D sprite in group.GetComponentsInChildren<RaycastSprite2D>(true))
                    Invoke(sprite, "OnDisable");
                foreach (RaycastWallDecoration2D decoration in group.GetComponentsInChildren<RaycastWallDecoration2D>(true))
                    Invoke(decoration, "OnDisable");
                Object.DestroyImmediate(group);
                view.rectTransform.sizeDelta = new Vector2(320f, 180f);
                Set(renderer, "shadowStrength", 0f);
                Set(renderer, "shadowStartDistance", start);
                Set(renderer, "shadowEndDistance", end);
                Set(renderer, "maxDistance", range);
                Set(renderer, "defaultWallTexture", wallImage);
                Set(renderer, "floorTexture", null);
                Set(renderer, "ceilingTexture", null);
                Set(renderer, "floorColor", floor);
                Set(renderer, "ceilingColor", ceiling);
            }
        }

        private static void ValidateDoor(RaycastRenderer2D renderer, RawImage view, Transform player,
            Texture2D doorImage, Texture2D wallImage, int layer, StringBuilder report)
        {
            Set(renderer, "defaultWallTexture", wallImage);
            Wall("Door Validation Far Wall", new Vector2(0f, 6f), new Vector2(20f, 0.2f), layer);
            var doorObject = new GameObject("Validation Sliding Door") { layer = layer };
            doorObject.SetActive(false);
            doorObject.transform.position = Origin + new Vector2(0f, 2f);
            BoxCollider2D collider = doorObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(1f, 0.12f);
            RaycastWallSurface2D surface = doorObject.AddComponent<RaycastWallSurface2D>();
            Set(surface, "image", doorImage);
            RaycastDoor2D door = doorObject.AddComponent<RaycastDoor2D>();
            Set(door, "slideOffset", new Vector2(1.1f, 0f));
            Set(door, "openSpeed", 2f);
            doorObject.SetActive(true);
            Invoke(door, "Awake");
            Invoke(door, "OnEnable");
            Physics2D.SyncTransforms();
            Require(IsBlue(Render(renderer, view)[90 * 320 + 160]),
                "A closed door renders its own image instead of the default wall texture.");

            RaycastDoorInteractor2D interactor = player.gameObject.AddComponent<RaycastDoorInteractor2D>();
            Set(interactor, "wallMask", (LayerMask)(1 << layer));
            Set(interactor, "interactionDistance", 1.6f);
            Require(!interactor.TryInteract(), "An out-of-range door cannot be operated.");
            Set(interactor, "interactionDistance", 3f);
            GameObject blocker = Wall("Door Interaction Blocker", new Vector2(0f, 1f),
                new Vector2(2f, 0.1f), layer);
            Physics2D.SyncTransforms();
            Require(!interactor.TryInteract(), "A nearer wall prevents operating a door through the wall.");
            blocker.SetActive(false);
            Physics2D.SyncTransforms();
            Require(interactor.TryInteract(), "The first unobstructed door in interaction range can be toggled.");
            door.SetOpen(false);

            // 문이 닫혀 있을 때 이동을 막고, 열렸을 때 동일한 통로를 통과하는지 확인한다.
            Rigidbody2D body = player.gameObject.AddComponent<Rigidbody2D>();
            player.gameObject.AddComponent<CircleCollider2D>().radius = 0.2f;
            RaycastPlayer2D controller = player.gameObject.AddComponent<RaycastPlayer2D>();
            Set(controller, "wallMask", (LayerMask)(1 << layer));
            Invoke(controller, "Awake");
            controller.Move(new Vector2(0f, 3f));
            Physics2D.SyncTransforms();
            Require(body.position.y < Origin.y + 1.8f && body.position.y > Origin.y + 1f,
                "A closed door blocks the actual player movement sweep.");
            body.position = Origin;
            player.position = Origin;
            Physics2D.SyncTransforms();
            Require(interactor.TryInteract(), "The closed door can be opened through its interaction component.");
            door.Step(0.275f);
            Physics2D.SyncTransforms();
            Color32[] halfOpenPixels = Render(renderer, view);
            Require(Mathf.Abs(door.OpenAmount - 0.5f) < 0.01f &&
                    Mathf.Abs(door.transform.position.x - Origin.x - 0.55f) < 0.01f &&
                    IsGray(halfOpenPixels[90 * 320 + 160]) && IsBlue(halfOpenPixels[90 * 320 + 200]),
                "A half-open door image follows its collider and reveals the opened part of the passage.");
            door.Step(1f);
            Physics2D.SyncTransforms();
            Require(door.OpenAmount >= 0.999f && collider.enabled &&
                    Mathf.Abs(door.transform.position.x - Origin.x - 1.1f) < 0.01f,
                "Opening moves the physical door into its pocket and keeps its collider with the image.");
            Require(IsGray(Render(renderer, view)[90 * 320 + 160]),
                "The opened doorway reveals the farther wall.");
            Require(interactor.TryInteract(),
                "A fully open door can be operated from its original doorway despite being inside the pocket.");
            door.Step(1f);
            Require(door.OpenAmount <= 0.001f && IsBlue(Render(renderer, view)[90 * 320 + 160]),
                "Operating the open doorway closes the door and restores its visible panel.");
            Require(interactor.TryInteract(), "The closed door can be opened a second time.");
            door.Step(1f);
            Physics2D.SyncTransforms();
            controller.Move(new Vector2(0f, 3f));
            Physics2D.SyncTransforms();
            Require(body.position.y > Origin.y + 2.9f,
                "The opened door permits player movement through the doorway.");

            body.position = Origin + new Vector2(0f, 2f);
            player.position = body.position;
            Physics2D.SyncTransforms();
            door.SetOpen(false);
            door.Step(1f);
            Physics2D.SyncTransforms();
            Require(!collider.OverlapPoint(player.position) && door.OpenAmount > 0.01f,
                "Closing waits rather than pushing the panel through a player standing in the doorway.");
            body.position = Origin;
            player.position = Origin;
            Physics2D.SyncTransforms();
            door.Step(1f);
            Physics2D.SyncTransforms();
            Require(door.OpenAmount <= 0.001f,
                "The door finishes closing after the doorway becomes clear.");
            report.AppendLine("PASS: door texture, interaction range/occlusion, animated collider, blocked/open passage and obstruction-safe closing.");
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

        private static RaycastWallDecoration2D Decoration(Texture2D image, Vector2 offset)
        {
            var item = new GameObject("Validation Wall Decoration");
            item.SetActive(false);
            item.transform.position = Origin + offset;
            RaycastWallDecoration2D decoration = item.AddComponent<RaycastWallDecoration2D>();
            Set(decoration, "image", image);
            Set(decoration, "twoSided", true);
            item.SetActive(true);
            Invoke(decoration, "OnEnable");
            return decoration;
        }

        private static RaycastSprite2D Sprite(Texture2D image, Vector2 offset, float height, float elevation)
        {
            var item = new GameObject("Validation Elevated Sprite");
            item.SetActive(false);
            item.transform.position = Origin + offset;
            RaycastSprite2D sprite = item.AddComponent<RaycastSprite2D>();
            Set(sprite, "image", image);
            Set(sprite, "height", height);
            Set(sprite, "elevation", elevation);
            item.SetActive(true);
            Invoke(sprite, "OnEnable");
            return sprite;
        }

        private static Color32[] Render(RaycastRenderer2D renderer, RawImage view)
        {
            Physics2D.SyncTransforms();
            Invoke(renderer, "RenderFrame");
            return ((Texture2D)view.texture).GetPixels32();
        }

        private static int Count(Color32[] pixels, Func<Color32, bool> predicate)
        {
            int count = 0;
            foreach (Color32 pixel in pixels) if (predicate(pixel)) count++;
            return count;
        }

        private static bool IsRed(Color32 pixel) => pixel.r > 30 && pixel.r > pixel.g * 2 && pixel.r > pixel.b * 2;
        private static bool IsBlue(Color32 pixel) => pixel.b > 30 && pixel.b > pixel.r * 2 && pixel.b > pixel.g * 2;
        private static bool IsGray(Color32 pixel) => pixel.r > 25 && pixel.r == pixel.g && pixel.g == pixel.b;
        private static bool IsBlack(Color32 pixel) => pixel.r == 0 && pixel.g == 0 && pixel.b == 0 && pixel.a == 255;
        private static bool Same(Color32 first, Color32 second) => first.r == second.r && first.g == second.g &&
            first.b == second.b && first.a == second.a;
        private static T Read<T>(object target, string field) => (T)target.GetType().GetField(field, Fields).GetValue(target);
        private static void Set(object target, string field, object value) => target.GetType().GetField(field, Fields).SetValue(target, value);
        private static void Invoke(object target, string method) => target.GetType().GetMethod(method, Fields)?.Invoke(target, null);
        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
