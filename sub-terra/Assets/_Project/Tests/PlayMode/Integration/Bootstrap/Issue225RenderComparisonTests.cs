using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using SubTerra.Gameplay.DemoWorld;
using SubTerra.Gameplay.Structural;
using SubTerra.App.Integration;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace SubTerra.App.Tests.PlayMode.Integration.Bootstrap
{
    public sealed class Issue225RenderComparisonTests
    {
        [UnityTest, Category("RenderRegression")]
        public IEnumerator Issue225_CompareAuthoredTilesWithAndWithoutClones()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                Assert.Ignore("실제 픽셀 비교에는 그래픽 장치가 필요합니다.");

            var root = new GameObject("Issue225 comparison", typeof(Grid));
            var cameraObject = new GameObject("Comparison camera", typeof(Camera));
            var target = new RenderTexture(640, 256, 24);
            try
            {
                Tilemap source = CreateMap(root.transform, "Foreground", 0);
                Tilemap background = CreateMap(root.transform, "Background", -1);
                Tilemap cracks = CreateMap(root.transform, "Cracks", 10);
                var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath("a97c105638bdf8b4a8650670310a4cd3"));
                Assert.That(material, Is.Not.Null);
                source.GetComponent<TilemapRenderer>().sharedMaterial = material;
                background.GetComponent<TilemapRenderer>().sharedMaterial = material;
                string[] names = { "Rock", "Copper", "Iron", "GasPocket" };
                for (int i = 0; i < names.Length; i++)
                {
                    source.SetTile(new Vector3Int(i, -25, 0), AssetDatabase.LoadAssetAtPath<TileBase>("Assets/_Project/Tilemaps/DemoWorld/" + names[i] + ".asset"));
                    background.SetTile(new Vector3Int(i, -25, 0), AssetDatabase.LoadAssetAtPath<TileBase>("Assets/_Project/Tilemaps/DemoWorld/Rock.asset"));
                    Assert.That(source.HasTile(new Vector3Int(i, -25, 0)), Is.True, names[i]);
                }
                var corners = source.gameObject.AddComponent<MineTileCornerVisual>();
                var feedback = root.AddComponent<StructuralCrackOverlay>();
                Set(feedback, "overlayTilemap", cracks);
                Set(feedback, "unsupportedShakeDistance", 0f);
                feedback.BindSourceTilemap(source);
                cracks.GetComponent<TilemapRenderer>().enabled = false;

                Camera camera = cameraObject.GetComponent<Camera>();
                camera.transform.position = new Vector3(2, -24.5f, -10);
                camera.orthographic = true;
                camera.orthographicSize = 1;
                camera.aspect = 2.5f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.targetTexture = target;
                yield return null;
                yield return null;
                string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../work_process/tools/issue225-pixel-comparison"));
                Directory.CreateDirectory(folder);
                Color32[] original = Capture(camera, target, folder, "01-original-rounded");
                for (int i = 0; i < names.Length; i++)
                    feedback.SetCell(new Vector3Int(i, -25, 0), StructuralRiskLevel.Caution, 0, StructuralRiskCause.Unsupported);
                yield return null;
                Compare(original, Capture(camera, target, folder, "02-clones-rounded"));
                for (int i = 0; i < names.Length; i++) feedback.ClearCell(new Vector3Int(i, -25, 0));
                yield return null;
                Capture(camera, target, folder, "03-original-restored");
                corners.enabled = false;
                yield return null;
                Capture(camera, target, folder, "04-original-no-corners");
                for (int i = 0; i < names.Length; i++)
                    feedback.SetCell(new Vector3Int(i, -25, 0), StructuralRiskLevel.Caution, 0, StructuralRiskCause.Unsupported);
                yield return null;
                Capture(camera, target, folder, "05-clones-no-corners");
                for (int i = 0; i < names.Length; i++) feedback.ClearCell(new Vector3Int(i, -25, 0));
                corners.enabled = true;
                var canvasObject = new GameObject("Darkness canvas", typeof(RectTransform), typeof(Canvas));
                canvasObject.transform.SetParent(root.transform, false);
                Canvas canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1;
                canvas.sortingOrder = 100;
                var imageObject = new GameObject("Darkness", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                imageObject.transform.SetParent(canvasObject.transform, false);
                var rect = imageObject.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                var player = new GameObject("Jumping player");
                player.transform.SetParent(root.transform, false);
                var darkness = imageObject.AddComponent<DepthDarknessOverlayController>();
                Set(darkness, "targetCamera", camera);
                Set(darkness, "currentDepth", 30);
                Set(darkness, "boundaryWeight", 1f);
                darkness.SetTerrainTilemap(source);
                darkness.SetPlayer(player.transform);
                for (int resource = 1; resource < names.Length; resource++)
                for (int step = 0; step < 2; step++)
                {
                    player.transform.position = new Vector3(resource + .5f, -24.5f + step * .35f, 0);
                    camera.transform.position = new Vector3(2, -24.5f + step * .35f, -10);
                    yield return null;
                    Color32[] darkOriginal = Capture(camera, target, folder, "06-dark-original-" + resource + "-" + step);
                    for (int i = 0; i < names.Length; i++)
                        feedback.SetCell(new Vector3Int(i, -25, 0), StructuralRiskLevel.Caution, 0, StructuralRiskCause.Unsupported);
                    yield return null;
                    Compare(darkOriginal, Capture(camera, target, folder, "07-dark-clones-" + resource + "-" + step));
                    for (int i = 0; i < names.Length; i++) feedback.ClearCell(new Vector3Int(i, -25, 0));
                }
                Debug.Log("Issue225 rendered comparison: " + folder + "; material=" + material.name + "; shader=" + material.shader.name);
            }
            finally
            {
                Object.Destroy(root);
                Object.Destroy(cameraObject);
                target.Release();
                Object.Destroy(target);
            }
        }

        private static Tilemap CreateMap(Transform parent, string name, int order)
        {
            var obj = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
            obj.transform.SetParent(parent, false);
            obj.GetComponent<TilemapRenderer>().sortingOrder = order;
            return obj.GetComponent<Tilemap>();
        }

        private static void Set(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

        private static Color32[] Capture(Camera camera, RenderTexture target, string folder, string name)
        {
            var request = new RenderPipeline.StandardRequest { destination = target };
            Assert.That(RenderPipeline.SupportsRenderRequest(camera, request), Is.True);
            RenderPipeline.SubmitRenderRequest(camera, request);
            RenderTexture previous = RenderTexture.active;
            var texture = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            try
            {
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(Path.Combine(folder, name + ".png"), texture.EncodeToPNG());
                var pixels = texture.GetPixels32();
                int nonBlack = 0;
                foreach (Color32 pixel in pixels) if (pixel.r + pixel.g + pixel.b > 25) nonBlack++;
                Assert.That(nonBlack, Is.GreaterThan(1000), name + " must contain visible authored tiles");
                return pixels;
            }
            finally
            {
                RenderTexture.active = previous;
                Object.Destroy(texture);
            }
        }

        private static void Compare(Color32[] original, Color32[] clones)
        {
            long difference = 0;
            for (int i = 0; i < original.Length; i++)
                difference += System.Math.Abs(original[i].r - clones[i].r)
                    + System.Math.Abs(original[i].g - clones[i].g) + System.Math.Abs(original[i].b - clones[i].b);
            Assert.That((double)difference / (original.Length * 3), Is.LessThan(.5),
                "같은 셀의 원본과 정지 복제본은 실제 출력 색이 같아야 한다.");
        }
    }
}
