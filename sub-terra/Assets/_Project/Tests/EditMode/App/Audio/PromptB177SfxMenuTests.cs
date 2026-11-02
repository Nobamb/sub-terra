using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Audio;
using SubTerra.App.Save;
using SubTerra.App.UI.Building;
using SubTerra.App.UI.HUD;
using SubTerra.App.UI.MainMenu;
using SubTerra.App.UI.Progression;
using SubTerra.App.UI.SurfaceBase;
using SubTerra.Shared.Audio;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SubTerra.App.Tests.Audio
{
    public sealed class PromptB177SfxMenuTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private readonly List<GameObject> spawned = new List<GameObject>();
        private readonly List<SfxId> requests = new List<SfxId>();

        [SetUp]
        public void SetUp()
        {
            spawned.Clear();
            requests.Clear();
            SfxRequests.PlayRequested += Record;
        }

        [TearDown]
        public void TearDown()
        {
            SfxRequests.PlayRequested -= Record;
            foreach (var go in spawned) if (go != null) Object.DestroyImmediate(go);
            spawned.Clear();
        }

        private void Record(SfxId id, float pitch) => requests.Add(id);
        private static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, Private).SetValue(target, value);
        private static T Get<T>(object target, string field) =>
            (T)target.GetType().GetField(field, Private).GetValue(target);
        private static object Call(object target, string method, params object[] args) =>
            target.GetType().GetMethod(method, Private).Invoke(target, args);

        private GameObject Spawn(string prefab)
        {
            var go = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/UI/" + prefab + ".prefab"));
            spawned.Add(go);
            return go;
        }

        [Test]
        public void MainAndSurfaceButtons_HoverOnly_AndSettingsStartHidden()
        {
            var main = Spawn("MainMenuPanel").GetComponent<MainMenuView>();
            Call(main, "OnEnable");
            AssertHover(Get<Button>(main, "settingsButton"));
            Assert.That(Get<GameObject>(main, "settingsRoot").activeSelf, Is.False);
            var surface = Spawn("SurfaceBasePanel").GetComponent<SurfaceBaseView>();
            Call(surface, "OnEnable");
            foreach (var field in new[] { "upgradeButton", "settingsButton", "inventoryButton", "exploreButton" })
                AssertHover(Get<Button>(surface, field));
            Assert.That(Get<GameObject>(surface, "settingsRoot").activeSelf, Is.False);
        }

        private static void AssertHover(Button button)
        {
            Assert.That(button, Is.Not.Null);
            var sound = button.GetComponent<UiSfxButton>();
            Assert.That(sound, Is.Not.Null, button.name);
            Assert.That(sound.ClickSfx, Is.EqualTo(SfxId.None), button.name);
            Assert.That(sound.Hover, Is.True, button.name);
        }

        [Test]
        public void LegacyGuideButton_UsesOwnerReference_HoverOnly()
        {
            var host = new GameObject("GuideHost", typeof(RectTransform));
            spawned.Add(host);
            var chrome = host.AddComponent<HudPanelChromeController>();
            var button = host.AddComponent<Button>();
            Set(chrome, "gameGuideOpenButton", button);
            Call(chrome, "WireButtons");
            Call(chrome, "WireButtons");
            AssertHover(button);
        }

        [Test]
        public void ConstructionTabsAndNodes_ClickOnce_AfterRepeatedWiring()
        {
            var go = new GameObject("Building", typeof(RectTransform), typeof(Button), typeof(BuildingMenuEntryButton));
            spawned.Add(go);
            var entry = go.GetComponent<BuildingMenuEntryButton>();
            Call(entry, "OnEnable"); Call(entry, "OnEnable");
            go.GetComponent<Button>().onClick.Invoke();
            Assert.That(requests, Is.EqualTo(new[] { SfxId.UiClick }));
            var surface = Spawn("SurfaceBasePanel");
            var view = surface.GetComponentInChildren<ProgressionPanelView>(true);
            Call(view, "WireCategoryTabsRuntime"); Call(view, "WireCategoryTabsRuntime");
            foreach (var button in Get<Button[]>(view, "categoryTabButtons"))
                Assert.That(button.GetComponent<UiSfxButton>().ClickSfx, Is.EqualTo(SfxId.UiClick));
            foreach (var node in surface.GetComponentsInChildren<UpgradeTreeNodeView>(true))
            {
                Call(node, "OnEnable");
                Assert.That(node.GetComponent<UiSfxButton>().ClickSfx, Is.EqualTo(SfxId.UiClick));
            }
        }

        [Test]
        public void Settings_EditMode_OpenCloseAndSilentRedraw_RequestNothing()
        {
            var view = Spawn("MainMenuPanel").GetComponent<MainMenuView>();
            view.SetSettingsVisible(true);
            var skin = Get<GameObject>(view, "settingsRoot").GetComponent<SettingsMenuSkin>();
            Call(skin, "OnEnable");
            view.SetSettingsDraft(new SettingsValues());
            Get<Slider>(skin, "volumeSlider").SetValueWithoutNotify(0.5f);
            skin.PlayApplySound();
            skin.PlayCloseAnimation(null);
            view.SetSettingsVisible(false);
            Assert.That(requests, Is.Empty);
        }

        [TestCase(ContinueStatus.LoadFailed, 1)]
        [TestCase(ContinueStatus.SceneLoadFailed, 1)]
        [TestCase(ContinueStatus.Success, 0)]
        public void ContinueResult_FailureOnly(ContinueStatus status, int expected)
        {
            var go = new GameObject("Main", typeof(RectTransform));
            spawned.Add(go);
            var view = go.AddComponent<MainMenuView>();
            var binder = go.AddComponent<MainMenuBinder>();
            Set(binder, "view", view);
            var loader = new LoadService(new PhysicalSaveFileSystem(),
                new SavePathPolicy(Path.Combine(Application.dataPath, "../Temp/b177-empty-slots")),
                new SaveDataMapper(new SystemSaveClock()), new SaveJsonCodec(new SaveMigrationService()));
            Set(binder, "presenter", new MainMenuPresenter(view, loader, "test"));
            Call(binder, "OnContinueCompleted", new ContinueResult(status, null));
            Assert.That(requests.FindAll(id => id == SfxId.UiCancelFail).Count, Is.EqualTo(expected));
        }

        [Test]
        public void MissingRuntime_ContinueAndNewGame_EachFailOnce()
        {
            Assert.That(SaveRuntimeController.Instance, Is.Null);
            var go = new GameObject("Main", typeof(RectTransform));
            spawned.Add(go);
            var view = go.AddComponent<MainMenuView>();
            var binder = go.AddComponent<MainMenuBinder>();
            Set(binder, "view", view);
            Call(binder, "OnContinue", 1);
            Assert.That(requests, Is.EqualTo(new[] { SfxId.UiCancelFail }));
            requests.Clear();
            Call(binder, "OnStartNewGame", 1);
            Assert.That(requests, Is.EqualTo(new[] { SfxId.UiCancelFail }));
        }

    }
}
