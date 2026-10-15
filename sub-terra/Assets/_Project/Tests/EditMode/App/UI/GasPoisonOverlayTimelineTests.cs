using UnityEngine;
using NUnit.Framework;
using SubTerra.App.State;
using SubTerra.App.UI.HUD;

namespace SubTerra.App.Tests.UI
{
    public sealed class GasPoisonOverlayTimelineTests
    {
        private const float Frame = 1f / 60f;

        private static GasPoisonOverlayFrame Run(GasPoisonOverlayTimeline timeline, float seconds)
        {
            var frame = timeline.Evaluate();
            var steps = (int)(seconds / Frame + 0.5f);
            for (var i = 0; i < steps; i++)
            {
                frame = timeline.Advance(Frame);
            }

            return frame;
        }

        [Test]
        public void Safe_AllAlphasAreZero()
        {
            var timeline = new GasPoisonOverlayTimeline();

            for (var i = 0; i < 300; i++)
            {
                var frame = timeline.Advance(Frame);
                Assert.That(frame.VignetteAlpha, Is.Zero);
                Assert.That(frame.FilterAlpha, Is.Zero);
                Assert.That(frame.Presence, Is.Zero);
                Assert.That(frame.IsVisible, Is.False);
            }
        }

        [Test]
        public void Critical_HasHigherMaxAlphaAndFasterPulseThanCaution()
        {
            var settings = new GasPoisonOverlaySettings();
            Assert.That(settings.Critical.VignetteMaxAlpha, Is.GreaterThan(settings.Caution.VignetteMaxAlpha));
            Assert.That(settings.Critical.PulsePeriod, Is.LessThan(settings.Caution.PulsePeriod));
            Assert.That(settings.Critical.FilterAlpha, Is.GreaterThan(settings.Caution.FilterAlpha));

            var cautionPeak = MeasurePeak(GasRiskLevel.Elevated, out var cautionPulses);
            var criticalPeak = MeasurePeak(GasRiskLevel.Hazard, out var criticalPulses);

            Assert.That(criticalPeak, Is.GreaterThan(cautionPeak));
            Assert.That(criticalPulses, Is.GreaterThan(cautionPulses));
            Assert.That(cautionPeak, Is.EqualTo(settings.Caution.VignetteMaxAlpha).Within(0.01f));
            Assert.That(criticalPeak, Is.EqualTo(settings.Critical.VignetteMaxAlpha).Within(0.01f));
        }

        [Test]
        public void Pulse_IsSmoothAndStaysBetweenFloorAndMax()
        {
            var settings = new GasPoisonOverlaySettings();
            var timeline = new GasPoisonOverlayTimeline(settings);
            timeline.SetLevel(GasRiskLevel.Hazard);
            Run(timeline, 1f);

            var previous = timeline.Evaluate().VignetteAlpha;
            var floor = settings.Critical.VignetteMaxAlpha * settings.Critical.VignetteMinAlphaRatio;
            for (var i = 0; i < 600; i++)
            {
                var alpha = timeline.Advance(Frame).VignetteAlpha;
                Assert.That(alpha, Is.InRange(floor - 0.001f, settings.Critical.VignetteMaxAlpha + 0.001f));
                Assert.That(System.Math.Abs(alpha - previous), Is.LessThan(0.03f));
                previous = alpha;
            }
        }

        [Test]
        public void Filter_IsFixedWhileLevelHolds()
        {
            var settings = new GasPoisonOverlaySettings();
            var timeline = new GasPoisonOverlayTimeline(settings);
            timeline.SetLevel(GasRiskLevel.Elevated);
            Run(timeline, 1f);

            for (var i = 0; i < 300; i++)
            {
                Assert.That(timeline.Advance(Frame).FilterAlpha,
                    Is.EqualTo(settings.Caution.FilterAlpha).Within(0.0001f));
            }
        }

