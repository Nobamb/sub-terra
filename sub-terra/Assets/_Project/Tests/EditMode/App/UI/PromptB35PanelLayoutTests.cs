using NUnit.Framework;
using SubTerra.App.Editor.DataValidation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SubTerra.App.Tests.UI
{
    /// <summary>prompt-B 35: 시설 건설 창 너비·좌우 컨텐츠 간격 +10%.</summary>
    public sealed class PromptB35PanelLayoutTests
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";
        private const string PrefabPath =
            "Assets/_Project/Prefabs/UI/BuildingMenu.prefab";

        [OneTimeSetUp]
        public void BuildLayout()
        {
            PromptB35LayoutBuilder.Build();
        }

        [Test]
        public void BuildingPanel_WidthIncreasedByTenPercent()
        {
            var scene = OpenIntegration();
            var building = FindTransform(scene, "BuildingPanel")
                ?? FindTransform(scene, "BuildingMenu");
            Assert.That(building, Is.Not.Null);

            var rect = building as RectTransform;
            Assert.That(rect, Is.Not.Null);
            Assert.That(
                rect.sizeDelta.x,
                Is.EqualTo(PromptB35LayoutBuilder.BuildingWidth).Within(0.5f));
            Assert.That(rect.sizeDelta.y, Is.EqualTo(560f).Within(0.5f));
        }

        [Test]
        public void BuildingPanel_LeftRightContentGapIncreased()
        {
            var scene = OpenIntegration();
            var building = FindTransform(scene, "BuildingPanel")
                ?? FindTransform(scene, "BuildingMenu");
            Assert.That(building, Is.Not.Null);

            var panelRoot = building.Find("PanelRoot") ?? building;
            var select = panelRoot.GetComponentsInChildren<Button>(true);
            RectTransform left = null;
            foreach (var button in select)
            {
                if (button.name.StartsWith("Select_"))
                {
                    left = button.GetComponent<RectTransform>();
                    break;
                }
            }

            var selection = panelRoot.Find("SelectionText") as RectTransform;
            Assert.That(left, Is.Not.Null, "좌측 시설 버튼이 필요합니다.");
            Assert.That(selection, Is.Not.Null, "우측 설명 텍스트가 필요합니다.");

            float leftEdge = left.anchoredPosition.x + left.sizeDelta.x;
            float rightStart = selection.anchoredPosition.x;
            float gap = rightStart - leftEdge;
            Assert.That(
                gap,
                Is.EqualTo(PromptB35LayoutBuilder.LeftRightGap).Within(1f),
                "좌우 컨텐츠 간격이 +10% 반영되어야 합니다.");
        }

        [Test]
        public void BuildingMenuPrefab_MatchesPanelWidth()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.That(prefab, Is.Not.Null);
            var rect = prefab.GetComponent<RectTransform>();
            Assert.That(rect, Is.Not.Null);
            Assert.That(
                rect.sizeDelta.x,
                Is.EqualTo(PromptB35LayoutBuilder.BuildingWidth).Within(0.5f));
        }

        private static Scene OpenIntegration()
        {
            return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        private static Transform FindTransform(Scene scene, string objectName)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == objectName)
                    {
                        return t;
                    }
                }
            }

            return null;
        }
    }
}
