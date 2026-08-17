using System.Linq;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.Progression;
using SubTerra.App.Tutorial;
using SubTerra.App.UI.Tutorial;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SubTerra.App.Tests.Tutorial
{
    /// <summary>prompt-B 53 퀘스트 순서·후반 조건·상세창 Scene 연결 회귀 검증.</summary>
    public sealed class PromptB53QuestUiTests
    {
        [Test]
        public void PromptB53_CatalogFollowsPlayableDependencyOrder()
        {
            var expected = new[]
            {
                DemoObjectiveIds.ExploreStart,
                DemoObjectiveIds.MineCopperIron,
                DemoObjectiveIds.PathGuide,
                DemoObjectiveIds.StructuralCrack,
                DemoObjectiveIds.PlaceSupport,
                DemoObjectiveIds.GasEncounter,
                DemoObjectiveIds.OutpostInstall,
                DemoObjectiveIds.ReturnRecommend,
                DemoObjectiveIds.Settlement,
                DemoObjectiveIds.BatteryUpgrade,
                DemoObjectiveIds.MineLithium,
                DemoObjectiveIds.DeepSignal,
                DemoObjectiveIds.DemoEnd
            };

            Assert.That(DemoObjectiveIds.Ordered, Is.EqualTo(expected));
            var pathGuide = DemoObjectiveCatalog.GetRequired(DemoObjectiveIds.PathGuide);
            Assert.That(pathGuide.Description, Does.Not.Contain("리튬"));
            Assert.That(pathGuide.NextActionHint, Does.Not.Contain("리튬"));

            var requirements = DeepZoneUnlockRule.Mvp.UpgradeRequirements;
            Assert.That(
                requirements.Any(r => r.UpgradeId == DataIds.Upgrades.DrillSpeed
                    && r.RequiredLevel == 2),
                Is.True);
            Assert.That(
                requirements.Any(r => r.UpgradeId == DataIds.Upgrades.DroneScan
                    && r.RequiredLevel == 2),
                Is.True);
            Assert.That(
                requirements.Any(r => r.UpgradeId == DataIds.Upgrades.GasResistance
                    && r.RequiredLevel == 1),
                Is.True);
        }

        [Test]
        public void PromptB53_IntegrationScene_HasClickableSummaryAndClosableDetailsPanel()
        {
            var scene = SceneManager.GetSceneByPath(PromptB53QuestUiBuilder.IntegrationScenePath);
            var closeAfter = !scene.IsValid() || !scene.isLoaded;
            if (closeAfter)
            {
                scene = EditorSceneManager.OpenScene(
                    PromptB53QuestUiBuilder.IntegrationScenePath,
                    OpenSceneMode.Additive);
            }

            try
            {
                var root = Find(scene, "DemoObjectiveRoot");
                var summary = Find(scene, "QuestSummaryButton");
                var details = Find(scene, "QuestDetailsPanel");
                var close = Find(scene, "QuestDetailsCloseButton");

                Assert.That(root, Is.Not.Null);
                Assert.That(summary, Is.Not.Null);
                Assert.That(details, Is.Not.Null);
                Assert.That(close, Is.Not.Null);

                var view = root.GetComponent<DemoObjectiveView>();
                var summaryButton = summary.GetComponent<Button>();
                var closeButton = close.GetComponent<Button>();
                Assert.That(view, Is.Not.Null);
                Assert.That(view.HasDetailsReferences(), Is.True);
                Assert.That(summaryButton, Is.Not.Null);
                Assert.That(closeButton, Is.Not.Null);
                Assert.That(details.activeSelf, Is.False);

                Assert.That(summaryButton.onClick.GetPersistentEventCount(), Is.EqualTo(1));
                Assert.That(
                    summaryButton.onClick.GetPersistentMethodName(0),
                    Is.EqualTo(nameof(DemoObjectiveView.OnObjectiveDetailsClicked)));
                Assert.That(closeButton.onClick.GetPersistentEventCount(), Is.EqualTo(1));
                Assert.That(
                    closeButton.onClick.GetPersistentMethodName(0),
                    Is.EqualTo(nameof(DemoObjectiveView.OnDetailsDismissClicked)));

                var detailsRect = details.GetComponent<RectTransform>();
                Assert.That(detailsRect.anchorMin, Is.EqualTo(new Vector2(0.5f, 0.5f)));
                Assert.That(detailsRect.anchorMax, Is.EqualTo(new Vector2(0.5f, 0.5f)));
                Assert.That(detailsRect.sizeDelta.x, Is.GreaterThanOrEqualTo(640f));
                Assert.That(detailsRect.sizeDelta.y, Is.GreaterThanOrEqualTo(320f));
            }
            finally
            {
                if (closeAfter && scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static GameObject Find(Scene scene, string objectName)
        {
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var transforms = roots[i].GetComponentsInChildren<Transform>(true);
                for (var j = 0; j < transforms.Length; j++)
                {
                    if (transforms[j].name == objectName)
                    {
                        return transforms[j].gameObject;
                    }
                }
            }

            return null;
        }
    }
}
