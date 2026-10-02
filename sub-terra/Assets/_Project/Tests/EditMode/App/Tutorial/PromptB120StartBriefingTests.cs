using System;
using NUnit.Framework;
using SubTerra.App.Tutorial;
using SubTerra.App.UI;
using SubTerra.App.UI.Tutorial;

namespace SubTerra.App.Tests.UI
{
    public sealed class PromptB120StartBriefingTests
    {
        private const string ExpectedBody =
            "지상에는 더 이상 캘 것이 남지 않았습니다.\n"
            + "배양시설에는 인류의 유전 정보가 보존되어 있습니다.\n"
            + "유전자 개량에 성공하면, 인류를 다시 태어나게 할 수 있습니다.\n"
            + "시설을 가동할 자원은 이제 땅 아래에 있습니다.\n"
            + "\n"
            + "내려가십시오. 캐고, 버티고, 살아서 돌아오십시오.";

        [Test]
        public void GuidanceBodyIsExactlyTheConfirmedText()
        {
            Assert.That(DemoObjectiveCatalog.IntroductionGuidanceBody, Is.EqualTo(ExpectedBody));
            Assert.That(DemoObjectiveCatalog.IntroductionGuidanceConfirmLabel, Is.EqualTo("작업 시작"));
        }

        [Test]
        public void IntroRunsAboutOneToOnePointFiveSecondsWithTwoOrThreeScreenGlitches()
        {
            Assert.That(StartBriefingTimeline.IntroDuration, Is.InRange(1.0f, 1.5f));
            Assert.That(StartBriefingTimeline.BurstCount, Is.InRange(2, 3));
            for (var i = 0; i < StartBriefingTimeline.BurstCount; i++)
            {
                var middle = StartBriefingTimeline.BurstStarts[i] + StartBriefingTimeline.BurstLength * 0.5f;
                Assert.That(StartBriefingTimeline.ActiveBurst(middle), Is.EqualTo(i));
            }

            Assert.That(StartBriefingTimeline.ActiveBurst(StartBriefingTimeline.GatherStart), Is.EqualTo(-1));
        }

        [Test]
        public void IntroOrderIsGlitchThenGatherThenSpreadThenFrameThenContent()
        {
            var lastBurstEnd = StartBriefingTimeline.BurstStarts[StartBriefingTimeline.BurstCount - 1]
                + StartBriefingTimeline.BurstLength;
            Assert.That(StartBriefingTimeline.GatherStart, Is.GreaterThanOrEqualTo(lastBurstEnd));
            Assert.That(StartBriefingTimeline.GatherEnd, Is.GreaterThan(StartBriefingTimeline.GatherStart));
            Assert.That(StartBriefingTimeline.SpreadEnd, Is.GreaterThan(StartBriefingTimeline.GatherEnd));
            Assert.That(StartBriefingTimeline.FrameOpenStart, Is.GreaterThanOrEqualTo(StartBriefingTimeline.SpreadEnd));
            Assert.That(StartBriefingTimeline.FrameOpenEnd, Is.GreaterThan(StartBriefingTimeline.FrameOpenStart));
            Assert.That(StartBriefingTimeline.ContentFadeEnd, Is.GreaterThan(StartBriefingTimeline.FrameOpenEnd));

            // 본문은 프레임이 다 열리기 전에는 보이지 않는다.
            Assert.That(StartBriefingTimeline.ContentAlphaIntro(StartBriefingTimeline.FrameOpenEnd), Is.EqualTo(0f));
            Assert.That(StartBriefingTimeline.ContentAlphaIntro(StartBriefingTimeline.IntroDuration), Is.EqualTo(1f));
            Assert.That(StartBriefingTimeline.FrameOpen(StartBriefingTimeline.FrameOpenStart), Is.EqualTo(0f));
            Assert.That(StartBriefingTimeline.FrameOpen(StartBriefingTimeline.FrameOpenEnd), Is.EqualTo(1f).Within(1e-4f));
        }

