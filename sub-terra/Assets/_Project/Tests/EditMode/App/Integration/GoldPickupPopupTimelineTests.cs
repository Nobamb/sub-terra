using NUnit.Framework;
using SubTerra.App.Integration;
using SubTerra.App.UI.FacilityNameTag;
using UnityEngine;

namespace SubTerra.App.Tests.Integration
{
    public sealed class GoldPickupPopupTimelineTests
    {
        private const float Tol = 0.001f;

        [Test]
        public void CoinsForAmount_FollowsLogRuleWithBounds()
        {
            Assert.That(GoldPickupPopupTimeline.CoinsForAmount(-5), Is.EqualTo(3));
            Assert.That(GoldPickupPopupTimeline.CoinsForAmount(0), Is.EqualTo(3));
            Assert.That(GoldPickupPopupTimeline.CoinsForAmount(1), Is.EqualTo(3));
            Assert.That(GoldPickupPopupTimeline.CoinsForAmount(9), Is.EqualTo(3));
            Assert.That(GoldPickupPopupTimeline.CoinsForAmount(10), Is.EqualTo(5));
            Assert.That(GoldPickupPopupTimeline.CoinsForAmount(99), Is.EqualTo(5));
            Assert.That(GoldPickupPopupTimeline.CoinsForAmount(100), Is.EqualTo(7));
            Assert.That(GoldPickupPopupTimeline.CoinsForAmount(999), Is.EqualTo(7));
            Assert.That(GoldPickupPopupTimeline.CoinsForAmount(1000), Is.EqualTo(8));
            Assert.That(GoldPickupPopupTimeline.CoinsForAmount(int.MaxValue), Is.EqualTo(8));
        }

        [Test]
        public void LandAndMergeCoins_UseTheirOwnOffsetsAndClamps()
        {
            Assert.That(GoldPickupPopupTimeline.BaseLandCoins(20), Is.EqualTo(5));
            Assert.That(GoldPickupPopupTimeline.BonusLandCoins(1), Is.EqualTo(5));
            Assert.That(GoldPickupPopupTimeline.BonusLandCoins(100), Is.EqualTo(9));
            Assert.That(GoldPickupPopupTimeline.BonusLandCoins(100000), Is.EqualTo(10));

            Assert.That(GoldPickupPopupTimeline.MergeCoins(0), Is.EqualTo(0));
            Assert.That(GoldPickupPopupTimeline.MergeCoins(-3), Is.EqualTo(0));
            Assert.That(GoldPickupPopupTimeline.MergeCoins(1), Is.EqualTo(2));
            Assert.That(GoldPickupPopupTimeline.MergeCoins(10), Is.EqualTo(4));
            Assert.That(GoldPickupPopupTimeline.MergeCoins(100), Is.EqualTo(5));
            Assert.That(GoldPickupPopupTimeline.MergeCoins(long.MaxValue), Is.EqualTo(5));
        }

        [Test]
        public void CoinLaunch_IsDeterministicFanFromTopEdge()
        {
            GoldPickupPopupTimeline.CoinLaunch(2, 6, 1, out float vx1, out float vy1, out float d1);
            GoldPickupPopupTimeline.CoinLaunch(2, 6, 1, out float vx2, out float vy2, out float d2);
            Assert.That((vx1, vy1, d1), Is.EqualTo((vx2, vy2, d2)));

            GoldPickupPopupTimeline.CoinLaunch(0, 6, 0, out float firstVx, out float firstVy, out float firstDelay);
            GoldPickupPopupTimeline.CoinLaunch(5, 6, 0, out float lastVx, out float lastVy, out float lastDelay);
            Assert.That(firstVx, Is.LessThan(0f), "왼쪽에서 오른쪽으로 부채꼴");
            Assert.That(lastVx, Is.GreaterThan(0f));
            Assert.That(firstVy, Is.GreaterThan(0f));
            Assert.That(lastVy, Is.GreaterThan(0f));
            Assert.That(lastDelay, Is.GreaterThan(firstDelay));
        }

