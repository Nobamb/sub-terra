using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SubTerra.App.Core;
using SubTerra.App.Save;
using SubTerra.App.Tutorial;
using SubTerra.App.UI.SurfaceBase;
using SubTerra.App.UI.Tutorial;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SubTerra.App.Tests.PlayMode
{
    public sealed class Issue267QuestCloseTests
    {
        private UiTestEnvironment environment;

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            environment?.Dispose();
            environment = null;
            var empty = SceneManager.CreateScene("QuestCloseCleanup");
            SceneManager.SetActiveScene(empty);
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
                if (SceneManager.GetSceneAt(i) != empty)
                    yield return SceneManager.UnloadSceneAsync(SceneManager.GetSceneAt(i));
        }

        [UnityTest]
        public IEnumerator Bootstrap_SurfaceAndMine_CloseInsideContentAtThreeResolutions()
        {
            var args = Environment.GetCommandLineArgs();
            int at = Array.IndexOf(args, "-subterra-save-root");
            Assert.That(at, Is.GreaterThanOrEqualTo(0));
            Assert.That(Path.GetFullPath(args[at + 1]).StartsWith(
                Path.GetFullPath("Temp") + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase), Is.True);
            environment = new UiTestEnvironment();
            GameBootstrapper.ResetInstanceForTests();
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Bootstrap/Bootstrap.unity");
            yield return UiTestWait.Until(() => SceneManager.GetActiveScene().name == "MainMenu", "main menu", 60f);
            var runtime = SaveRuntimeController.Instance;
            Assert.That(runtime.StartNewGame(2, true), Is.True);
            yield return UiTestWait.Until(() => SceneManager.GetActiveScene().name == "SurfaceBase" && runtime.IsUiReady,
                "surface ready", 60f);
            GameBootstrapper.Instance.State.SetDemoProgress(DemoObjectiveIds.MineCopper, 1, false);
            GameBootstrapper.Instance.State.SetQuestRewardSettlement(string.Empty, 1);
            for (int context = 0; context < 2; context++)
            {
                if (context == 1)
                {
                    Assert.That(runtime.TryStartExploration(out var reason), Is.True, reason);
                    yield return UiTestWait.Until(() => SceneManager.GetActiveScene().name == "Mine_Demo_Integration"
                        && runtime.IsUiReady, "mine ready", 60f);
                    yield return UiTestEnvironment.DismissBriefing();
                }
                environment.BindInput();
                var view = Object.FindAnyObjectByType<DemoObjectiveView>(FindObjectsInactive.Include);
                Assert.That(view, Is.Not.Null);
                var panel = view.transform.Find("QuestDetailsPanel").gameObject;
                var close = panel.transform.Find("QuestDetailsCloseButton").GetComponent<Button>();
                foreach (var size in new[] { new Vector2Int(1366, 768), new Vector2Int(1920, 1080), new Vector2Int(2560, 1440) })
                {
                    yield return environment.Resolution.Set(size.x, size.y);
                    if (context == 0)
                        Assert.That(Object.FindAnyObjectByType<SurfaceBaseQuestPanel>().Open(GameBootstrapper.Instance.State), Is.True);
                    else view.OnObjectiveDetailsClicked();
                    yield return new WaitForSecondsRealtime(1f);
                    Canvas.ForceUpdateCanvases();
                    var rect = (RectTransform)close.transform;
                    var title = (RectTransform)panel.transform.Find("QuestDetailsTitle");
                    var corners = new Vector3[4];
                    rect.GetWorldCorners(corners);
                    float left = corners[0].x;
                    title.GetWorldCorners(corners);
                    Assert.That(left, Is.GreaterThan(corners[2].x), "X overlaps title");
                    Assert.That(rect.anchoredPosition, Is.EqualTo(new Vector2(-72f, -72f)));
                    Assert.That(rect.sizeDelta, Is.EqualTo(new Vector2(61f, 61f)));
                    Assert.That(close.GetComponent<UnityEngine.UI.Mask>().showMaskGraphic, Is.False);
                    UiTestWait.AssertClickTarget(close);
                    string stage = Array.IndexOf(args, "-subterra-ui-before") >= 0 ? "before" : "after";
                    Capture(panel, stage + "-" + context + "-" + size.x);
                    InputSystem.QueueStateEvent(environment.Mouse, new MouseState { position = close.transform.position });
                    yield return new WaitForSecondsRealtime(.15f);
                    UiTestWait.AssertClickTarget(close);
                    if (size.x == 1920) Capture(panel, stage + "-hover-" + context);
                    yield return UiTestWait.Click(environment.Mouse, close);
                    yield return UiTestWait.Until(() => !panel.activeSelf, "quest close animation", 5f);
                    Assert.That(panel.activeSelf, Is.False);
                }
            }
        }

        private static void Capture(GameObject panel, string name)
        {
            var canvas = panel.GetComponentInParent<Canvas>().rootCanvas;
            var owner = new GameObject("QuestCaptureCamera", typeof(Camera));
            var camera = owner.GetComponent<Camera>();
            camera.transform.position = new Vector3(0, 0, -10);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.015f, .025f, .035f);
            camera.cullingMask = 1 << panel.layer;
            var previousMode = canvas.renderMode;
            var previousCamera = canvas.worldCamera;
            float previousDistance = canvas.planeDistance;
            var target = RenderTexture.GetTemporary(Screen.width, Screen.height, 24);
            var previous = RenderTexture.active;
            var image = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            try
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
                camera.targetTexture = target;
                Canvas.ForceUpdateCanvases();
                UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,
                    new UnityEngine.Rendering.RenderPipeline.StandardRequest { destination = target });
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
                image.Apply();
                string directory = Path.GetFullPath("Builds/Issue267-CornerReview");
                Directory.CreateDirectory(directory);
                File.WriteAllBytes(Path.Combine(directory, name + ".png"), image.EncodeToPNG());
            }
            finally
            {
                canvas.renderMode = previousMode;
                canvas.worldCamera = previousCamera;
                canvas.planeDistance = previousDistance;
                camera.targetTexture = null;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                Object.Destroy(image);
                Object.Destroy(owner);
            }
        }
    }
}
