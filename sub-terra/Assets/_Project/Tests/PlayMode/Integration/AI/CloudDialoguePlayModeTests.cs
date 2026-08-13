using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using SubTerra.App.AI;
using SubTerra.App.Core.Data;
using SubTerra.App.Drone;
using SubTerra.App.Drone.Dialogue;
using SubTerra.App.UI.Drone;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.TestTools;

namespace SubTerra.App.Tests.PlayMode.AI
{
    public sealed class CloudDialoguePlayModeTests
    {
        private readonly List<Object> created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (var i = created.Count - 1; i >= 0; i--)
            {
                Object.Destroy(created[i]);
            }

            created.Clear();
        }

        [UnityTest]
        public IEnumerator J_F01_CloudSuccess_OnlyReplacesDisplayedSentence()
        {
            var fixture = CreateFixture(
                new ImmediateTransport(
                    new DialogueTransportResult(
                        true,
                        200,
                        "{\"dialogue\":\"위험한 가스입니다. 즉시 이탈하세요.\"}")));
            var original = fixture.Analysis.Analyze(fixture.Provider.CreateContext());

            var task = fixture.Presenter.RequestCloudDialogueAsync(
                CloudDialogueEvent.GasDetected);
            yield return WaitUntilCompleted(task);

            Assert.That(task.IsFaulted, Is.False);
            Assert.That(task.Result.UsedCloud, Is.True);
            Assert.That(task.Result.Analysis.RecommendedAction, Is.EqualTo(original.RecommendedAction));
            Assert.That(task.Result.Analysis.Recommendation.Score, Is.EqualTo(original.Recommendation.Score));
            Assert.That(fixture.DialogueView.Last.Text, Does.Contain("즉시 이탈"));
        }

        [UnityTest]
        public IEnumerator J_F02_ServerFailure_DisplaysTemplateAndKeepsPlaying()
        {
            var fixture = CreateFixture(
                new ImmediateTransport(
                    new DialogueTransportResult(false, 500, string.Empty)));

            var task = fixture.Presenter.RequestCloudDialogueAsync(
                CloudDialogueEvent.GasDetected);
            yield return WaitUntilCompleted(task);

            Assert.That(task.IsFaulted, Is.False);
            Assert.That(task.Result.UsedCloud, Is.False);
            Assert.That(fixture.DialogueView.Last.Text, Is.EqualTo("가스 위험 0.8"));
            Assert.That(task.Result.Analysis.RecommendedAction, Is.EqualTo(DroneAction.LeaveGasZone));
        }

        [UnityTest]
        public IEnumerator J_F04_Unbind_CancelsRequestAndDiscardsLateUiCallback()
        {
            var transport = new CancellationTransport();
            var fixture = CreateFixture(transport);
            var displayedBeforeRequest = fixture.DialogueView.DialogueCount;

            var task = fixture.Presenter.RequestCloudDialogueAsync(
                CloudDialogueEvent.ManualAnalysis);
            yield return WaitUntil(() => transport.Started);

            fixture.Presenter.Unbind();
            yield return WaitUntilCompleted(task);

            Assert.That(task.IsFaulted, Is.False);
            Assert.That(task.Result.WasCancelled, Is.True);
            Assert.That(fixture.DialogueView.DialogueCount, Is.EqualTo(displayedBeforeRequest));
        }

        private static IEnumerator WaitUntilCompleted(Task task)
        {
            yield return WaitUntil(() => task.IsCompleted);
        }

        private static IEnumerator WaitUntil(System.Func<bool> condition)
        {
            const int maxFrames = 120;
            var frames = 0;
            while (!condition())
            {
                frames++;
                Assert.That(frames, Is.LessThanOrEqualTo(maxFrames), "PlayMode wait exceeded 120 frames.");
                yield return null;
            }
        }

