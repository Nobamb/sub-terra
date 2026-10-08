using NUnit.Framework;
using SubTerra.App.Integration;

namespace SubTerra.App.Tests.Integration
{
    public sealed class GoldPickupPopupStateTests
    {
        private const float Tol = 0.001f;

        [Test]
        public void Create_KeepsConfirmedBaseAndBonusSeparate()
        {
            var state = new GoldPickupPopupState(120, 30);
            Assert.That((state.TotalBase, state.TotalBonus), Is.EqualTo((120, 30)));
            Assert.That((state.DisplayBase, state.DisplayBonus), Is.EqualTo((120, 30)));
            Assert.That(state.HasBonusRow, Is.True);
            Assert.That(state.BonusFromStart, Is.True);
            Assert.That(state.ExpandProgress, Is.EqualTo(1f), "처음부터 추가가 있으면 높이 확장 없이 시작");

            var baseOnly = new GoldPickupPopupState(120, 0);
            Assert.That(baseOnly.HasBonusRow, Is.False);
            Assert.That(baseOnly.ExpandProgress, Is.EqualTo(0f));

            var clamped = new GoldPickupPopupState(-5, -9);
            Assert.That((clamped.TotalBase, clamped.TotalBonus), Is.EqualTo((0, 0)));
        }

        [Test]
        public void Advance_RaisesEachLandingOnceAtItsTime()
        {
            var state = new GoldPickupPopupState(120, 30);
            Assert.That(state.Advance(0.69f), Is.EqualTo(GoldPickupPopupEvents.None));
            Assert.That(state.Advance(0.02f), Is.EqualTo(GoldPickupPopupEvents.BaseLanded), "0.70초 기본 착지");
            Assert.That(state.Advance(0.10f), Is.EqualTo(GoldPickupPopupEvents.None));
            Assert.That(state.Advance(0.10f), Is.EqualTo(GoldPickupPopupEvents.BonusLanded), "0.86초 추가 착지(기본 뒤)");
            Assert.That(state.Advance(0.10f), Is.EqualTo(GoldPickupPopupEvents.None));

            var baseOnly = new GoldPickupPopupState(120, 0);
            GoldPickupPopupEvents all = baseOnly.Advance(1.0f);
            Assert.That(all, Is.EqualTo(GoldPickupPopupEvents.BaseLanded), "추가가 없으면 추가 착지·두 번째 분출 없음");
        }

        [Test]
        public void Exit_StartsAfterSettleHoldWithoutBonusAndLaterWithBonus()
        {
            var plain = new GoldPickupPopupState(100, 0);
            Assert.That(plain.ExitStartTime, Is.EqualTo(GoldPickupPopupTimeline.BaseOnlyExitStart).Within(Tol));
            plain.Advance(GoldPickupPopupTimeline.BaseOnlyExitStart - 0.01f);
            Assert.That(plain.IsExiting, Is.False);
            Assert.That(plain.ExitProgress, Is.EqualTo(0f));
            Assert.That(plain.Advance(0.02f) & GoldPickupPopupEvents.ExitStarted, Is.EqualTo(GoldPickupPopupEvents.ExitStarted));
            Assert.That(plain.IsExiting, Is.True);
            plain.Advance(GoldPickupPopupTimeline.ExitSeconds * 0.5f);
            Assert.That(plain.ExitProgress, Is.GreaterThan(0.4f).And.LessThan(0.7f));
            Assert.That(plain.IsFinished, Is.False, "퇴장은 등장의 역순이라 충분히 길다.");
            Assert.That(plain.Advance(GoldPickupPopupTimeline.ExitSeconds * 0.6f) & GoldPickupPopupEvents.Finished, Is.EqualTo(GoldPickupPopupEvents.Finished));
            Assert.That(plain.ExitProgress, Is.EqualTo(1f));

            var bonus = new GoldPickupPopupState(100, 20);
            Assert.That(bonus.ExitStartTime, Is.EqualTo(GoldPickupPopupTimeline.BonusExitStart).Within(Tol));
            bonus.Advance(GoldPickupPopupTimeline.BonusExitStart + GoldPickupPopupTimeline.ExitSeconds - 0.02f);
            Assert.That(bonus.IsFinished, Is.False, "총 약 1.8초");
            bonus.Advance(0.05f);
            Assert.That(bonus.IsFinished, Is.True);
        }