        [Test]
        public void GlitchIsStrongOnEntranceAndWeakWhileReading()
        {
            Assert.That(StartBriefingTimeline.IntroIntensity(0.9f), Is.GreaterThan(0.8f));
            Assert.That(
                StartBriefingTimeline.IntroIntensity(StartBriefingTimeline.IntroDuration),
                Is.EqualTo(StartBriefingTimeline.SustainIntensity).Within(1e-4f));
            Assert.That(StartBriefingTimeline.SustainIntensity, Is.InRange(0.1f, 0.35f));
        }

        [Test]
        public void CloseTakesPointThreeToPointFiveSecondsAndFadesGlitchBodyAndLight()
        {
            Assert.That(StartBriefingTimeline.CloseDuration, Is.InRange(0.3f, 0.5f));
            var d = StartBriefingTimeline.CloseDuration;
            Assert.That(StartBriefingTimeline.CloseIntensity(0f), Is.EqualTo(StartBriefingTimeline.SustainIntensity).Within(1e-4f));
            Assert.That(StartBriefingTimeline.CloseIntensity(d), Is.EqualTo(0f).Within(1e-4f));
            Assert.That(
                StartBriefingTimeline.CloseIntensity(d * 0.5f),
                Is.LessThan(StartBriefingTimeline.CloseIntensity(d * 0.25f)));
            Assert.That(StartBriefingTimeline.ContentAlphaClose(0f), Is.EqualTo(1f));
            Assert.That(StartBriefingTimeline.ContentAlphaClose(d), Is.EqualTo(0f));
            Assert.That(StartBriefingTimeline.CloseLight(0f), Is.EqualTo(1f).Within(1e-4f));
            Assert.That(StartBriefingTimeline.CloseLight(d), Is.EqualTo(0f).Within(1e-4f));
            Assert.That(StartBriefingTimeline.CloseOverallAlpha(d), Is.EqualTo(0f));
        }

        [Test]
        public void PlannerSpacesGlitchesFartherApartAsIntensityDrops()
        {
            var high = Average(1f, 400);
            var sustain = Average(StartBriefingTimeline.SustainIntensity, 400);
            var faint = Average(0.05f, 400);
            Assert.That(high, Is.LessThan(sustain));
            Assert.That(sustain, Is.LessThan(faint));
            Assert.That(new BriefingGlitchPlanner(() => 0.5f).NextDelay(0f), Is.EqualTo(float.PositiveInfinity));
        }

        [Test]
        public void PlannerDelaysAreIrregularAndNeverZero()
        {
            var random = new Random(7);
            var planner = new BriefingGlitchPlanner(() => (float)random.NextDouble());
            var distinct = new System.Collections.Generic.HashSet<int>();
            for (var i = 0; i < 200; i++)
            {
                var delay = planner.NextDelay(StartBriefingTimeline.SustainIntensity);
                Assert.That(delay, Is.GreaterThan(0.05f));
                distinct.Add((int)(delay * 20f));
            }

            Assert.That(distinct.Count, Is.GreaterThan(8));
        }

        [Test]
        public void PlannerUsesEveryGlitchKind()
        {
            var random = new Random(3);
            var planner = new BriefingGlitchPlanner(() => (float)random.NextDouble());
            var seen = new System.Collections.Generic.HashSet<BriefingGlitchKind>();
            for (var i = 0; i < 200; i++)
            {
                seen.Add(planner.PickKind());
            }

            Assert.That(seen.Count, Is.EqualTo(3));
        }

        [Test]
        public void LifecycleClosesOnlyOnceAndOnlyFromShown()
        {
            var life = new BriefingLifecycle();
            Assert.That(life.TryBeginClose(), Is.False, "숨김 상태에서는 닫기를 받지 않는다.");
            life.Begin();
            Assert.That(life.TryBeginClose(), Is.False, "대기 중에는 닫기를 받지 않는다.");
            Assert.That(life.TryStartIntro(), Is.True);
            Assert.That(life.TryStartIntro(), Is.False);
            Assert.That(life.BlocksBackground, Is.True);
            Assert.That(life.TryBeginClose(), Is.False, "등장 연출 중에는 닫기를 받지 않는다.");
            Assert.That(life.TryFinishIntro(), Is.True);

            Assert.That(life.TryBeginClose(), Is.True);
            Assert.That(life.TryBeginClose(), Is.False, "연속 클릭은 한 번만 인정한다.");
            Assert.That(life.BlocksBackground, Is.True);
            Assert.That(life.TryCompleteClose(), Is.True);
            Assert.That(life.TryCompleteClose(), Is.False, "종료 처리는 한 번만 인정한다.");
            Assert.That(life.BlocksBackground, Is.False);
        }

