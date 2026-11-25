using NUnit.Framework;
using SubTerra.Gameplay.Player;
using UnityEngine;

namespace SubTerra.App.Tests.EditMode.Readiness
{
    /// <summary>B-211 가변 점프 최소 높이와 착지 손맛 수치.</summary>
    public sealed class PromptB211JumpFeelTests
    {
        private const float JumpImpulse = 11f;
        private const float Gravity = 9.81f * 3f;
        private const float FixedStep = 0.02f;
        private const float CutMultiplier = 0.5f;
        private const float MinApex = 1.1f;

        // 물리 틱(반암시적 오일러)으로 점프 한 번을 따라가 최고 높이와 자른 시각을 잰다.
        private static float SimulateApex(float cargoMultiplier, float releaseAt, out float cutAt)
        {
            float v = JumpImpulse * cargoMultiplier - Gravity * FixedStep;
            float y = v * FixedStep;
            float time = FixedStep;
            float apex = y;
            bool armed = true;
            cutAt = -1f;
            while (v > 0f || y > 0f)
            {
                if (armed)
                {
                    if (v <= 0f) armed = false;
                    else if (time >= releaseAt
                        && VariableJumpRule.CanCut(y, v, CutMultiplier, MinApex, Gravity, FixedStep))
                    {
                        v *= CutMultiplier;
                        armed = false;
                        cutAt = time;
                    }
                }

                v -= Gravity * FixedStep;
                y += v * FixedStep;
                time += FixedStep;
                apex = Mathf.Max(apex, y);
                if (v < 0f && y <= 0f) break;
            }

            return apex;
        }

        [TestCase(1f)]
        [TestCase(0.85f)]
        [TestCase(0.75f)]
        public void TapJump_NeverDropsBelowMinimumApex_AndHoldKeepsCurrentHeight(float cargo)
        {
            float held = SimulateApex(cargo, float.PositiveInfinity, out var heldCut);
            float tapped = SimulateApex(cargo, 0f, out var tapCut);

            Assert.That(heldCut, Is.LessThan(0f), "길게 누르면 자르지 않는다");
            Assert.That(tapped, Is.GreaterThanOrEqualTo(1f), "짧게 눌러도 1칸 턱 위로 오른다");
            Assert.That(tapped, Is.GreaterThanOrEqualTo(Mathf.Min(MinApex, held) - 0.0001f),
                "자른 점프도 최소 꼭대기 1.1칸(그보다 낮은 배율은 원래 높이)을 지킨다");
            Assert.That(tapped, Is.LessThanOrEqualTo(held + 0.0001f));
            TestContext.WriteLine($"cargo {cargo}: held {held:0.000}, tapped {tapped:0.000}, cut at {tapCut:0.00}s");
        }

        [Test]
        public void EmptyCargo_TapIsClearlyLowerThanHold()
        {
            float held = SimulateApex(1f, float.PositiveInfinity, out _);
            float tapped = SimulateApex(1f, 0f, out var cutAt);
            Assert.That(held, Is.GreaterThan(1.9f), "빈 몸 최대 높이는 약 2칸 그대로");
            Assert.That(tapped, Is.LessThan(held - 0.5f));
            Assert.That(cutAt, Is.InRange(0.05f, 0.15f), "빈 몸은 뜬 뒤 약 0.1초에 자른다");
        }

        [Test]
        public void FullCargo_MaxApexBelowMinimum_IsNeverCut()
        {
            float tapped = SimulateApex(0.75f, 0f, out var cutAt);
            float held = SimulateApex(0.75f, float.PositiveInfinity, out _);
            Assert.That(cutAt, Is.LessThan(0f), "가득 찬 화물은 최대 높이가 1.1보다 낮아 자르지 않는다");
            Assert.That(tapped, Is.EqualTo(held).Within(0.0001f));
        }

        [Test]
        public void CanCut_IgnoresFallingOrStoppedJump()
        {
            Assert.That(VariableJumpRule.CanCut(1.5f, 0f, CutMultiplier, MinApex, Gravity, FixedStep), Is.False);
            Assert.That(VariableJumpRule.CanCut(1.5f, -2f, CutMultiplier, MinApex, Gravity, FixedStep), Is.False);
        }

