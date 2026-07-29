using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using SubTerra.App.AI;
using SubTerra.App.Core.Data;
using SubTerra.App.Drone;
using SubTerra.App.Drone.Dialogue;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.App.Tests.AI
{
    public sealed class CloudDialogueGeneratorTests
    {
        private readonly List<UnityEngine.Object> created =
            new List<UnityEngine.Object>();
        private DroneAnalysisSettings analysisSettings;
        private DialogueTemplateData template;
        private ManualClock clock;
        private DroneAnalysisResult analysis;

        [SetUp]
        public void SetUp()
        {
            analysisSettings = Track(
                ScriptableObject.CreateInstance<DroneAnalysisSettings>());
            analysisSettings.EditorSetDefaults();
            template = Track(ScriptableObject.CreateInstance<DialogueTemplateData>());
            template.EditorSet(
                DataIds.Dialogue.DroneGasWarning,
                "가스 경고",
                "gas",
                100,
                "가스 위험 {gasRisk}. 이탈하세요.");
            clock = new ManualClock();

            var context = SafeContext();
            context.gasRisk = 0.8f;
            analysis = new DroneAnalysisService(analysisSettings).Analyze(context);
        }

        [TearDown]
        public void TearDown()
        {
            for (var i = created.Count - 1; i >= 0; i--)
            {
                UnityEngine.Object.DestroyImmediate(created[i]);
            }

            created.Clear();
        }

        [Test]
        public async Task J_F01_Success_UsesCloudTextWithoutChangingPhaseIDecision()
        {
            var transport = new RecordingTransport(
                new DialogueTransportResult(
                    true,
                    200,
                    "{\"dialogue\":\"가스 수치가 위험합니다. 즉시 이탈하세요.\"}"));
            var generator = CreateGenerator(transport);

            var result = await generator.GenerateAsync(
                analysis,
                CloudDialogueEvent.GasDetected,
                CancellationToken.None);

            Assert.That(result.UsedCloud, Is.True);
            Assert.That(result.Analysis, Is.SameAs(analysis));
            Assert.That(result.Analysis.RecommendedAction, Is.EqualTo(DroneAction.LeaveGasZone));
            Assert.That(
                result.Analysis.Recommendation.Reasons[0],
                Is.SameAs(analysis.Recommendation.Reasons[0]));
            Assert.That(result.Dialogue.Text, Does.Contain("즉시 이탈"));

            var request = JsonUtility.FromJson<CloudDialogueRequestDto>(
                transport.LastRequestJson);
            Assert.That(request.actionKey, Is.EqualTo(nameof(DroneAction.LeaveGasZone)));
            Assert.That(request.reasonKeys, Is.EqualTo(new[] { "gas_risk" }));
            Assert.That(request.facts.Single().value, Is.EqualTo(0.8d).Within(0.001d));
            Assert.That(request.language, Is.EqualTo("ko"));
            Assert.That(transport.LastRequestJson, Does.Not.Contain("Candidates"));
            Assert.That(transport.LastRequestJson, Does.Not.Contain("Message"));
        }

        [Test]
        public async Task J_F02_HttpAndPayloadFailures_ImmediatelyUseSameTemplate()
        {
            var failures = new[]
            {
                new DialogueTransportResult(false, 0, string.Empty),
                new DialogueTransportResult(false, 401, string.Empty),
                new DialogueTransportResult(false, 429, string.Empty),
                new DialogueTransportResult(false, 500, string.Empty),
                new DialogueTransportResult(true, 200, "not-json"),
                new DialogueTransportResult(true, 200, "{\"dialogue\":\"<b>위험</b>\"}"),
                new DialogueTransportResult(true, 200, "{\"dialogue\":\"\"}")
            };

            for (var i = 0; i < failures.Length; i++)
            {
                var generator = CreateGenerator(new RecordingTransport(failures[i]));
                var result = await generator.GenerateAsync(
                    analysis,
                    CloudDialogueEvent.GasDetected,
                    CancellationToken.None);

                Assert.That(result.UsedCloud, Is.False, $"failure index {i}");
                Assert.That(
                    result.Dialogue.Text,
                    Is.EqualTo("가스 위험 0.8. 이탈하세요."),
                    $"failure index {i}");
                Assert.That(result.Analysis, Is.SameAs(analysis));
            }
        }

        [Test]
        public async Task J_F02_Timeout_UsesTemplateWithoutPropagatingException()
        {
            var options = Options(timeoutMilliseconds: 10);
            var transport = new NeverCompletingTransport();
            var generator = CreateGenerator(transport, options);

            var result = await generator.GenerateAsync(
                analysis,
                CloudDialogueEvent.GasDetected,
                CancellationToken.None);

            Assert.That(result.UsedCloud, Is.False);
            Assert.That(result.Dialogue.Text, Is.EqualTo("가스 위험 0.8. 이탈하세요."));
        }

        [Test]
        public void J_F03_Policy_EnforcesConcurrencyCooldownEventAndSessionLimits()
        {
            var options = new CloudDialogueOptions(
                true,
                "https://dialogue.example.invalid/v1",
                globalCooldownSeconds: 2d,
                duplicateEventWindowSeconds: 10d,
                maxSessionCalls: 3,
                maxCallsPerEvent: 1);
            var policy = new CloudDialoguePolicy(clock, options);

            Assert.That(
                policy.TryBegin(CloudDialogueEvent.GasDetected, out var first),
                Is.True);
            Assert.That(
                policy.TryBegin(CloudDialogueEvent.NewDepthZone, out _),
                Is.False,
                "동시 요청은 하나만 허용한다.");
            first.Dispose();

            clock.NowValue = 1d;
            Assert.That(
                policy.TryBegin(CloudDialogueEvent.NewDepthZone, out _),
                Is.False,
                "전체 쿨다운 동안 새 이벤트도 제한한다.");

            clock.NowValue = 3d;
            Assert.That(
                policy.TryBegin(CloudDialogueEvent.NewDepthZone, out var second),
                Is.True);
            second.Dispose();

            clock.NowValue = 20d;
            Assert.That(
                policy.TryBegin(CloudDialogueEvent.GasDetected, out _),
                Is.False,
                "이벤트별 세션 상한을 넘길 수 없다.");
            Assert.That(
                policy.TryBegin(CloudDialogueEvent.CollapseImminent, out var third),
                Is.True);
            third.Dispose();

            clock.NowValue = 23d;
            Assert.That(
                policy.TryBegin(CloudDialogueEvent.PowerShortage, out _),
                Is.False,
                "전체 세션 상한을 넘길 수 없다.");
            Assert.That(policy.SessionCalls, Is.EqualTo(3));

            var duplicateOptions = new CloudDialogueOptions(
                true,
                "https://dialogue.example.invalid/v1",
                globalCooldownSeconds: 0d,
                duplicateEventWindowSeconds: 10d,
                maxSessionCalls: 4,
                maxCallsPerEvent: 2);
            var duplicatePolicy = new CloudDialoguePolicy(clock, duplicateOptions);
            clock.NowValue = 100d;
            Assert.That(
                duplicatePolicy.TryBegin(CloudDialogueEvent.GasDetected, out var duplicateFirst),
                Is.True);
            duplicateFirst.Dispose();
            clock.NowValue = 105d;
            Assert.That(
                duplicatePolicy.TryBegin(CloudDialogueEvent.GasDetected, out _),
                Is.False,
                "같은 이벤트는 중복 억제 구간에 재호출하지 않는다.");
            clock.NowValue = 111d;
            Assert.That(
                duplicatePolicy.TryBegin(CloudDialogueEvent.GasDetected, out var duplicateAfter),
                Is.True);
            duplicateAfter.Dispose();
        }

        [Test]
        public async Task J_F05_DefaultDisabled_UsesTemplateWithoutNetworkWait()
        {
            var transport = new RecordingTransport(
                new DialogueTransportResult(true, 200, "{\"dialogue\":\"cloud\"}"));
            var generator = CreateGenerator(
                transport,
                new CloudDialogueOptions(false, string.Empty));

            var result = await generator.GenerateAsync(
                analysis,
                CloudDialogueEvent.ManualAnalysis,
                CancellationToken.None);

            Assert.That(result.UsedCloud, Is.False);
            Assert.That(transport.CallCount, Is.Zero);
            Assert.That(result.Dialogue.Text, Is.EqualTo("가스 위험 0.8. 이탈하세요."));

            var enabled = CreateGenerator(transport);
            await enabled.GenerateAsync(
                analysis,
                CloudDialogueEvent.Unknown,
                CancellationToken.None);
            Assert.That(
                transport.CallCount,
                Is.Zero,
                "allowlist에 없는 이벤트는 네트워크 경로를 열지 않는다.");
        }

        [Test]
        public void J_S02_RequestDto_ContainsOnlyAllowlistedFields()
        {
            var fields = typeof(CloudDialogueRequestDto)
                .GetFields(BindingFlags.Instance | BindingFlags.Public)
                .Select(field => field.Name)
                .OrderBy(name => name)
                .ToArray();

            Assert.That(
                fields,
                Is.EqualTo(new[] { "actionKey", "facts", "language", "reasonKeys" }));
            Assert.That(
                new CloudDialogueOptions(true, "http://insecure.invalid").CanUseCloud,
                Is.False);
            Assert.That(
                new CloudDialogueOptions(true, "https://safe.invalid").CanUseCloud,
                Is.True);

            var defaultConfig = Track(
                ScriptableObject.CreateInstance<CloudDialogueConfig>());
            Assert.That(defaultConfig.CreateOptions().CanUseCloud, Is.False);
        }

        private CloudDialogueGenerator CreateGenerator(
            IDialogueTransport transport,
            CloudDialogueOptions options = null)
        {
            var selectedOptions = options ?? Options();
            var fallback = new TemplateDialogueGenerator(
                new[] { template },
                clock,
                analysisSettings);
            return new CloudDialogueGenerator(
                fallback,
                transport,
                selectedOptions,
                new CloudDialoguePolicy(clock, selectedOptions));
        }

        private static CloudDialogueOptions Options(int timeoutMilliseconds = 1000)
        {
            return new CloudDialogueOptions(
                true,
                "https://dialogue.example.invalid/v1",
                timeoutMilliseconds: timeoutMilliseconds,
                globalCooldownSeconds: 0d,
                duplicateEventWindowSeconds: 0d,
                maxSessionCalls: 10,
                maxCallsPerEvent: 10);
        }

        private static DroneContextDto SafeContext()
        {
            return new DroneContextDto
            {
                depth = 10,
                currentEnergy = 100,
                returnEnergyEstimate = 20,
                structuralIntegrity = 1f,
                gasRisk = 0f,
                unsettledCargoValue = 0,
                cargoWeight = 0f,
                nearestBaseDistance = 20f,
                nearbyMineralIds = new List<string>(),
                returnPathAvailable = true
            };
        }

        private T Track<T>(T value) where T : UnityEngine.Object
        {
            created.Add(value);
            return value;
        }

        private sealed class ManualClock : IDroneClock
        {
            public double NowValue;
            public double Now => NowValue;
        }

        private sealed class RecordingTransport : IDialogueTransport
        {
            private readonly DialogueTransportResult result;

            public int CallCount { get; private set; }
            public string LastRequestJson { get; private set; }

            public RecordingTransport(DialogueTransportResult transportResult)
            {
                result = transportResult;
            }

            public Task<DialogueTransportResult> SendAsync(
                string endpoint,
                string requestJson,
                int timeoutMilliseconds,
                CancellationToken cancellationToken)
            {
                CallCount++;
                LastRequestJson = requestJson;
                return Task.FromResult(result);
            }
        }

        private sealed class NeverCompletingTransport : IDialogueTransport
        {
            public async Task<DialogueTransportResult> SendAsync(
                string endpoint,
                string requestJson,
                int timeoutMilliseconds,
                CancellationToken cancellationToken)
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
                return null;
            }
        }
    }
}