        [Test]
        public void CoinTrajectory_RisesFallsAndFadesWithinLifetime()
        {
            var origin = new Vector2(-13f, 40f);
            GoldPickupPopupTimeline.CoinLaunch(2, 5, 0, out float vx, out float vy, out _);
            Assert.That(GoldPickupPopupTimeline.CoinPosition(origin, vx, vy, 0f), Is.EqualTo(origin));
            Assert.That(GoldPickupPopupTimeline.CoinPosition(origin, vx, vy, 0.2f).y, Is.GreaterThan(origin.y));
            Assert.That(
                GoldPickupPopupTimeline.CoinPosition(origin, vx, vy, GoldPickupPopupTimeline.CoinLifetime).y,
                Is.LessThan(GoldPickupPopupTimeline.CoinPosition(origin, vx, vy, 0.25f).y),
                "정점 뒤에는 중력으로 떨어진다.");

            Assert.That(GoldPickupPopupTimeline.CoinLifetime, Is.InRange(0.5f, 0.6f));
            Assert.That(GoldPickupPopupTimeline.CoinAlpha(0.2f), Is.EqualTo(1f));
            Assert.That(GoldPickupPopupTimeline.CoinAlpha(0.45f), Is.GreaterThan(0f).And.LessThan(1f));
            Assert.That(GoldPickupPopupTimeline.CoinAlpha(GoldPickupPopupTimeline.CoinLifetime), Is.EqualTo(0f));
            Assert.That(GoldPickupPopupTimeline.MaxLiveCoins, Is.EqualTo(24));
        }

        [Test]
        public void CoinPopScale_StartsSmallOvershootsThenSettlesAtOne()
        {
            Assert.That(GoldPickupPopupTimeline.CoinPopScale(0f), Is.EqualTo(0.3f).Within(Tol));
            Assert.That(GoldPickupPopupTimeline.CoinPopScale(GoldPickupPopupTimeline.CoinPopSeconds * 0.6f), Is.GreaterThan(1f));
            Assert.That(GoldPickupPopupTimeline.CoinPopScale(GoldPickupPopupTimeline.CoinPopSeconds), Is.EqualTo(1f).Within(Tol));
            Assert.That(GoldPickupPopupTimeline.CoinPopScale(0.4f), Is.EqualTo(1f).Within(Tol));
        }

        [Test]
        public void Timing_AppearThenLeverThenBaseFallThenBonusFall()
        {
            Assert.That(GoldPickupPopupTimeline.AppearSeconds, Is.GreaterThan(0.25f), "눈에 보이게 천천히 뜬다.");
            Assert.That(
                GoldPickupPopupTimeline.LeverDownStart,
                Is.GreaterThanOrEqualTo(GoldPickupPopupTimeline.AppearSeconds), "팝업이 다 뜬 뒤에 레버를 당긴다.");
            Assert.That(GoldPickupPopupTimeline.BaseFallStart, Is.EqualTo(GoldPickupPopupTimeline.LeverDownEnd).Within(Tol), "복귀가 시작될 때 기본 낙하");
            Assert.That(GoldPickupPopupTimeline.BonusFallStart, Is.GreaterThan(GoldPickupPopupTimeline.BaseFallStart));
            Assert.That(
                GoldPickupPopupTimeline.BonusFallStart,
                Is.LessThan(GoldPickupPopupTimeline.LandTime(GoldPickupPopupTimeline.BaseFallStart)),
                "추가는 기본이 내려오는 도중 이어서 시작한다.");
            Assert.That(
                GoldPickupPopupTimeline.LandTime(GoldPickupPopupTimeline.BonusFallStart),
                Is.GreaterThan(GoldPickupPopupTimeline.LandTime(GoldPickupPopupTimeline.BaseFallStart)), "기본 → 추가 순서로 착지");
            Assert.That(GoldPickupPopupTimeline.LandTime(GoldPickupPopupTimeline.BaseFallStart), Is.EqualTo(0.7f).Within(Tol));
            Assert.That(GoldPickupPopupTimeline.LandTime(GoldPickupPopupTimeline.BonusFallStart), Is.EqualTo(0.86f).Within(Tol));
            Assert.That(
                GoldPickupPopupTimeline.ExitFloor(false),
                Is.GreaterThan(GoldPickupPopupTimeline.SettledTime(GoldPickupPopupTimeline.BaseFallStart)));
            Assert.That(
                GoldPickupPopupTimeline.ExitFloor(true),
                Is.GreaterThan(GoldPickupPopupTimeline.SettledTime(GoldPickupPopupTimeline.BonusFallStart)));
            Assert.That(GoldPickupPopupTimeline.ExitFloor(false) + GoldPickupPopupTimeline.ExitSeconds, Is.EqualTo(1.65f).Within(Tol));
            Assert.That(GoldPickupPopupTimeline.ExitFloor(true) + GoldPickupPopupTimeline.ExitSeconds, Is.EqualTo(1.8f).Within(Tol));
        }

