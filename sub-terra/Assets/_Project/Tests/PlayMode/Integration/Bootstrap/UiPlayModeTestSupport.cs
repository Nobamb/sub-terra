using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Core;
using SubTerra.App.Integration;
using SubTerra.App.Save;
using SubTerra.App.State;
using SubTerra.App.Tutorial;
using SubTerra.App.UI;
using SubTerra.App.UI.Tutorial;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SubTerra.App.Tests.PlayMode
{
    internal static class UiTestWait
    {
        public static IEnumerator Delay(float seconds, string stage)
        {
            var timer = Stopwatch.StartNew();
            yield return new WaitForSecondsRealtime(seconds);
            Record(stage, "fixed " + seconds.ToString(CultureInfo.InvariantCulture), timer.Elapsed.TotalSeconds);
        }

        public static IEnumerator Until(Func<bool> ready, string target, float timeout = 10f, string stage = "ready")
        {
            var timer = Stopwatch.StartNew();
            try
            {
                while (!ready())
                {
                    Assert.That(timer.Elapsed.TotalSeconds, Is.LessThan(timeout), "Timed out waiting for " + target);
                    yield return null;
                }
            }
            finally { Record(stage, target, timer.Elapsed.TotalSeconds); }
        }

        public static void Record(string stage, string target, double seconds)
        {
            Directory.CreateDirectory("Temp");
            File.AppendAllText("Temp/ui-test-stages.tsv", TestContext.CurrentContext.Test.FullName + "\t" + stage
                + "\t" + target + "\t" + seconds.ToString("F6", CultureInfo.InvariantCulture) + "\n");
        }

        public static IEnumerator Press(Keyboard keyboard, Key key)
        {
            yield return FocusGameView();
            if (!keyboard.enabled) InputSystem.EnableDevice(keyboard);
            keyboard.MakeCurrent();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
            yield return Until(() => keyboard[key].isPressed, "input key down " + key, 2f, "input");
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return Until(() => !keyboard[key].isPressed, "input key up " + key, 2f, "input");
            yield return null;
        }

        public static void AssertClickTarget(Button button)
        {
            Canvas.ForceUpdateCanvases();
            Assert.That(button.IsInteractable(), Is.True, button.name + " is not interactable");
            var pointer = new PointerEventData(EventSystem.current) { position = button.transform.position };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits, Is.Not.Empty, button.name + " has no raycast hit");
            Assert.That(hits[0].gameObject.GetComponentInParent<Button>(), Is.SameAs(button), button.name + " is obstructed");
        }

        public static IEnumerator Click(Mouse mouse, Button button)
        {
            yield return FocusGameView();
            if (!mouse.enabled) InputSystem.EnableDevice(mouse);
            AssertClickTarget(button);
            mouse.MakeCurrent();
            var position = (Vector2)button.transform.position;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position });
            yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position, buttons = 1 });
            yield return Until(() => mouse.leftButton.isPressed, "mouse down " + button.name, 2f, "input");
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position });
            yield return Until(() => !mouse.leftButton.isPressed, "mouse up " + button.name, 2f, "input");
            yield return null;
            yield return null;
        }

        public static IEnumerator FocusGameView()
        {
#if UNITY_EDITOR
            if (!Application.isBatchMode)
            {
                var type = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
                var view = UnityEditor.EditorWindow.GetWindow(type);
                view.Focus();
                yield return Until(() => UnityEditor.EditorWindow.focusedWindow == view, "Game View input focus", 2f, "input");
            }
#endif
            yield return null;
        }

        public static IEnumerator Capture(string path)
        {
            Assert.That(Application.isBatchMode, Is.False, "Visual capture requires a non-batch Editor.");
            Assert.That(SystemInfo.graphicsDeviceType, Is.Not.EqualTo(UnityEngine.Rendering.GraphicsDeviceType.Null));
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            if (File.Exists(path)) File.Delete(path);
            Canvas.ForceUpdateCanvases();
            ScreenCapture.CaptureScreenshot(path);
            yield return Until(() => File.Exists(path) && new FileInfo(path).Length > 24,
                "rendered screenshot " + Path.GetFileName(path), 10f, "capture");
            var bytes = File.ReadAllBytes(path);
            Assert.That(bytes[0], Is.EqualTo(137), "Screenshot is not PNG");
            int Dimension(int offset) => (bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];
            Assert.That(Dimension(16), Is.EqualTo(Screen.width), "Screenshot width");
            Assert.That(Dimension(20), Is.EqualTo(Screen.height), "Screenshot height");
        }
    }

    internal sealed class UiTestEnvironment : IDisposable
    {
        public readonly Keyboard Keyboard;
        public readonly Mouse Mouse;
        public readonly UiTestResolution Resolution = new UiTestResolution();
        private readonly float originalScale = Time.timeScale;
        private readonly InputSettings originalInputSettings;
        private readonly InputSettings testInputSettings;
        private InputActionAsset uiActions;
        private ReadOnlyArray<InputDevice>? previousDevices;
        public SaveRuntimeController Save { get; private set; }

        public UiTestEnvironment()
        {
            // Synthetic input must also be delivered when the automation host has OS focus.
            originalInputSettings = InputSystem.settings;
            testInputSettings = Object.Instantiate(originalInputSettings);
            testInputSettings.hideFlags = HideFlags.HideAndDontSave;
            testInputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            testInputSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.settings = testInputSettings;
            Keyboard = InputSystem.AddDevice<Keyboard>();
            Mouse = InputSystem.AddDevice<Mouse>();
        }

        public IEnumerator LoadIntegration(bool useCatalog = true)
        {
            var timer = Stopwatch.StartNew();
            GameBootstrapper.ResetInstanceForTests();
            var root = new GameObject("UI_TestRuntime");
            var bootstrap = root.AddComponent<GameBootstrapper>();
            bootstrap.enabled = false;
#if UNITY_EDITOR
            if (useCatalog)
            {
                var serialized = new UnityEditor.SerializedObject(bootstrap);
                serialized.FindProperty("gameDataCatalog").objectReferenceValue =
                    UnityEditor.AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/_Project/Data/Catalog/GameDataCatalog.asset");
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
#endif
            var state = GameState.CreateNew();
            state.BeginRun();
            // 대상 패널과 무관한 최초 브리핑 대신 진행 중인 세션 데이터로 준비한다.
            state.SetDemoProgress(DemoObjectiveIds.MineCopper, 1, false);
            state.SetQuestRewardSettlement(string.Empty, 1);
            bootstrap.TryReplaceState(state);
            Save = root.AddComponent<SaveRuntimeController>();
            yield return UiTestWait.Until(() => Save.InventoryService != null, "SaveRuntime inventory services");
            Assert.That(Save.ActiveSlot, Is.Zero, "Tests must not use player saves");
            Save.SetReady(true);
            var load = SceneManager.LoadSceneAsync(SceneNames.Integration);
            yield return UiTestWait.Until(() => load.isDone, "Integration scene load", stage: "scene");
            yield return UiTestWait.Until(() =>
            {
                var binder = Object.FindAnyObjectByType<IntegrationRuntimeBinder>();
                return binder != null && binder.IsUiActivated;
            }, "IntegrationRuntimeBinder.IsUiActivated");
            yield return Resolution.Set(1920, 1080);
            BindInput();
            yield return DismissBriefing();
            UiTestWait.Record("setup-total", "isolated Integration runtime", timer.Elapsed.TotalSeconds);
        }

        internal static IEnumerator DismissBriefing()
        {
            var view = Object.FindAnyObjectByType<DemoObjectiveView>();
            var guidance = view != null ? (GameObject)typeof(DemoObjectiveView)
                .GetField("guidanceRoot", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(view) : null;
            var motion = guidance != null ? guidance.GetComponent<StartBriefingPopupMotion>() : null;
            if (guidance != null && guidance.activeInHierarchy && motion != null)
            {
                yield return UiTestWait.Until(() => motion == null || !motion.isActiveAndEnabled || motion.IsShown,
                    "start briefing shown before dismissal", stage: "animation");
                if (motion != null && motion.isActiveAndEnabled) view.OnDismissClicked();
                yield return UiTestWait.Until(() => motion == null || !motion.isActiveAndEnabled,
                    "start briefing close animation", stage: "animation");
            }
            yield return UiTestWait.Until(() => !UiPauseGate.IsHeld, "UI pause gate released after briefing");
        }

        public void BindInput()
        {
            Assert.That(EventSystem.current, Is.Not.Null, "UI EventSystem");
            var module = EventSystem.current.GetComponent<InputSystemUIInputModule>();
            Assert.That(module, Is.Not.Null, "InputSystemUIInputModule");
            uiActions = module.actionsAsset;
            previousDevices = uiActions.devices;
            uiActions.devices = new InputDevice[] { Keyboard, Mouse };
            Keyboard.MakeCurrent();
            Mouse.MakeCurrent();
            if (!Keyboard.enabled) InputSystem.EnableDevice(Keyboard);
            if (!Mouse.enabled) InputSystem.EnableDevice(Mouse);
        }

        public void Dispose()
        {
            try { Resolution.Dispose(); }
            finally
            {
                if (uiActions != null) uiActions.devices = previousDevices;
                InputSystem.RemoveDevice(Keyboard);
                InputSystem.RemoveDevice(Mouse);
                InputSystem.settings = originalInputSettings;
                Object.DestroyImmediate(testInputSettings);
                GameBootstrapper.ResetInstanceForTests();
                if (SaveRuntimeController.Instance != null) Object.DestroyImmediate(SaveRuntimeController.Instance.gameObject);
                Time.timeScale = originalScale;
            }
        }
    }

    internal sealed class UiTestResolution : IDisposable
    {
#if UNITY_EDITOR
        private UnityEditor.EditorWindow view;
        private PropertyInfo selected;
        private object group;
        private int previous;
        private readonly string label = "SubTerra Test " + Guid.NewGuid().ToString("N");
        private readonly Dictionary<Vector2Int, int> sizes = new Dictionary<Vector2Int, int>();
#endif
        public IEnumerator Set(int width, int height)
        {
#if UNITY_EDITOR
            var assembly = typeof(UnityEditor.Editor).Assembly;
            if (view == null)
            {
                var type = assembly.GetType("UnityEditor.GameView");
                view = UnityEditor.EditorWindow.GetWindow(type);
                selected = type.GetProperty("selectedSizeIndex", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                previous = (int)selected.GetValue(view);
                var sizesType = assembly.GetType("UnityEditor.GameViewSizes");
                var singleton = typeof(UnityEditor.ScriptableSingleton<>).MakeGenericType(sizesType);
                var instance = singleton.GetProperty("instance").GetValue(null);
                group = sizesType.GetMethod("GetGroup").Invoke(instance, new[] {
                    Enum.Parse(assembly.GetType("UnityEditor.GameViewSizeGroupType"), "Standalone") });
            }
            var size = new Vector2Int(width, height);
            if (!sizes.TryGetValue(size, out var index))
            {
                var groupType = group.GetType();
                var count = (int)groupType.GetMethod("GetTotalCount").Invoke(group, null);
                index = -1;
                for (var i = 0; i < count; i++)
                {
                    var existing = groupType.GetMethod("GetGameViewSize").Invoke(group, new object[] { i });
                    var sizeType = existing.GetType();
                    if ((int)sizeType.GetProperty("width").GetValue(existing) == width
                        && (int)sizeType.GetProperty("height").GetValue(existing) == height) { index = i; break; }
                }
                if (index < 0)
                {
                    var created = Activator.CreateInstance(assembly.GetType("UnityEditor.GameViewSize"), new[] {
                        Enum.Parse(assembly.GetType("UnityEditor.GameViewSizeType"), "FixedResolution"), (object)width, height, label });
                    groupType.GetMethod("AddCustomSize").Invoke(group, new[] { created });
                    index = count;
                }
                sizes.Add(size, index);
            }
            if ((int)selected.GetValue(view) != index) selected.SetValue(view, index);
            view.Focus();
            view.Repaint();
#else
            Screen.SetResolution(width, height, false);
#endif
            yield return UiTestWait.Until(() => Screen.width == width && Screen.height == height,
                "Game View " + width + "x" + height, 5f, "resolution");
            Canvas.ForceUpdateCanvases();
            yield return null;
            Assert.That(Screen.width, Is.EqualTo(width));
            Assert.That(Screen.height, Is.EqualTo(height));
        }

        public void Dispose()
        {
#if UNITY_EDITOR
            if (view == null) return;
            selected.SetValue(view, previous);
            var type = group.GetType();
            var total = (int)type.GetMethod("GetTotalCount").Invoke(group, null);
            var builtin = (int)type.GetMethod("GetBuiltinCount").Invoke(group, null);
            for (var index = total - 1; index >= builtin; index--)
            {
                var size = type.GetMethod("GetGameViewSize").Invoke(group, new object[] { index });
                if ((string)size.GetType().GetProperty("baseText").GetValue(size) == label)
                    type.GetMethod("RemoveCustomSize").Invoke(group, new object[] { index });
            }
            view.Repaint();
#endif
        }
    }
}
