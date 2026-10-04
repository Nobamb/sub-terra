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
        private UiTestResolution resolution;
        private UiTestEnvironment environment;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            environment = new UiTestEnvironment();
            resolution = environment.Resolution;
            keyboard = environment.Keyboard;
            mouse = environment.Mouse;
            starts = 0;
            surfaceLoads = 0;
            testRoot = Path.Combine(Path.GetTempPath(), "subterra-issue119-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(testRoot);
            paths = new SavePathPolicy(testRoot);
            var fileSystem = new PhysicalSaveFileSystem();
            var mapper = new SaveDataMapper(new SystemSaveClock());
            var codec = new SaveJsonCodec(new SaveMigrationService());
            save = new SaveService(fileSystem, paths, mapper, codec);
            GameBootstrapper.ResetInstanceForTests();
            SceneManager.LoadScene(SceneNames.Bootstrap);
            yield return UiTestWait.Until(() =>
            {
                binder = UnityEngine.Object.FindAnyObjectByType<MainMenuBinder>();
                return binder != null && binder.IsBound && SaveRuntimeController.Instance != null;
            }, "MainMenuBinder.IsBound and SaveRuntimeController");
            binder.enabled = false;
            var runtime = SaveRuntimeController.Instance;
            Assert.That(runtime.ActiveSlot, Is.Zero);
            SetField(runtime, "saveService", save);
            SetField(runtime, "loadService", new LoadService(fileSystem, paths, mapper, codec));
            SetField(runtime, "<Thumbnails>k__BackingField", new SaveThumbnailService(paths));
            binder.enabled = true;
            view = binder.GetComponent<MainMenuView>();
            environment.BindInput();
            binder.Presenter.StartNewGameConfirmed += _ => starts++;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            try { if (environment != null) environment.Dispose(); }
            finally { if (Directory.Exists(testRoot)) Directory.Delete(testRoot, true); }
            yield return null;
        }

        [UnityTest]
        public IEnumerator OccupiedSlot_CancelCloseKeySortingAndConfirm()
        {
            yield return VerifyOccupiedSlot(new[] { new Vector2Int(1920, 1080) }, false);
        }

        [UnityTest, Category("Visual")]
        public IEnumerator OccupiedSlot_LayoutAndInput_AtTwoAspectRatios()
        {
            yield return VerifyOccupiedSlot(new[] { new Vector2Int(1920, 1080), new Vector2Int(1280, 1024) }, true);
        }

        private IEnumerator VerifyOccupiedSlot(Vector2Int[] sizes, bool visual)
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
            foreach (var size in sizes)
            {
                yield return resolution.Set(size.x, size.y);
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
                if (visual) yield return UiTestWait.Capture(Path.Combine(Application.dataPath,
                    "../Temp/issue119-" + size.x + "x" + size.y + ".png"));
                var yes = window.Find("OverwriteYes").GetComponent<UnityEngine.UI.Button>();
                var skin = yes.GetComponent<MenuSpriteButtonSkin>();
                skin.OnPointerEnter(new PointerEventData(EventSystem.current));
                yield return UiTestWait.Until(() => skin.OverlayAlpha >= 0.99f, "overwrite confirm hover animation", stage: "animation");
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
                var settingsRoot = (GameObject)GetField(view, "settingsRoot");
                yield return UiTestWait.Until(() => settingsRoot.activeInHierarchy, "settings popup open");
                var settingsSkin = settingsRoot.GetComponent<SettingsMenuSkin>();
                yield return UiTestWait.Until(() => GetField(settingsSkin, "openRoutine") == null,
                    "settings opening animation", stage: "animation");
                Assert.That(settingsRoot.GetComponent<Canvas>().sortingOrder,
                    Is.GreaterThan(window.GetComponent<Canvas>().sortingOrder));
                yield return PressX();
                yield return UiTestWait.Until(() => !binder.Presenter.Settings.IsOpen && !settingsRoot.activeInHierarchy,
                    "settings close animation", stage: "animation");
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
            yield return UiTestWait.Until(() => surfaceLoads == 1 && SaveRuntimeController.Instance.IsUiReady, "SurfaceBase ready after new game", stage: "scene");
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

        [UnityTest, Category("Visual")]
        public IEnumerator EmptySlot_StartsWithoutPopup_AtFiveByFourAspectRatio()
        {
            yield return StartEmptySlot(1280, 1024);
        }

        private IEnumerator StartEmptySlot(int width, int height)
        {
            yield return resolution.Set(width, height);
            Assert.That(Screen.width, Is.EqualTo(width));
            Assert.That(Screen.height, Is.EqualTo(height));
            binder.Presenter.SelectSlot(2);
            var button = view.transform.Find("MenuContent/NewGameButton").GetComponent<UnityEngine.UI.Button>();
            button.onClick.Invoke();
            button.onClick.Invoke();
            Assert.That(view.IsOverwriteConfirmVisible, Is.False);
            yield return UiTestWait.Until(() => surfaceLoads == 1 && SaveRuntimeController.Instance.IsUiReady, "SurfaceBase ready after new game", stage: "scene");
            Assert.That(starts, Is.EqualTo(1));
            Assert.That(surfaceLoads, Is.EqualTo(1));
            Assert.That(SaveRuntimeController.Instance.ActiveSlot, Is.EqualTo(2));
        }

        private IEnumerator Click(GameObject target)
        {
            mouse.MakeCurrent();
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
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position });
            yield return null;
            mouse.MakeCurrent();
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position, buttons = 1 });
            yield return null;
            yield return null;
            mouse.MakeCurrent();
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position + Vector2.one * 2f, buttons = 1 });
            yield return null;
            yield return null;
            mouse.MakeCurrent();
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position + Vector2.one * 2f });
            yield return null;
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

    }
}