        [Test]
        public void RemainingRise_MatchesContinuousFormulaWithoutStep()
        {
            Assert.That(VariableJumpRule.RemainingRise(11f, Gravity, 0f),
                Is.EqualTo(121f / (2f * Gravity)).Within(0.0001f));
            Assert.That(VariableJumpRule.RemainingRise(11f, Gravity, FixedStep),
                Is.LessThan(121f / (2f * Gravity)), "물리 틱은 연속식보다 조금 덜 오른다");
        }

        [Test]
        public void JumpSequence_SpeedCut_ContinuesRiseFramesWithoutSkipping()
        {
            var sequence = new PlayerJumpAnimationSequence(new PlayerJumpAnimationSettings());
            var uncorrected = new PlayerJumpAnimationSequence(new PlayerJumpAnimationSettings());
            const float step = 1f / 60f;
            float v = 11f;
            sequence.StartJump(v);
            uncorrected.StartJump(v);
            sequence.Tick(0f, true, v, false);
            uncorrected.Tick(0f, true, v, false);
            for (int i = 0; i < 9; i++)
            {
                v -= Gravity * step;
                sequence.Tick(step, true, v, false);
                uncorrected.Tick(step, true, v, false);
            }

            Assert.That(sequence.Phase, Is.EqualTo(PlayerJumpPhase.Rise));
            int before = sequence.FrameIndex;
            v *= CutMultiplier;
            sequence.ContinueAfterSpeedCut(v);
            v -= Gravity * step;
            sequence.Tick(step, true, v, false);
            uncorrected.Tick(step, true, v, false);
            int cutStep = sequence.FrameIndex - before;
            int beforeNext = sequence.FrameIndex;
            sequence.Tick(step, true, v - Gravity * step, false);
            int nextStep = sequence.FrameIndex - beforeNext;
            // 남은 상승이 짧아져 한 틱에 1~2장씩 나가는 것은 자연스럽다. 잘린 틱만 크게 뛰면 안 된다.
            Assert.That(cutStep, Is.LessThanOrEqualTo(nextStep + 1), "잘린 순간 상승 그림이 한 번에 건너뛰지 않는다");
            Assert.That(uncorrected.FrameIndex - before, Is.GreaterThan(cutStep), "보정 없이는 그림이 크게 건너뛴다");
            v -= Gravity * step;

            int guard = 0;
            while (v > 0f && guard++ < 200)
            {
                v -= Gravity * step;
                sequence.Tick(step, true, v, false);
            }

            sequence.Tick(step, true, v - Gravity * step, false);
            Assert.That(sequence.Phase, Is.EqualTo(PlayerJumpPhase.Apex).Or.EqualTo(PlayerJumpPhase.FallLoop),
                "잘린 점프도 꼭대기→낙하로 넘어간다");
        }

        [Test]
        public void LandingSquash_InterpolatesBetweenLightAndHeavy()
        {
            var light = new Vector2(1.05f, 0.95f);
            var heavy = new Vector2(1.18f, 0.82f);
            AssertVector(PlayerLandingFeedbackRules.LandingSquash(0.3f, light, heavy), light);
            AssertVector(PlayerLandingFeedbackRules.LandingSquash(3f, light, heavy), heavy);
            AssertVector(PlayerLandingFeedbackRules.LandingSquash(12f, light, heavy), heavy);
            AssertVector(PlayerLandingFeedbackRules.LandingSquash(1.65f, light, heavy), new Vector2(1.115f, 0.885f));
            AssertVector(PlayerLandingFeedbackRules.ApplyStrength(heavy, 0f), Vector2.one);
            AssertVector(PlayerLandingFeedbackRules.ApplyStrength(heavy, 1.5f), new Vector2(1.27f, 0.73f));
        }

        [Test]
        public void RecoverWeight_IsSmoothAndHasNoOvershoot()
        {
            Assert.That(PlayerLandingFeedbackRules.RecoverWeight(0f, 0.12f), Is.EqualTo(1f));
            Assert.That(PlayerLandingFeedbackRules.RecoverWeight(0.12f, 0.12f), Is.EqualTo(0f));
            float previous = 1f;
            for (float t = 0f; t <= 0.2f; t += 0.005f)
            {
                float w = PlayerLandingFeedbackRules.RecoverWeight(t, 0.12f);
                Assert.That(w, Is.InRange(0f, 1f));
                Assert.That(w, Is.LessThanOrEqualTo(previous + 0.00001f));
                previous = w;
            }
        }

