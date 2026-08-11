using System.Linq;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.UI.Building;
using SubTerra.App.UI.HUD;
using SubTerra.App.UI.Inventory;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SubTerra.App.Tests.UI
{
    /// <summary>prompt-B 32번: 우측 중앙 버튼 제거, X 닫기, 단축키 토글, 인벤토리 아이콘.</summary>
    public sealed class PromptB32PanelLayoutTests
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";

        public void BuildLayout()
        {
            PromptB32LayoutBuilder.Build();
        }

        [Test]
        public void IntegrationScene_RemovesRightCenterOpenButtons()
        {
            var scene = OpenIntegration();
            var canvas = Find<Canvas>(scene, "HUDCanvas");
            Assert.That(canvas, Is.Not.Null);

            var shortcutBar = FindTransform(scene, "PanelShortcutBar");
            Assert.That(
                IsRemovedOrNonLegacyShortcut(
                    canvas.transform.Find("OpenGameGuideButton"),
                    shortcutBar),
                Is.True);
            Assert.That(
                IsRemovedOrNonLegacyShortcut(
                    canvas.transform.Find("OpenBuildingMenuButton"),
                    shortcutBar),
                Is.True);
            // 드론 재열기 제거는 prompt-B 34 단계가 담당한다.
        }

        [Test]
        public void BuildingPanel_HasOnlyXClose_AndIncreasedSize()
        {
            var scene = OpenIntegration();
            var building = FindTransform(scene, "BuildingPanel")
                ?? FindTransform(scene, "BuildingMenu");
            Assert.That(building, Is.Not.Null);

            var rect = building as RectTransform;
            Assert.That(
                rect.sizeDelta.x,
                Is.EqualTo(PromptB35LayoutBuilder.BuildingWidth).Within(0.5f));
            Assert.That(
                rect.sizeDelta.y,
                Is.EqualTo(PromptB35LayoutBuilder.BuildingHeight).Within(0.5f));

            var closeButtons = building.GetComponentsInChildren<Button>(true)
                .Where(b => b.name == "CloseButton")
                .ToArray();
            Assert.That(closeButtons.Length, Is.EqualTo(1));

            var label = closeButtons[0].GetComponentInChildren<TMP_Text>(true);
            Assert.That(label, Is.Not.Null);
            Assert.That(label.text, Is.EqualTo("×"));

            // "닫기" 텍스트 버튼이 없어야 한다.
            var koreanClose = building.GetComponentsInChildren<TMP_Text>(true)
                .Any(t => t != null && t.text != null && t.text.Contains("닫기")
                    && t.GetComponentInParent<Button>() != null
                    && t.GetComponentInParent<Button>().name == "CloseButton");
            Assert.That(koreanClose, Is.False);
        }

        [Test]
        public void ShortcutBar_WiresToChromeToggleMethods()
        {
            var scene = OpenIntegration();
            var bar = FindTransform(scene, "PanelShortcutBar");
            var chrome = Find<Canvas>(scene, "HUDCanvas")
                ?.GetComponent<HudPanelChromeController>();
            Assert.That(bar, Is.Not.Null);
            Assert.That(chrome, Is.Not.Null);

            var buttons = bar.GetComponentsInChildren<Button>(true);
            Assert.That(buttons.Length, Is.GreaterThanOrEqualTo(3));

            bool HasListener(Button button, string method)
            {
                for (var i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                {
                    if (button.onClick.GetPersistentTarget(i) == chrome
                        && button.onClick.GetPersistentMethodName(i) == method)
                    {
                        return true;
                    }
                }

                return false;
            }

            var buildingBtn = buttons.FirstOrDefault(b =>
            {
                var t = b.GetComponentInChildren<TMP_Text>(true);
                return t != null && t.text.Contains("시설");
            });
            var inventoryBtn = buttons.FirstOrDefault(b =>
            {
                var t = b.GetComponentInChildren<TMP_Text>(true);
                return t != null
                    && (t.text.Contains("화물") || t.text.Contains("인벤토리"));
            });
            var guideBtn = buttons.FirstOrDefault(b =>
            {
                var t = b.GetComponentInChildren<TMP_Text>(true);
                return t != null && t.text.Contains("가이드");
            });

            Assert.That(buildingBtn, Is.Not.Null);
            Assert.That(inventoryBtn, Is.Not.Null);
            Assert.That(guideBtn, Is.Not.Null);
            Assert.That(HasListener(buildingBtn, nameof(HudPanelChromeController.ToggleBuildingMenu)), Is.True);
            Assert.That(HasListener(inventoryBtn, nameof(HudPanelChromeController.ToggleInventoryPanel)), Is.True);
            Assert.That(HasListener(guideBtn, nameof(HudPanelChromeController.ToggleGameGuide)), Is.True);
        }

        [Test]
        public void ChromeController_ClosesBuildingRootCompletely()
        {
            var host = new GameObject("ChromeHost");
            host.SetActive(false);
            var buildingRoot = new GameObject("BuildingRoot");
            var diggerRoot = new GameObject("DiggerRoot");
            var guideRoot = new GameObject("GuideRoot");
            var inventoryRoot = new GameObject("InventoryRoot");
            var openDigger = new GameObject("OpenDigger");
            openDigger.AddComponent<RectTransform>();
            openDigger.AddComponent<Image>();
            openDigger.AddComponent<Button>();
            var closeBuilding = new GameObject("CloseBuilding");
            closeBuilding.AddComponent<RectTransform>();
            closeBuilding.AddComponent<Image>();
            closeBuilding.AddComponent<Button>();
            try
            {
                var chrome = host.AddComponent<HudPanelChromeController>();
                var so = new SerializedObject(chrome);
                so.FindProperty("buildingMenuRoot").objectReferenceValue = buildingRoot;
                so.FindProperty("buildingCloseButton").objectReferenceValue =
                    closeBuilding.GetComponent<Button>();
                so.FindProperty("buildingOpenButton").objectReferenceValue = null;
                so.FindProperty("diggerBotRoot").objectReferenceValue = diggerRoot;
                so.FindProperty("diggerOpenButton").objectReferenceValue =
                    openDigger.GetComponent<Button>();
                so.FindProperty("gameGuideRoot").objectReferenceValue = guideRoot;
                so.FindProperty("gameGuideOpenButton").objectReferenceValue = null;
                so.FindProperty("inventoryPanelRoot").objectReferenceValue = inventoryRoot;
                so.FindProperty("buildingMenuOpen").boolValue = true;
                so.FindProperty("diggerBotOpen").boolValue = true;
                so.FindProperty("gameGuideOpen").boolValue = false;
                so.FindProperty("inventoryPanelOpen").boolValue = false;
                so.ApplyModifiedPropertiesWithoutUndo();

                host.SetActive(true);

                Assert.That(chrome.HasRequiredReferences(), Is.True);

                chrome.CloseBuildingMenu();
                Assert.That(chrome.IsBuildingMenuOpen, Is.False);
                Assert.That(buildingRoot.activeSelf, Is.False);

                chrome.ToggleBuildingMenu();
                Assert.That(chrome.IsBuildingMenuOpen, Is.True);
                Assert.That(buildingRoot.activeSelf, Is.True);

                chrome.ToggleGameGuide();
                Assert.That(chrome.IsGameGuideOpen, Is.True);
                Assert.That(guideRoot.activeSelf, Is.True);
                chrome.ToggleGameGuide();
                Assert.That(chrome.IsGameGuideOpen, Is.False);

                chrome.ToggleInventoryPanel();
                Assert.That(chrome.IsInventoryPanelOpen, Is.True);
                Assert.That(inventoryRoot.activeSelf, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(buildingRoot);
                Object.DestroyImmediate(diggerRoot);
                Object.DestroyImmediate(guideRoot);
                Object.DestroyImmediate(inventoryRoot);
                Object.DestroyImmediate(openDigger);
                Object.DestroyImmediate(closeBuilding);
            }
        }

        [Test]
        public void MineralIcons_UseTerrainVisuals_NotOnlyPlaceholder()
        {
            var copper = AssetDatabase.LoadAssetAtPath<MineralData>(
                "Assets/_Project/Data/Minerals/Mineral_Copper.asset");
            var iron = AssetDatabase.LoadAssetAtPath<MineralData>(
                "Assets/_Project/Data/Minerals/Mineral_Iron.asset");
            var lithium = AssetDatabase.LoadAssetAtPath<MineralData>(
                "Assets/_Project/Data/Minerals/Mineral_Lithium.asset");

            Assert.That(copper, Is.Not.Null);
            Assert.That(iron, Is.Not.Null);
            Assert.That(lithium, Is.Not.Null);
            Assert.That(copper.Icon, Is.Not.Null);
            Assert.That(iron.Icon, Is.Not.Null);
            Assert.That(lithium.Icon, Is.Not.Null);
            Assert.That(copper.Icon.name, Does.Contain("Copper").IgnoreCase);
            Assert.That(iron.Icon.name, Does.Contain("Iron").IgnoreCase);
            Assert.That(lithium.Icon.name, Does.Contain("Lithium").IgnoreCase);
        }

        [Test]
        public void InventoryPanel_HasStackRowsWithIcons()
        {
            var scene = OpenIntegration();
            var inventory = FindTransform(scene, "InventoryPanel");
            Assert.That(inventory, Is.Not.Null);
            Assert.That(inventory.Find("PanelRoot"), Is.Not.Null);

            var view = inventory.GetComponent<InventoryPanelView>();
            Assert.That(view, Is.Not.Null);
            Assert.That(view.HasRequiredReferences(), Is.True);

            var rows = inventory.GetComponentsInChildren<InventoryStackRowView>(true);
            Assert.That(rows.Length, Is.GreaterThanOrEqualTo(3));

            foreach (var row in rows)
            {
                var iconTf = row.transform.Find("Icon");
                Assert.That(iconTf, Is.Not.Null);
                var image = iconTf.GetComponent<Image>();
                Assert.That(image, Is.Not.Null);
                Assert.That(image.sprite, Is.Not.Null);
            }
        }

        private static Scene OpenIntegration()
        {
            var scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            return scene;
        }

        private static T Find<T>(Scene scene, string name)
            where T : Component
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true))
                .FirstOrDefault(item => item.name == name);
        }

        private static Transform FindTransform(Scene scene, string name)
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(item => item.name == name);
        }

        private static bool IsRemovedOrNonLegacyShortcut(Transform button, Transform shortcutBar)
        {
            return button == null
                || !button.gameObject.activeSelf
                || (shortcutBar != null && button.IsChildOf(shortcutBar));
        }
    }
}
