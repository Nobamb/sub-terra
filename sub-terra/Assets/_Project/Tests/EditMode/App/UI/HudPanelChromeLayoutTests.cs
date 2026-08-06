using System.Linq;
using NUnit.Framework;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.UI.Building;
using SubTerra.App.UI.Drone;
using SubTerra.App.UI.HUD;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SubTerra.App.Tests.UI
{
    /// <summary>prompt-B 30번 HUD 창 겹침 해소·토글 구조 정적 검증.</summary>
    public sealed class HudPanelChromeLayoutTests
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";

        [OneTimeSetUp]
        public void BuildLayout()
        {
            HudPanelChromeLayoutBuilder.Build();
            // prompt-B 31 / 31-1 가이드·좌측 건설·Surface 설정 등 적용.
            PromptB31_1LayoutBuilder.Build();
            // prompt-B 31-2 건설 목록 텍스트 제거·우측 폭·인벤토리 패널.
            PromptB31_2LayoutBuilder.Build();
            // prompt-B 32: 우측 중앙 버튼 제거·X 닫기·단축키 단일 경로.
            PromptB32LayoutBuilder.Build();
        }

        [Test]
        public void DialoguePrefab_IntegratesRecommendationTexts()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/UI/DroneDialoguePanel.prefab");
            Assert.That(prefab, Is.Not.Null);
            var view = prefab.GetComponent<DroneDialoguePanelView>();
            Assert.That(view, Is.Not.Null);
            Assert.That(view.HasRequiredReferences(), Is.True);
            Assert.That(view.HasIntegratedReasonTexts(), Is.True);
        }

        [Test]
        public void BuildingMenuPrefab_HasXCloseButton()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/UI/BuildingMenu.prefab");
            Assert.That(prefab, Is.Not.Null);
            var view = prefab.GetComponent<BuildingMenuView>();
            Assert.That(view, Is.Not.Null);
            Assert.That(view.CloseButton, Is.Not.Null);
            var label = view.CloseButton.GetComponentInChildren<TMPro.TMP_Text>(true);
            Assert.That(label, Is.Not.Null);
            Assert.That(label.text, Is.EqualTo("×"));
        }

        [Test]
        public void IntegrationScene_DoesNotOverlapPrimaryPanels()
        {
            var scene = OpenIntegration();
            var canvas = Find<Canvas>(scene, "HUDCanvas");
            Assert.That(canvas, Is.Not.Null);

            var basic = FindRect(scene, "BasicHUD");
            var title = FindRect(scene, "ObjectiveTitle");
            var digger = FindRect(scene, "DroneDialoguePanel");
            var legend = FindTransform(scene, "TerrainLegendPanel");
            var building = FindRect(scene, "BuildingPanel")
                ?? FindRect(scene, "BuildingMenu");
            var reason = FindTransform(scene, "DroneReasonPanel");
            var guide = FindTransform(scene, "GameGuidePanel");

            Assert.That(basic, Is.Not.Null);
            Assert.That(title, Is.Not.Null);
            Assert.That(digger, Is.Not.Null);
            Assert.That(building, Is.Not.Null);
            Assert.That(guide, Is.Not.Null);

            // 스테이터스 유지, 퀘스트는 그 아래.
            Assert.That(basic.anchoredPosition.y, Is.EqualTo(-16f).Within(0.5f));
            Assert.That(title.anchoredPosition.y, Is.LessThan(basic.anchoredPosition.y - basic.sizeDelta.y));

            // prompt-B 31: Digger-Bot은 기존 범례 자리(최하단), 범례는 비활성.
            Assert.That(digger.anchoredPosition.y, Is.EqualTo(24f).Within(0.5f));
            if (legend != null)
            {
                Assert.That(legend.gameObject.activeSelf, Is.False);
            }

            Assert.That(guide.gameObject.activeSelf, Is.False);

            // 우측 단독 추천 창은 비활성.
            if (reason != null)
            {
                Assert.That(reason.gameObject.activeSelf, Is.False);
            }

            var chrome = canvas.GetComponent<HudPanelChromeController>();
            Assert.That(chrome, Is.Not.Null);
            Assert.That(chrome.HasRequiredReferences(), Is.True);

            // prompt-B 32: 우측 중앙 시설/가이드 재열기 버튼 제거, 드론만 유지.
            var openBuilding = canvas.transform.Find("OpenBuildingMenuButton");
            var openDigger = canvas.transform.Find("OpenDiggerBotButton");
            var openGuide = canvas.transform.Find("OpenGameGuideButton");
            Assert.That(openBuilding, Is.Null);
            Assert.That(openGuide, Is.Null);
            Assert.That(openDigger, Is.Not.Null);
            Assert.That(openDigger.GetComponent<Button>(), Is.Not.Null);

            // prompt-B 32: 좌측 목록 텍스트 숨김, 패널 폭 480(+20), I키용 인벤토리.
            var listText = building.Find("PanelRoot/BuildingListText")
                ?? building.Find("BuildingListText");
            if (listText != null)
            {
                Assert.That(listText.gameObject.activeSelf, Is.False);
            }

            Assert.That(building.sizeDelta.x, Is.EqualTo(480f).Within(0.5f));
            Assert.That(building.sizeDelta.y, Is.EqualTo(560f).Within(0.5f));
            var selection = building.Find("PanelRoot/SelectionText") as RectTransform
                ?? building.Find("SelectionText") as RectTransform;
            if (selection != null)
            {
                // 좌측 버튼(20+132) + 10px 간격 = 162.
                Assert.That(selection.anchoredPosition.x, Is.EqualTo(162f).Within(0.5f));
            }

            var inventory = FindTransform(scene, "InventoryPanel");
            Assert.That(inventory, Is.Not.Null);
            Assert.That(inventory.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void ChromeController_TogglesPanels()
        {
            var host = new GameObject("ChromeHost");
            var buildingRoot = new GameObject("BuildingRoot");
            var diggerRoot = new GameObject("DiggerRoot");
            var guideRoot = new GameObject("GuideRoot");
            var inventoryRoot = new GameObject("InventoryRoot");
            var openBuilding = new GameObject("OpenBuilding");
            openBuilding.AddComponent<RectTransform>();
            openBuilding.AddComponent<UnityEngine.UI.Image>();
            openBuilding.AddComponent<Button>();
            var openDigger = new GameObject("OpenDigger");
            openDigger.AddComponent<RectTransform>();
            openDigger.AddComponent<UnityEngine.UI.Image>();
            openDigger.AddComponent<Button>();
            var openGuide = new GameObject("OpenGuide");
            openGuide.AddComponent<RectTransform>();
            openGuide.AddComponent<UnityEngine.UI.Image>();
            openGuide.AddComponent<Button>();
            try
            {
                var chrome = host.AddComponent<HudPanelChromeController>();
                var so = new SerializedObject(chrome);
                so.FindProperty("buildingMenuRoot").objectReferenceValue = buildingRoot;
                so.FindProperty("buildingOpenButton").objectReferenceValue =
                    openBuilding.GetComponent<Button>();
                so.FindProperty("diggerBotRoot").objectReferenceValue = diggerRoot;
                so.FindProperty("diggerOpenButton").objectReferenceValue =
                    openDigger.GetComponent<Button>();
                so.FindProperty("gameGuideRoot").objectReferenceValue = guideRoot;
                so.FindProperty("gameGuideOpenButton").objectReferenceValue =
                    openGuide.GetComponent<Button>();
                so.FindProperty("inventoryPanelRoot").objectReferenceValue = inventoryRoot;
                so.FindProperty("buildingMenuOpen").boolValue = true;
                so.FindProperty("diggerBotOpen").boolValue = true;
                so.FindProperty("gameGuideOpen").boolValue = false;
                so.FindProperty("inventoryPanelOpen").boolValue = false;
                so.ApplyModifiedPropertiesWithoutUndo();

                // Awake 경로 재현.
                chrome.SendMessage("Awake", SendMessageOptions.DontRequireReceiver);

                chrome.CloseBuildingMenu();
                Assert.That(chrome.IsBuildingMenuOpen, Is.False);
                Assert.That(buildingRoot.activeSelf, Is.False);
                // prompt-B 32: 우측 중앙 재열기 버튼은 사용하지 않는다.
                Assert.That(openBuilding.activeSelf, Is.False);

                chrome.ToggleBuildingMenu();
                Assert.That(chrome.IsBuildingMenuOpen, Is.True);

                chrome.OpenInventoryPanel();
                Assert.That(chrome.IsInventoryPanelOpen, Is.True);
                Assert.That(inventoryRoot.activeSelf, Is.True);
                chrome.CloseInventoryPanel();
                Assert.That(chrome.IsInventoryPanelOpen, Is.False);
                Assert.That(inventoryRoot.activeSelf, Is.False);

                chrome.OpenBuildingMenu();
                Assert.That(chrome.IsBuildingMenuOpen, Is.True);
                Assert.That(buildingRoot.activeSelf, Is.True);
                Assert.That(openBuilding.activeSelf, Is.False);

                chrome.CloseDiggerBot();
                Assert.That(chrome.IsDiggerBotOpen, Is.False);
                Assert.That(diggerRoot.activeSelf, Is.False);
                Assert.That(openDigger.activeSelf, Is.True);

                chrome.OpenDiggerBot();
                Assert.That(chrome.IsDiggerBotOpen, Is.True);
                Assert.That(diggerRoot.activeSelf, Is.True);
                Assert.That(openDigger.activeSelf, Is.False);

                chrome.OpenGameGuide();
                Assert.That(chrome.IsGameGuideOpen, Is.True);
                Assert.That(guideRoot.activeSelf, Is.True);
                // 우측 중앙 가이드 버튼은 숨김(상단 G 단축키 사용).
                Assert.That(openGuide.activeSelf, Is.False);

                chrome.CloseGameGuide();
                Assert.That(chrome.IsGameGuideOpen, Is.False);
                Assert.That(guideRoot.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(buildingRoot);
                Object.DestroyImmediate(diggerRoot);
                Object.DestroyImmediate(guideRoot);
                Object.DestroyImmediate(inventoryRoot);
                Object.DestroyImmediate(openBuilding);
                Object.DestroyImmediate(openDigger);
                Object.DestroyImmediate(openGuide);
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

        private static RectTransform FindRect(Scene scene, string name)
        {
            return FindTransform(scene, name) as RectTransform;
        }

        private static Transform FindTransform(Scene scene, string name)
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(item => item.name == name);
        }
    }
}
