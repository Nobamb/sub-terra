using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.Drone;
using SubTerra.App.Drone.Dialogue;
using SubTerra.App.UI.Drone;
using SubTerra.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace SubTerra.App.Tests.PlayMode.Drone
{
    public sealed class DronePlayModeTests
    {
        [UnityTest]
        public IEnumerator I_SharedProvider_ProducesRecommendationReasonAndOfflineDialogue()
        {
            var settings = ScriptableObject.CreateInstance<DroneAnalysisSettings>();
            settings.EditorSetDefaults();
            var template = ScriptableObject.CreateInstance<DialogueTemplateData>();
            template.EditorSet(
                DataIds.Dialogue.DroneGasWarning,
                "Gas",
                "gas",
                500,
                "가스 위험 {gasRisk}. 이탈하세요.");
            var provider = new FakeProvider
            {
                Context = new DroneContextDto
                {
                    depth = 40,
                    currentEnergy = 10,
                    returnEnergyEstimate = 12,
                    structuralIntegrity = 1f,
                    gasRisk = 0.7f,
                    unsettledCargoValue = 0,
                    nearestBaseDistance = 20f,
                    nearbyMineralIds = new List<string>
                    {
                        DataIds.Minerals.Lithium
                    },
                    returnPathAvailable = true
                }
            };
            var dialogueRoot = new GameObject("DroneDialoguePanel", typeof(RectTransform));
            var dialogueText = CreateText(dialogueRoot.transform, "DialogueText");
            var dialogueView = dialogueRoot.AddComponent<DroneDialoguePanelView>();
            SetField(dialogueView, "panelRoot", dialogueRoot);
            SetField(dialogueView, "dialogueText", dialogueText);

            var reasonRoot = new GameObject("DroneReasonPanel", typeof(RectTransform));
            var actionText = CreateText(reasonRoot.transform, "ActionText");
            var reasonText = CreateText(reasonRoot.transform, "ReasonText");
            var reasonView = reasonRoot.AddComponent<DroneReasonPanelView>();
            SetField(reasonView, "panelRoot", reasonRoot);
            SetField(reasonView, "actionText", actionText);
            SetField(reasonView, "reasonText", reasonText);
            var presenter = new DroneRecommendationPresenter(dialogueView, reasonView);

            var analysis = new DroneAnalysisService(settings);
            presenter.Bind(
                provider,
                analysis,
                new TemplateDialogueGenerator(
                    new[] { template },
                    new FixedClock(),
                    settings));

            var result = analysis.Analyze(provider.Context);
            Assert.That(result.RecommendedAction, Is.EqualTo(DroneAction.LeaveGasZone));
            Assert.That(result.Recommendation.Reasons[0].ActualValue, Is.EqualTo(0.7d).Within(0.001d));
            Assert.That(dialogueText.text, Is.EqualTo("가스 위험 0.7. 이탈하세요."));
            Assert.That(actionText.text, Does.Contain("가스 구역 이탈"));
            Assert.That(reasonText.text, Does.Contain("가스 위험 0.7"));
            Assert.That(dialogueRoot.activeSelf, Is.True);
            Assert.That(reasonRoot.activeSelf, Is.True);

            presenter.Unbind();
            Assert.That(dialogueRoot.activeSelf, Is.False);
            Assert.That(reasonRoot.activeSelf, Is.False);

            Object.Destroy(dialogueRoot);
            Object.Destroy(reasonRoot);
            Object.Destroy(settings);
            Object.Destroy(template);
            yield return null;
        }

        private sealed class FakeProvider : IDroneContextProvider
        {
            public DroneContextDto Context;
            public DroneContextDto CreateContext() => Context;
        }

        private sealed class FixedClock : IDroneClock
        {
            public double Now => 0d;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            return root.AddComponent<TextMeshProUGUI>();
        }

        private static void SetField<T>(object target, string name, T value)
        {
            target.GetType()
                .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(target, value);
        }
    }
}
