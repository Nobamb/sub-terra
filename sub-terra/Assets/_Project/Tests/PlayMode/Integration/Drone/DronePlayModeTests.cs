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
            // 하단 패널 가시성은 HudPanelChromeController가 소유한다.
            // Presenter 해제는 패널을 임의로 닫지 않는다.
            Assert.That(dialogueRoot.activeSelf, Is.True);
            Assert.That(reasonRoot.activeSelf, Is.True);

            Object.Destroy(dialogueRoot);
            Object.Destroy(reasonRoot);
            Object.Destroy(settings);
            Object.Destroy(template);
            yield return null;
        }

        [UnityTest]
        public IEnumerator K_F01_WorldDialogue_FollowsSocketAndClampsInsideCamera()
        {
            var cameraObject = new GameObject("Main Camera", typeof(Camera));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;

            var socketObject = new GameObject("ViewSocket");
            socketObject.transform.position = new Vector3(100f, 100f, 0f);
            var canvasObject = new GameObject(
                "WorldDialogueCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasGroup));
            canvasObject.transform.SetParent(socketObject.transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var group = canvasObject.GetComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;
            var text = CreateText(canvasObject.transform, "DialogueText");
            text.raycastTarget = false;

            var socket = socketObject.AddComponent<DroneDialogueSocket>();
            SetField(socket, "anchor", socketObject.transform);
            SetField(socket, "visualRoot", canvasObject.GetComponent<RectTransform>());
            SetField(socket, "worldCanvas", canvas);
            SetField(socket, "canvasGroup", group);
            SetField(socket, "dialogueText", text);
            SetField(socket, "worldCamera", camera);

            socket.SetVisible(true);
            socket.SetDialogue(new DroneDialogueResult(
                "dialogue.test",
                "긴급 경고",
                false,
                false,
                true));
            socket.RefreshPosition();

            var viewport = camera.WorldToViewportPoint(canvasObject.transform.position);
            Assert.That(group.alpha, Is.EqualTo(1f));
            Assert.That(group.blocksRaycasts, Is.False);
            Assert.That(viewport.x, Is.InRange(0.079f, 0.921f));
            Assert.That(viewport.y, Is.InRange(0.119f, 0.881f));

            Object.Destroy(socketObject);
            Object.Destroy(cameraObject);
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
