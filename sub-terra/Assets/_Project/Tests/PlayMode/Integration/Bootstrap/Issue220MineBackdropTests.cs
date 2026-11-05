using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using SubTerra.App.Core;
using SubTerra.App.Integration;
using SubTerra.App.Save;
using SubTerra.App.Tutorial;
using SubTerra.App.UI;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SubTerra.App.Tests.PlayMode
{
    public sealed class Issue220MineBackdropTests
    {
        private UiTestEnvironment runtimeEnvironment;

        [UnityTearDown]
        public IEnumerator CleanupRuntime()
        {
            if (runtimeEnvironment == null) yield break;
            runtimeEnvironment.Dispose();
            runtimeEnvironment = null;
            var empty = SceneManager.CreateScene("BackdropCleanup");
            SceneManager.SetActiveScene(empty);
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
                if (SceneManager.GetSceneAt(i) != empty)
                    yield return SceneManager.UnloadSceneAsync(SceneManager.GetSceneAt(i));
        }

        [UnityTest]
        public IEnumerator BootstrapExplorationAndContinue_KeepOneBackdropBehindTerrain()
        {
            var args = Environment.GetCommandLineArgs();
            int at = Array.IndexOf(args, "-subterra-save-root");
            Assert.That(at, Is.GreaterThanOrEqualTo(0), "Isolated QA save root required.");
            string qa = Path.GetFullPath(args[at + 1]);
            string temp = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp")) + Path.DirectorySeparatorChar;
            Assert.That(qa.StartsWith(temp, StringComparison.OrdinalIgnoreCase), Is.True);
            runtimeEnvironment = new UiTestEnvironment();
            GameBootstrapper.ResetInstanceForTests();
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Bootstrap/Bootstrap.unity");
            yield return UiTestWait.Until(() => SceneManager.GetActiveScene().name == "MainMenu", "main menu", 60f);
            var runtime = SaveRuntimeController.Instance;
            Assert.That(runtime.StartNewGame(2, true), Is.True);
            yield return UiTestWait.Until(() => SceneManager.GetActiveScene().name == "SurfaceBase" && runtime.IsUiReady,
                "surface ready", 60f);
            // Initial briefing captures WaitForEndOfFrame, which batchmode does not dispatch.
            // Keep this backdrop test on the existing in-progress quest path.
            GameBootstrapper.Instance.State.SetDemoProgress(DemoObjectiveIds.MineCopper, 1, false);
            GameBootstrapper.Instance.State.SetQuestRewardSettlement(string.Empty, 1);
            Assert.That(runtime.TryStartExploration(out var reason), Is.True, reason);
            yield return UiTestWait.Until(() => SceneManager.GetActiveScene().name == SceneNames.Integration && runtime.IsUiReady,
                "mine ready", 60f);
            yield return UiTestEnvironment.DismissBriefing();
            yield return runtimeEnvironment.Resolution.Set(1920, 1080);
            AssertBackdrop();
            yield return null;
            var camera = Camera.main;
            var target = RenderTexture.GetTemporary(960, 540, 24);
            var previous = RenderTexture.active;
            var image = new Texture2D(960, 540, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,
                    new UnityEngine.Rendering.RenderPipeline.StandardRequest { destination = target });
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 960, 540), 0, 0);
                image.Apply();
                string directory = Path.GetFullPath("../work_process/images/mine-depth-backdrop");
                Directory.CreateDirectory(directory);
                File.WriteAllBytes(Path.Combine(directory, "bootstrap-integration.png"), image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                Object.Destroy(image);
            }
            Assert.That(runtime.SaveCurrent().IsSuccess, Is.True);
            ContinueResult continued = null;
            runtime.BeginContinue(2, result => continued = result);
            yield return UiTestWait.Until(() => continued != null, "continue complete", 60f);
            Assert.That(continued.IsSuccess, Is.True, continued.Status.ToString());
            yield return UiTestWait.Until(() => runtime.IsUiReady, "continued mine ready", 60f);
            AssertBackdrop();
        }

        private static void AssertBackdrop()
        {
            // DontSave runtime renderers are intentionally omitted by Object.FindObjectsByType.
            var binder = Object.FindAnyObjectByType<IntegrationRuntimeBinder>();
            Assert.That(binder, Is.Not.Null);
            var backdrops = binder.GetComponentsInChildren<MineBackdropController>(true);
            Assert.That(backdrops.Length, Is.EqualTo(1));
            var background = backdrops[0].GetComponent<MeshRenderer>();
            var mining = Object.FindAnyObjectByType<SubTerra.Gameplay.Mining.MiningSystem>();
            Assert.That(background.enabled, Is.True);
            Assert.That(background.sortingOrder, Is.LessThan(mining.ForegroundTilemap.GetComponent<UnityEngine.Tilemaps.TilemapRenderer>().sortingOrder));
            Assert.That(backdrops[0].CurrentLayerWeights.x, Is.GreaterThan(0.99f));
        }

        [Test]
        public void Backdrop_CoversCameraSmoothsTeleportAndReusesOwnedRenderer()
        {
            var source = Resources.Load<MineBackdropSettings>(MineBackdropSettings.ResourcePath);
            Assert.That(source, Is.Not.Null);
            Assert.That(source.image, Is.Not.Null);
            var config = Object.Instantiate(source);
            var owner = new GameObject("Issue220_Owner");
            var cameraObject = new GameObject("Issue220_Camera", typeof(Camera));
            var target = new GameObject("Issue220_Focus");
            bool originalReduceMotion = AccessibilityPreferences.ReduceMotion;
            try
            {
                AccessibilityPreferences.ReduceMotion = false;
                var camera = cameraObject.GetComponent<Camera>();
                camera.enabled = false;
                camera.orthographic = true;
                camera.orthographicSize = 4f;
                camera.aspect = 2f;
                var backdrop = MineBackdropController.AttachTo(owner.transform, camera, target.transform, null);
                Assert.That(backdrop, Is.Not.Null);
                backdrop.Configure(config, camera, target.transform);
                Assert.That(MineBackdropController.AttachTo(owner.transform, camera, target.transform, null), Is.SameAs(backdrop));
                var start = backdrop.CurrentViewport;
                target.transform.position = new Vector3(30, -40, 0);
                camera.transform.position = new Vector3(25, -35, -10);
                var destination = config.Viewport(target.transform.position, camera.aspect);
                backdrop.Refresh(.016f);
                Assert.That(backdrop.CurrentLayerWeights.z, Is.GreaterThan(0f).And.LessThan(1f));
                Assert.That(backdrop.CurrentViewport.center.y, Is.LessThan(start.center.y));
                Assert.That(backdrop.CurrentViewport.center.y, Is.GreaterThan(destination.center.y));
                var renderer = backdrop.GetComponent<MeshRenderer>();
                Assert.That(renderer.bounds.size.x, Is.EqualTo(16f).Within(.001f));
                Assert.That(renderer.bounds.size.y, Is.EqualTo(8f).Within(.001f));
                Assert.That(renderer.bounds.center.x, Is.EqualTo(camera.transform.position.x));
                Assert.That(renderer.bounds.center.y, Is.EqualTo(camera.transform.position.y));
                Assert.That(renderer.sortingOrder, Is.LessThan(-1000));
                Assert.That(renderer.sharedMaterial.mainTexture, Is.SameAs(config.image));
                Assert.That(backdrop.GetComponent<Collider>(), Is.Null);
                Assert.That(backdrop.GetComponent<Collider2D>(), Is.Null);
                Assert.That(backdrop.gameObject.hideFlags & HideFlags.DontSave, Is.EqualTo(HideFlags.DontSave));
                backdrop.enabled = false;
                Assert.That(renderer.enabled, Is.False);
                backdrop.enabled = true;
                Assert.That(renderer.enabled, Is.True);
                AccessibilityPreferences.ReduceMotion = true;
                backdrop.Refresh(.016f);
                Assert.That(backdrop.CurrentLayerWeights.z, Is.EqualTo(1f));
                var fixedViewport = config.Viewport(config.surfaceOrigin, camera.aspect);
                Assert.That(backdrop.CurrentViewport.x, Is.EqualTo(fixedViewport.x).Within(.00001f));
                Assert.That(backdrop.CurrentViewport.y, Is.EqualTo(fixedViewport.y).Within(.00001f));
                foreach (float depth in new[] { 5f, 17f, 25f, 37f, 40f })
                {
                    backdrop.FocusCellYProvider = () => config.mineralSurfaceY - depth;
                    backdrop.Refresh(0f);
                    Assert.That(backdrop.CurrentLayerWeights, Is.EqualTo(config.LayerWeights(depth)));
                    Capture(camera, depth);
                }
            }
            finally
            {
                Object.DestroyImmediate(owner);
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(config);
                AccessibilityPreferences.ReduceMotion = originalReduceMotion;
            }
        }

        private static void Capture(Camera camera, float depth)
        {
            const int width = 480, height = 240;
            var target = RenderTexture.GetTemporary(width, height, 24);
            var previous = RenderTexture.active;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,
                    new UnityEngine.Rendering.RenderPipeline.StandardRequest { destination = target });
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                image.Apply();
                var directory = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,
                    "../../work_process/images/mine-depth-backdrop"));
                System.IO.Directory.CreateDirectory(directory);
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(directory, "depth-" + depth + ".png"), image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                Object.DestroyImmediate(image);
            }
        }
    }
}