        [Test]
        public void Enter_RisesMonotonicallyAndFinishesInEnterSeconds()
        {
            var settings = new GasPoisonOverlaySettings();
            var timeline = new GasPoisonOverlayTimeline(settings);
            timeline.SetLevel(GasRiskLevel.Elevated);

            var previousPresence = 0f;
            var previousFilter = 0f;
            var steps = (int)(settings.EnterSeconds / Frame);
            for (var i = 0; i < steps; i++)
            {
                var frame = timeline.Advance(Frame);
                Assert.That(frame.Presence, Is.GreaterThanOrEqualTo(previousPresence));
                Assert.That(frame.FilterAlpha, Is.GreaterThanOrEqualTo(previousFilter));
                previousPresence = frame.Presence;
                previousFilter = frame.FilterAlpha;
            }

            Assert.That(previousPresence, Is.GreaterThan(0.9f));
            Assert.That(Run(timeline, 0.1f).Presence, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void Enter_VignetteRisesMonotonicallyWhenPulseIsFlat()
        {
            var flat = new GasPoisonLevelSettings(0.35f, 2f, 1f, 0.06f, 1.18f);
            var timeline = new GasPoisonOverlayTimeline(new GasPoisonOverlaySettings(flat, flat));
            timeline.SetLevel(GasRiskLevel.Elevated);

            var previous = 0f;
            for (var i = 0; i < 24; i++)
            {
                var alpha = timeline.Advance(Frame).VignetteAlpha;
                Assert.That(alpha, Is.GreaterThanOrEqualTo(previous));
                previous = alpha;
            }
        }

        [Test]
        public void Exit_FallsMonotonicallyAndReachesZeroInExitSeconds()
        {
            var settings = new GasPoisonOverlaySettings();
            var timeline = new GasPoisonOverlayTimeline(settings);
            timeline.SetLevel(GasRiskLevel.Hazard);
            Run(timeline, 1f);
            timeline.SetLevel(GasRiskLevel.Safe);

            var previous = timeline.Evaluate().Presence;
            var steps = (int)(settings.ExitSeconds / Frame);
            for (var i = 0; i < steps / 2; i++)
            {
                var frame = timeline.Advance(Frame);
                Assert.That(frame.Presence, Is.LessThanOrEqualTo(previous));
                previous = frame.Presence;
            }

            Assert.That(previous, Is.GreaterThan(0f), "절반 시점에는 아직 남아 있어야 한다");

            for (var i = steps / 2; i < steps + 2; i++)
            {
                var frame = timeline.Advance(Frame);
                Assert.That(frame.Presence, Is.LessThanOrEqualTo(previous));
                previous = frame.Presence;
            }

            var last = timeline.Evaluate();
            Assert.That(last.Presence, Is.Zero);
            Assert.That(last.VignetteAlpha, Is.Zero);
            Assert.That(last.FilterAlpha, Is.Zero);
        }

        [Test]
        public void LevelChange_DoesNotJumpInASingleFrame()
        {
            var timeline = new GasPoisonOverlayTimeline();
            timeline.SetLevel(GasRiskLevel.Elevated);
            Run(timeline, 1.5f);

            var before = timeline.Evaluate();
            timeline.SetLevel(GasRiskLevel.Hazard);
            var previousAlpha = before.VignetteAlpha;
            var previousFilter = before.FilterAlpha;
            var previousScale = before.VignetteScale;
            for (var i = 0; i < 60; i++)
            {
                var frame = timeline.Advance(Frame);
                Assert.That(System.Math.Abs(frame.VignetteAlpha - previousAlpha), Is.LessThan(0.08f));
                Assert.That(System.Math.Abs(frame.FilterAlpha - previousFilter), Is.LessThan(0.02f));
                Assert.That(System.Math.Abs(frame.VignetteScale - previousScale), Is.LessThan(0.05f));
                previousAlpha = frame.VignetteAlpha;
                previousFilter = frame.FilterAlpha;
                previousScale = frame.VignetteScale;
            }

            Assert.That(previousFilter, Is.EqualTo(new GasPoisonOverlaySettings().Critical.FilterAlpha).Within(0.0001f));
        }

        [Test]
        public void LevelChange_PresenceStaysFullBetweenNonSafeLevels()
        {
            var timeline = new GasPoisonOverlayTimeline();
            timeline.SetLevel(GasRiskLevel.Hazard);
            Run(timeline, 1f);
            timeline.SetLevel(GasRiskLevel.Elevated);

            for (var i = 0; i < 30; i++)
            {
                Assert.That(timeline.Advance(Frame).Presence, Is.EqualTo(1f).Within(0.0001f));
            }
        }

        [Test]
        public void Reset_TurnsEverythingOffImmediately()
        {
            var timeline = new GasPoisonOverlayTimeline();
            timeline.SetLevel(GasRiskLevel.Hazard);
            Run(timeline, 1f);
            Assert.That(timeline.Evaluate().IsVisible, Is.True);

            timeline.Reset();

            var frame = timeline.Evaluate();
            Assert.That(frame.VignetteAlpha, Is.Zero);
            Assert.That(frame.FilterAlpha, Is.Zero);
            Assert.That(frame.Presence, Is.Zero);
            Assert.That(timeline.Target, Is.EqualTo(GasRiskLevel.Safe));
            Assert.That(timeline.Advance(Frame).IsVisible, Is.False);
        }

        [Test]
        public void ZeroOrNegativeDelta_DoesNotMoveValues()
        {
            var timeline = new GasPoisonOverlayTimeline();
            timeline.SetLevel(GasRiskLevel.Elevated);

            Assert.That(timeline.Advance(0f).Presence, Is.Zero);
            Assert.That(timeline.Advance(-1f).Presence, Is.Zero);
        }

        [Test]
        public void VignetteTexture_IsClearAtCenterAndOpaqueAtEdges()
        {
            Assert.That(GasPoisonVignetteTexture.AlphaAt(0f), Is.Zero);
            Assert.That(GasPoisonVignetteTexture.AlphaAt(GasPoisonVignetteTexture.InnerRadius), Is.Zero);
            Assert.That(GasPoisonVignetteTexture.AlphaAt(1f), Is.GreaterThan(0.9f));
            Assert.That(GasPoisonVignetteTexture.AlphaAt(1.5f), Is.EqualTo(1f));
        }

        /// <summary>진입 후 6초 동안 최대 알파와 맥박(국소 최대) 횟수를 잰다.</summary>
        [Test]
        public void Streaks_FollowVignettePulseAndAreStrongerInCritical()
        {
            var settings = new GasPoisonOverlaySettings();
            var caution = new GasPoisonOverlayTimeline(settings);
            caution.SetLevel(GasRiskLevel.Elevated);
            var critical = new GasPoisonOverlayTimeline(settings);
            critical.SetLevel(GasRiskLevel.Hazard);
            Run(caution, 1f);
            Run(critical, 1f);

            var cautionFrame = caution.Evaluate();
            var criticalFrame = critical.Evaluate();
            Assert.That(cautionFrame.StreakAlpha,
                Is.EqualTo(cautionFrame.VignetteAlpha * settings.Caution.StreakAlphaRatio).Within(0.0001f));
            Assert.That(settings.Critical.StreakAlphaRatio, Is.GreaterThan(settings.Caution.StreakAlphaRatio));
            Assert.That(criticalFrame.StreakAlpha, Is.GreaterThan(0f));

            critical.Reset();
            Assert.That(critical.Evaluate().StreakAlpha, Is.Zero);
        }

        [Test]
        public void RaggedVignette_IsClearAtCenterOpaqueAtEdgesAndDeterministic()
        {
            Assert.That(GasPoisonVignetteTexture.RaggedAlphaAt(0f, 256f, 144f), Is.Zero);
            for (var i = 0; i < GasPoisonVignetteTexture.Width; i += 7)
            {
                // 화면 가장자리 중앙(거리 1)에서는 노이즈가 있어도 대부분 진해야 한다.
                Assert.That(GasPoisonVignetteTexture.RaggedAlphaAt(1f, i, 3f), Is.GreaterThan(0.6f));
                Assert.That(GasPoisonVignetteTexture.RaggedAlphaAt(1.4f, i, 3f), Is.GreaterThan(0.99f));
            }

            Assert.That(GasPoisonVignetteTexture.RaggedAlphaAt(0.85f, 120f, 40f),
                Is.EqualTo(GasPoisonVignetteTexture.RaggedAlphaAt(0.85f, 120f, 40f)));

            // 경계가 실제로 울퉁불퉁한지: 같은 거리에서 위치마다 알파가 달라야 한다.
            var min = 1f;
            var max = 0f;
            for (var i = 0; i < 200; i++)
            {
                var a = GasPoisonVignetteTexture.RaggedAlphaAt(0.86f, i * 2.3f, 10f + i);
                min = System.Math.Min(min, a);
                max = System.Math.Max(max, a);
            }

            Assert.That(max - min, Is.GreaterThan(0.3f));
        }

        [Test]
        public void StreakTexture_IsClearAtCenterAndHasRadialLinesTowardEdges()
        {
            Assert.That(GasPoisonVignetteTexture.StreakAlphaAt(0.02f, 0.01f), Is.Zero);

            var lit = 0;
            var dark = 0;
            for (var i = 0; i < 720; i++)
            {
                var angle = i / 720f * Mathf.PI * 2f;
                var a = GasPoisonVignetteTexture.StreakAlphaAt(Mathf.Cos(angle) * 0.95f, Mathf.Sin(angle) * 0.95f);
                if (a > 0.1f) lit++; else dark++;
            }

            Assert.That(lit, Is.GreaterThan(30));
            Assert.That(dark, Is.GreaterThan(lit));
        }

        private static float MeasurePeak(GasRiskLevel level, out int pulses)
        {
            var timeline = new GasPoisonOverlayTimeline();
            timeline.SetLevel(level);
            Run(timeline, 1f);

            var peak = 0f;
            pulses = 0;
            var previous = timeline.Evaluate().VignetteAlpha;
            var rising = false;
            for (var i = 0; i < 360; i++)
            {
                var alpha = timeline.Advance(Frame).VignetteAlpha;
                if (alpha > peak)
                {
                    peak = alpha;
                }

                if (alpha > previous)
                {
                    rising = true;
                }
                else if (alpha < previous && rising)
                {
                    pulses++;
                    rising = false;
                }

                previous = alpha;
            }

            return peak;
        }
    }
}
