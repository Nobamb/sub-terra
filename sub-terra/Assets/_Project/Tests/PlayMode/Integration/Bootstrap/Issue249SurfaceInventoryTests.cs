#if UNITY_EDITOR
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.UI.Inventory;
using SubTerra.App.UI.MainMenu;
using SubTerra.App.UI.SurfaceBase;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace SubTerra.App.Tests.PlayMode
{
    public sealed class Issue249SurfaceInventoryTests
    {
        private GameObject root;
        private Keyboard keyboard;
        private SurfaceBaseView view;
        private SurfaceBaseBinder binder;
        private SettingsSession settings;
        private Camera captureCamera;
        private UiTestEnvironment environment;

        private string WindowState()
        {
            var top = SubTerra.App.UI.PopupWindowSorting.Top;
            return $"settings={settings.IsOpen}, reset={view.IsMineResetConfirmVisible}, closing={view.IsSecondaryClosing}, "
                + $"quest={binder.GetComponent<SurfaceBaseQuestPanel>().IsOpen}, "
                + $"sell={root.GetComponentInChildren<SubTerra.App.UI.Economy.EconomyPanelBinder>(true).IsModalVisible}, "
                + $"top={(top != null ? top.name + "/" + top.transform.parent?.name : "none")}";
        }

        private IEnumerator Capture(string name)
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) yield break;
            var directory = System.IO.Path.GetFullPath("../work_process/images/issue249");
            System.IO.Directory.CreateDirectory(directory);
            yield return null;
            var target = RenderTexture.GetTemporary(960, 540, 24);
            var previous = RenderTexture.active;
            var texture = new Texture2D(960, 540, TextureFormat.RGB24, false);
            var canvas = root.GetComponentInParent<Canvas>();
            var oldMode = canvas.renderMode;
            var oldCamera = canvas.worldCamera;
            try
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = captureCamera;
                captureCamera.targetTexture = target;
                Canvas.ForceUpdateCanvases();
                UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(captureCamera,
                    new UnityEngine.Rendering.RenderPipeline.StandardRequest { destination = target });
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, 960, 540), 0, 0);
                texture.Apply();
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(directory, name + ".png"), texture.EncodeToPNG());
            }
            finally
            {
                captureCamera.targetTexture = null;
                canvas.renderMode = oldMode;
                canvas.worldCamera = oldCamera;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                Object.Destroy(texture);
            }
        }

        private IEnumerator Create()
        {
            UpgradeWindowMotion.ResetSessionForTests();
            environment = new UiTestEnvironment();
            yield return environment.LoadSurfaceBase(() =>
            {
                binder = Object.FindAnyObjectByType<SurfaceBaseBinder>();
                return binder != null && binder.IsBound;
            });
            view = binder.GetComponent<SurfaceBaseView>();
            root = view.gameObject;
            settings = (SettingsSession)typeof(SurfaceBaseBinder)
                .GetField("settings", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(binder);
            captureCamera = new GameObject("SurfaceCaptureCamera", typeof(Camera)).GetComponent<Camera>();
            captureCamera.transform.position = new Vector3(0, 0, -10);
            captureCamera.clearFlags = CameraClearFlags.SolidColor;
            captureCamera.backgroundColor = Color.black;
            keyboard = environment.Keyboard;
            _ = keyboard.iKey.wasPressedThisFrame;
            _ = keyboard.uKey.wasPressedThisFrame;
            _ = keyboard.escapeKey.wasPressedThisFrame;
        }

        private IEnumerator Press(Key key)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
        }

        private static IEnumerator WaitForUpgrade(UpgradeWindowMotion motion, bool open)
        {
            var deadline = Time.realtimeSinceStartup + 8f;
            while ((open ? !motion.IsSettled : motion.IsClosing) && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(open ? motion.IsSettled : !motion.IsClosing, Is.True, "Upgrade motion timed out.");
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (captureCamera != null) Object.Destroy(captureCamera.gameObject);
            environment?.Dispose();
            var empty = UnityEngine.SceneManagement.SceneManager.CreateScene("Issue249Cleanup");
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(empty);
            for (var i = UnityEngine.SceneManagement.SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (scene != empty) yield return UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(scene);
            }
        }

        [UnityTest]
        public IEnumerator ShortcutsToggleAndEscapeClosesWithoutOverlapping()
        {
            yield return Create();
            yield return Capture("surface-actions");
            root.transform.Find("SurfaceBaseContent/InventoryButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.That(view.IsInventoryVisible, Is.True, WindowState());
            view.InventoryPanel.CloseButton.onClick.Invoke();
            Assert.That(view.IsInventoryVisible, Is.False);
            yield return new WaitForSecondsRealtime(.25f);
            yield return Press(Key.I);
            Assert.That(view.IsInventoryVisible, Is.True, WindowState());
            yield return new WaitForSecondsRealtime(.25f);
            yield return Capture("surface-inventory");
            yield return Press(Key.U);
            Assert.That(view.IsUpgradeVisible, Is.False, "다른 창 위에 겹쳐 열지 않는다.");
            yield return Press(Key.I);
            Assert.That(view.IsInventoryVisible, Is.False);
            yield return new WaitForSecondsRealtime(.25f);
            Assert.That(view.InventoryPanel.gameObject.activeInHierarchy, Is.False);
            yield return Press(Key.U);
            Assert.That(view.IsUpgradeVisible, Is.True);
            var motion = view.UpgradeMotion;
            Assert.That(motion.IsOpening, Is.True, "U must start the existing lock animation.");
            Assert.That(motion.IsFullOpen, Is.True);
            yield return WaitForUpgrade(motion, true);
            yield return Capture("surface-upgrade");
            yield return Press(Key.Escape);
            Assert.That(view.IsUpgradeVisible, Is.False);
            Assert.That(settings.IsOpen, Is.False, "ESC 한 번으로 설정창까지 열면 안 된다.");
            Assert.That(motion.IsClosing, Is.True);
            Assert.That(view.IsSecondaryClosing, Is.True);
            binder.ToggleInventory();
            Assert.That(view.IsInventoryVisible, Is.False, "Closing upgrade still blocks another window.");
            binder.ToggleUpgrade();
            Assert.That(motion.IsOpening, Is.True, "U route can reopen during closing.");
            yield return WaitForUpgrade(motion, true);
            yield return Press(Key.X);
            Assert.That(motion.IsClosing, Is.True);
            yield return WaitForUpgrade(motion, false);
            root.transform.Find("SurfaceBaseContent/UpgradeButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.That(motion.IsOpening, Is.True, "Button uses the same motion as U.");
            Assert.That(motion.IsFullOpen, Is.False, "A later open uses the short version.");
            yield return WaitForUpgrade(motion, true);
            yield return Press(Key.U);
            Assert.That(motion.IsClosing, Is.True);
            yield return WaitForUpgrade(motion, false);
            settings.Open();
            yield return Press(Key.I);
            Assert.That(view.IsInventoryVisible, Is.False);
        }

        [UnityTest]
        public IEnumerator ReusedInventoryReadsLiveServiceAndReopensDuringClose()
        {
            yield return Create();
            var service = environment.Save.InventoryService;
            service.TryAddMineral(DataIds.Minerals.Copper, 7);
            binder.ToggleInventory();
            Assert.That(view.IsInventoryVisible, Is.True, WindowState());
            yield return new WaitForSecondsRealtime(.25f);
            StringAssert.Contains("7", view.InventoryPanel.StacksText.text);
            service.TryReduceMineral(DataIds.Minerals.Copper, 3);
            StringAssert.Contains("4", view.InventoryPanel.StacksText.text);
            view.SetInventoryVisible(false);
            yield return null;
            binder.ToggleInventory();
            yield return new WaitForSecondsRealtime(.3f);
            Assert.That(view.IsInventoryVisible, Is.True);
            Assert.That(view.InventoryPanel.PanelRoot.activeInHierarchy, Is.True);
            StringAssert.Contains("4", view.InventoryPanel.StacksText.text);
            Assert.That(view.InventoryPanel.GetComponent<Canvas>().sortingOrder,
                Is.GreaterThan(view.InventoryPanel.transform.parent.GetComponent<Canvas>().sortingOrder));
        }
    }
}
#endif