        [Test]
        public void Lever_StaysUpUntilFrameIsUpThenPullsDownAndReturnsWithSingleSmallSettle()
        {
            Assert.That(GoldPickupPopupTimeline.LeverAngle(0f), Is.EqualTo(0f));
            Assert.That(GoldPickupPopupTimeline.LeverAngle(GoldPickupPopupTimeline.AppearSeconds), Is.EqualTo(0f), "등장 동안 레버는 올라가 있다.");
            Assert.That(GoldPickupPopupTimeline.LeverAngle(GoldPickupPopupTimeline.LeverDownStart), Is.EqualTo(0f));

            float previous = 0f;
            for (var step = 1; step <= 20; step++)
            {
                float t = Mathf.Lerp(GoldPickupPopupTimeline.LeverDownStart, GoldPickupPopupTimeline.LeverDownEnd, step / 20f);
                float angle = GoldPickupPopupTimeline.LeverAngle(t);
                Assert.That(angle, Is.GreaterThanOrEqualTo(previous), "하강 동안 각도는 줄지 않는다.");
                previous = angle;
            }

            Assert.That(previous, Is.EqualTo(GoldPickupPopupTimeline.LeverPullDegrees).Within(0.5f));

            float min = 0f;
            int signChanges = 0;
            bool wasPositive = true;
            for (var step = 1; step < 100; step++)
            {
                float t = Mathf.Lerp(GoldPickupPopupTimeline.LeverDownEnd, GoldPickupPopupTimeline.LeverReturnEnd, step / 100f);
                float angle = GoldPickupPopupTimeline.LeverAngle(t);
                min = Mathf.Min(min, angle);
                bool positive = angle >= 0f;
                if (positive != wasPositive)
                {
                    signChanges++;
                    wasPositive = positive;
                }
            }

            Assert.That(signChanges, Is.LessThanOrEqualTo(1), "반복 바운스 없음");
            Assert.That(min, Is.GreaterThan(-8f), "복귀 탄성 정착은 몇 도 이내");
            Assert.That(GoldPickupPopupTimeline.LeverAngle(GoldPickupPopupTimeline.LeverReturnEnd), Is.EqualTo(0f));
            Assert.That(GoldPickupPopupTimeline.LeverAngle(3f), Is.EqualTo(0f));
        }

        [Test]
        public void LeverPerspective_ShortensRodAndGrowsBallAsItComesTowardTheViewer()
        {
            Assert.That(GoldPickupPopupTimeline.LeverRodScale(0f), Is.EqualTo(1f).Within(Tol));
            Assert.That(GoldPickupPopupTimeline.LeverRodScale(90f), Is.EqualTo(0f).Within(Tol), "정면을 향하면 막대는 점이 된다.");
            Assert.That(GoldPickupPopupTimeline.LeverRodScale(GoldPickupPopupTimeline.LeverPullDegrees), Is.LessThan(0f), "끝까지 당기면 아래로 뒤집힌다.");
            Assert.That(GoldPickupPopupTimeline.LeverBallScale(0f), Is.EqualTo(1f).Within(Tol));
            Assert.That(GoldPickupPopupTimeline.LeverBallScale(90f), Is.GreaterThan(1.3f), "가까워지며 커진다.");
            Assert.That(
                GoldPickupPopupTimeline.LeverBallScale(GoldPickupPopupTimeline.LeverPullDegrees),
                Is.GreaterThan(GoldPickupPopupTimeline.LeverBallScale(0f)));
        }