        [Test]
        public void FootAnchor_KeepsSoleOnGround()
        {
            const float foot = -0.35f;
            foreach (var scaleY in new[] { 0.82f, 0.95f, 1.12f })
            {
                float offset = PlayerLandingFeedbackRules.FootAnchorOffset(foot, scaleY);
                Assert.That(offset + foot * scaleY, Is.EqualTo(foot).Within(0.0001f));
            }
        }

        [Test]
        public void Squash_SkipsLowLadderAndKnockoutLandings_ButKeepsDamagedLanding()
        {
            Assert.That(PlayerLandingFeedbackRules.ShouldSquash(Land(0.29f)), Is.False);
            Assert.That(PlayerLandingFeedbackRules.ShouldSquash(Land(2f, ladder: true)), Is.False);
            Assert.That(PlayerLandingFeedbackRules.ShouldSquash(Land(30f, damaged: true, canAct: false)), Is.False);
            Assert.That(PlayerLandingFeedbackRules.ShouldSquash(Land(12f, damaged: true)), Is.True);
            Assert.That(PlayerLandingFeedbackRules.ShouldSquash(Land(0.3f)), Is.True);
        }

        [Test]
        public void Dust_SizeAndCountFollowFallHeight()
        {
            AssertDust(Land(0.2f), PlayerLandingDustSize.None, 0);
            AssertDust(Land(0.4f), PlayerLandingDustSize.Small, 2);
            AssertDust(Land(0.9f), PlayerLandingDustSize.Small, 3);
            AssertDust(Land(2f), PlayerLandingDustSize.Medium, 4);
            AssertDust(Land(3f), PlayerLandingDustSize.Large, 6);
            AssertDust(Land(5.5f), PlayerLandingDustSize.Large, 7);
            AssertDust(Land(9f), PlayerLandingDustSize.Large, 8);
            AssertDust(Land(12f, damaged: true), PlayerLandingDustSize.Large, 8);
            AssertDust(Land(5f, ladder: true), PlayerLandingDustSize.None, 0);
            AssertDust(Land(2f, stable: true), PlayerLandingDustSize.Small, 3);
            AssertDust(Land(0.5f, stable: true), PlayerLandingDustSize.None, 0);
            AssertDust(Land(30f, stable: true, damaged: true), PlayerLandingDustSize.Medium, 5);
        }

        [Test]
        public void LandingShake_OnlyHighUndamagedLandings()
        {
            AssertShake(Land(3f), true, 0.05f);
            AssertShake(Land(5.5f), true, 0.075f);
            AssertShake(Land(8f), true, 0.10f);
            AssertShake(Land(20f), true, 0.10f);
            AssertShake(Land(2.9f), false, 0f);
            AssertShake(Land(12f, damaged: true), false, 0f);
            AssertShake(Land(5f, stable: true), false, 0f);
            AssertShake(Land(5f, ladder: true), false, 0f);
            Assert.That(PlayerLandingFeedbackRules.TryResolveShake(Land(5f), true, false, out _, out _), Is.False,
                "디버그 falling(흔들림 억제)에서는 흔들지 않는다");
            Assert.That(PlayerLandingFeedbackRules.TryResolveShake(Land(5f), false, true, out _, out _), Is.False,
                "줄인 움직임에서는 흔들지 않는다");
        }

        private static PlayerLandingInfo Land(float distance, bool ladder = false, bool stable = false,
            bool damaged = false, bool canAct = true) => new(distance, ladder, stable, damaged, canAct);

        private static void AssertDust(PlayerLandingInfo landing, PlayerLandingDustSize size, int count)
        {
            var actual = PlayerLandingFeedbackRules.ResolveDustSize(landing);
            Assert.That(actual, Is.EqualTo(size), "높이 " + landing.Distance);
            Assert.That(PlayerLandingFeedbackRules.ResolveDustCount(actual, landing), Is.EqualTo(count),
                "높이 " + landing.Distance);
        }

        private static void AssertShake(PlayerLandingInfo landing, bool expected, float amplitude)
        {
            bool shake = PlayerLandingFeedbackRules.TryResolveShake(landing, false, false, out var a, out var d);
            Assert.That(shake, Is.EqualTo(expected), "높이 " + landing.Distance);
            if (!expected) return;
            Assert.That(a, Is.EqualTo(amplitude).Within(0.0001f));
            Assert.That(d, Is.EqualTo(0.1f).Within(0.0001f));
        }

        private static void AssertVector(Vector2 actual, Vector2 expected)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(0.0001f));
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(0.0001f));
        }
    }
}
