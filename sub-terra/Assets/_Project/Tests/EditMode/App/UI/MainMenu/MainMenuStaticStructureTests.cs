using System.IO;
using System.Linq;
using NUnit.Framework;
using SubTerra.App.Core;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.UI.MainMenu;
using SubTerra.App.UI.SurfaceBase;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace SubTerra.App.Tests.UI.MainMenu
{
    /// <summary>L-S01~S05 정적/구조 검증.</summary>
    public sealed class MainMenuStaticStructureTests
    {
        [OneTimeSetUp]
        public void BuildScenes()
        {
            PhaseLMenuSceneBuilder.Build();
        }

        [Test]
        public void L_S01_MainMenu_ExposesNewContinueSlotsSettingsQuitVersion()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                PhaseLMenuSceneBuilder.MainMenuPrefabPath);
            Assert.That(prefab, Is.Not.Null);
            var view = prefab.GetComponent<MainMenuView>();
            Assert.That(view, Is.Not.Null);
            Assert.That(view.HasRequiredReferences(), Is.True);
            Assert.That(prefab.GetComponent<MainMenuBinder>(), Is.Not.Null);

            var scene = EditorSceneManager.OpenScene(
                PhaseLMenuSceneBuilder.MainMenuScenePath,
                OpenSceneMode.Additive);
            try
            {
                Assert.That(FindInScene<MainMenuBinder>(scene), Is.Not.Null);
                Assert.That(FindInScene<EventSystem>(scene), Is.Not.Null);
                var eventSystems = 0;
                foreach (var root in scene.GetRootGameObjects())
                {
                    eventSystems += root.GetComponentsInChildren<EventSystem>(true).Length;
                }

                Assert.That(eventSystems, Is.EqualTo(1), "L-S05: active EventSystem must be unique");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void L_S02_SurfaceBase_ExposesSellCraftUpgradeGoalsExplore()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                PhaseLMenuSceneBuilder.SurfaceBasePrefabPath);
            Assert.That(prefab, Is.Not.Null);
            var view = prefab.GetComponent<SurfaceBaseView>();
            Assert.That(view, Is.Not.Null);
            Assert.That(view.HasRequiredReferences(), Is.True);
            var binder = prefab.GetComponent<SurfaceBaseBinder>();
            Assert.That(binder, Is.Not.Null);

            var scene = EditorSceneManager.OpenScene(
                PhaseLMenuSceneBuilder.SurfaceBaseScenePath,
                OpenSceneMode.Additive);
            try
            {
                Assert.That(FindInScene<SurfaceBaseBinder>(scene), Is.Not.Null);
                Assert.That(FindInScene<EventSystem>(scene), Is.Not.Null);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }

            Assert.That(
                EditorBuildSettings.scenes.Any(
                    s => s.enabled && s.path == PhaseLMenuSceneBuilder.SurfaceBaseScenePath),
                Is.True);
            Assert.That(SceneNames.SurfaceBase, Is.EqualTo("SurfaceBase"));
        }

        [Test]
        public void L_S03_SurfaceMenu_DoesNotDuplicateEconomyProgressionLogic()
        {
            var surfaceDir = Path.Combine(
                Application.dataPath, "_Project", "Scripts", "App", "UI", "SurfaceBase");
            var menuDir = Path.Combine(
                Application.dataPath, "_Project", "Scripts", "App", "UI", "MainMenu");
            foreach (var dir in new[] { surfaceDir, menuDir })
            {
                Assert.That(Directory.Exists(dir), Is.True, dir);
                foreach (var file in Directory.GetFiles(dir, "*.cs"))
                {
                    var text = File.ReadAllText(file);
                    Assert.That(text, Does.Not.Contain("TrySellMineral"), file);
                    Assert.That(text, Does.Not.Contain("TryReduceMany"), file);
                    Assert.That(text, Does.Not.Contain("TryPurchase"), file);
                    Assert.That(text, Does.Not.Contain("AddGold("), file);
                }
            }

            // 탐사 가드는 런타임 단일 진입점만.
            var surfacePresenter = File.ReadAllText(
                Path.Combine(surfaceDir, "SurfaceBasePresenter.cs"));
            var surfaceBinder = File.ReadAllText(
                Path.Combine(surfaceDir, "SurfaceBaseBinder.cs"));
            Assert.That(surfacePresenter, Does.Not.Contain("ExplorationStartGuard"));
            Assert.That(surfaceBinder, Does.Contain("TryStartExploration"));
            Assert.That(surfaceBinder, Does.Not.Contain("new ExplorationStartGuard"));

            // 기존 서비스 경로 파일은 유지
            Assert.That(
                File.Exists(Path.Combine(
                    Application.dataPath, "_Project", "Scripts", "App", "Economy", "EconomyService.cs")),
                Is.True);
            Assert.That(
                File.Exists(Path.Combine(
                    Application.dataPath, "_Project", "Scripts", "App", "Progression", "ProgressionService.cs")),
                Is.True);
        }

        [Test]
        public void L_S04_OverwriteAndDeletePaths_RequireConfirmedSlot()
        {
            var gatePath = Path.Combine(
                Application.dataPath,
                "_Project",
                "Scripts",
                "App",
                "UI",
                "MainMenu",
                "NewGameOverwriteGate.cs");
            var runtimePath = Path.Combine(
                Application.dataPath,
                "_Project",
                "Scripts",
                "App",
                "Save",
                "SaveRuntimeController.cs");
            var gate = File.ReadAllText(gatePath);
            var runtime = File.ReadAllText(runtimePath);
            Assert.That(gate, Does.Contain("AwaitingOverwriteConfirm"));
            Assert.That(gate, Does.Contain("CancelOverwrite"));
            Assert.That(runtime, Does.Contain("confirmOverwrite"));
            Assert.That(runtime, Does.Contain("RequiresOverwriteConfirm"));
            Assert.That(runtime, Does.Contain("SceneNames.SurfaceBase"));
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var component = root.GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }
    }
}