        [Test]
        public void Frame_RisesFromBelowAndReusesNameTagTimelineWithoutChangingIt()
        {
            Assert.That(GoldPickupPopupTimeline.FrameLevel(0f, 0f), Is.EqualTo(0f));
            Assert.That(GoldPickupPopupTimeline.Frame(0f).FrameAlpha, Is.EqualTo(0f));
            FacilityNameTagFrame done = GoldPickupPopupTimeline.Frame(GoldPickupPopupTimeline.FrameLevel(GoldPickupPopupTimeline.AppearSeconds, 0f));
            FacilityNameTagFrame expected = FacilityNameTagTimeline.Evaluate(1f, true);
            Assert.That(done.Spread, Is.EqualTo(1f));
            Assert.That(done.FrameAlpha, Is.EqualTo(expected.FrameAlpha));
            Assert.That(
                GoldPickupPopupTimeline.Frame(GoldPickupPopupTimeline.FrameLevel(GoldPickupPopupTimeline.AppearSeconds * 0.1f, 0f)).Spread,
                Is.EqualTo(0f), "처음엔 작은 모서리 빛만");
            Assert.That(
                GoldPickupPopupTimeline.Frame(GoldPickupPopupTimeline.FrameLevel(GoldPickupPopupTimeline.AppearSeconds * 0.5f, 0f)).Spread,
                Is.GreaterThan(0f).And.LessThan(1f));

            Assert.That(GoldPickupPopupTimeline.Slide(0f), Is.EqualTo(GoldPickupPopupTimeline.SlideRisePx), "아래에서 시작");
            Assert.That(GoldPickupPopupTimeline.Slide(1f), Is.EqualTo(0f).Within(Tol), "최종 위치");
            float previous = GoldPickupPopupTimeline.SlideRisePx + 1f;
            for (var step = 0; step <= 10; step++)
            {
                float slide = GoldPickupPopupTimeline.Slide(step / 10f);
                Assert.That(slide, Is.LessThanOrEqualTo(previous), "등장 동안 계속 위로 올라간다.");
                previous = slide;
            }

            Assert.That(GoldPickupPopupTimeline.LeverAlpha(0f), Is.EqualTo(0f));
            Assert.That(GoldPickupPopupTimeline.LeverAlpha(1f), Is.EqualTo(1f).Within(Tol));
        }

        [Test]
        public void HologramBoot_FlickersOnlyWhileTurningOnAndScansBottomToTop()
        {
            Assert.That(GoldPickupPopupTimeline.Flicker(0f), Is.EqualTo(1f));
            bool dimmed = false;
            for (var step = 0; step < 70; step++)
            {
                float value = GoldPickupPopupTimeline.Flicker(step * 0.003f);
                Assert.That(value, Is.InRange(0.5f, 1f));
                dimmed |= value < 1f;
            }

            Assert.That(dimmed, Is.True, "켜질 때 잠깐 깜박인다.");
            Assert.That(GoldPickupPopupTimeline.Flicker(GoldPickupPopupTimeline.AppearSeconds), Is.EqualTo(1f));
            Assert.That(GoldPickupPopupTimeline.Flicker(5f), Is.EqualTo(1f));

            Assert.That(GoldPickupPopupTimeline.BootScanAlpha(0f), Is.EqualTo(0f));
            Assert.That(GoldPickupPopupTimeline.BootScanAlpha(1f), Is.EqualTo(0f));
            Assert.That(GoldPickupPopupTimeline.BootScanAlpha(0.55f), Is.GreaterThan(0.5f));
            Assert.That(
                GoldPickupPopupTimeline.BootScanPosition(0.8f),
                Is.GreaterThan(GoldPickupPopupTimeline.BootScanPosition(0.4f)), "아래에서 위로 훑는다.");
        }

