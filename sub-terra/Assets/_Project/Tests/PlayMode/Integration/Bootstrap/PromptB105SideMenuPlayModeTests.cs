using System.Collections;
using System.IO;
using NUnit.Framework;
using SubTerra.App.Integration;
using SubTerra.App.UI;
using SubTerra.App.UI.HUD;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace SubTerra.App.Tests.PlayMode
{
    public sealed class PromptB105SideMenuPlayModeTests
    {
        private UiTestEnvironment environment;
        private Keyboard keyboard;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (environment != null) environment.Dispose();
            yield return null;
        }

        [UnityTest]
        public IEnumerator InGameMenu_ActionsHoverTransitionsAndHiddenShortcuts()
        {
            environment = new UiTestEnvironment();
            keyboard = environment.Keyboard;
            yield return environment.LoadIntegration(false);
            var save = environment.Save;
            var menu = Object.FindFirstObjectByType<GameplaySideMenuController>();
            Assert.That(menu, Is.Not.Null);
            var open = (RectTransform)menu.transform.Find("OpenMenuRoot");
            var closed = (RectTransform)menu.transform.Find("ClosedMenuRoot");
            var bar = open.Find("PanelShortcutBar");
            var iconNames = new[] { "icon-facility", "icon-inventory", "icon-upgrade", "icon-guide", "icon-settings", "icon-quit" };
            var buttonNames = new[] { "Open0", "Open1", "Open2", "Open3", "SettingsShortcut", "QuitShortcut" };
            for (int i = 0; i < buttonNames.Length; i++)
            {
                var iconImage = bar.Find(buttonNames[i]).Find("MenuIcon").GetComponent<UnityEngine.UI.Image>();
                Assert.That(iconImage.sprite, Is.Not.Null, buttonNames[i]);
                Assert.That(iconImage.sprite.name, Is.EqualTo(iconNames[i]));
            }
            var chrome = menu.GetComponentInParent<HudPanelChromeController>();
            var panels = menu.GetComponentInParent<PanelToggleController>();
            var underground = menu.GetComponentInParent<UndergroundMenuController>();
            Assert.That(menu.IsOpen, Is.True);

            chrome.CloseBuildingMenu();
            yield return Click(bar, "Open0"); Assert.That(chrome.IsBuildingMenuOpen, Is.True);
            yield return Click(bar, "Open0"); Assert.That(chrome.IsBuildingMenuOpen, Is.False);
            yield return Click(bar, "Open1"); Assert.That(chrome.IsInventoryPanelOpen, Is.True);
            yield return Click(bar, "Open1"); Assert.That(chrome.IsInventoryPanelOpen, Is.False);
            yield return Click(bar, "Open2"); Assert.That(panels.IsVisible(RuntimePanelId.Upgrade), Is.True);
            yield return Click(bar, "Open2"); Assert.That(panels.IsVisible(RuntimePanelId.Upgrade), Is.False);
            yield return Click(bar, "Open3"); Assert.That(chrome.IsGameGuideOpen, Is.True);
            yield return Click(bar, "Open3"); Assert.That(chrome.IsGameGuideOpen, Is.False);
            yield return Click(bar, "SettingsShortcut"); Assert.That(underground.IsSettingsOpen, Is.True);
            underground.CloseSettings();
            yield return UiTestWait.Until(() => !underground.IsSettingsOpen, "underground settings closed");
            var quit = bar.Find("QuitShortcut").GetComponent<UnityEngine.UI.Button>();
            Assert.That(quit.onClick.GetPersistentMethodName(0), Is.EqualTo("RequestQuit"));

            Time.timeScale = 0f;
            InputSystem.QueueStateEvent(environment.Mouse, new MouseState { position = Vector2.zero });
            yield return null;
            yield return null;
            var pointer = new PointerEventData(EventSystem.current);
            foreach (var skin in open.GetComponentsInChildren<SideMenuButtonView>())
            {
                var hover = skin.transform.Find("HoverImage").GetComponent<UnityEngine.UI.Image>();
                skin.OnPointerExit(pointer);
                yield return UiTestWait.Until(() => hover.color.a <= 0.01f, skin.name + " initial hover fade", 1.2f, "animation");
                skin.OnPointerEnter(pointer);
                yield return UiTestWait.Until(() => hover.color.a >= 0.25f, skin.name + " hover intermediate alpha", 1.2f, "animation");
                Assert.That(hover.color.a, Is.InRange(0.25f, 0.85f),
                    skin.name + " active=" + skin.isActiveAndEnabled + " interactable=" + skin.GetComponent<UnityEngine.UI.Button>().IsInteractable());
                Assert.That(skin.ActiveParticleCount, Is.GreaterThan(0), skin.name + " hover particles");
                yield return UiTestWait.Until(() => hover.color.a >= 0.99f, skin.name + " hover completion", 0.8f, "animation");
                Assert.That(hover.color.a, Is.EqualTo(1f).Within(0.02f));
                skin.OnPointerExit(pointer);
                yield return UiTestWait.Until(() => hover.color.a <= 0.01f, skin.name + " hover fade completion", 1.2f, "animation");
                Assert.That(hover.color.a, Is.Zero.Within(0.01f));
            }

            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Slash));
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            Assert.That(menu.IsTransitioning, Is.True);
            yield return UiTestWait.Until(() => !menu.IsTransitioning, "side menu slash close", 1.2f, "animation");
            Assert.That(menu.IsOpen, Is.False);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Slash));
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            Assert.That(menu.IsTransitioning, Is.True);
            yield return UiTestWait.Until(() => !menu.IsTransitioning, "side menu slash reopen", 1.2f, "animation");
            Assert.That(menu.IsOpen, Is.True);
            for (int pass = 0; pass < 2; pass++)
            {
                var outgoing = pass == 0 ? open : closed;
                var incoming = pass == 0 ? closed : open;
                menu.Toggle();
                menu.Toggle(); // 전환 중 중복 입력을 무시한다.
                Assert.That(menu.IsTransitioning, Is.True);
                yield return UiTestWait.Delay(0.25f, "animation-midpoint");
                Assert.That(outgoing.anchoredPosition.x, Is.GreaterThan(0));
                Assert.That(incoming.gameObject.activeSelf, Is.False);
                yield return UiTestWait.Until(() =>
                {
                    Assert.That(open.gameObject.activeSelf && closed.gameObject.activeSelf, Is.False);
                    return !menu.IsTransitioning;
                }, "side menu transition completion", 1f, "animation");
                Assert.That(menu.IsTransitioning, Is.False);
                Assert.That(incoming.anchoredPosition.x, Is.Zero.Within(0.01f));
                Assert.That(outgoing.anchoredPosition.x, Is.GreaterThan(outgoing.rect.width));
                if (pass == 0)
                {
                    Assert.That(menu.IsOpen, Is.False);
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
            AssertSideInsideScreen(menu, open);
            UiTestWait.AssertClickTarget(bar.Find("Open0").GetComponent<UnityEngine.UI.Button>());
        }

        [UnityTest, Category("Visual")]
        public IEnumerator InGameMenu_OpenClosedLayout_AtThreeResolutions()
        {
            environment = new UiTestEnvironment();
            keyboard = environment.Keyboard;
            yield return environment.LoadIntegration(false);
            var menu = Object.FindAnyObjectByType<GameplaySideMenuController>();
            var open = (RectTransform)menu.transform.Find("OpenMenuRoot");
            var closed = (RectTransform)menu.transform.Find("ClosedMenuRoot");
            var evidence = Path.GetFullPath("Temp/visual/prompt-b105-2");
            yield return UiTestWait.Capture(Path.Combine(evidence, "menu-open.png"));
            yield return UiTestWait.Press(keyboard, Key.Slash);
            yield return UiTestWait.Until(() => !menu.IsTransitioning, "side menu slash close", stage: "animation");
            Assert.That(menu.IsOpen, Is.False);
            yield return UiTestWait.Capture(Path.Combine(evidence, "menu-closed-slash.png"));
            yield return UiTestWait.Press(keyboard, Key.Slash);
            yield return UiTestWait.Until(() => !menu.IsTransitioning, "side menu slash reopen", stage: "animation");
            Assert.That(menu.IsOpen, Is.True);
            menu.Toggle();
            yield return UiTestWait.Until(() => !menu.IsTransitioning, "side menu close", stage: "animation");
            Assert.That(menu.IsOpen, Is.False);
            yield return UiTestWait.Capture(Path.Combine(evidence, "menu-closed.png"));
            menu.Toggle();
            yield return UiTestWait.Until(() => !menu.IsTransitioning, "side menu reopen", stage: "animation");
            foreach (var resolution in new[] { new Vector2Int(1920, 1080), new Vector2Int(2560, 1440), new Vector2Int(1366, 768) })
            {
                yield return environment.Resolution.Set(resolution.x, resolution.y);
                AssertSideInsideScreen(menu, open);
                yield return UiTestWait.Capture(Path.Combine(evidence, $"menu-open-{resolution.x}x{resolution.y}.png"));
                menu.Toggle();
                yield return UiTestWait.Until(() => !menu.IsTransitioning, "side menu close at " + resolution, stage: "animation");
                Assert.That(menu.IsOpen, Is.False);
                AssertSideInsideScreen(menu, closed);
                yield return UiTestWait.Capture(Path.Combine(evidence, $"menu-closed-{resolution.x}x{resolution.y}.png"));
                menu.Toggle();
                yield return UiTestWait.Until(() => !menu.IsTransitioning, "side menu reopen at " + resolution, stage: "animation");
                Assert.That(menu.IsOpen, Is.True);
            }
        }

        private static void AssertSideInsideScreen(GameplaySideMenuController menu, RectTransform root)
        {
            var corners = new Vector3[4];
            root.GetWorldCorners(corners);
            var canvas = menu.GetComponentInParent<Canvas>().rootCanvas;
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Assert.That(RectTransformUtility.WorldToScreenPoint(camera, corners[2]).x, Is.EqualTo(Screen.width).Within(2f));
            Assert.That(RectTransformUtility.WorldToScreenPoint(camera, corners[0]).y, Is.GreaterThan(0));
        }

        private IEnumerator Click(Transform parent, string name) => UiTestWait.Click(environment.Mouse, parent.Find(name).GetComponent<UnityEngine.UI.Button>());

    }
}
