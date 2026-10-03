using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Core;
using SubTerra.App.Inventory;
using SubTerra.App.Progression;
using SubTerra.App.Save;
using SubTerra.App.State;
using SubTerra.App.UI;
using SubTerra.App.UI.MainMenu;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SubTerra.App.Tests.PlayMode
{
    public sealed class Issue119OverwritePopupPlayModeTests
    {
        private string testRoot;
        private SavePathPolicy paths;
        private SaveService save;
        private MainMenuBinder binder;
        private MainMenuView view;
        private Keyboard keyboard;
        private Mouse mouse;
        private int starts;
        private int surfaceLoads;
#if UNITY_EDITOR
        private UnityEditor.EditorWindow gameView;
        private object sizeGroup;
        private PropertyInfo selectedSize;
        private int previousSize;
        private int customSizeCount;
#endif

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            starts = 0;
            surfaceLoads = 0;
            testRoot = Path.Combine(Path.GetTempPath(), "subterra-issue119-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(testRoot);
            paths = new SavePathPolicy(testRoot);
            var fileSystem = new PhysicalSaveFileSystem();
            var mapper = new SaveDataMapper(new SystemSaveClock());
            var codec = new SaveJsonCodec(new SaveMigrationService());
            save = new SaveService(fileSystem, paths, mapper, codec);
            keyboard = InputSystem.AddDevice<Keyboard>();
            mouse = InputSystem.AddDevice<Mouse>();
            GameBootstrapper.ResetInstanceForTests();
            SceneManager.LoadScene(SceneNames.Bootstrap);
            for (var frame = 0; frame < 120; frame++)
            {
                yield return null;
                binder = UnityEngine.Object.FindFirstObjectByType<MainMenuBinder>();
                if (binder != null && binder.IsBound) break;
            }
            Assert.That(binder, Is.Not.Null);
            binder.enabled = false;
            var runtime = SaveRuntimeController.Instance;
            Assert.That(runtime.ActiveSlot, Is.Zero);
            SetField(runtime, "saveService", save);
            SetField(runtime, "loadService", new LoadService(fileSystem, paths, mapper, codec));
            SetField(runtime, "<Thumbnails>k__BackingField", new SaveThumbnailService(paths));
            binder.enabled = true;
            view = binder.GetComponent<MainMenuView>();
            binder.Presenter.StartNewGameConfirmed += _ => starts++;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (mouse != null) InputSystem.RemoveDevice(mouse);
            GameBootstrapper.ResetInstanceForTests();
            if (SaveRuntimeController.Instance != null)
                UnityEngine.Object.Destroy(SaveRuntimeController.Instance.gameObject);
            yield return null;
#if UNITY_EDITOR
            if (gameView != null)
            {
                selectedSize.SetValue(gameView, previousSize);
                var added = (int)sizeGroup.GetType().GetMethod("GetCustomCount").Invoke(sizeGroup, null) - customSizeCount;
                for (var i = 0; i < added; i++)
                {
                    var total = (int)sizeGroup.GetType().GetMethod("GetTotalCount").Invoke(sizeGroup, null);
                    sizeGroup.GetType().GetMethod("RemoveCustomSize").Invoke(sizeGroup, new object[] { total - 1 });
                }
            }
#endif
            if (Directory.Exists(testRoot)) Directory.Delete(testRoot, true);
        }

        [UnityTest]
        public IEnumerator OccupiedSlot_CancelCloseKeySortingAndConfirm_AtTwoAspectRatios()
        {
            var state = GameState.CreateNew();
            state.SetGold(777);
            var context = new SaveCaptureContext(state, new InventoryState(), new UpgradeState(),
                null, null, SceneNames.SurfaceBase, "issue119-test");
            Assert.That(save.Save(1, context).IsSuccess, Is.True);
            Assert.That(save.Save(1, context).IsSuccess, Is.True);
            paths.TryGetPaths(1, out var slot);
            var before = File.ReadAllBytes(slot.Normal);
            var backup = File.ReadAllBytes(slot.Backup);
            var runtimeState = GameBootstrapper.Instance.State;
            var window = (RectTransform)view.transform.Find("OverwriteConfirm");
            foreach (var size in new[] { new Vector2Int(1920, 1080), new Vector2Int(1280, 1024) })
            {
                SetGameViewSize(size.x, size.y);
                yield return new WaitForSecondsRealtime(0.2f);
                Assert.That(Screen.width, Is.EqualTo(size.x));
                Assert.That(Screen.height, Is.EqualTo(size.y));
                binder.Presenter.SelectSlot(1);
                Assert.That(binder.Presenter.RequestNewGame(), Is.EqualTo(NewGameRequestStatus.AwaitingOverwriteConfirm));
                yield return null;
                var body = window.Find("OverwriteMessage").GetComponent<TMP_Text>();
                body.ForceMeshUpdate();
                Assert.That(body.isTextOverflowing, Is.False);
                Assert.That(body.textInfo.lineCount, Is.EqualTo(1));
                var corners = new Vector3[4];
                window.GetWorldCorners(corners);
                Assert.That(corners[0].x, Is.GreaterThan(0));
                Assert.That(corners[0].y, Is.GreaterThan(0));
                Assert.That(corners[2].x, Is.LessThan(Screen.width));
                Assert.That(corners[2].y, Is.LessThan(Screen.height));
                ScreenCapture.CaptureScreenshot(Path.Combine(Application.dataPath,
                    "../Temp/issue119-" + size.x + "x" + size.y + ".png"));
                var yes = window.Find("OverwriteYes").GetComponent<UnityEngine.UI.Button>();
                var skin = yes.GetComponent<MenuSpriteButtonSkin>();
                skin.OnPointerEnter(new PointerEventData(EventSystem.current));
                yield return new WaitForSecondsRealtime(0.25f);
                Assert.That(skin.OverlayAlpha, Is.EqualTo(1f).Within(0.01f));
                Assert.That(yes.transform.localScale, Is.EqualTo(Vector3.one));
                skin.OnPointerExit(new PointerEventData(EventSystem.current));
                foreach (var name in new[] { "OverwriteNo", "OverwriteClose" })
                {
                    if (!view.IsOverwriteConfirmVisible) binder.Presenter.RequestNewGame();
                    yield return null;
                    Canvas.ForceUpdateCanvases();
                    yield return Click(window.Find(name).gameObject);
                    Assert.That(view.IsOverwriteConfirmVisible, Is.False, name);
                    Assert.That(starts, Is.Zero);
                    Assert.That(File.ReadAllBytes(slot.Normal), Is.EqualTo(before));
                    Assert.That(File.ReadAllBytes(slot.Backup), Is.EqualTo(backup));
                    Assert.That(GameBootstrapper.Instance.State, Is.SameAs(runtimeState));
                }
                binder.Presenter.RequestNewGame();
                yield return PressX();
                Assert.That(view.IsOverwriteConfirmVisible, Is.False);
                binder.Presenter.RequestNewGame();
                binder.Presenter.OpenSettings();
                view.RaiseSettingsAbovePopups();
                yield return new WaitForSecondsRealtime(0.5f);
                var settingsRoot = (GameObject)GetField(view, "settingsRoot");
                Assert.That(settingsRoot.GetComponent<Canvas>().sortingOrder,
                    Is.GreaterThan(window.GetComponent<Canvas>().sortingOrder));
                yield return PressX();
                yield return new WaitForSecondsRealtime(0.5f);
                Assert.That(binder.Presenter.Settings.IsOpen, Is.False);
                Assert.That(view.IsOverwriteConfirmVisible, Is.True);
                yield return PressX();
                Assert.That(view.IsOverwriteConfirmVisible, Is.False);
                Assert.That(starts, Is.Zero);
                Assert.That(File.ReadAllBytes(slot.Normal), Is.EqualTo(before));
                Assert.That(GameBootstrapper.Instance.State, Is.SameAs(runtimeState));
            }
            binder.Presenter.RequestNewGame();
            var presenter = binder.Presenter;
            var confirm = window.Find("OverwriteYes").GetComponent<UnityEngine.UI.Button>();
            confirm.onClick.Invoke();
            confirm.onClick.Invoke();
            presenter.ConfirmOverwriteNewGame();
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.That(starts, Is.EqualTo(1));
            Assert.That(surfaceLoads, Is.EqualTo(1));
            Assert.That(SaveRuntimeController.Instance.ActiveSlot, Is.EqualTo(1));
            Assert.That(File.ReadAllBytes(slot.Normal), Is.Not.EqualTo(before));
        }

        [UnityTest]
        public IEnumerator EmptySlot_StartsWithoutPopup_AndIgnoresRepeatedNewGameClick()
        {
            yield return StartEmptySlot(1920, 1080);
        }

        [UnityTest]
        public IEnumerator EmptySlot_StartsWithoutPopup_AtFiveByFourAspectRatio()
        {
            yield return StartEmptySlot(1280, 1024);
        }

        private IEnumerator StartEmptySlot(int width, int height)
        {
            SetGameViewSize(width, height);
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(Screen.width, Is.EqualTo(width));
            Assert.That(Screen.height, Is.EqualTo(height));
            binder.Presenter.SelectSlot(2);
            var button = view.transform.Find("MenuContent/NewGameButton").GetComponent<UnityEngine.UI.Button>();
            button.onClick.Invoke();
            button.onClick.Invoke();
            Assert.That(view.IsOverwriteConfirmVisible, Is.False);
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.That(starts, Is.EqualTo(1));
            Assert.That(surfaceLoads, Is.EqualTo(1));
            Assert.That(SaveRuntimeController.Instance.ActiveSlot, Is.EqualTo(2));
        }

        private IEnumerator Click(GameObject target)
        {
            var position = (Vector2)target.transform.position;
            var pointer = new PointerEventData(EventSystem.current) { position = position };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits.Count, Is.GreaterThan(0));
            Assert.That(hits[0].gameObject.GetComponentInParent<UnityEngine.UI.Button>().gameObject, Is.SameAs(target));
            var window = view.transform.Find("OverwriteConfirm").GetComponent<PopupWindowDrag>();
            pointer.pointerPressRaycast = hits[0];
            window.OnPointerDown(pointer);
            Assert.That(GetField(window, "tracking"), Is.False);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position, buttons = 1 });
            yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position + Vector2.one * 2f, buttons = 1 });
            yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position + Vector2.one * 2f });
            yield return null;
        }

        private IEnumerator PressX()
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.X));
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == SceneNames.SurfaceBase) surfaceLoads++;
        }

        private static void SetField(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

        private static object GetField(object target, string name) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);

        private void SetGameViewSize(int width, int height)
        {
#if UNITY_EDITOR
            var assembly = typeof(UnityEditor.Editor).Assembly;
            var viewType = assembly.GetType("UnityEditor.GameView");
            if (gameView == null)
            {
                gameView = UnityEditor.EditorWindow.GetWindow(viewType);
                selectedSize = viewType.GetProperty("selectedSizeIndex", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                previousSize = (int)selectedSize.GetValue(gameView);
                var sizesType = assembly.GetType("UnityEditor.GameViewSizes");
                var singleton = typeof(UnityEditor.ScriptableSingleton<>).MakeGenericType(sizesType);
                var sizes = singleton.GetProperty("instance").GetValue(null);
                sizeGroup = sizesType.GetMethod("GetGroup").Invoke(sizes, new[] { Enum.Parse(assembly.GetType("UnityEditor.GameViewSizeGroupType"), "Standalone") });
                customSizeCount = (int)sizeGroup.GetType().GetMethod("GetCustomCount").Invoke(sizeGroup, null);
            }
            var size = Activator.CreateInstance(assembly.GetType("UnityEditor.GameViewSize"), new[] {
                Enum.Parse(assembly.GetType("UnityEditor.GameViewSizeType"), "FixedResolution"), (object)width, height, "Issue119 QA" });
            sizeGroup.GetType().GetMethod("AddCustomSize").Invoke(sizeGroup, new[] { size });
            var count = (int)sizeGroup.GetType().GetMethod("GetTotalCount").Invoke(sizeGroup, null);
            selectedSize.SetValue(gameView, count - 1);
            gameView.Focus();
            gameView.Repaint();
#endif
        }
    }
}