        [Test]
        public void Advance_WithHugeStepStillRunsEveryEventOnce()
        {
            var state = new GoldPickupPopupState(100, 20);
            GoldPickupPopupEvents events = state.Advance(10f);
            Assert.That(events, Is.EqualTo(
                GoldPickupPopupEvents.BaseLanded
                | GoldPickupPopupEvents.BonusLanded
                | GoldPickupPopupEvents.ExitStarted
                | GoldPickupPopupEvents.Finished));
            Assert.That(state.Advance(1f), Is.EqualTo(GoldPickupPopupEvents.None));
        }

        [Test]
        public void Merge_SumsConfirmedValuesAndRollsDisplayMonotonically()
        {
            var state = new GoldPickupPopupState(120, 0);
            state.Advance(0.3f);
            GoldPickupMergeResult result = state.Merge(30, 0);
            Assert.That(result.Changed, Is.True);
            Assert.That((result.EffectiveBase, result.EffectiveBonus), Is.EqualTo((30, 0)));
            Assert.That(result.BonusRowOpened, Is.False);
            Assert.That(state.TotalBase, Is.EqualTo(150));
            Assert.That(state.DisplayBase, Is.EqualTo(120), "표시값은 롤업으로 올라간다.");

            int previous = state.DisplayBase;
            for (var step = 0; step < 30; step++)
            {
                state.Advance(0.01f);
                Assert.That(state.DisplayBase, Is.GreaterThanOrEqualTo(previous));
                Assert.That(state.DisplayBase, Is.LessThanOrEqualTo(150));
                previous = state.DisplayBase;
            }

            Assert.That(state.DisplayBase, Is.EqualTo(150), "약 0.22초 뒤 정확히 목표");
        }

        [Test]
        public void Merge_ChainedMergesNeverDropOrOvershoot()
        {
            var state = new GoldPickupPopupState(100, 10);
            state.Advance(0.3f);
            int previous = state.DisplayBase;
            int expected = 100;
            for (var merge = 0; merge < 6; merge++)
            {
                state.Merge(15, 5);
                expected += 15;
                for (var step = 0; step < 5; step++)
                {
                    state.Advance(0.02f);
                    Assert.That(state.DisplayBase, Is.GreaterThanOrEqualTo(previous));
                    Assert.That(state.DisplayBase, Is.LessThanOrEqualTo(expected));
                    Assert.That(state.DisplayBonus, Is.LessThanOrEqualTo(state.TotalBonus));
                    previous = state.DisplayBase;
                }
            }

            state.Advance(0.5f);
            Assert.That((state.DisplayBase, state.DisplayBonus), Is.EqualTo((190, 40)));
            Assert.That(state.MergeCount, Is.EqualTo(6));
        }

        [Test]
        public void Merge_IgnoresZeroNegativeAndSaturatedAdditions()
        {
            var state = new GoldPickupPopupState(int.MaxValue - 5, 0);
            Assert.That(state.Merge(0, 0).Changed, Is.False);
            Assert.That(state.Merge(-4, -1).Changed, Is.False);
            Assert.That(state.LastMergeAt, Is.EqualTo(-1f), "변화 없는 합치기는 퇴장을 미루지 않는다.");

            GoldPickupMergeResult result = state.Merge(10, 0);
            Assert.That(result.EffectiveBase, Is.EqualTo(5), "합산 오버플로는 포화시킨다.");
            Assert.That(state.TotalBase, Is.EqualTo(int.MaxValue));
            Assert.That(state.Merge(10, 0).Changed, Is.False);
            Assert.That(result.EffectiveTotal, Is.EqualTo(5L));
        }

