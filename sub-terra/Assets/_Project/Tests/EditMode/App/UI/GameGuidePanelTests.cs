using System.Linq;
using NUnit.Framework;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.UI.HUD;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SubTerra.App.Tests.UI
{
    /// <summary>prompt-B 31번 게임 가이드 창·Digger 최하단 배치 정적 검증.</summary>
    public sealed class GameGuidePanelTests
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";
        private const string GuidePrefabPath =
            "Assets/_Project/Prefabs/UI/GameGuidePanel.prefab";

        [OneTimeSetUp]
        public void BuildLayout()
        {
            GameGuidePanelBuilder.Build();
        }

        [Test]
        public void GuidePrefab_HasTabsScrollAndClose()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GuidePrefabPath);
            Assert.That(prefab, Is.Not.Null);

            var view = prefab.GetComponent<GameGuidePanelView>();
            Assert.That(view, Is.Not.Null);
            Assert.That(view.HasRequiredReferences(), Is.True);
            Assert.That(view.CloseButton, Is.Not.Null);

            var scroll = prefab.GetComponentInChildren<ScrollRect>(true);
            Assert.That(scroll, Is.Not.Null);
            Assert.That(scroll.vertical, Is.True);
            Assert.That(scroll.horizontal, Is.False);

            var body = prefab.GetComponentsInChildren<TextMeshProUGUI>(true)
                .FirstOrDefault(t => t.name == "BodyText");
            Assert.That(body, Is.Not.Null);
            Assert.That(body.fontSize, Is.EqualTo(GameGuidePanelView.GuideFontSize).Within(0.1f));
            Assert.That(body.font, Is.Not.Null);

            var tabButtons = prefab.GetComponentsInChildren<Button>(true)
                .Where(b => b.name.StartsWith("TabButton_"))
                .ToArray();
            Assert.That(tabButtons.Length, Is.EqualTo(GameGuidePanelView.TabCount));
        }

        [Test]
        public void GuideContent_CoversThreeMajorSections()
        {
            var controls = GameGuidePanelView.GetTabBody(GameGuidePanelView.GuideTab.Controls);
            var mechanics = GameGuidePanelView.GetTabBody(GameGuidePanelView.GuideTab.Mechanics);
            var resources = GameGuidePanelView.GetTabBody(GameGuidePanelView.GuideTab.Resources);

            Assert.That(controls, Does.Contain("좌우 이동"));
            Assert.That(controls, Does.Contain("채굴"));
            Assert.That(mechanics, Does.Contain("독성 가스"));
            Assert.That(mechanics, Does.Contain("버팀목"));
            Assert.That(resources, Does.Contain("리튬"));
            Assert.That(resources, Does.Contain("Digger-Bot"));
        }

        [Test]
        public void IntegrationScene_GuideStartsHidden_DiggerAtBottom_LegendDisabled()
        {
            var scene = OpenIntegration();
            var canvas = Find<Canvas>(scene, "HUDCanvas");
            Assert.That(canvas, Is.Not.Null);

            var digger = FindRect(scene, "DroneDialoguePanel");
            Assert.That(digger, Is.Not.Null);
            Assert.That(digger.anchoredPosition.y, Is.EqualTo(24f).Within(0.5f));

            var legend = FindTransform(scene, "TerrainLegendPanel");
            if (legend != null)
            {
                Assert.That(legend.gameObject.activeSelf, Is.False);
            }

            var guide = FindTransform(scene, "GameGuidePanel");
            Assert.That(guide, Is.Not.Null);
            Assert.That(guide.gameObject.activeSelf, Is.False);

            var guideView = guide.GetComponent<GameGuidePanelView>();
            Assert.That(guideView, Is.Not.Null);
            Assert.That(guideView.HasRequiredReferences(), Is.True);

            var openGuide = canvas.transform.Find("OpenGameGuideButton");
            Assert.That(openGuide, Is.Not.Null);
            Assert.That(openGuide.GetComponent<Button>(), Is.Not.Null);
            Assert.That(openGuide.gameObject.activeSelf, Is.True);

            var chrome = canvas.GetComponent<HudPanelChromeController>();
            Assert.That(chrome, Is.Not.Null);
            Assert.That(chrome.HasRequiredReferences(), Is.True);
            Assert.That(chrome.IsGameGuideOpen, Is.False);
        }

        [Test]
        public void ChromeController_TogglesGameGuide()
        {
            var host = new GameObject("ChromeHost");
            var guideRoot = new GameObject("GuideRoot");
            var buildingRoot = new GameObject("BuildingRoot");
            var diggerRoot = new GameObject("DiggerRoot");
            var openGuide = new GameObject("OpenGuide");
            openGuide.AddComponent<RectTransform>();
            openGuide.AddComponent<Image>();
            openGuide.AddComponent<Button>();
            var openBuilding = CreateButton("OpenBuilding");
            var openDigger = CreateButton("OpenDigger");
            try
            {
                var chrome = host.AddComponent<HudPanelChromeController>();
                var so = new SerializedObject(chrome);
                so.FindProperty("buildingMenuRoot").objectReferenceValue = buildingRoot;
                so.FindProperty("buildingOpenButton").objectReferenceValue = openBuilding;
                so.FindProperty("diggerBotRoot").objectReferenceValue = diggerRoot;
                so.FindProperty("diggerOpenButton").objectReferenceValue = openDigger;
                so.FindProperty("gameGuideRoot").objectReferenceValue = guideRoot;
                so.FindProperty("gameGuideOpenButton").objectReferenceValue =
                    openGuide.GetComponent<Button>();
                so.FindProperty("buildingMenuOpen").boolValue = true;
                so.FindProperty("diggerBotOpen").boolValue = true;
                so.FindProperty("gameGuideOpen").boolValue = false;
                so.ApplyModifiedPropertiesWithoutUndo();

                chrome.SendMessage("Awake", SendMessageOptions.DontRequireReceiver);

                Assert.That(chrome.IsGameGuideOpen, Is.False);
                Assert.That(guideRoot.activeSelf, Is.False);

                chrome.OpenGameGuide();
                Assert.That(chrome.IsGameGuideOpen, Is.True);
                Assert.That(guideRoot.activeSelf, Is.True);
                // 가이드 열기 버튼은 항상 접근 가능.
                Assert.That(openGuide.activeSelf, Is.True);

                chrome.CloseGameGuide();
                Assert.That(chrome.IsGameGuideOpen, Is.False);
                Assert.That(guideRoot.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(guideRoot);
                Object.DestroyImmediate(buildingRoot);
                Object.DestroyImmediate(diggerRoot);
                Object.DestroyImmediate(openGuide);
                Object.DestroyImmediate(openBuilding.gameObject);
                Object.DestroyImmediate(openDigger.gameObject);
            }
        }

        [Test]
        public void GuideView_SelectsTabsAndKeepsFontSize()
        {
            var root = new GameObject("GuideRuntime");
            var panel = new GameObject("PanelRoot");
            panel.transform.SetParent(root.transform);
            var bodyGo = new GameObject("BodyText", typeof(RectTransform));
            bodyGo.transform.SetParent(panel.transform);
            var body = bodyGo.AddComponent<TextMeshProUGUI>();
            body.fontSize = 12f;

            var contentGo = new GameObject("Content", typeof(RectTransform));
            var content = contentGo.GetComponent<RectTransform>();
            content.sizeDelta = new Vector2(800f, 100f);

            var tab0 = CreateButton("Tab0");
            var tab1 = CreateButton("Tab1");
            var tab2 = CreateButton("Tab2");
            var close = CreateButton("Close");

            try
            {
                var view = root.AddComponent<GameGuidePanelView>();
                var so = new SerializedObject(view);
                so.FindProperty("panelRoot").objectReferenceValue = panel;
                so.FindProperty("closeButton").objectReferenceValue = close;
                so.FindProperty("bodyText").objectReferenceValue = body;
                so.FindProperty("contentRoot").objectReferenceValue = content;
                var tabs = so.FindProperty("tabButtons");
                tabs.arraySize = 3;
                tabs.GetArrayElementAtIndex(0).objectReferenceValue = tab0;
                tabs.GetArrayElementAtIndex(1).objectReferenceValue = tab1;
                tabs.GetArrayElementAtIndex(2).objectReferenceValue = tab2;
                so.ApplyModifiedPropertiesWithoutUndo();

                view.SendMessage("Awake", SendMessageOptions.DontRequireReceiver);
                Assert.That(body.fontSize, Is.EqualTo(GameGuidePanelView.GuideFontSize).Within(0.1f));
                Assert.That(body.text, Does.Contain("기본 게임 조작법"));

                view.SelectTab(GameGuidePanelView.GuideTab.Mechanics);
                Assert.That(view.ActiveTab, Is.EqualTo(GameGuidePanelView.GuideTab.Mechanics));
                Assert.That(body.text, Does.Contain("핵심 게임 메커니즘"));
                Assert.That(body.fontSize, Is.EqualTo(GameGuidePanelView.GuideFontSize).Within(0.1f));

                view.SelectTab(GameGuidePanelView.GuideTab.Resources);
                Assert.That(body.text, Does.Contain("리튬"));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(tab0.gameObject);
                Object.DestroyImmediate(tab1.gameObject);
                Object.DestroyImmediate(tab2.gameObject);
                Object.DestroyImmediate(close.gameObject);
            }
        }

        private static Button CreateButton(string name)
        {
            var go = new GameObject(name);
            go.AddComponent<RectTransform>();
            go.AddComponent<Image>();
            return go.AddComponent<Button>();
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
