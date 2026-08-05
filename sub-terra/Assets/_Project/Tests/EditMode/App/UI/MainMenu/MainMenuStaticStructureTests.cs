using System.IO;
using System.Linq;
using NUnit.Framework;
using SubTerra.App.Core;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.UI;
using SubTerra.App.UI.MainMenu;
using SubTerra.App.UI.SurfaceBase;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace SubTerra.App.Tests.UI.MainMenu
{
    /// <summary>L-S01~S05 정적/구조 검증.</summary>
    public sealed class MainMenuStaticStructureTests
    {
        [OneTimeSetUp]
        public void BuildScenes()
        {
            PhaseLMenuSceneBuilder.Build();
            PhaseOUiPolishBuilder.Build();
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
            foreach (var text in prefab.GetComponentsInChildren<TMP_Text>(true))
            {
                Assert.That(text.font, Is.Not.Null, text.name);
                Assert.That(text.font.material, Is.Not.Null, text.name + " material");
                Assert.That(text.font.atlasTextures, Is.Not.Null, text.name + " atlas");
                Assert.That(text.font.atlasTextures.Length, Is.GreaterThan(0), text.name + " atlas count");
                Assert.That(text.font.atlasTextures[0], Is.Not.Null, text.name + " atlas[0]");
            }

            var scene = EditorSceneManager.OpenScene(
                PhaseLMenuSceneBuilder.MainMenuScenePath,
                OpenSceneMode.Additive);
            try
            {
                Assert.That(FindInScene<MainMenuBinder>(scene), Is.Not.Null);
                Assert.That(FindInScene<SafeAreaFitter>(scene), Is.Not.Null);
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
                Assert.That(FindInScene<SafeAreaFitter>(scene), Is.Not.Null);
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

        [Test]
        public void Prompt16_MainMenuAndOverwriteDialog_AreCenteredAndEnlarged()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                PhaseLMenuSceneBuilder.MainMenuPrefabPath);
            var content = prefab.transform.Find("MenuContent") as RectTransform;
            Assert.That(content, Is.Not.Null);
            Assert.That(content.anchoredPosition, Is.EqualTo(Vector2.zero));
            Assert.That(content.sizeDelta.x, Is.GreaterThanOrEqualTo(900f));
            Assert.That(content.sizeDelta.y, Is.GreaterThanOrEqualTo(780f));

            var overwrite = prefab.transform.Find("OverwriteConfirm") as RectTransform;
            Assert.That(overwrite, Is.Not.Null);
            Assert.That(overwrite.anchoredPosition, Is.EqualTo(Vector2.zero));
            Assert.That(overwrite.sizeDelta.x, Is.EqualTo(624f).Within(0.1f));
            Assert.That(overwrite.sizeDelta.y, Is.EqualTo(286f).Within(0.1f));

            var message = overwrite.Find("OverwriteMessage").GetComponent<TMP_Text>();
            var yes = overwrite.Find("OverwriteYes").GetComponent<RectTransform>();
            var no = overwrite.Find("OverwriteNo").GetComponent<RectTransform>();
            Assert.That(message.fontSize, Is.EqualTo(23.4f).Within(0.1f));
            Assert.That(message.rectTransform.sizeDelta, Is.EqualTo(new Vector2(546f, 78f)));
            Assert.That(message.rectTransform.anchoredPosition.y, Is.EqualTo(28.6f).Within(0.1f));
            Assert.That(yes.sizeDelta, Is.EqualTo(new Vector2(182f, 57.2f)));
            Assert.That(no.sizeDelta, Is.EqualTo(new Vector2(182f, 57.2f)));
            Assert.That(yes.anchoredPosition.y, Is.EqualTo(-28.6f).Within(0.1f));
            Assert.That(no.anchoredPosition.y, Is.EqualTo(-28.6f).Within(0.1f));
        }

        [Test]
        public void Prompt16_SurfaceBaseInformation_IsOneCenteredGroup()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                PhaseLMenuSceneBuilder.SurfaceBasePrefabPath);
            var content = prefab.transform.Find("SurfaceBaseContent") as RectTransform;
            Assert.That(content, Is.Not.Null);
            Assert.That(content.anchorMin, Is.EqualTo(new Vector2(0.5f, 0.5f)));
            Assert.That(content.anchorMax, Is.EqualTo(new Vector2(0.5f, 0.5f)));
            Assert.That(content.anchoredPosition, Is.EqualTo(Vector2.zero));

            var centeredPaths = new[]
            {
                "EnergyText",
                "GoalsText",
                "DeepZoneText",
                "RecentRunText",
                "ExploreButton",
                "EconomyPanel/EcoStatus",
                "ProgressionPanel/UpgradeList",
                "ProgressionPanel/ProgDeep"
            };
            // prompt-B 31-1: 새로고침 제거, 설정·종료 추가.
            Assert.That(content.Find("RefreshButton"), Is.Null);
            Assert.That(content.Find("SettingsButton"), Is.Not.Null);
            Assert.That(content.Find("QuitButton"), Is.Not.Null);
            foreach (var path in centeredPaths)
            {
                var rect = content.Find(path) as RectTransform;
                Assert.That(rect, Is.Not.Null, path);
                Assert.That(rect.anchoredPosition.x, Is.EqualTo(0f).Within(0.1f), path);
            }
        }

        [Test]
        public void Prompt16_IntegrationTerrain_IsReadableBoundedAndExplained()
        {
            var scene = EditorSceneManager.OpenScene(
                PhaseOUiPolishBuilder.IntegrationScenePath,
                OpenSceneMode.Additive);
            try
            {
                var tilemap = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Tilemap>(true))
                    .FirstOrDefault(item => item.name == "ForegroundTilemap");
                Assert.That(tilemap, Is.Not.Null);
                // Phase B owns left/right edges as BoundaryRock; Phase O must not repaint them as Rock.
                var boundary = AssetDatabase.LoadAssetAtPath<TileBase>(
                    "Assets/_Project/Tilemaps/DemoWorld/BoundaryRock.asset");
                Assert.That(boundary, Is.Not.Null);
                Assert.That(
                    tilemap.GetTile(new Vector3Int(-40, -2, 0)),
                    Is.SameAs(boundary));
                Assert.That(
                    tilemap.GetTile(new Vector3Int(40, -2, 0)),
                    Is.SameAs(boundary));
                Assert.That(
                    tilemap.GetTile(new Vector3Int(-40, 5, 0)),
                    Is.SameAs(boundary));
                Assert.That(
                    tilemap.GetTile(new Vector3Int(40, 5, 0)),
                    Is.SameAs(boundary));
                Assert.That(
                    tilemap.GetSprite(new Vector3Int(0, -2, 0)).bounds.size.x,
                    Is.EqualTo(1f).Within(0.01f));

                var terrainCollider = tilemap.GetComponent<TilemapCollider2D>();
                Assert.That(terrainCollider, Is.Not.Null);
                Assert.That(terrainCollider.enabled, Is.True);
                Assert.That(
                    terrainCollider.compositeOperation,
                    Is.EqualTo(Collider2D.CompositeOperation.None));
                Assert.That(tilemap.GetTile(new Vector3Int(0, -41, 0)), Is.Not.Null);

                var legend = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<TMP_Text>(true))
                    .FirstOrDefault(item => item.name == "LegendText");
                Assert.That(legend, Is.Not.Null);
                var legendPanel = legend.transform.parent.GetComponent<RectTransform>();
                Assert.That(legendPanel.anchorMin, Is.EqualTo(new Vector2(0.5f, 0f)));
                Assert.That(legendPanel.anchorMax, Is.EqualTo(new Vector2(0.5f, 0f)));
                Assert.That(legendPanel.anchoredPosition.x, Is.EqualTo(0f));
                Assert.That(legend.text, Does.Contain("[##] 암반"));
                Assert.That(legend.text, Does.Contain("(Cu) 구리"));
                Assert.That(legend.text, Does.Contain("~~~ 가스"));
                Assert.That(legend.text, Does.Contain("봉인 신호"));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
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
    }
}