        [Test]
        public void Merge_OpensBonusRowOnlyOnceAndExpandsHeight()
        {
            var state = new GoldPickupPopupState(100, 0);
            state.Advance(0.5f);
            GoldPickupMergeResult first = state.Merge(0, 20);
            Assert.That(first.BonusRowOpened, Is.True);
            Assert.That(state.HasBonusRow, Is.True);
            Assert.That(state.BonusFromStart, Is.False);
            Assert.That(state.DisplayBonus, Is.EqualTo(20), "추가 줄은 목표값으로 떨어진다.");
            Assert.That(state.BonusFallStart, Is.EqualTo(0.5f + 0.05f).Within(Tol));
            Assert.That(state.ExpandProgress, Is.EqualTo(0f).Within(Tol));
            state.Advance(0.05f);
            Assert.That(state.ExpandProgress, Is.GreaterThan(0f).And.LessThan(1f));
            Assert.That(
                state.Advance(0.21f) & GoldPickupPopupEvents.BonusLanded,
                Is.EqualTo(GoldPickupPopupEvents.BonusLanded));
            Assert.That(state.ExpandProgress, Is.EqualTo(1f).Within(Tol));

            GoldPickupMergeResult second = state.Merge(0, 10);
            Assert.That(second.BonusRowOpened, Is.False, "이미 추가 줄이 있으면 롤업만 한다.");
            Assert.That(state.DisplayBonus, Is.EqualTo(20));
            state.Advance(0.3f);
            Assert.That(state.DisplayBonus, Is.EqualTo(30));
            Assert.That(state.Advance(0.01f) & GoldPickupPopupEvents.BonusLanded, Is.EqualTo(GoldPickupPopupEvents.None));
        }

        [Test]
        public void Merge_DefersExitByHoldAndSettle()
        {
            var state = new GoldPickupPopupState(100, 0);
            state.Advance(1.0f);
            state.Merge(10, 0);
            Assert.That(state.ExitStartTime, Is.EqualTo(1.0f + 0.45f).Within(Tol), "마지막 합치기 후 최소 0.45초");
            Assert.That(state.Advance(0.4f) & GoldPickupPopupEvents.ExitStarted, Is.EqualTo(GoldPickupPopupEvents.None));
            Assert.That(state.Advance(0.1f) & GoldPickupPopupEvents.ExitStarted, Is.EqualTo(GoldPickupPopupEvents.ExitStarted));

            var open = new GoldPickupPopupState(100, 0);
            open.Merge(0, 20);
            Assert.That(
                open.ExitStartTime,
                Is.GreaterThanOrEqualTo(GoldPickupPopupTimeline.SettledTime(open.BonusFallStart) - Tol),
                "숫자 정착 이후에만 퇴장");
        }

        [Test]
        public void Merge_DuringExitReversesProgressContinuouslyAndCancelsExit()
        {
            var state = new GoldPickupPopupState(100, 0);
            state.Advance(GoldPickupPopupTimeline.BaseOnlyExitStart + 0.25f);
            Assert.That(state.IsExiting, Is.True);
            float before = state.ExitProgress;
            Assert.That(before, Is.GreaterThan(0.3f).And.LessThan(1f));
            state.MarkHudLaunched();

            GoldPickupMergeResult result = state.Merge(10, 0);
            Assert.That(result.ExitCancelled, Is.True);
            Assert.That(state.IsExiting, Is.False);
            Assert.That(state.ExitProgress, Is.EqualTo(before).Within(0.0001f), "끊김 없이 현재 값에서 이어간다.");

            state.Advance(GoldPickupPopupTimeline.RestoreSeconds * 0.4f);
            Assert.That(state.ExitProgress, Is.LessThan(before).And.GreaterThan(0f), "글자와 프레임이 되돌아온다.");
            state.Advance(GoldPickupPopupTimeline.RestoreSeconds);
            Assert.That(state.ExitProgress, Is.EqualTo(0f), "복원이 끝나면 완전히 떠 있는 상태");
            Assert.That(state.HudLaunched, Is.True, "취소·재개로 HUD 비행 표시가 지워지지 않는다.");
        }

        [Test]
        public void Merge_AfterFinishedIsIgnored()
        {
            var state = new GoldPickupPopupState(100, 0);
            state.Advance(5f);
            Assert.That(state.IsFinished, Is.True);
            Assert.That(state.Merge(10, 10).Changed, Is.False);
            Assert.That(state.TotalBase, Is.EqualTo(100));
        }
    }
}
