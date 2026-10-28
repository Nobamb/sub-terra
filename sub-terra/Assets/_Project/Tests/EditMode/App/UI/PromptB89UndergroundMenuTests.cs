using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.Integration;
using SubTerra.App.UI.HUD;
using SubTerra.App.UI;
using SubTerra.App.UI.SurfaceBase;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SubTerra.App.Tests.UI
{
    public sealed class PromptB89UndergroundMenuTests
    {
        private Scene scene;
        private UndergroundMenuController menu;
        private float originalVolume;

        [SetUp]
        public void SetUp()
        {
            originalVolume = AudioListener.volume;
            scene = EditorSceneManager.OpenScene(PromptB89UndergroundMenuBuilder.ScenePath, OpenSceneMode.Additive);
            menu = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<UndergroundMenuController>(true)).Single();
            Invoke(menu, "OnEnable");
        }

        [TearDown]
        public void TearDown()
        {
            Invoke(menu, "OnDisable");
            EditorSceneManager.CloseScene(scene, true);
            AudioListener.volume = originalVolume;
        }

        [Test]
        public void Shortcuts_HaveSingleCorrectAction_AndDoNotOverlap()
        {
            var bar = menu.GetComponentsInChildren<Transform>(true).Single(t => t.name == "PanelShortcutBar");
            var settings = bar.Find("SettingsShortcut").GetComponent<Button>();
            var quit = bar.Find("QuitShortcut").GetComponent<Button>();
            Assert.That(settings.GetComponentInChildren<TMPro.TMP_Text>(true).text, Is.EqualTo("설정(esc)"));
            Assert.That(settings.onClick.GetPersistentEventCount(), Is.EqualTo(1));
            Assert.That(settings.onClick.GetPersistentTarget(0), Is.EqualTo(menu));
            Assert.That(settings.onClick.GetPersistentMethodName(0), Is.EqualTo("OpenSettings"));
            Assert.That(quit.onClick.GetPersistentEventCount(), Is.EqualTo(1));
            Assert.That(quit.onClick.GetPersistentTarget(0), Is.EqualTo(menu));
            Assert.That(quit.onClick.GetPersistentMethodName(0), Is.EqualTo("RequestQuit"));
            var rects = bar.GetComponentsInChildren<Button>().Select(b => (RectTransform)b.transform)
                .OrderByDescending(r => r.anchoredPosition.y).ToArray();
            for (int i = 1; i < rects.Length; i++)
                Assert.That(rects[i - 1].anchoredPosition.y - rects[i].anchoredPosition.y,
                    Is.GreaterThanOrEqualTo(rects[i - 1].rect.height));
        }

        [Test]
        public void Escape_TogglesSettingsWithoutClosingOtherPanels()
        {
            var chrome = menu.GetComponent<HudPanelChromeController>();
            chrome.OpenBuildingMenu();
            menu.HandleEscape();
            Assert.That(chrome.IsBuildingMenuOpen, Is.True);
            Assert.That(menu.IsSettingsOpen, Is.True);
            menu.HandleEscape();
            Assert.That(menu.IsSettingsOpen, Is.False);
            Assert.That(chrome.IsBuildingMenuOpen, Is.True);
        }

        [Test]
        public void X_ClosesOnePanelAtATimeWithoutOpeningSettings()
        {
            var chrome = menu.GetComponent<HudPanelChromeController>();
            chrome.OpenBuildingMenu();
            chrome.OpenInventoryPanel();
            menu.HandleCloseTopPopup();
            Assert.That(chrome.IsInventoryPanelOpen, Is.False);
            Assert.That(chrome.IsBuildingMenuOpen, Is.True);
            menu.HandleCloseTopPopup();
            Assert.That(chrome.IsBuildingMenuOpen || chrome.IsInventoryPanelOpen, Is.False);
            Assert.That(menu.IsSettingsOpen, Is.False);
        }

        [Test]
        public void Issue253_XClosesQuestDetailsAndConsumesRepeatedClose()
        {
            var view = scene.GetRootGameObjects().SelectMany(root =>
                root.GetComponentsInChildren<SubTerra.App.UI.Tutorial.DemoObjectiveView>(true)).Single();
            view.SetDetailsVisible(true);
            var top = PopupWindowSorting.Top;
            Assert.That(top, Is.Not.Null);
            var motion = top.GetComponent<TerminalWindowOpenMotion>();
            Assert.That(motion, Is.Not.Null);
            motion.Advance(1f);
            var dismisses = 0;
            System.Action close = () => { dismisses++; view.SetDetailsVisible(false); };
            view.DetailsDismissRequested += close;
            try
            {
                menu.HandleCloseTopPopup();
                menu.HandleCloseTopPopup();
                Assert.That(dismisses, Is.EqualTo(1));
                Assert.That(motion.IsClosing, Is.True);
                motion.Advance(1f);
                Assert.That(top.gameObject.activeSelf, Is.False);
            }
            finally { view.DetailsDismissRequested -= close; }
        }

        [Test]
        public void Issue217_XClosesTopQuestRewardThroughExistingConfirmAction()
        {
            var view = scene.GetRootGameObjects().SelectMany(root =>
                root.GetComponentsInChildren<SubTerra.App.UI.Tutorial.DemoObjectiveView>(true)).Single();
            var motion = view.GetComponentInChildren<SubTerra.App.UI.Tutorial.QuestClearPopupMotion>(true);
            // EditMode는 Unity의 Awake/OnEnable을 실행하지 않으므로 기존 닫기 연출을 초기화한다.
            Invoke(motion, "Awake");
            Invoke(motion.GetComponent<SubTerra.App.UI.Tutorial.StartBriefingPopupMotion>(), "OnEnable");
            view.SetClaimVisible(true);
            var confirms = 0;
            System.Action close = () => { confirms++; view.SetClaimVisible(false); };
            view.ClaimConfirmRequested += close;
            try
            {
                Assert.That(PopupWindowSorting.Top, Is.Not.Null);
                menu.HandleCloseTopPopup();
                menu.HandleCloseTopPopup();
                Assert.That(confirms, Is.EqualTo(1));
                Assert.That(menu.IsSettingsOpen, Is.False);
            }
            finally { view.ClaimConfirmRequested -= close; }
        }

        [Test]
        public void Escape_ClosesSettingsAndRestoresPreviewVolume()
        {
            menu.OpenSettings();
            float appliedVolume = SubTerra.App.UI.MainMenu.SettingsRuntimeApplier.LoadOrDefaults().MasterVolume;
            AudioListener.volume = 0.123f;
            menu.HandleEscape();
            Assert.That(menu.IsSettingsOpen, Is.False);
            Assert.That(AudioListener.volume, Is.EqualTo(appliedVolume).Within(0.001f));
        }

        [Test]
        public void Settings_ReusesSurfaceControlsWithoutSurfaceGameplayBinder()
        {
            var host = menu.transform.Find("UndergroundSettings");
            Assert.That(host.GetComponent<SurfaceBaseBinder>(), Is.Null);
            Assert.That(host.childCount, Is.EqualTo(1));
            var view = host.GetComponent<SurfaceBaseView>();
            var serialized = new SerializedObject(view);
            foreach (var name in new[] { "settingsRoot", "masterVolumeSlider", "resolutionDropdown",
                "languageDropdown", "frameRateDropdown", "settingsApplyButton", "settingsCancelButton", "settingsDefaultsButton" })
                Assert.That(serialized.FindProperty(name).objectReferenceValue, Is.Not.Null, name);
            menu.OpenSettings();
            Assert.That(host.GetChild(0).gameObject.activeSelf, Is.True);
            menu.CloseSettings();
            Assert.That(host.GetChild(0).gameObject.activeSelf, Is.False);
        }

        private static void Invoke(object target, string method) => target.GetType()
            .GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, null);
    }
}
