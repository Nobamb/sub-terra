using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Core;
using SubTerra.App.Integration;
using SubTerra.App.Save;
using SubTerra.App.State;
using SubTerra.App.UI;
using SubTerra.App.UI.HUD;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SubTerra.App.Tests.PlayMode
{
    public sealed class PromptB105SideMenuPlayModeTests
    {
        private Keyboard keyboard;
        private float originalTimeScale;
#if UNITY_EDITOR
        private UnityEditor.EditorWindow gameView;
        private object sizeGroup;
        private PropertyInfo selectedSize;
        private int previousSize;
        private int customSizeCount;
#endif

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = originalTimeScale;
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            GameBootstrapper.ResetInstanceForTests();
#if UNITY_EDITOR
            if (gameView != null)
            {
                selectedSize.SetValue(gameView, previousSize);
                int builtin = (int)sizeGroup.GetType().GetMethod("GetBuiltinCount").Invoke(sizeGroup, null);
                while ((int)sizeGroup.GetType().GetMethod("GetCustomCount").Invoke(sizeGroup, null) > customSizeCount)
                {
                    int total = (int)sizeGroup.GetType().GetMethod("GetTotalCount").Invoke(sizeGroup, null);
                    sizeGroup.GetType().GetMethod("RemoveCustomSize").Invoke(sizeGroup, new object[] { total - builtin - 1 });
                }
            }
#endif
            yield return null;
        }

        [UnityTest]
        public IEnumerator InGameMenu_ActionsHoverTransitionsAndHiddenShortcuts()
        {
            originalTimeScale = Time.timeScale;
            GameBootstrapper.ResetInstanceForTests();
            var runtimeRoot = new GameObject("PromptB105_TestRuntime");
            var bootstrap = runtimeRoot.AddComponent<GameBootstrapper>();
            bootstrap.enabled = false;
            var state = GameState.CreateNew();
            state.BeginRun();
            bootstrap.TryReplaceState(state);
            var save = runtimeRoot.AddComponent<SaveRuntimeController>();
            yield return null;
            yield return null;
            // 슬롯을 시작/불러오지 않아 실제 사용자 저장 파일에 쓰지 않는다.
            Assert.That(save.ActiveSlot, Is.Zero);
            save.SetReady(true);
            SceneManager.LoadScene(SceneNames.Integration);
            yield return null;
            yield return null;
            yield return new WaitForSecondsRealtime(1f);
            var menu = Object.FindFirstObjectByType<GameplaySideMenuController>();
            Assert.That(menu, Is.Not.Null);
            var open = (RectTransform)menu.transform.Find("OpenMenuRoot");
            var closed = (RectTransform)menu.transform.Find("ClosedMenuRoot");
            var bar = open.Find("PanelShortcutBar");
            var chrome = menu.GetComponentInParent<HudPanelChromeController>();
            var panels = menu.GetComponentInParent<PanelToggleController>();
            var underground = menu.GetComponentInParent<UndergroundMenuController>();
            Assert.That(menu.IsOpen, Is.True);

            chrome.CloseBuildingMenu();
            Click(bar, "Open0"); Assert.That(chrome.IsBuildingMenuOpen, Is.True);
            Click(bar, "Open0"); Assert.That(chrome.IsBuildingMenuOpen, Is.False);
            Click(bar, "Open1"); Assert.That(chrome.IsInventoryPanelOpen, Is.True);
            Click(bar, "Open1"); Assert.That(chrome.IsInventoryPanelOpen, Is.False);
            Click(bar, "Open2"); Assert.That(panels.IsVisible(RuntimePanelId.Upgrade), Is.True);
            Click(bar, "Open2"); Assert.That(panels.IsVisible(RuntimePanelId.Upgrade), Is.False);
            Click(bar, "Open3"); Assert.That(chrome.IsGameGuideOpen, Is.True);
            Click(bar, "Open3"); Assert.That(chrome.IsGameGuideOpen, Is.False);
            Click(bar, "SettingsShortcut"); Assert.That(underground.IsSettingsOpen, Is.True);
            underground.CloseSettings();
            yield return new WaitForSecondsRealtime(1f);
            var quit = bar.Find("QuitShortcut").GetComponent<UnityEngine.UI.Button>();
            Assert.That(quit.onClick.GetPersistentMethodName(0), Is.EqualTo("RequestQuit"));

            Time.timeScale = 0f;
            var pointer = new PointerEventData(EventSystem.current);
            foreach (var skin in open.GetComponentsInChildren<SideMenuButtonView>())
            {
                skin.OnPointerEnter(pointer);
                yield return new WaitForSecondsRealtime(0.15f);
                var hover = skin.transform.Find("HoverImage").GetComponent<UnityEngine.UI.Image>();
                Assert.That(hover.color.a, Is.InRange(0.25f, 0.85f),
                    skin.name + " active=" + skin.isActiveAndEnabled + " interactable=" + skin.GetComponent<UnityEngine.UI.Button>().IsInteractable());
                yield return new WaitForSecondsRealtime(0.2f);
                Assert.That(hover.color.a, Is.EqualTo(1f).Within(0.01f));
                skin.OnPointerExit(pointer);
                yield return new WaitForSecondsRealtime(0.35f);
                Assert.That(hover.color.a, Is.Zero.Within(0.01f));
            }

            string evidence = Path.GetFullPath("../work_process/MVP2/UI-fix-markdown-document/evidence/prompt-b105");
            Directory.CreateDirectory(evidence);
            ScreenCapture.CaptureScreenshot(Path.Combine(evidence, "menu-open.png"));
            yield return new WaitForSecondsRealtime(0.2f);
            for (int pass = 0; pass < 2; pass++)
            {
                var outgoing = pass == 0 ? open : closed;
                var incoming = pass == 0 ? closed : open;
                menu.Toggle();
                menu.Toggle(); // 전환 중 중복 입력을 무시한다.
                Assert.That(menu.IsTransitioning, Is.True);
                yield return new WaitForSecondsRealtime(0.25f);
                Assert.That(outgoing.anchoredPosition.x, Is.GreaterThan(0));
                Assert.That(incoming.gameObject.activeSelf, Is.False);
                float deadline = Time.realtimeSinceStartup + 1f;
                while (menu.IsTransitioning && Time.realtimeSinceStartup < deadline)
                {
                    Assert.That(open.gameObject.activeSelf && closed.gameObject.activeSelf, Is.False);
                    yield return null;
                }
                Assert.That(menu.IsTransitioning, Is.False);
                Assert.That(incoming.anchoredPosition.x, Is.Zero.Within(0.01f));
                Assert.That(outgoing.anchoredPosition.x, Is.GreaterThan(outgoing.rect.width));
                if (pass == 0)
                {
                    Assert.That(menu.IsOpen, Is.False);
                    ScreenCapture.CaptureScreenshot(Path.Combine(evidence, "menu-closed.png"));
                    yield return new WaitForSecondsRealtime(0.2f);
                    keyboard = InputSystem.AddDevice<Keyboard>();
                    foreach (var key in new[] { Key.B, Key.I, Key.U, Key.G, Key.Escape })
                    {
                        InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
                        yield return null;
                        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                        yield return null;
                        switch (key)
                        {
                            case Key.B: Assert.That(chrome.IsBuildingMenuOpen, Is.True); chrome.CloseBuildingMenu(); break;
                            case Key.I: Assert.That(chrome.IsInventoryPanelOpen, Is.True); chrome.CloseInventoryPanel(); break;
                            case Key.U: Assert.That(panels.IsVisible(RuntimePanelId.Upgrade), Is.True); panels.CloseUpgrade(); break;
                            case Key.G: Assert.That(chrome.IsGameGuideOpen, Is.True); chrome.CloseGameGuide(); break;
                            case Key.Escape: Assert.That(underground.IsSettingsOpen, Is.True); underground.CloseSettings(); break;
                        }
                        Assert.That(menu.IsOpen, Is.False);
                    }
                }
            }
            menu.Toggle();
            yield return null;
            menu.gameObject.SetActive(false);
            menu.gameObject.SetActive(true);
            Assert.That(menu.IsOpen, Is.True);
            Assert.That(menu.IsTransitioning, Is.False);
            Assert.That(open.anchoredPosition.x, Is.Zero);
            Assert.That(save.ActiveSlot, Is.Zero);
            foreach (var resolution in new[] { new Vector2Int(1920, 1080), new Vector2Int(2560, 1440), new Vector2Int(1366, 768) })
            {
                SetGameViewSize(resolution.x, resolution.y);
                yield return new WaitForSecondsRealtime(0.3f);
                Canvas.ForceUpdateCanvases();
                Assert.That(Screen.width, Is.EqualTo(resolution.x));
                Assert.That(Screen.height, Is.EqualTo(resolution.y));
                var corners = new Vector3[4];
                open.GetWorldCorners(corners);
                var canvas = menu.GetComponentInParent<Canvas>().rootCanvas;
                var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
                Assert.That(RectTransformUtility.WorldToScreenPoint(camera, corners[2]).x, Is.EqualTo(Screen.width).Within(2f));
                Assert.That(RectTransformUtility.WorldToScreenPoint(camera, corners[0]).y, Is.GreaterThan(0));
                ScreenCapture.CaptureScreenshot(Path.Combine(evidence, $"menu-open-{resolution.x}x{resolution.y}.png"));
                yield return new WaitForSecondsRealtime(0.15f);
                menu.Toggle();
                yield return new WaitForSecondsRealtime(0.9f);
                Assert.That(menu.IsOpen, Is.False);
                closed.GetWorldCorners(corners);
                Assert.That(RectTransformUtility.WorldToScreenPoint(camera, corners[2]).x, Is.EqualTo(Screen.width).Within(2f));
                Assert.That(RectTransformUtility.WorldToScreenPoint(camera, corners[0]).y, Is.GreaterThan(0));
                ScreenCapture.CaptureScreenshot(Path.Combine(evidence, $"menu-closed-{resolution.x}x{resolution.y}.png"));
                yield return new WaitForSecondsRealtime(0.15f);
                menu.Toggle();
                yield return new WaitForSecondsRealtime(0.9f);
                Assert.That(menu.IsOpen, Is.True);
            }
        }

        private static void Click(Transform parent, string name) => parent.Find(name).GetComponent<UnityEngine.UI.Button>().onClick.Invoke();

        private void SetGameViewSize(int width, int height)
        {
#if UNITY_EDITOR
            var editorAssembly = typeof(UnityEditor.Editor).Assembly;
            var viewType = editorAssembly.GetType("UnityEditor.GameView");
            if (gameView == null)
            {
                gameView = UnityEditor.EditorWindow.GetWindow(viewType);
                selectedSize = viewType.GetProperty("selectedSizeIndex", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                previousSize = (int)selectedSize.GetValue(gameView);
                var sizesType = editorAssembly.GetType("UnityEditor.GameViewSizes");
                var singleton = typeof(UnityEditor.ScriptableSingleton<>).MakeGenericType(sizesType);
                var sizes = singleton.GetProperty("instance").GetValue(null);
                var groupType = editorAssembly.GetType("UnityEditor.GameViewSizeGroupType");
                sizeGroup = sizesType.GetMethod("GetGroup").Invoke(sizes, new[] { System.Enum.Parse(groupType, "Standalone") });
                customSizeCount = (int)sizeGroup.GetType().GetMethod("GetCustomCount").Invoke(sizeGroup, null);
            }
            var sizeType = editorAssembly.GetType("UnityEditor.GameViewSize");
            var modeType = editorAssembly.GetType("UnityEditor.GameViewSizeType");
            var size = System.Activator.CreateInstance(sizeType, new[] { System.Enum.Parse(modeType, "FixedResolution"), (object)width, height, "PromptB105 QA" });
            sizeGroup.GetType().GetMethod("AddCustomSize").Invoke(sizeGroup, new[] { size });
            int count = (int)sizeGroup.GetType().GetMethod("GetTotalCount").Invoke(sizeGroup, null);
            selectedSize.SetValue(gameView, count - 1);
            gameView.Repaint();
#endif
        }
    }
}