        [Test]
        public void Height_ExpandsOnceWhenBonusRowOpens()
        {
            Assert.That(GoldPickupPopupTimeline.Height(0f), Is.EqualTo(44f));
            Assert.That(GoldPickupPopupTimeline.Height(1f), Is.EqualTo(72f));
            Assert.That(GoldPickupPopupTimeline.ExpandProgress(5f, false, false, 0f), Is.EqualTo(0f));
            Assert.That(GoldPickupPopupTimeline.ExpandProgress(0f, true, true, 0f), Is.EqualTo(1f));

            Assert.That(GoldPickupPopupTimeline.ExpandProgress(1f, true, false, 1f), Is.EqualTo(0f));
            float mid = GoldPickupPopupTimeline.ExpandProgress(1.05f, true, false, 1f);
            Assert.That(mid, Is.GreaterThan(0.5f).And.LessThan(1f), "ease-out이라 절반 시점에 절반보다 많이 진행");
            Assert.That(GoldPickupPopupTimeline.ExpandProgress(1.1f, true, false, 1f), Is.EqualTo(1f).Within(Tol));
        }

        [Test]
        public void Line_FallsEaseInThenOvershootsOnceAndSettlesExactly()
        {
            const float start = 40f;
            float fs = GoldPickupPopupTimeline.BaseFallStart;
            float over = GoldPickupPopupTimeline.MainOvershootPx;
            GoldPickupLineMotion before = GoldPickupPopupTimeline.Line(fs - 0.05f, fs, start, over);
            Assert.That(before.OffsetY, Is.EqualTo(start));
            Assert.That(before.Blur, Is.EqualTo(0f));

            float previous = start + 1f;
            for (var step = 1; step <= 10; step++)
            {
                float t = fs + GoldPickupPopupTimeline.FallSeconds * step / 10f * 0.999f;
                GoldPickupLineMotion motion = GoldPickupPopupTimeline.Line(t, fs, start, over);
                Assert.That(motion.OffsetY, Is.LessThan(previous), "낙하 중 계속 내려온다.");
                Assert.That(motion.GhostDir, Is.EqualTo(1f), "낙하 잔상은 위쪽 꼬리");
                previous = motion.OffsetY;
            }

            GoldPickupLineMotion mid = GoldPickupPopupTimeline.Line(fs + GoldPickupPopupTimeline.FallSeconds * 0.5f, fs, start, over);
            Assert.That(mid.OffsetY, Is.EqualTo(start * 0.75f).Within(Tol), "ease-in: 절반 시간에 25%만 이동");
            Assert.That(mid.ScaleY, Is.GreaterThan(1.15f));
            Assert.That(mid.ScaleX, Is.LessThan(0.97f));

            float land = GoldPickupPopupTimeline.LandTime(fs);
            GoldPickupLineMotion dip = GoldPickupPopupTimeline.Line(land + GoldPickupPopupTimeline.SettleSeconds * 0.5f, fs, start, over);
            Assert.That(dip.OffsetY, Is.EqualTo(-over).Within(Tol), "착지 후 overshoot 한 번");

            GoldPickupLineMotion blurGone = GoldPickupPopupTimeline.Line(land + GoldPickupPopupTimeline.BlurClearSeconds, fs, start, over);
            Assert.That(blurGone.Blur, Is.EqualTo(0f).Within(Tol), "착지 직후 블러 제거");
            Assert.That(blurGone.ScaleY, Is.EqualTo(1f).Within(Tol));

            GoldPickupLineMotion settled = GoldPickupPopupTimeline.Line(land + GoldPickupPopupTimeline.SettleSeconds + 0.001f, fs, start, over);
            Assert.That(settled.OffsetY, Is.EqualTo(0f));
            Assert.That(settled.ScaleX, Is.EqualTo(1f));
            Assert.That(settled.ScaleY, Is.EqualTo(1f));
            Assert.That(settled.Blur, Is.EqualTo(0f));
            Assert.That(settled.Speed, Is.EqualTo(0f));
        }