        [Test]
        public void LifecycleResetAllowsReopenFromCleanState()
        {
            var life = new BriefingLifecycle();
            life.Begin();
            life.TryStartIntro();
            life.TryFinishIntro();
            life.TryBeginClose();
            life.Reset();
            Assert.That(life.Phase, Is.EqualTo(BriefingPhase.Hidden));
            life.Begin();
            Assert.That(life.TryStartIntro(), Is.True);
            Assert.That(life.TryFinishIntro(), Is.True);
            Assert.That(life.TryBeginClose(), Is.True);
        }

        [Test]
        public void PauseOwnershipFreezesAndRestoresOwnFreeze()
        {
            var scale = 1f;
            var pause = new PauseOwnership(() => scale, value => scale = value);
            Assert.That(pause.Acquire("start-briefing"), Is.True);
            Assert.That(scale, Is.EqualTo(0f));
            Assert.That(pause.IsHeld, Is.True);
            Assert.That(pause.Acquire("start-briefing"), Is.False, "같은 소유자는 한 번만 잡힌다.");
            Assert.That(pause.Release("start-briefing"), Is.True);
            Assert.That(scale, Is.EqualTo(1f));
            Assert.That(pause.IsHeld, Is.False);
            Assert.That(pause.Release("start-briefing"), Is.False);
        }

        [Test]
        public void PauseOwnershipKeepsAnotherSystemsPause()
        {
            var scale = 0f; // 다른 시스템이 이미 정지
            var pause = new PauseOwnership(() => scale, value => scale = value);
            pause.Acquire("start-briefing");
            pause.Release("start-briefing");
            Assert.That(scale, Is.EqualTo(0f), "브리핑이 적용하지 않은 정지는 풀지 않는다.");
        }

        [Test]
        public void PauseOwnershipDoesNotOverwriteScaleChangedByOthers()
        {
            var scale = 1f;
            var pause = new PauseOwnership(() => scale, value => scale = value);
            pause.Acquire("start-briefing");
            scale = 0.5f; // 연출 중 다른 시스템이 배속을 바꿈
            pause.Release("start-briefing");
            Assert.That(scale, Is.EqualTo(0.5f));
        }

        [Test]
        public void PauseOwnershipWaitsForEveryOwnerAndRestoresPreviousSpeed()
        {
            var scale = 2f;
            var pause = new PauseOwnership(() => scale, value => scale = value);
            var changed = 0;
            pause.Changed += () => changed++;
            pause.Acquire("start-briefing");
            pause.Acquire("other");
            pause.Release("start-briefing");
            Assert.That(scale, Is.EqualTo(0f));
            Assert.That(pause.IsHeldBy("other"), Is.True);
            pause.Release("other");
            Assert.That(scale, Is.EqualTo(2f));
            Assert.That(changed, Is.EqualTo(4));
        }

        [Test]
        public void PauseOwnershipReleaseAllClearsEverything()
        {
            var scale = 1f;
            var pause = new PauseOwnership(() => scale, value => scale = value);
            pause.Acquire("a");
            pause.Acquire("b");
            pause.ReleaseAll();
            Assert.That(pause.IsHeld, Is.False);
            Assert.That(scale, Is.EqualTo(1f));
        }

        private static float Average(float intensity, int samples)
        {
            var random = new Random(11);
            var planner = new BriefingGlitchPlanner(() => (float)random.NextDouble());
            var total = 0f;
            for (var i = 0; i < samples; i++)
            {
                total += planner.NextDelay(intensity);
            }

            return total / samples;
        }
    }
}
