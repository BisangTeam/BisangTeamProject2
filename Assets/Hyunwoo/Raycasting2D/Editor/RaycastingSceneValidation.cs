using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Hyunwoo.Raycasting2D.Editor
{
    public static class RaycastingSceneValidation
    {
        private const string ScenePath = "Assets/Hyunwoo/Raycasting2D/Scenes/Raycasting2D.unity";
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        [MenuItem("Hyunwoo/2D Raycasting/Validate Scene")]
        public static void Validate()
        {
            var report = new StringBuilder();
            Directory.CreateDirectory("Logs");
            try
            {
                ValidateScene(report);
                report.AppendLine("PASS: all raycasting scene checks completed.");
                Debug.Log(report.ToString());
            }
            catch (Exception exception)
            {
                report.AppendLine("FAIL: " + exception);
                Debug.LogError(report.ToString());
                throw;
            }
            finally
            {
                File.WriteAllText("Logs/Raycasting2D-validation.txt", report.ToString(), Encoding.UTF8);
            }
        }

        private static void ValidateScene(StringBuilder report)
        {
            Require(!EditorApplication.isPlaying, "Run validation outside Play Mode.");
            Require(!EditorSceneManager.GetSceneManagerSetup().Any(item =>
                SceneManager.GetSceneByPath(item.path).isDirty), "Save open scenes before validating.");

            Scene existing = SceneManager.GetSceneByPath(ScenePath);
            bool openedForValidation = !existing.IsValid() || !existing.isLoaded;
            Scene scene = openedForValidation
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive)
                : existing;
            Scene previousActive = SceneManager.GetActiveScene();
            Scene testScene = default;
            Component renderer = null;
            Transform player = null;
            Rigidbody2D body = null;
            Vector3 originalPosition = default;
            Quaternion originalRotation = default;
            bool initialized = false;

            try
            {
                GameObject[] objects = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                    .Select(item => item.gameObject).ToArray();
                foreach (GameObject item in objects)
                    Require(item.GetComponents<Component>().All(component => component != null),
                        "Missing script on " + item.name);

                renderer = objects.SelectMany(item => item.GetComponents<MonoBehaviour>())
                    .SingleOrDefault(component => component.GetType().Namespace == "Hyunwoo.Raycasting2D" &&
                        component.GetType().GetMethod("RenderFrame", Fields) != null);
                Require(renderer != null, "Raycasting renderer exists in the saved scene.");
                player = Read<Transform>(renderer, "player");
                RawImage view = Read<RawImage>(renderer, "view");
                int mask = Read<LayerMask>(renderer, "wallMask").value;
                Require(player != null && view != null && mask != 0, "Renderer references and wall mask are assigned.");
                Require(Mathf.Approximately(Read<float>(renderer, "fieldOfView"), 80f), "Initial horizontal FOV is 80 degrees.");
                Require(objects.SelectMany(item => item.GetComponents<RawImage>()).Count() == 1,
                    "The first-person view uses one RawImage.");

                TilemapCollider2D[] mapColliders = objects.SelectMany(item => item.GetComponents<TilemapCollider2D>()).ToArray();
                Require(mapColliders.Length > 0, "The map contains a TilemapCollider2D.");
                foreach (TilemapCollider2D collider in mapColliders)
                {
                    collider.ProcessTilemapChanges();
                    Require((mask & (1 << collider.gameObject.layer)) != 0, "Wall mask includes the Tilemap layer.");
                    Require(collider.GetComponent<Tilemap>().GetUsedTilesCount() > 0, "The wall tilemap contains saved tiles.");
                }
                Physics2D.SyncTransforms();
                report.AppendLine("PASS: saved scene, script references, tilemap walls, 320 x 180 resolution and 80-degree FOV.");

                originalPosition = player.position;
                originalRotation = player.rotation;
                body = player.GetComponent<Rigidbody2D>();
                CircleCollider2D circle = player.GetComponent<CircleCollider2D>();
                Require(body != null && circle != null && body.bodyType == RigidbodyType2D.Kinematic,
                    "Player has a Kinematic Rigidbody2D and CircleCollider2D.");
                Component controller = player.GetComponents<MonoBehaviour>()
                    .SingleOrDefault(component => component.GetType().GetMethod("Move", Fields, null,
                        new[] { typeof(Vector2) }, null) != null);
                Require(controller != null, "Player movement controller exists.");
                int movementMask = Read<LayerMask>(controller, "wallMask").value;
                Require(movementMask == mask, "Player and renderer use the same saved wall mask.");
                report.AppendLine("INFO: renderer wall mask=" + mask + ", player wall mask=" + movementMask + ".");
                InvokeIfPresent(controller, "Awake");
                InvokeIfPresent(renderer, "Awake");
                initialized = true;
                Invoke(renderer, "RenderFrame");
                // 해상도는 상수로 정해져 있으므로, 실제로 생성된 결과 이미지의 크기를 확인한다.
                Texture2D texture = view.texture as Texture2D;
                Require(texture != null && texture.width == 320 && texture.height == 180 &&
                    texture.filterMode == FilterMode.Point, "RawImage receives a 320 x 180 point-filtered Texture2D.");
                Dictionary<FieldInfo, object> buffers = renderer.GetType().GetFields(Fields)
                    .Where(field => field.FieldType == typeof(Color32[]) || field.FieldType == typeof(Color[]))
                    .Where(field => field.GetValue(renderer) != null)
                    .ToDictionary(field => field, field => field.GetValue(renderer));
                Require(buffers.Count > 0, "Renderer retains its pixel buffer.");
                Invoke(renderer, "RenderFrame");
                Require(ReferenceEquals(texture, view.texture), "Consecutive frames reuse the Texture2D.");
                foreach (var buffer in buffers)
                    Require(ReferenceEquals(buffer.Value, buffer.Key.GetValue(renderer)), "Consecutive frames reuse the pixel array.");
                File.WriteAllBytes("Logs/Raycasting2D-validation.png", texture.EncodeToPNG());
                report.AppendLine("PASS: RawImage output, point filtering, reused texture and pixel buffer. Preview saved.");

                testScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                SceneManager.SetActiveScene(testScene);
                int wallLayer = Enumerable.Range(0, 32).First(layer => (mask & (1 << layer)) != 0);
                Vector2 origin = new Vector2(1000f, 1000f);
                SetPose(player, body, origin);
                CreateWall("Validation Flat Wall", new Vector2(1000f, 1006f), new Vector2(80f, 1f), wallLayer);
                Physics2D.SyncTransforms();
                Invoke(renderer, "RenderFrame");
                Color32[] pixels = texture.GetPixels32();
                int[] farSpans = Enumerable.Range(0, texture.width)
                    .Select(column => WallSpan(pixels, texture.width, texture.height, column)).ToArray();
                Require(farSpans.Min() > 0 && farSpans.Max() - farSpans.Min() <= 1,
                    "A flat wall has constant projected height across all columns (no fisheye).");
                Require(!Same(pixels[texture.width / 2], pixels[(texture.height - 1) * texture.width + texture.width / 2]),
                    "Floor and ceiling have distinct colors.");
                GameObject nearWall = CreateWall("Validation Nearest Wall", new Vector2(1000f, 1003f), new Vector2(80f, 1f), wallLayer);
                Physics2D.SyncTransforms();
                Invoke(renderer, "RenderFrame");
                int nearSpan = WallSpan(texture.GetPixels32(), texture.width, texture.height, texture.width / 2);
                Require(nearSpan > farSpans[texture.width / 2] * 1.5f,
                    "The nearer wall occludes the farther wall and projects taller.");
                Object.DestroyImmediate(nearWall);
                report.AppendLine("PASS: flat-wall perspective across all 320 columns, distinct floor/ceiling and nearest-wall occlusion.");

                float radius = circle.radius * Mathf.Max(Mathf.Abs(player.lossyScale.x), Mathf.Abs(player.lossyScale.y));
                SetPose(player, body, origin);
                RaycastHit2D sweepHit = Physics2D.CircleCast(body.position, radius, Vector2.up, 100f, movementMask);
                report.AppendLine("INFO: forward sweep setup: body=" + body.position.ToString("F4") +
                    ", transform=" + player.position.ToString("F4") + ", radius=" + radius.ToString("F4") +
                    ", hit=" + (sweepHit.collider != null ? sweepHit.collider.name : "none") +
                    ", distance=" + sweepHit.distance.ToString("F4") + ", normal=" + sweepHit.normal.ToString("F4") + ".");
                Require(sweepHit.collider != null && sweepHit.distance > 5f && sweepHit.distance < 5.5f,
                    "The test wall is present in the player's physics query.");
                Vector2 sweptPosition = InvokeMove(controller, new Vector2(0f, 100f));
                report.AppendLine("INFO: 100-unit sweep result: body=" + sweptPosition.ToString("F4") +
                    ", transform=" + player.position.ToString("F4") + ".");
                Require(sweptPosition.y > origin.y + 4f && sweptPosition.y + radius <= 1005.52f,
                    "A 100-unit movement cannot tunnel through a wall. Actual body=" + sweptPosition.ToString("F4") +
                    ", expected center Y between 1004 and " + (1005.52f - radius).ToString("F4") + ".");

                SetPose(player, body, origin);
                Vector2 slidePosition = InvokeMove(controller, new Vector2(3f, 10f));
                report.AppendLine("INFO: diagonal slide result: body=" + slidePosition.ToString("F4") +
                    ", transform=" + player.position.ToString("F4") + ".");
                Require(slidePosition.x > origin.x + 2.8f && slidePosition.y > origin.y + 4f &&
                    slidePosition.y + radius <= 1005.52f,
                    "Diagonal movement slides along a wall. Actual body=" + slidePosition.ToString("F4") + ".");

                CreateWall("Validation Corner Wall", new Vector2(1003f, 1000f), new Vector2(1f, 30f), wallLayer);
                SetPose(player, body, origin);
                Vector2 cornerPosition = InvokeMove(controller, new Vector2(10f, 10f));
                report.AppendLine("INFO: corner result: body=" + cornerPosition.ToString("F4") +
                    ", transform=" + player.position.ToString("F4") + ".");
                Require(cornerPosition.x + radius <= 1002.52f && cornerPosition.y + radius <= 1005.52f &&
                    cornerPosition.x > origin.x + 1f && cornerPosition.y > origin.y + 4f,
                    "Diagonal movement remains outside both walls at a corner. Actual body=" + cornerPosition.ToString("F4") + ".");
                report.AppendLine("PASS: Kinematic player wall collision, 100-unit sweep, diagonal sliding and corner collision.");
            }
            finally
            {
                if (player != null && body != null)
                {
                    player.SetPositionAndRotation(originalPosition, originalRotation);
                    body.position = originalPosition;
                    body.rotation = originalRotation.eulerAngles.z;
                    Physics2D.SyncTransforms();
                }
                if (testScene.IsValid() && testScene.isLoaded)
                    EditorSceneManager.CloseScene(testScene, true);
                if (previousActive.IsValid() && previousActive.isLoaded)
                    SceneManager.SetActiveScene(previousActive);
                if (initialized && renderer != null)
                {
                    RawImage view = Read<RawImage>(renderer, "view");
                    Texture texture = view.texture;
                    view.texture = null;
                    if (texture != null) Object.DestroyImmediate(texture);
                }
                if (openedForValidation && scene.IsValid() && scene.isLoaded)
                    EditorSceneManager.CloseScene(scene, true);
                else if (scene.IsValid() && scene.isLoaded)
                {
                    // Reopen the saved scene to discard transient test state and editor-mode textures.
                    bool wasActive = previousActive == scene;
                    Scene reopened;
                    if (SceneManager.sceneCount == 1)
                        reopened = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                    else
                    {
                        EditorSceneManager.CloseScene(scene, true);
                        reopened = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
                    }
                    if (wasActive) SceneManager.SetActiveScene(reopened);
                }
            }
        }

        private static int WallSpan(Color32[] pixels, int width, int height, int column)
        {
            Color32 floor = pixels[column];
            Color32 ceiling = pixels[(height - 1) * width + column];
            int first = height;
            int last = -1;
            for (int y = 0; y < height; y++)
            {
                Color32 pixel = pixels[y * width + column];
                if (Same(pixel, floor) || Same(pixel, ceiling)) continue;
                first = Mathf.Min(first, y);
                last = Mathf.Max(last, y);
            }
            return last < first ? 0 : last - first + 1;
        }

        private static bool Same(Color32 a, Color32 b)
        {
            return a.r == b.r && a.g == b.g && a.b == b.b && a.a == b.a;
        }

        private static GameObject CreateWall(string name, Vector2 position, Vector2 size, int layer)
        {
            var wall = new GameObject(name) { layer = layer };
            wall.transform.position = position;
            wall.AddComponent<BoxCollider2D>().size = size;
            return wall;
        }

        private static void SetPose(Transform player, Rigidbody2D body, Vector2 position)
        {
            player.SetPositionAndRotation(position, Quaternion.identity);
            body.position = position;
            body.rotation = 0f;
            Physics2D.SyncTransforms();
        }

        private static Vector2 InvokeMove(Component controller, Vector2 displacement)
        {
            controller.GetType().GetMethod("Move", Fields, null, new[] { typeof(Vector2) }, null)
                .Invoke(controller, new object[] { displacement });
            // Move writes the physics pose. In Edit Mode the Transform may still have its old pose;
            // SyncTransforms here would copy that old Transform back into the Rigidbody2D.
            return controller.GetComponent<Rigidbody2D>().position;
        }

        private static T Read<T>(Component component, string name)
        {
            FieldInfo field = component.GetType().GetField(name, Fields);
            Require(field != null, "Expected serialized field: " + name);
            return (T)field.GetValue(component);
        }

        private static void Invoke(Component component, string method)
        {
            component.GetType().GetMethod(method, Fields).Invoke(component, null);
        }

        private static void InvokeIfPresent(Component component, string method)
        {
            component.GetType().GetMethod(method, Fields)?.Invoke(component, null);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