        [Test]
        public void LineRise_IsTheTimeReverseOfTheFallWithTailsBelow()
        {
            const float start = 40f;
            GoldPickupLineMotion at0 = GoldPickupPopupTimeline.LineRise(0f, start);
            Assert.That(at0.OffsetY, Is.EqualTo(0f).Within(Tol), "퇴장 시작은 정착 위치에서");
            Assert.That(at0.ScaleX, Is.EqualTo(1f).Within(Tol));
            Assert.That(at0.ScaleY, Is.EqualTo(1f).Within(Tol));
            Assert.That(at0.Blur, Is.EqualTo(0f).Within(Tol), "끊김 없이 블러가 서서히 붙는다.");

            GoldPickupLineMotion end = GoldPickupPopupTimeline.LineRise(1f, start);
            Assert.That(end.OffsetY, Is.EqualTo(start).Within(Tol), "슬롯 위쪽 바깥으로 나간다.");
            Assert.That(end.GhostDir, Is.EqualTo(-1f), "올라갈 때 꼬리는 아래쪽");

            float previous = -1f;
            for (var step = 0; step <= 10; step++)
            {
                float offset = GoldPickupPopupTimeline.LineRise(step / 10f, start).OffsetY;
                Assert.That(offset, Is.GreaterThanOrEqualTo(previous), "계속 올라간다.");
                previous = offset;
            }

            GoldPickupLineMotion mid = GoldPickupPopupTimeline.LineRise(0.5f, start);
            Assert.That(mid.Blur, Is.GreaterThan(0.3f));
            Assert.That(mid.ScaleY, Is.GreaterThan(1f));
        }

        [Test]
        public void ExitOrder_BonusTextLeavesFirstThenBaseThenFrameFolds()
        {
            Assert.That(GoldPickupPopupTimeline.TextExit(0f, true), Is.EqualTo(0f));
            Assert.That(GoldPickupPopupTimeline.TextExit(0f, false), Is.EqualTo(0f));
            Assert.That(GoldPickupPopupTimeline.TextExit(0.15f, true), Is.GreaterThan(GoldPickupPopupTimeline.TextExit(0.15f, false)), "추가 줄이 먼저");
            Assert.That(GoldPickupPopupTimeline.TextExit(1f, true), Is.EqualTo(1f));
            Assert.That(GoldPickupPopupTimeline.TextExit(1f, false), Is.EqualTo(1f));

            float baseDone = 0f;
            for (var step = 0; step <= 100; step++)
            {
                float progress = step / 100f;
                if (GoldPickupPopupTimeline.TextExit(progress, false) >= 1f)
                {
                    baseDone = progress;
                    break;
                }
            }

            Assert.That(GoldPickupPopupTimeline.ExitFold(baseDone), Is.LessThanOrEqualTo(0.1f), "글자가 거의 빠진 뒤에 프레임이 접힌다.");
            Assert.That(GoldPickupPopupTimeline.ExitFold(0.3f), Is.EqualTo(0f));
            Assert.That(GoldPickupPopupTimeline.ExitFold(1f), Is.EqualTo(1f).Within(Tol));

            Assert.That(GoldPickupPopupTimeline.FrameLevel(5f, 0f), Is.EqualTo(1f));
            Assert.That(GoldPickupPopupTimeline.FrameLevel(5f, 1f), Is.EqualTo(0f), "퇴장 끝에는 등장 시작 상태");
            Assert.That(GoldPickupPopupTimeline.Slide(GoldPickupPopupTimeline.FrameLevel(5f, 1f)), Is.EqualTo(GoldPickupPopupTimeline.SlideRisePx), "아래로 가라앉으며 사라진다.");
        }

        [Test]
        public void Kick_PushesDownBrieflyAndReturnsToZero()
        {
            Assert.That(GoldPickupPopupTimeline.Kick(-0.01f, 3f), Is.EqualTo(0f));
            Assert.That(GoldPickupPopupTimeline.Kick(GoldPickupPopupTimeline.KickSeconds * 0.5f, 3f), Is.EqualTo(3f).Within(Tol));
            Assert.That(GoldPickupPopupTimeline.Kick(GoldPickupPopupTimeline.KickSeconds, 3f), Is.EqualTo(0f));
        }

