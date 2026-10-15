using System.Linq;
using NUnit.Framework;
using SubTerra.App.Core;
using SubTerra.App.Integration;
using SubTerra.App.Save;
using SubTerra.App.UI.HUD;
using SubTerra.App.UI.Save;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace SubTerra.App.Tests.Save
{
    public sealed class SaveRuntimeWiringTests
    {
        [Test]
        public void PendingMineRestore_SaveUsesCachedSeedAndRepeatedEntryKeepsGuard()
        {
            var root = new GameObject("PendingMineRestore");
            root.SetActive(false); // Awake의 실제 사용자 저장 서비스 초기화 방지
            var providerRoot = new GameObject("UnrestoredMineProvider");
            try
            {
                var runtime = root.AddComponent<SaveRuntimeController>();
                var provider = providerRoot.AddComponent<SubTerra.Gameplay.Snapshot.WorldSnapshotSystem>();
                provider.ConfigureBaseWorldIdentity(20260731, 1);
                const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                var cache = (MineWorldCache)typeof(SaveRuntimeController).GetField("mineWorldCache", flags).GetValue(runtime);
                cache.ReplaceFromProvider(new SubTerra.Shared.WorldSnapshotDto { worldSeed = 777 });
                var pending = typeof(SaveRuntimeController).GetField("pendingMineWorldRestore", flags);
                pending.SetValue(runtime, true);
                var capture = typeof(SaveRuntimeController).GetMethod("CaptureContext", flags);
                var context = (SaveCaptureContext)capture.Invoke(runtime, null);
                Assert.That(context.WorldProvider, Is.Null);
                Assert.That(context.MineWorldFallback.worldSeed, Is.EqualTo(777));
                Assert.That(cache.Peek().worldSeed, Is.EqualTo(777));
                Assert.That(runtime.TryStartExploration(out _), Is.False);
                Assert.That((bool)pending.GetValue(runtime), Is.True);
                pending.SetValue(runtime, false);
                Assert.That(((SaveCaptureContext)capture.Invoke(runtime, null)).WorldProvider, Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(providerRoot);
                Object.DestroyImmediate(root);
            }
        }

        private const string BootstrapPath =
            "Assets/_Project/Scenes/Bootstrap/Bootstrap.unity";
        private const string MainMenuPath =
            "Assets/_Project/Scenes/App/MainMenu.unity";
        private const string IntegrationPath =
            "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";

        [Test]
        public void K_S01_RuntimeScenes_HaveSaveRootMenuAndLoadableIntegration()
        {
            var bootstrapScene = EditorSceneManager.OpenScene(
                BootstrapPath,
                OpenSceneMode.Additive);
            try
            {
                Assert.That(
                    FindInScene<GameBootstrapper>(bootstrapScene),
                    Is.Not.Null);
                Assert.That(
                    FindInScene<SaveRuntimeController>(bootstrapScene),
                    Is.Not.Null);
            }
            finally
            {
                EditorSceneManager.CloseScene(bootstrapScene, true);
            }

            var mainMenuScene = EditorSceneManager.OpenScene(
                MainMenuPath,
                OpenSceneMode.Additive);
            try
            {
                var binder = FindInScene<SaveSlotPanelBinder>(mainMenuScene);
                Assert.That(binder, Is.Not.Null);
                Assert.That(binder.HasRequiredReferences(), Is.True);
                Assert.That(FindInScene<EventSystem>(mainMenuScene), Is.Not.Null);
            }
            finally
            {
                EditorSceneManager.CloseScene(mainMenuScene, true);
            }

            Assert.That(
                AssetDatabase.LoadAssetAtPath<SceneAsset>(IntegrationPath),
                Is.Not.Null);
            Assert.That(
                EditorBuildSettings.scenes.Any(
                    scene => scene.enabled && scene.path == IntegrationPath),
                Is.True);

            var integrationScene = EditorSceneManager.OpenScene(
                IntegrationPath,
                OpenSceneMode.Additive);
            try
            {
                Assert.That(
                    FindInScene<IntegrationRuntimeBinder>(integrationScene),
                    Is.Not.Null);
                Assert.That(FindInScene<HudBinder>(integrationScene), Is.Not.Null);
                Assert.That(FindInScene<EventSystem>(integrationScene), Is.Not.Null);

                // 채굴 지형은 Foreground. Background 등 빈 레이어가 먼저 잡히지 않게 이름으로 찾는다.
                var foreground = integrationScene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Tilemap>(true))
                    .FirstOrDefault(tm => tm.gameObject.name == "ForegroundTilemap");
                Assert.That(foreground, Is.Not.Null);
                Assert.That(foreground.GetUsedTilesCount(), Is.GreaterThan(0));

                // M-S01: 4 Tilemap 기준 계층
                var tilemapNames = integrationScene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Tilemap>(true))
                    .Select(tm => tm.gameObject.name)
                    .ToHashSet();
                Assert.That(tilemapNames, Does.Contain("BackgroundTilemap"));
                Assert.That(tilemapNames, Does.Contain("ForegroundTilemap"));
                Assert.That(tilemapNames, Does.Contain("HazardTilemap"));
                Assert.That(tilemapNames, Does.Contain("BuildingTilemap"));

                Assert.That(
                    HasComponent(integrationScene, "SubTerra.Gameplay.Player.PlayerMovement"),
                    Is.True);
                Assert.That(
                    HasComponent(integrationScene, "SubTerra.Gameplay.Snapshot.WorldSnapshotSystem"),
                    Is.True);
                Assert.That(
                    HasComponent(integrationScene, "SubTerra.App.Integration.GameplayHazardStatusBridge"),
                    Is.True);
                Assert.That(
                    HasComponent(integrationScene, "SubTerra.App.Integration.OutpostRuntimeBridge"),
                    Is.True);
                Assert.That(
                    HasComponent(integrationScene, "SubTerra.App.Integration.DroneContextProviderAdapter"),
                    Is.True);
            }
            finally
            {
                EditorSceneManager.CloseScene(integrationScene, true);
            }
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

        private static bool HasComponent(Scene scene, string fullTypeName)
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true))
                .Any(component =>
                    component != null
                    && component.GetType().FullName == fullTypeName);
        }
    }
}
