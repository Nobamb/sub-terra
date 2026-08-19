using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.Progression;
using SubTerra.App.UI.Progression;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SubTerra.App.Tests.UI
{
    public sealed class PromptB56UpgradePanelTests
    {
        [Test]
        public void UpgradePanel_DoesNotContainSharedEquipmentUpgradeTitle()
        {
            var scene = SceneManager.GetSceneByPath(
                PromptB56UpgradePanelBuilder.IntegrationScenePath);
            var openedHere = !scene.IsValid() || !scene.isLoaded;
            if (openedHere)
            {
                scene = EditorSceneManager.OpenScene(
                    PromptB56UpgradePanelBuilder.IntegrationScenePath,
                    OpenSceneMode.Additive);
            }

            try
            {
                var labels = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<ProgressionPanelView>(true))
                    .SelectMany(view => view.GetComponentsInChildren<TMP_Text>(true));

                Assert.That(
                    labels.Any(label => label.text == "장비 업그레이드 [U]"),
                    Is.False);
            }
            finally
            {
                if (openedHere)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        [Test]
        public void DeepZoneTab_WhenUnlocked_ShowsMaximumLevelMessage()
        {
            var root = new GameObject("ProgressionPanel");
            var detailObject = new GameObject("UpgradeDetail");
            detailObject.transform.SetParent(root.transform);
            var detail = detailObject.AddComponent<TextMeshProUGUI>();
            var view = root.AddComponent<ProgressionPanelView>();
            SetPrivateField(view, "detailText", detail);

            try
            {
                view.SetActiveCategory(UpgradeCategory.DeepZone);
                view.SetDeepZoneAccess(new ZoneAccessResult(true, false, string.Empty));

                Assert.That(detail.text, Does.Contain("심층 구역: 최대 레벨"));
                Assert.That(detail.text, Does.Contain("더 이상 업그레이드할 수 없습니다."));
                Assert.That(detail.text, Does.Not.Contain("해금 조건"));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }
    }
}