        [Test]
        public void Ghosts_GrowWithSpeedAndFadeStepwise()
        {
            Assert.That(GoldPickupPopupTimeline.GhostOffset(0f, 0, 10f), Is.EqualTo(0f));
            Assert.That(GoldPickupPopupTimeline.GhostAlpha(0f, 0), Is.EqualTo(0f));
            Assert.That(GoldPickupPopupTimeline.GhostOffset(1f, 0, 10f), Is.GreaterThan(GoldPickupPopupTimeline.GhostOffset(0.5f, 0, 10f)));
            Assert.That(GoldPickupPopupTimeline.GhostOffset(1f, 1, 10f), Is.GreaterThan(GoldPickupPopupTimeline.GhostOffset(1f, 0, 10f)));
            Assert.That(GoldPickupPopupTimeline.GhostAlpha(1f, 1), Is.LessThan(GoldPickupPopupTimeline.GhostAlpha(1f, 0)));
            Assert.That(GoldPickupPopupTimeline.GhostAlpha(1f, 2), Is.LessThan(GoldPickupPopupTimeline.GhostAlpha(1f, 1)));
            Assert.That(GoldPickupPopupTimeline.GhostAlpha(1f, 3), Is.LessThan(GoldPickupPopupTimeline.GhostAlpha(1f, 2)));
            Assert.That(GoldPickupPopupTimeline.GhostAlpha(1f, 0), Is.GreaterThan(GoldPickupPopupTimeline.GhostAlpha(0.5f, 0)));
            Assert.That(
                GoldPickupPopupTimeline.BonusGhostGap, Is.LessThan(GoldPickupPopupTimeline.MainGhostGap),
                "작은 글자라 잔상 간격이 더 좁다.");
            Assert.That(GoldPickupPopupTimeline.MainGhostCount, Is.InRange(3, 4));
            Assert.That(GoldPickupPopupTimeline.BonusGhostCount, Is.InRange(2, 4));
        }

        [Test]
        public void RollValue_IsMonotonicIntegerAndNeverExceedsTarget()
        {
            Assert.That(GoldPickupPopupTimeline.RollValue(100, 160, 0f), Is.EqualTo(100));
            Assert.That(GoldPickupPopupTimeline.RollValue(100, 160, GoldPickupPopupTimeline.RollupSeconds), Is.EqualTo(160));
            Assert.That(GoldPickupPopupTimeline.RollValue(100, 160, 5f), Is.EqualTo(160));

            int previous = 100;
            for (var step = 0; step <= 40; step++)
            {
                int value = GoldPickupPopupTimeline.RollValue(100, 160, step * 0.01f);
                Assert.That(value, Is.GreaterThanOrEqualTo(previous));
                Assert.That(value, Is.LessThanOrEqualTo(160));
                previous = value;
            }

            int big = GoldPickupPopupTimeline.RollValue(0, int.MaxValue, 0.1f);
            Assert.That(big, Is.GreaterThan(0).And.LessThanOrEqualTo(int.MaxValue));
            Assert.That(GoldPickupPopupTimeline.RollValue(int.MaxValue - 1, int.MaxValue, 0.1f), Is.LessThanOrEqualTo(int.MaxValue));
        }

        [Test]
        public void Sweep_And_EdgeFlash_AreShortOneShots()
        {
            Assert.That(GoldPickupPopupTimeline.SweepProgress(0f), Is.EqualTo(0f));
            Assert.That(GoldPickupPopupTimeline.SweepProgress(GoldPickupPopupTimeline.SweepSeconds * 0.5f), Is.EqualTo(0.5f).Within(Tol));
            Assert.That(GoldPickupPopupTimeline.SweepProgress(1f), Is.EqualTo(1f));
            Assert.That(GoldPickupPopupTimeline.EdgeFlash(-0.01f), Is.EqualTo(0f));
            Assert.That(GoldPickupPopupTimeline.EdgeFlash(GoldPickupPopupTimeline.EdgeFlashSeconds * 0.5f), Is.GreaterThan(0.9f));
            Assert.That(GoldPickupPopupTimeline.EdgeFlash(GoldPickupPopupTimeline.EdgeFlashSeconds + 0.01f), Is.EqualTo(0f));
        }
    }
}
