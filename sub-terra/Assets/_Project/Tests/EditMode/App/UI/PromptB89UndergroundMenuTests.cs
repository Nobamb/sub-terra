using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.Integration;
using SubTerra.App.UI.HUD;
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
        public void Escape_ClosesExistingPanelBeforeOpeningSettings()
        {
            var chrome = menu.GetComponent<HudPanelChromeController>();
            chrome.OpenBuildingMenu();
            menu.HandleEscape();
            Assert.That(chrome.IsBuildingMenuOpen, Is.False);
            Assert.That(menu.IsSettingsOpen, Is.False);
            menu.HandleEscape();
            Assert.That(menu.IsSettingsOpen, Is.True);
            menu.HandleEscape();
            Assert.That(menu.IsSettingsOpen, Is.False);
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
