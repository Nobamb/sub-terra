using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Integration;
using SubTerra.App.UI.DamageFeedback;
using SubTerra.App.UI.EmergencyEscape;
using SubTerra.Gameplay.Player;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Tests.UI
{
    /// <summary>
    /// prompt-B 207 (#359): 낙하·붕괴 피해에만 짧은 붉은 글리치·틴트. 가스·산소 0 틱 제외,
    /// 연속 피격 시 길이 누적 없음, 모션 감소면 왜곡 없이 틴트만, 입력을 막지 않는 오버레이.
    /// </summary>
    public sealed class PromptB207DamageHitGlitchTests
    {
        private readonly List<Object> created = new List<Object>();
        private readonly List<DamageHitGlitchController> controllers = new List<DamageHitGlitchController>();

        [TearDown]
        public void TearDown()
        {
            AccessibilityPreferences.ReduceMotion = false;
            foreach (var controller in controllers)
            {
                if (controller != null)
                {
                    controller.Unbind();
                    controller.Clear();
                }
            }

            controllers.Clear();
            for (var i = created.Count - 1; i >= 0; i--)
            {
                if (created[i] != null)
                {
                    Object.DestroyImmediate(created[i]);
                }
            }

            created.Clear();
        }

        // ---------------------------------------------------------------- 시간표

        [Test]
        public void Settings_DefaultsMatchDraftConstants_AndStayWeakerThanWarp()
        {
            var timeline = new DamageHitGlitchTimeline();
            Assert.That(timeline.Duration, Is.EqualTo(0.28f).Within(0.0001f));
            Assert.That(timeline.Duration, Is.InRange(0.2f, 0.4f));
            Assert.That(timeline.TintMaxAlpha, Is.EqualTo(0.22f).Within(0.0001f));
            Assert.That(timeline.GlitchStrength, Is.EqualTo(0.28f).Within(0.0001f));
            Assert.That(timeline.GlitchStrength, Is.LessThan(EscapeWarpTimeline.MaxGlitchIntensity));
        }

        [TestCase(RunFailureCause.Fall)]
        [TestCase(RunFailureCause.StructuralCollapse)]
        public void Timeline_GeneralDamage_ShowsTintAndGlitchAtStart_AndClearsAfterDuration(RunFailureCause cause)
        {
            var timeline = new DamageHitGlitchTimeline();
            Assert.That(timeline.TryStart(cause, 10f), Is.True);

            var start = timeline.Evaluate(10f, false);
            Assert.That(start.IsActive, Is.True);
            Assert.That(start.TintAlpha, Is.EqualTo(0.22f).Within(0.0001f));
            Assert.That(start.GlitchIntensity, Is.GreaterThan(0f));
            Assert.That(start.GlitchIntensity, Is.LessThanOrEqualTo(0.28f + 0.0001f));

            var middle = timeline.Evaluate(10f + (timeline.Duration * 0.5f), false);
            Assert.That(middle.TintAlpha, Is.LessThan(start.TintAlpha).And.GreaterThan(0f));

            var end = timeline.Evaluate(10f + timeline.Duration, false);
            Assert.That(end.IsActive, Is.False);
            Assert.That(end.TintAlpha, Is.EqualTo(0f));
            Assert.That(end.GlitchIntensity, Is.EqualTo(0f));
            Assert.That(timeline.IsRunning, Is.False);
        }

        [TestCase(RunFailureCause.GasExposure)]
        [TestCase(RunFailureCause.OxygenDepleted)]
        [TestCase(RunFailureCause.PowerDepleted)]
        [TestCase(RunFailureCause.Unknown)]
        public void Timeline_ExcludedCauses_NeverShowTintOrGlitch(RunFailureCause cause)
        {
            var timeline = new DamageHitGlitchTimeline();
            Assert.That(DamageHitGlitchTimeline.ShouldTrigger(cause), Is.False);
            Assert.That(timeline.TryStart(cause, 3f), Is.False);

            var frame = timeline.Evaluate(3f, false);
            Assert.That(frame.IsActive, Is.False);
            Assert.That(frame.TintAlpha, Is.EqualTo(0f));
            Assert.That(frame.GlitchIntensity, Is.EqualTo(0f));
        }

        [Test]
        public void Timeline_ConsecutiveHits_RestartWithoutAccumulatingLength()
        {
            var timeline = new DamageHitGlitchTimeline();
            var duration = timeline.Duration;
            timeline.TryStart(RunFailureCause.Fall, 0f);
            timeline.TryStart(RunFailureCause.StructuralCollapse, 0.1f);
            timeline.TryStart(RunFailureCause.Fall, 0.2f);

            var restarted = timeline.Evaluate(0.2f, false);
            Assert.That(restarted.TintAlpha, Is.EqualTo(0.22f).Within(0.0001f), "다시 맞으면 처음부터 시작한다.");
            Assert.That(timeline.Evaluate(0.2f + duration - 0.001f, false).IsActive, Is.True);
            Assert.That(timeline.Evaluate(0.2f + duration, false).IsActive, Is.False,
                "마지막 피격으로부터 Duration 뒤에는 끝난다(남은 시간을 더하지 않는다).");
        }

        [Test]
        public void Timeline_ReduceMotion_KeepsTintOnly()
        {
            var timeline = new DamageHitGlitchTimeline();
            timeline.TryStart(RunFailureCause.Fall, 0f);
            for (var t = 0f; t < timeline.Duration; t += 0.02f)
            {
                var frame = timeline.Evaluate(t, true);
                Assert.That(frame.GlitchIntensity, Is.EqualTo(0f), "모션 감소면 왜곡 0.");
            }

            var restart = new DamageHitGlitchTimeline();
            restart.TryStart(RunFailureCause.Fall, 0f);
            Assert.That(restart.Evaluate(0f, true).TintAlpha, Is.EqualTo(0.22f).Within(0.0001f));
            Assert.That(restart.Evaluate(restart.Duration, true).IsActive, Is.False, "같은 길이로 끝난다.");
        }

        [Test]
        public void Timeline_InspectorDuration_IsClampedToIssueRange()
        {
            var settings = new DamageHitGlitchSettings { hitGlitchDuration = 3f, hitGlitchStrength = 2f };
            var timeline = new DamageHitGlitchTimeline(settings);
            Assert.That(timeline.Duration, Is.LessThanOrEqualTo(0.4f));
            Assert.That(timeline.GlitchStrength, Is.LessThanOrEqualTo(EscapeWarpTimeline.MaxGlitchIntensity));
        }

        // ---------------------------------------------------------------- 오버레이

        [Test]
        public void Overlay_DoesNotBlockInput_AndHidesWhenInactive()
        {
            var overlay = new DamageHitGlitchOverlay(new DamageHitGlitchSettings());
            created.Add(overlay.Root);

            Assert.That(overlay.Root.GetComponent<GraphicRaycaster>(), Is.Null, "입력을 막지 않는다.");
            foreach (var graphic in overlay.Root.GetComponentsInChildren<Graphic>(true))
            {
                Assert.That(graphic.raycastTarget, Is.False, graphic.name);
            }

            var canvas = overlay.Root.GetComponent<Canvas>();
            Assert.That(canvas.sortingOrder, Is.EqualTo(DamageHitGlitchOverlay.SortingOrder));
            Assert.That(overlay.Root.activeSelf, Is.False);

            overlay.Apply(new DamageHitGlitchFrame(0.2f, 0.25f, 11, true));
            Assert.That(overlay.Root.activeSelf, Is.True);
            Assert.That(overlay.TintAlpha, Is.EqualTo(0.2f).Within(0.0001f));
            Assert.That(overlay.GlitchVisible, Is.True);

            overlay.Apply(new DamageHitGlitchFrame(0.2f, 0f, 11, true));
            Assert.That(overlay.GlitchVisible, Is.False, "왜곡 0이면 글리치 층을 끈다.");
            Assert.That(overlay.TintAlpha, Is.GreaterThan(0f));

            overlay.Apply(DamageHitGlitchFrame.Inactive);
            Assert.That(overlay.Root.activeSelf, Is.False);
        }

        // ---------------------------------------------------------------- 생존 컨트롤러 연결

        [Test]
        public void Controller_FallDamage_StartsRedGlitch_ThenClears()
        {
            var (survival, controller) = CreateBoundPair();
            Assert.That(survival.ApplyFall(30f), Is.True);

            Assert.That(controller.IsPlaying, Is.True);
            var now = Time.unscaledTime;
            var frame = controller.Tick(now);
            Assert.That(frame.TintAlpha, Is.GreaterThan(0f));
            Assert.That(frame.GlitchIntensity, Is.GreaterThan(0f));
            Assert.That(controller.Overlay, Is.Not.Null);
            Assert.That(controller.Overlay.TintAlpha, Is.GreaterThan(0f));

            var end = controller.Tick(now + controller.Settings.hitGlitchDuration + 0.01f);
            Assert.That(end.TintAlpha, Is.EqualTo(0f));
            Assert.That(end.GlitchIntensity, Is.EqualTo(0f));
            Assert.That(controller.Overlay.Root.activeSelf, Is.False);
        }

        [Test]
        public void Controller_CollapseDamage_WithReduceMotion_ShowsTintOnly()
        {
            AccessibilityPreferences.ReduceMotion = true;
            var (survival, controller) = CreateBoundPair();
            Assert.That(survival.ApplyCollapseImpact(), Is.True);

            var frame = controller.Tick(Time.unscaledTime);
            Assert.That(frame.TintAlpha, Is.GreaterThan(0f));
            Assert.That(frame.GlitchIntensity, Is.EqualTo(0f));
            Assert.That(controller.Overlay.GlitchVisible, Is.False);
        }

        [Test]
        public void Controller_GasDamage_DoesNotStartRedGlitch()
        {
            var (survival, controller) = CreateBoundPair();
            var health = survival.GetHealth().Current;
            Assert.That(survival.ApplyGasFailure(new GasExposureFailureInputDto
            {
                gasZoneId = "gas.test",
                damage = 5,
                severity = GasExposureFailureSeverity.Damage
            }), Is.True);

            Assert.That(survival.GetHealth().Current, Is.LessThan(health));
            Assert.That(controller.IsPlaying, Is.False);
            Assert.That(controller.Overlay, Is.Null);
        }

        [Test]
        public void Controller_OxygenDepletionTick_DoesNotStartRedGlitch()
        {
            var (survival, controller) = CreateBoundPair();
            survival.BindOxygenDepth(() => 40f);
            survival.RestoreOxygen(0f);
            var health = survival.GetHealth().Current;

            var advance = typeof(PlayerSurvivalController).GetMethod("AdvanceOxygen",
                BindingFlags.Instance | BindingFlags.NonPublic);
            advance.Invoke(survival, new object[] { 2.5f });

            Assert.That(survival.GetHealth().Current, Is.LessThan(health), "산소 0 체력 틱이 실제로 났다.");
            Assert.That(controller.IsPlaying, Is.False);
            Assert.That(controller.Overlay, Is.Null);
        }

        [Test]
        public void Controller_BlockedByInvulnerability_DoesNotRaiseDamageApplied()
        {
            var (survival, _) = CreateBoundPair();
            var raised = new List<RunFailureCause>();
            survival.DamageApplied += (cause, damage, fatal) => raised.Add(cause);

            Assert.That(survival.ApplyFall(30f), Is.True);
            Assert.That(survival.ApplyFall(30f), Is.False, "무적 시간 안의 두 번째 낙하는 막힌다.");
            Assert.That(raised, Is.EqualTo(new[] { RunFailureCause.Fall }));
        }

        private (PlayerSurvivalController survival, DamageHitGlitchController controller) CreateBoundPair()
        {
            var host = new GameObject("PromptB207_Survival");
            created.Add(host);
            var survival = host.AddComponent<PlayerSurvivalController>();
            var controller = host.AddComponent<DamageHitGlitchController>();
            controllers.Add(controller);
            controller.Bind(survival);
            return (survival, controller);
        }
    }
}