        private Fixture CreateFixture(IDialogueTransport transport)
        {
            var settings = Track(
                ScriptableObject.CreateInstance<DroneAnalysisSettings>());
            settings.EditorSetDefaults();
            var template = Track(
                ScriptableObject.CreateInstance<DialogueTemplateData>());
            template.EditorSet(
                DataIds.Dialogue.DroneGasWarning,
                "가스",
                "gas",
                100,
                "가스 위험 {gasRisk}");

            var clock = new FixedClock();
            var fallback = new TemplateDialogueGenerator(
                new[] { template },
                clock,
                settings);
            var options = new CloudDialogueOptions(
                true,
                "https://dialogue.example.invalid/v1",
                timeoutMilliseconds: 1000,
                globalCooldownSeconds: 0d,
                duplicateEventWindowSeconds: 0d,
                maxSessionCalls: 10,
                maxCallsPerEvent: 10);
            var cloud = new CloudDialogueGenerator(
                fallback,
                transport,
                options,
                new CloudDialoguePolicy(clock, options));
            var provider = new FixedProvider();
            var dialogueView = new RecordingDialogueView();
            var reasonView = new RecordingReasonView();
            var presenter = new DroneRecommendationPresenter(dialogueView, reasonView);
            var analysis = new DroneAnalysisService(settings);
            presenter.Bind(provider, analysis, fallback, cloud);

            return new Fixture(
                presenter,
                provider,
                analysis,
                dialogueView);
        }

        private T Track<T>(T value) where T : Object
        {
            created.Add(value);
            return value;
        }

        private sealed class Fixture
        {
            public DroneRecommendationPresenter Presenter { get; }
            public FixedProvider Provider { get; }
            public DroneAnalysisService Analysis { get; }
            public RecordingDialogueView DialogueView { get; }

            public Fixture(
                DroneRecommendationPresenter presenter,
                FixedProvider provider,
                DroneAnalysisService analysis,
                RecordingDialogueView dialogueView)
            {
                Presenter = presenter;
                Provider = provider;
                Analysis = analysis;
                DialogueView = dialogueView;
            }
        }

        private sealed class FixedProvider : IDroneContextProvider
        {
            public DroneContextDto CreateContext()
            {
                return new DroneContextDto
                {
                    depth = 10,
                    currentEnergy = 100,
                    returnEnergyEstimate = 20,
                    structuralIntegrity = 1f,
                    gasRisk = 0.8f,
                    unsettledCargoValue = 0,
                    cargoWeight = 0f,
                    nearestBaseDistance = 20f,
                    nearbyMineralIds = new List<string>(),
                    returnPathAvailable = true
                };
            }
        }

        private sealed class FixedClock : IDroneClock
        {
            public double Now => 0d;
        }

        private sealed class RecordingDialogueView : IDroneDialogueView
        {
            public DroneDialogueResult Last { get; private set; }
            public int DialogueCount { get; private set; }

            public void SetDialogue(DroneDialogueResult dialogue)
            {
                Last = dialogue;
                DialogueCount++;
            }

            public void SetVisible(bool visible)
            {
            }
        }

        private sealed class RecordingReasonView : IDroneReasonView
        {
            public void SetAnalysis(DroneAnalysisResult analysis)
            {
            }

            public void SetVisible(bool visible)
            {
            }
        }

        private sealed class ImmediateTransport : IDialogueTransport
        {
            private readonly DialogueTransportResult result;

            public ImmediateTransport(DialogueTransportResult transportResult)
            {
                result = transportResult;
            }

            public Task<DialogueTransportResult> SendAsync(
                string endpoint,
                string requestJson,
                int timeoutMilliseconds,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(result);
            }
        }

        private sealed class CancellationTransport : IDialogueTransport
        {
            public bool Started { get; private set; }

            public async Task<DialogueTransportResult> SendAsync(
                string endpoint,
                string requestJson,
                int timeoutMilliseconds,
                CancellationToken cancellationToken)
            {
                Started = true;
                await Task.Delay(Timeout.Infinite, cancellationToken);
                return null;
            }
        }
    }
}
