using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.Integration;
using SubTerra.App.Progression;
using SubTerra.App.Tutorial;
using SubTerra.App.UI.Tutorial;
using SubTerra.Gameplay.Player;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SubTerra.App.Tests.Tutorial
{
    /// <summary>prompt-B 60 퀘스트 순서와 기존 상세창 Scene 연결 회귀 검증.</summary>
    public sealed class PromptB53QuestUiTests
    {
        [Test]
        public void PromptB60_CatalogFollowsRequestedOrder()
        {
            var expected = new[]
            {
                DemoObjectiveIds.MineBlock,
                DemoObjectiveIds.MineCopper,
                DemoObjectiveIds.UpgradeDrillSpeed,
                DemoObjectiveIds.TravelToSurface,
                DemoObjectiveIds.ReturnToMine,
                DemoObjectiveIds.MineIron,
                DemoObjectiveIds.PlaceSupportInDanger,
                DemoObjectiveIds.PlaceLadder,
                DemoObjectiveIds.PlaceLightAtDepth,
                DemoObjectiveIds.StoreMineral,
                DemoObjectiveIds.InstallOutpostCore,
                DemoObjectiveIds.ChargeNearOutpost,
                DemoObjectiveIds.UnlockDeepZone,
                DemoObjectiveIds.MineLithium,
                DemoObjectiveIds.PurifyGasWithOutpost,
                DemoObjectiveIds.SellAtSettlement,
                DemoObjectiveIds.EmergencyEscapeReturn
            };

            Assert.That(DemoObjectiveIds.Ordered, Is.EqualTo(expected));
            Assert.That(DeepZoneUnlockRule.Mvp.RequiredCompletedObjectives, Is.EqualTo(12));

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

                var objectiveClone = Object.Instantiate(root);
                var canvasHost = new GameObject("PromptB60_3_TutorialCanvasHost");
                canvasHost.AddComponent<Canvas>();
                objectiveClone.transform.SetParent(canvasHost.transform, false);
                try
                {
                    var cloneView = objectiveClone.GetComponent<DemoObjectiveView>();
                    var cloneGuidance = FindChild(objectiveClone.transform, "GuidancePanel");
                    cloneView.SetGuidanceVisible(true);
                    var guidanceCanvas = cloneGuidance.GetComponent<Canvas>();
                    Assert.That(
                        cloneGuidance.GetSiblingIndex(),
                        Is.EqualTo(objectiveClone.transform.childCount - 1),
                        "첫 안내 팝업은 Tutorial Canvas 내부에서 가장 앞에 그려져야 한다.");
                    Assert.That(guidanceCanvas, Is.Not.Null);
                    Assert.That(cloneGuidance.GetComponent<GraphicRaycaster>(), Is.Not.Null);
                    Assert.That(guidanceCanvas.overrideSorting, Is.True);
                    Assert.That(
                        guidanceCanvas.sortingOrder,
                        Is.EqualTo(UiLayerPriority.IntroductionGuidance));
                    Assert.That(
                        UiLayerPriority.IntroductionGuidance,
                        Is.GreaterThan(UiLayerPriority.ModalPanel));
                }
                finally
                {
                    Object.DestroyImmediate(canvasHost);
                }
            }
            finally
            {
                if (closeAfter && scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        [Test]
        public void PromptB60_2_MineReturnBridgeTracksActualElevatorCenter()
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
                var bridge = FindComponent<ElevatorTravelBridge>(scene);
                var elevator = FindComponent<ElevatorController>(scene);
                Assert.That(bridge, Is.Not.Null);
                Assert.That(elevator, Is.Not.Null);
                Assert.That(bridge.transform.position.x, Is.Not.EqualTo(elevator.transform.position.x));

                typeof(ElevatorTravelBridge)
                    .GetMethod("ResolveTargets", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(bridge, null);
                var resolved = typeof(ElevatorTravelBridge)
                    .GetField("elevatorTransform", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.GetValue(bridge) as Transform;

                Assert.That(resolved, Is.SameAs(elevator.transform));
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

        private static Transform FindChild(Transform root, string objectName)
        {
            return root.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(child => child.name == objectName);
        }

        private static T FindComponent<T>(Scene scene) where T : Component
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true))
                .FirstOrDefault();
        }
    }
}
