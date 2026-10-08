using NUnit.Framework;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.Integration;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Tests.Integration
{
    /// <summary>골드 획득 팝업: 순수 Timeline·State 계산과 View 구성 계약 (prompt-B 142).</summary>
    public sealed class PromptB142GoldPickupPopupTests
    {
        private const float Tolerance = 0.002f;

        // ---------- Timeline ----------

        [Test]
        public void Timeline_StageTimesMatchTheRequestedSchedule()
        {
            Assert.That(GoldPickupPopupTimeline.FrameDuration, Is.EqualTo(0.14f).Within(Tolerance));
            Assert.That(GoldPickupPopupTimeline.LeverPullStart, Is.EqualTo(0.04f).Within(Tolerance));
            Assert.That(GoldPickupPopupTimeline.LeverPullEnd, Is.EqualTo(0.13f).Within(Tolerance));
            Assert.That(GoldPickupPopupTimeline.LeverReturnEnd, Is.EqualTo(0.22f).Within(Tolerance));
            Assert.That(GoldPickupPopupTimeline.BaseFallStart, Is.EqualTo(0.13f).Within(Tolerance));
            Assert.That(GoldPickupPopupTimeline.BaseFallEnd, Is.EqualTo(0.25f).Within(Tolerance));
            Assert.That(GoldPickupPopupTimeline.BonusFallStart, Is.EqualTo(0.28f).Within(Tolerance));
            Assert.That(GoldPickupPopupTimeline.BonusFallEnd, Is.EqualTo(0.40f).Within(Tolerance));
            Assert.That(
                GoldPickupPopupTimeline.BaseFallStart,
                Is.EqualTo(GoldPickupPopupTimeline.LeverPullEnd).Within(Tolerance),
                "기본 낙하는 레버가 올라오기 시작할 때 시작한다.");
            Assert.That(
                GoldPickupPopupTimeline.BonusFallStart - GoldPickupPopupTimeline.BaseFallStart,
                Is.EqualTo(0.15f).Within(Tolerance));
            Assert.That(GoldPickupPopupTimeline.ExitDuration, Is.EqualTo(0.22f).Within(Tolerance));
            Assert.That(GoldPickupPopupTimeline.ExitRise, Is.EqualTo(0.15f).Within(Tolerance));
            Assert.That(GoldPickupPopupTimeline.MergeHold, Is.EqualTo(0.45f).Within(Tolerance));
            Assert.That(GoldPickupPopupTimeline.MergeRollDuration, Is.EqualTo(0.22f).Within(Tolerance));
            Assert.That(GoldPickupPopupTimeline.MergeRestoreDuration, Is.EqualTo(0.06f).Within(Tolerance));
            Assert.That(GoldPickupPopupTimeline.ExpandDuration, Is.EqualTo(0.1f).Within(Tolerance));
        }

        [Test]
        public void Timeline_FrameIsFixedWidthWithSeparateLeverSpace()
        {
            Assert.That(
                GoldPickupPopupTimeline.BodyWidth + GoldPickupPopupTimeline.LeverZoneWidth,
                Is.EqualTo(GoldPickupPopupTimeline.FrameWidth));
            Assert.That(GoldPickupPopupTimeline.BodyWidth, Is.EqualTo(200f));
            Assert.That(GoldPickupPopupTimeline.BaseHeight, Is.EqualTo(44f));
            Assert.That(GoldPickupPopupTimeline.BonusHeight, Is.EqualTo(72f));
            Assert.That(GoldPickupPopupTimeline.EdgeThickness, Is.EqualTo(1.5f));
        }

        [Test]
        public void Timeline_FrameHeight_ExpandsOnceWithEaseOut()
        {
            Assert.That(GoldPickupPopupTimeline.FrameHeight(false, 5f), Is.EqualTo(44f));
            Assert.That(GoldPickupPopupTimeline.FrameHeight(true, -1f), Is.EqualTo(44f));
            Assert.That(GoldPickupPopupTimeline.FrameHeight(true, 0f), Is.EqualTo(44f));
            float half = GoldPickupPopupTimeline.FrameHeight(true, GoldPickupPopupTimeline.ExpandDuration * 0.5f);
            Assert.That(half, Is.GreaterThan(58f), "ease-out이라 절반 시점에 절반 이상 커져 있다.");
            Assert.That(GoldPickupPopupTimeline.FrameHeight(true, GoldPickupPopupTimeline.ExpandDuration), Is.EqualTo(72f));
            Assert.That(GoldPickupPopupTimeline.FrameHeight(true, 3f), Is.EqualTo(72f));

            float previous = 44f;
            for (var i = 0; i <= 20; i++)
            {
                float height = GoldPickupPopupTimeline.FrameHeight(true, i * 0.01f);
                Assert.That(height, Is.GreaterThanOrEqualTo(previous));
                previous = height;
            }
        }

        [Test]
        public void Timeline_Lever_PullsDownThenReturnsWithSingleSettle()
        {
            Assert.That(GoldPickupPopupTimeline.LeverAngle(0f), Is.Zero);
            Assert.That(GoldPickupPopupTimeline.LeverAngle(GoldPickupPopupTimeline.LeverPullStart), Is.Zero);
            Assert.That(
                GoldPickupPopupTimeline.LeverAngle(GoldPickupPopupTimeline.LeverPullEnd),
                Is.EqualTo(-GoldPickupPopupTimeline.LeverPullAngle).Within(0.01f));

            float previous = 0f;
            for (var t = 0.04f; t <= 0.13f; t += 0.005f)
            {
                float angle = GoldPickupPopupTimeline.LeverAngle(t);
                Assert.That(angle, Is.LessThanOrEqualTo(previous + 0.0001f), "하강 구간은 단조롭게 당겨진다.");
                previous = angle;
            }

            // 복귀: 양수 방향으로 한 번만 살짝 넘치고 0에 정착한다. 반복 바운스는 없다.
            var crossings = 0;
            float last = GoldPickupPopupTimeline.LeverAngle(0.1301f);
            float maxOvershoot = 0f;
            for (var t = 0.131f; t <= 0.2199f; t += 0.001f)
            {
                float angle = GoldPickupPopupTimeline.LeverAngle(t);
                if ((last < 0f && angle > 0f) || (last > 0f && angle < 0f))
                {
                    crossings++;
                }

                maxOvershoot = Mathf.Max(maxOvershoot, angle);
                last = angle;
            }

            Assert.That(crossings, Is.LessThanOrEqualTo(1));
            Assert.That(maxOvershoot, Is.InRange(0f, 12f));
            Assert.That(GoldPickupPopupTimeline.LeverAngle(GoldPickupPopupTimeline.LeverReturnEnd), Is.Zero);
            Assert.That(GoldPickupPopupTimeline.LeverAngle(2f), Is.Zero);
        }

        [Test]
        public void Timeline_Row_FallsEaseInThenSettlesToExactRest()
        {
            const float slot = GoldPickupPopupTimeline.MainSlotHeight;
            var pre = GoldPickupPopupTimeline.EvaluateRow(-0.2f, slot);
            Assert.That(pre.OffsetY, Is.EqualTo(slot), "슬롯 위쪽 밖에서 출발한다.");
            Assert.That(pre.Blur, Is.Zero);

            // ease-in: 절반 시간에 절반 거리보다 덜 내려와 있다.
            var mid = GoldPickupPopupTimeline.EvaluateRow(GoldPickupPopupTimeline.FallDuration * 0.5f, slot);
            Assert.That(mid.OffsetY, Is.GreaterThan(slot * 0.5f));

            float previous = slot + 1f;
            for (var t = 0.0005f; t < GoldPickupPopupTimeline.FallDuration; t += 0.005f)
            {
                float offset = GoldPickupPopupTimeline.EvaluateRow(t, slot).OffsetY;
                Assert.That(offset, Is.LessThan(previous));
                previous = offset;
            }

            var land = GoldPickupPopupTimeline.EvaluateRow(GoldPickupPopupTimeline.FallDuration, slot);
            Assert.That(land.OffsetY, Is.EqualTo(0f).Within(0.001f));
            Assert.That(land.ScaleY, Is.EqualTo(1.3f).Within(0.001f));
            Assert.That(land.ScaleX, Is.EqualTo(0.94f).Within(0.001f));
            Assert.That(land.Blur, Is.EqualTo(1f).Within(0.001f));

            float lowest = 0f;
            for (var s = 0f; s < GoldPickupPopupTimeline.SettleDuration; s += 0.002f)
            {
                lowest = Mathf.Min(
                    lowest,
                    GoldPickupPopupTimeline.EvaluateRow(GoldPickupPopupTimeline.FallDuration + s, slot).OffsetY);
            }

            Assert.That(lowest, Is.InRange(-4f, -2f), "착지 후 2~4px overshoot.");

            // float 오차를 피하려고 경계보다 1ms 뒤를 본다.
            var blurGone = GoldPickupPopupTimeline.EvaluateRow(
                GoldPickupPopupTimeline.FallDuration + GoldPickupPopupTimeline.BlurClearDuration + 0.001f, slot);
            Assert.That(blurGone.Blur, Is.Zero, "착지 후 0.05초 안에 블러 제거.");
            Assert.That(blurGone.OffsetY, Is.LessThan(0f).And.GreaterThan(-4f), "블러가 사라진 뒤에도 복귀는 이어진다.");

            var settled = GoldPickupPopupTimeline.EvaluateRow(
                GoldPickupPopupTimeline.FallDuration + GoldPickupPopupTimeline.SettleDuration + 0.001f, slot);
            Assert.That(settled.OffsetY, Is.Zero);
            Assert.That(settled.ScaleX, Is.EqualTo(1f));
            Assert.That(settled.ScaleY, Is.EqualTo(1f));
            Assert.That(settled.Blur, Is.Zero);
        }

        [Test]
        public void Timeline_Ghosts_ScaleWithSpeedAndFadeStepwise()
        {
            Assert.That(GoldPickupPopupTimeline.MainGhostCount, Is.InRange(2, 3));
            Assert.That(GoldPickupPopupTimeline.BonusGhostCount, Is.InRange(2, 3));
            Assert.That(GoldPickupPopupTimeline.BonusGhostSpacing, Is.LessThan(GoldPickupPopupTimeline.MainGhostSpacing));

            Assert.That(GoldPickupPopupTimeline.GhostAlpha(0, 0f), Is.Zero);
            Assert.That(GoldPickupPopupTimeline.GhostOffset(2, 0f, 9f), Is.Zero);
            Assert.That(
                GoldPickupPopupTimeline.GhostOffset(0, 1f, 9f),
                Is.GreaterThan(GoldPickupPopupTimeline.GhostOffset(0, 0.5f, 9f)));
            for (var ghost = 0; ghost < 2; ghost++)
            {
                Assert.That(
                    GoldPickupPopupTimeline.GhostOffset(ghost + 1, 0.8f, 9f),
                    Is.GreaterThan(GoldPickupPopupTimeline.GhostOffset(ghost, 0.8f, 9f)));
                Assert.That(
                    GoldPickupPopupTimeline.GhostAlpha(ghost + 1, 0.8f),
                    Is.LessThan(GoldPickupPopupTimeline.GhostAlpha(ghost, 0.8f)));
            }

            Assert.That(
                GoldPickupPopupTimeline.GhostAlpha(0, 1f),
                Is.GreaterThan(GoldPickupPopupTimeline.GhostAlpha(0, 0.4f)));
        }

        [Test]
        public void Timeline_CoinCounts_FollowBoundaryRules()
        {
            Assert.That(GoldPickupPopupTimeline.CoinCountFor(0), Is.EqualTo(3));
            Assert.That(GoldPickupPopupTimeline.CoinCountFor(-50), Is.EqualTo(3));
            Assert.That(GoldPickupPopupTimeline.CoinCountFor(1), Is.EqualTo(3));
            Assert.That(GoldPickupPopupTimeline.CoinCountFor(9), Is.EqualTo(3));
            Assert.That(GoldPickupPopupTimeline.CoinCountFor(10), Is.EqualTo(5));
            Assert.That(GoldPickupPopupTimeline.CoinCountFor(99), Is.EqualTo(5));
            Assert.That(GoldPickupPopupTimeline.CoinCountFor(100), Is.EqualTo(7));
            Assert.That(GoldPickupPopupTimeline.CoinCountFor(999), Is.EqualTo(7));
            Assert.That(GoldPickupPopupTimeline.CoinCountFor(1000), Is.EqualTo(8));
            Assert.That(GoldPickupPopupTimeline.CoinCountFor(int.MaxValue), Is.EqualTo(8));

            Assert.That(GoldPickupPopupTimeline.BaseLandingCoinCount(20), Is.EqualTo(5));
            Assert.That(GoldPickupPopupTimeline.BonusLandingCoinCount(5), Is.EqualTo(5));
            Assert.That(GoldPickupPopupTimeline.BonusLandingCoinCount(20), Is.EqualTo(7));
            Assert.That(GoldPickupPopupTimeline.BonusLandingCoinCount(1000000), Is.EqualTo(10));
            for (var amount = 1; amount < 100000; amount *= 3)
            {
                Assert.That(GoldPickupPopupTimeline.BonusLandingCoinCount(amount), Is.InRange(4, 10));
            }

            Assert.That(GoldPickupPopupTimeline.MergeCoinCount(0), Is.Zero);
            Assert.That(GoldPickupPopupTimeline.MergeCoinCount(-9), Is.Zero);
            Assert.That(GoldPickupPopupTimeline.MergeCoinCount(1), Is.EqualTo(2));
            Assert.That(GoldPickupPopupTimeline.MergeCoinCount(10), Is.EqualTo(4));
            Assert.That(GoldPickupPopupTimeline.MergeCoinCount(100), Is.EqualTo(5));
            Assert.That(GoldPickupPopupTimeline.MergeCoinCount(int.MaxValue), Is.EqualTo(5));
        }

        [Test]
        public void Timeline_CoinSpawnLimit_NeverExceedsTwentyFourAlive()
        {
            Assert.That(GoldPickupPopupTimeline.MaxAliveCoins, Is.EqualTo(24));
            Assert.That(GoldPickupPopupTimeline.LimitCoinSpawn(0, 8), Is.EqualTo(8));
            Assert.That(GoldPickupPopupTimeline.LimitCoinSpawn(20, 8), Is.EqualTo(4));
            Assert.That(GoldPickupPopupTimeline.LimitCoinSpawn(24, 5), Is.Zero);
            Assert.That(GoldPickupPopupTimeline.LimitCoinSpawn(30, 5), Is.Zero);
            Assert.That(GoldPickupPopupTimeline.LimitCoinSpawn(3, 0), Is.Zero);
            Assert.That(GoldPickupPopupTimeline.LimitCoinSpawn(-4, 10), Is.EqualTo(10));
        }

        [Test]
        public void Timeline_CoinTrajectory_IsDeterministicFanThenGravityFall()
        {
            for (var index = 0; index < 8; index++)
            {
                GoldPickupPopupTimeline.CoinLaunch(index, 8, out float vx1, out float vy1, out float d1);
                GoldPickupPopupTimeline.CoinLaunch(index, 8, out float vx2, out float vy2, out float d2);
                Assert.That(vx1, Is.EqualTo(vx2));
                Assert.That(vy1, Is.EqualTo(vy2));
                Assert.That(d1, Is.EqualTo(d2));
                Assert.That(vy1, Is.GreaterThan(0f), "위쪽 가장자리에서 위로 분출한다.");
            }

            GoldPickupPopupTimeline.CoinLaunch(0, 8, out float leftVx, out _, out _);
            GoldPickupPopupTimeline.CoinLaunch(7, 8, out float rightVx, out _, out _);
            Assert.That(leftVx, Is.LessThan(0f));
            Assert.That(rightVx, Is.GreaterThan(0f));

            GoldPickupPopupTimeline.CoinLaunch(3, 8, out float vx, out float vy, out _);
            Vector2 origin = new Vector2(10f, 44f);
            Vector2 start = GoldPickupPopupTimeline.CoinPosition(origin, vx, vy, 0f);
            Vector2 apex = GoldPickupPopupTimeline.CoinPosition(origin, vx, vy, vy / GoldPickupPopupTimeline.CoinGravity);
            Vector2 late = GoldPickupPopupTimeline.CoinPosition(origin, vx, vy, GoldPickupPopupTimeline.CoinLife);
            Assert.That(start, Is.EqualTo(origin));
            Assert.That(apex.y, Is.GreaterThan(origin.y));
            Assert.That(late.y, Is.LessThan(apex.y), "중력으로 떨어진다.");
            Assert.That(GoldPickupPopupTimeline.CoinLife, Is.EqualTo(0.5f).Within(Tolerance));
        }

        [Test]
        public void Timeline_Perimeter_StaysOnTheFrameBorder()
        {
            const float w = 256f;
            const float h = 72f;
            Vector2 start = GoldPickupPopupTimeline.PerimeterPoint(0f, w, h, out bool horizontal);
            Assert.That(start, Is.EqualTo(new Vector2(-w * 0.5f, h)));
            Assert.That(horizontal, Is.True);

            for (var i = 0; i <= 40; i++)
            {
                Vector2 p = GoldPickupPopupTimeline.PerimeterPoint(i / 40f, w, h, out _);
                bool onBorder = Mathf.Abs(Mathf.Abs(p.x) - w * 0.5f) < 0.01f
                    || Mathf.Abs(p.y) < 0.01f
                    || Mathf.Abs(p.y - h) < 0.01f;
                Assert.That(onBorder, Is.True, "i=" + i + " p=" + p);
                Assert.That(Mathf.Abs(p.x), Is.LessThanOrEqualTo(w * 0.5f + 0.01f));
                Assert.That(p.y, Is.InRange(-0.01f, h + 0.01f));
            }

            Assert.That(GoldPickupPopupTimeline.SweepStrength(-0.1f), Is.Zero);
            Assert.That(GoldPickupPopupTimeline.SweepStrength(GoldPickupPopupTimeline.SweepDuration), Is.Zero);
            Assert.That(
                GoldPickupPopupTimeline.SweepStrength(GoldPickupPopupTimeline.SweepDuration * 0.5f),
                Is.EqualTo(1f).Within(0.001f));
            Assert.That(GoldPickupPopupTimeline.SweepDuration, Is.EqualTo(0.18f).Within(Tolerance));
        }

        // ---------- State ----------

        [Test]
        public void State_ConfirmedValuesAreSeparatedAndNeverNegative()
        {
            var state = new GoldPickupPopupState(20, 15);
            Assert.That(state.BaseTotal, Is.EqualTo(20));
            Assert.That(state.BonusTotal, Is.EqualTo(15));
            Assert.That(state.HasBonus, Is.True);
            Assert.That(state.BaseDisplay, Is.EqualTo(20), "최초 표시는 롤업 없이 확정값이다.");

            var plain = new GoldPickupPopupState(-4, -9);
            Assert.That(plain.BaseTotal, Is.Zero);
            Assert.That(plain.BonusTotal, Is.Zero);
            Assert.That(plain.HasBonus, Is.False);
        }

        [Test]
        public void State_BaseOnly_ExitsAt068AndEndsAt090()
        {
            var state = new GoldPickupPopupState(120, 0);
            Assert.That(state.ExitStartAge, Is.EqualTo(0.68f).Within(Tolerance));
            float exitAt = -1f;
            for (var i = 0; i < 400 && !state.Finished; i++)
            {
                state.Advance(0.005f);
                if (exitAt < 0f && state.Exiting)
                {
                    exitAt = state.Age;
                }
            }

            Assert.That(state.Finished, Is.True);
            Assert.That(exitAt, Is.EqualTo(0.68f).Within(0.01f));
            Assert.That(state.Age, Is.EqualTo(0.90f).Within(0.01f));
        }

        [Test]
        public void State_WithBonus_ExitsAt083AndEndsAt105()
        {
            var state = new GoldPickupPopupState(120, 30);
            Assert.That(state.ExitStartAge, Is.EqualTo(0.83f).Within(Tolerance));
            float exitAt = -1f;
            for (var i = 0; i < 400 && !state.Finished; i++)
            {
                state.Advance(0.005f);
                if (exitAt < 0f && state.Exiting)
                {
                    exitAt = state.Age;
                }
            }

            Assert.That(exitAt, Is.EqualTo(0.83f).Within(0.01f));
            Assert.That(state.Age, Is.EqualTo(1.05f).Within(0.01f));
        }

        [Test]
        public void State_ExitRiseAndFadeAreContinuousAndBounded()
        {
            var state = new GoldPickupPopupState(10, 0);
            float previous = 0f;
            for (var i = 0; i < 400 && !state.Finished; i++)
            {
                state.Advance(0.005f);
                Assert.That(state.ExitAmount, Is.GreaterThanOrEqualTo(previous - 0.0001f));
                previous = state.ExitAmount;
            }

            Assert.That(previous, Is.EqualTo(1f));
            Assert.That(GoldPickupPopupTimeline.ExitRiseWorld(1f), Is.EqualTo(0.15f).Within(0.0001f));
            Assert.That(GoldPickupPopupTimeline.ExitAlpha(1f), Is.Zero);
            Assert.That(GoldPickupPopupTimeline.ExitAlpha(0f), Is.EqualTo(1f));
        }

        [Test]
        public void State_LandingEvents_FireOnceAndBonusOnlyWhenPresent()
        {
            var state = new GoldPickupPopupState(10, 5);
            state.Advance(0.20f);
            Assert.That(state.TakeBaseLanding(), Is.False);
            state.Advance(0.06f);
            Assert.That(state.TakeBaseLanding(), Is.True);
            Assert.That(state.TakeBaseLanding(), Is.False);
            Assert.That(state.TakeBonusLanding(), Is.False);
            state.Advance(0.15f);
            Assert.That(state.TakeBonusLanding(), Is.True);
            Assert.That(state.TakeBonusLanding(), Is.False);

            var plain = new GoldPickupPopupState(10, 0);
            plain.Advance(0.6f);
            Assert.That(plain.TakeBaseLanding(), Is.True);
            Assert.That(plain.TakeBonusLanding(), Is.False, "추가가 0이면 두 번째 분출을 생략한다.");
            Assert.That(plain.HasBonus, Is.False);
        }

        [Test]
        public void State_Merge_SumsAndRollsUpMonotonicallyWithoutOvershoot()
        {
            var state = new GoldPickupPopupState(100, 0);
            state.Advance(0.5f);
            Assert.That(state.Merge(50, 0), Is.EqualTo(50));
            Assert.That(state.BaseTotal, Is.EqualTo(150));
            Assert.That(state.BaseDisplay, Is.EqualTo(100), "롤업은 현재 표시값에서 시작한다.");

            int previous = state.BaseDisplay;
            for (var i = 0; i < 40; i++)
            {
                state.Advance(0.01f);
                int display = state.BaseDisplay;
                Assert.That(display, Is.GreaterThanOrEqualTo(previous));
                Assert.That(display, Is.LessThanOrEqualTo(150));
                previous = display;
            }

            Assert.That(state.BaseDisplay, Is.EqualTo(150));
        }

        [Test]
        public void State_ConsecutiveMerges_ContinueFromCurrentDisplay()
        {
            var state = new GoldPickupPopupState(100, 20);
            state.Advance(0.5f);
            state.Merge(60, 0);
            state.Advance(0.08f);
            int midway = state.BaseDisplay;
            Assert.That(midway, Is.InRange(101, 159));

            Assert.That(state.Merge(40, 5), Is.EqualTo(45));
            Assert.That(state.BaseDisplay, Is.EqualTo(midway), "연속 합치기에도 표시값이 뒤로 가지 않는다.");
            Assert.That(state.MergeCount, Is.EqualTo(2));

            int previous = midway;
            int bonusPrevious = state.BonusDisplay;
            for (var i = 0; i < 40; i++)
            {
                state.Advance(0.01f);
                Assert.That(state.BaseDisplay, Is.GreaterThanOrEqualTo(previous));
                Assert.That(state.BonusDisplay, Is.GreaterThanOrEqualTo(bonusPrevious));
                previous = state.BaseDisplay;
                bonusPrevious = state.BonusDisplay;
            }

            Assert.That(state.BaseDisplay, Is.EqualTo(200));
            Assert.That(state.BonusDisplay, Is.EqualTo(25));
        }

        [Test]
        public void State_Merge_ProtectsAgainstOverflowAndZeroIncrease()
        {
            var state = new GoldPickupPopupState(int.MaxValue - 3, 0);
            Assert.That(state.Merge(10, 0), Is.EqualTo(3));
            Assert.That(state.BaseTotal, Is.EqualTo(int.MaxValue));
            int mergesBefore = state.MergeCount;
            Assert.That(state.Merge(1, 0), Is.Zero, "더 늘지 않으면 분출하지 않는다.");
            Assert.That(state.Merge(0, 0), Is.Zero);
            Assert.That(state.Merge(-5, -5), Is.Zero);
            Assert.That(state.MergeCount, Is.EqualTo(mergesBefore));
            Assert.That(state.BaseTotal, Is.EqualTo(int.MaxValue));
            state.Advance(1f);
            Assert.That(state.BaseDisplay, Is.EqualTo(int.MaxValue));

            var both = new GoldPickupPopupState(int.MaxValue - 1, int.MaxValue - 1);
            Assert.That(both.Merge(5, 5), Is.EqualTo(2), "증가분 합계도 int 상한에서 멈춘다.");
        }

        [Test]
        public void State_FirstBonusViaMerge_ExpandsHeightAndDropsLineOnlyOnce()
        {
            var state = new GoldPickupPopupState(10, 0);
            state.Advance(0.5f);
            Assert.That(state.HasBonus, Is.False);
            Assert.That(state.FrameHeight, Is.EqualTo(44f));

            state.Merge(0, 7);
            Assert.That(state.HasBonus, Is.True);
            Assert.That(state.BonusStart, Is.EqualTo(0.5f).Within(Tolerance));
            Assert.That(state.BonusFallTime, Is.EqualTo(-GoldPickupPopupTimeline.LateBonusFallDelay).Within(Tolerance));
            Assert.That(state.BonusDisplay, Is.EqualTo(7), "최초 추가는 롤업 없이 낙하로 보여 준다.");

            state.Advance(0.05f);
            Assert.That(state.FrameHeight, Is.InRange(45f, 71f));
            state.Advance(0.06f);
            Assert.That(state.FrameHeight, Is.EqualTo(72f));

            float start = state.BonusStart;
            float fallStartTime = state.BonusFallTime;
            state.Merge(0, 3);
            Assert.That(state.BonusStart, Is.EqualTo(start), "이미 추가 줄이 있으면 확장을 다시 하지 않는다.");
            Assert.That(state.BonusFallTime, Is.EqualTo(fallStartTime), "추가 줄 낙하도 다시 하지 않는다.");
            Assert.That(state.FrameHeight, Is.EqualTo(72f));
            Assert.That(state.BonusTotal, Is.EqualTo(10));
        }

        [Test]
        public void State_ExitIsDeferredAfterLastMergeAndSettle()
        {
            var state = new GoldPickupPopupState(10, 0);
            state.Advance(0.5f);
            state.Merge(5, 0);
            Assert.That(state.ExitStartAge, Is.GreaterThanOrEqualTo(0.5f + GoldPickupPopupTimeline.MergeHold - 0.0001f));

            state.Advance(0.3f);
            Assert.That(state.Exiting, Is.False);
            state.Merge(5, 0);
            Assert.That(state.ExitStartAge, Is.GreaterThanOrEqualTo(0.8f + GoldPickupPopupTimeline.MergeHold - 0.0001f));

            var late = new GoldPickupPopupState(10, 0);
            late.Advance(0.2f);
            late.Merge(0, 4);
            Assert.That(
                late.ExitStartAge,
                Is.GreaterThanOrEqualTo(late.LandingEnd + GoldPickupPopupTimeline.ExitLead - 0.0001f),
                "나중에 생긴 추가 줄의 정착 이후로 퇴장이 밀린다.");
        }

        [Test]
        public void State_MergeDuringExit_RestoresSmoothlyAndHudStartsOnlyOnce()
        {
            var state = new GoldPickupPopupState(20, 0);
            state.Advance(state.ExitStartAge + 0.1f);
            Assert.That(state.Exiting, Is.True);
            Assert.That(state.HudSequenceStarted, Is.True);
            float before = state.ExitAmount;
            float hudClock = state.HudClock;
            Assert.That(before, Is.GreaterThan(0.2f));

            Assert.That(state.Merge(5, 0), Is.EqualTo(5));
            Assert.That(state.Exiting, Is.False);
            Assert.That(state.ExitAmount, Is.EqualTo(before), "합치기 순간 알파·높이가 튀지 않는다.");

            float previous = before;
            for (var i = 0; i < 6; i++)
            {
                state.Advance(0.01f);
                Assert.That(state.ExitAmount, Is.LessThanOrEqualTo(previous + 0.0001f));
                Assert.That(state.HudClock, Is.GreaterThanOrEqualTo(hudClock));
                hudClock = state.HudClock;
                previous = state.ExitAmount;
            }

            state.Advance(0.01f);
            Assert.That(state.ExitAmount, Is.Zero, "약 0.06초에 알파가 복원된다.");
            Assert.That(state.Finished, Is.False);

            // 다시 퇴장해도 HUD 시퀀스는 처음 한 번의 것이다(재시작 없음).
            while (!state.Finished)
            {
                state.Advance(0.01f);
                Assert.That(state.HudClock, Is.GreaterThanOrEqualTo(hudClock));
                hudClock = state.HudClock;
            }

            Assert.That(state.HudSequenceStarted, Is.True);
            Assert.That(state.Merge(5, 0), Is.Zero, "끝난 팝업에는 합치지 않는다.");
        }

        // ---------- View ----------

        [Test]
        public void View_FrameWidthIsFixedRegardlessOfAmount()
        {
            var font = LoadFontOrIgnore();
            var small = GoldPickupPopupVisual.Create(null, font, null);
            var large = GoldPickupPopupVisual.Create(null, font, null);
            try
            {
                var smallState = new GoldPickupPopupState(5, 0);
                var largeState = new GoldPickupPopupState(1234567890, 987654321);
                smallState.Advance(0.6f);
                largeState.Advance(0.6f);
                small.Tick(0.6f, smallState);
                large.Tick(0.6f, largeState);

                Assert.That(small.FrameWidthNow, Is.EqualTo(GoldPickupPopupTimeline.FrameWidth).Within(0.01f));
                Assert.That(large.FrameWidthNow, Is.EqualTo(small.FrameWidthNow));
                Assert.That(large.FrameHeightNow, Is.EqualTo(GoldPickupPopupTimeline.BonusHeight));
                Assert.That(small.FrameHeightNow, Is.EqualTo(GoldPickupPopupTimeline.BaseHeight));
                Assert.That(small.MainFill.enableAutoSizing, Is.True, "긴 숫자는 TMP 자동 크기로 맞춘다.");
            }
            finally
            {
                small.Destroy();
                large.Destroy();
            }
        }

        [Test]
        public void View_LeverIsDecorativeAndFollowsTheTimeline()
        {
            var font = LoadFontOrIgnore();
            var visual = GoldPickupPopupVisual.Create(null, font, null);
            try
            {
                Assert.That(visual.Root.GetComponentsInChildren<Button>(true), Is.Empty, "레버는 클릭 버튼이 아니다.");
                Assert.That(visual.Root.GetComponentsInChildren<Selectable>(true), Is.Empty);

                var state = new GoldPickupPopupState(20, 0);
                visual.Tick(0f, state);
                Assert.That(visual.LeverAngleNow, Is.EqualTo(0f).Within(0.01f));

                state.Advance(GoldPickupPopupTimeline.LeverPullEnd);
                visual.Tick(GoldPickupPopupTimeline.LeverPullEnd, state);
                Assert.That(visual.LeverAngleNow, Is.EqualTo(-GoldPickupPopupTimeline.LeverPullAngle).Within(0.5f));

                state.Advance(0.3f);
                visual.Tick(0.3f, state);
                Assert.That(visual.LeverAngleNow, Is.EqualTo(0f).Within(0.01f), "복귀 후 위로 정착한다.");
            }
            finally
            {
                visual.Destroy();
            }
        }

        [Test]
        public void View_RowsStaggerBlurAndRestoreExactly()
        {
            var font = LoadFontOrIgnore();
            var visual = GoldPickupPopupVisual.Create(null, font, null);
            try
            {
                var state = new GoldPickupPopupState(120, 30);

                // 기본 낙하 중(0.22): 기본 줄만 보이고 추가 줄은 아직 시작 전.
                state.Advance(0.22f);
                visual.Tick(0.22f, state);
                Assert.That(visual.MainLineRect.gameObject.activeSelf, Is.True);
                Assert.That(visual.BonusLineRect.gameObject.activeSelf, Is.False, "추가는 약 0.15초 늦게 시작한다.");
                Assert.That(visual.MainGhostActiveCount, Is.EqualTo(GoldPickupPopupTimeline.MainGhostCount));
                Assert.That(visual.MainLineRect.localScale.y, Is.GreaterThan(1f));
                Assert.That(visual.MainLineRect.localScale.x, Is.LessThan(1f));

                // 추가 낙하 중(0.34).
                state.Advance(0.12f);
                visual.Tick(0.12f, state);
                Assert.That(visual.BonusLineRect.gameObject.activeSelf, Is.True);
                Assert.That(visual.BonusGhostActiveCount, Is.EqualTo(GoldPickupPopupTimeline.BonusGhostCount));

                // 정착 후: 잔상 꺼짐, 위치 정확히 0, scale 정확히 1.
                state.Advance(0.4f);
                visual.Tick(0.4f, state);
                Assert.That(visual.MainGhostActiveCount, Is.Zero);
                Assert.That(visual.BonusGhostActiveCount, Is.Zero);
                Assert.That(visual.MainLineRect.anchoredPosition, Is.EqualTo(Vector2.zero));
                Assert.That(visual.MainLineRect.localScale, Is.EqualTo(Vector3.one));
                Assert.That(visual.BonusLineRect.anchoredPosition, Is.EqualTo(Vector2.zero));
                Assert.That(visual.BonusLineRect.localScale, Is.EqualTo(Vector3.one));
                Assert.That(visual.MainFill.text, Is.EqualTo("+120 G"));
                Assert.That(visual.BonusFill.text, Is.EqualTo("추가 골드 +30 G"));
            }
            finally
            {
                visual.Destroy();
            }
        }

        [Test]
        public void View_BonusLineStaysOffWhenThereIsNoBonus()
        {
            var font = LoadFontOrIgnore();
            var visual = GoldPickupPopupVisual.Create(null, font, null);
            try
            {
                var state = new GoldPickupPopupState(40, 0);
                state.Advance(0.6f);
                visual.Tick(0.6f, state);
                Assert.That(visual.BonusMaskActive, Is.False);
                Assert.That(visual.FrameHeightNow, Is.EqualTo(GoldPickupPopupTimeline.BaseHeight));
                Assert.That(visual.MainFill.text, Is.EqualTo("+40 G"));
            }
            finally
            {
                visual.Destroy();
            }
        }

        [Test]
        public void View_GhostsShareFontAndMaterialWithoutInstances()
        {
            var font = LoadFontOrIgnore();
            var visual = GoldPickupPopupVisual.Create(null, font, null);
            try
            {
                Assert.That(visual.MainGhostCount, Is.InRange(2, 3));
                for (var i = 0; i < visual.MainGhostCount; i++)
                {
                    TMP_Text ghost = visual.MainGhost(i);
                    Assert.That(ghost.font, Is.SameAs(visual.MainFill.font));
                    Assert.That(ghost.fontSharedMaterial, Is.SameAs(visual.MainFill.fontSharedMaterial));
                    Assert.That(ghost.raycastTarget, Is.False);
                }

                Assert.That(visual.BonusGhost(0).fontSharedMaterial, Is.SameAs(visual.MainFill.fontSharedMaterial));
            }
            finally
            {
                visual.Destroy();
            }
        }

        [Test]
        public void View_SlotsUseRectMaskAndCoinsLiveOutsideTheMask()
        {
            var font = LoadFontOrIgnore();
            var texture = new Texture2D(8, 8);
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, 8f, 8f), new Vector2(0.5f, 0.5f), 8f);
            var visual = GoldPickupPopupVisual.Create(null, font, sprite);
            try
            {
                Assert.That(visual.Root.GetComponentsInChildren<RectMask2D>(true).Length, Is.EqualTo(2));
                Assert.That(visual.SpawnBurst(1), Is.EqualTo(1));
                Transform coin = visual.Root.transform.Find("CoinLayer/Coin");
                Assert.That(coin, Is.Not.Null);
                Assert.That(coin.GetComponentInParent<RectMask2D>(), Is.Null, "금화는 슬롯 마스크 밖 팝업 레이어에 있다.");
            }
            finally
            {
                visual.Destroy();
                Object.DestroyImmediate(sprite);
                Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void View_CoinBurstsAreCappedAndExpire()
        {
            var font = LoadFontOrIgnore();
            var texture = new Texture2D(8, 8);
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, 8f, 8f), new Vector2(0.5f, 0.5f), 8f);
            var visual = GoldPickupPopupVisual.Create(null, font, sprite);
            try
            {
                var state = new GoldPickupPopupState(5, 0);
                state.Advance(0.3f);
                visual.Tick(0.3f, state);

                Assert.That(visual.SpawnBurst(8), Is.EqualTo(8));
                Assert.That(visual.SpawnBurst(20), Is.EqualTo(16), "상한 24를 넘는 생성은 생략한다.");
                Assert.That(visual.ActiveCoinCount, Is.EqualTo(24));
                Assert.That(visual.SpawnBurst(5), Is.Zero, "기존 금화는 유지하고 초과 생성만 생략한다.");
                Assert.That(visual.ActiveCoinCount, Is.EqualTo(24));

                foreach (Image image in visual.Root.GetComponentsInChildren<Image>(true))
                {
                    Assert.That(image.raycastTarget, Is.False, image.name);
                }

                visual.Tick(0.2f, state);
                Assert.That(visual.ActiveCoinCount, Is.EqualTo(24));
                visual.Tick(0.6f, state);
                Assert.That(visual.ActiveCoinCount, Is.Zero, "금화 수명은 약 0.5초다.");
                Assert.That(visual.SpawnBurst(4), Is.EqualTo(4), "끝난 금화는 재사용된다.");
            }
            finally
            {
                visual.Destroy();
                Object.DestroyImmediate(sprite);
                Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void View_FollowPositionAndExitRiseAreSeparate()
        {
            var font = LoadFontOrIgnore();
            var visual = GoldPickupPopupVisual.Create(null, font, null);
            try
            {
                var anchor = new Vector3(3f, 2f, 0f);
                visual.SetWorldPosition(anchor, 0f);
                Assert.That(visual.Root.transform.position, Is.EqualTo(anchor));
                visual.SetWorldPosition(anchor + Vector3.right, GoldPickupPopupTimeline.ExitRiseWorld(1f));
                Assert.That(
                    visual.Root.transform.position.y,
                    Is.EqualTo(2f + GoldPickupPopupTimeline.ExitRise).Within(0.0001f));
                Assert.That(visual.Root.transform.position.x, Is.EqualTo(4f).Within(0.0001f));
            }
            finally
            {
                visual.Destroy();
            }
        }

        [Test]
        public void View_DestroyRemovesEverything()
        {
            var font = LoadFontOrIgnore();
            var visual = GoldPickupPopupVisual.Create(null, font, null);
            GameObject root = visual.Root;
            visual.Destroy();
            Assert.That(visual.IsAlive, Is.False);
            Assert.That(root == null, Is.True);
        }

        [Test]
        public void View_ExitFadesWholeCanvasGroup()
        {
            var font = LoadFontOrIgnore();
            var visual = GoldPickupPopupVisual.Create(null, font, null);
            try
            {
                var state = new GoldPickupPopupState(5, 0);
                state.Advance(state.ExitStartAge + GoldPickupPopupTimeline.ExitDuration * 0.5f);
                visual.Tick(0f, state);
                Assert.That(visual.CanvasAlpha, Is.EqualTo(0.5f).Within(0.02f));
            }
            finally
            {
                visual.Destroy();
            }
        }

        private static TMP_FontAsset LoadFontOrIgnore()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PromptB1001GoldPickupBuilder.FontSdfPath);
            Assume.That(font, Is.Not.Null);
            return font;
        }
    }
}
