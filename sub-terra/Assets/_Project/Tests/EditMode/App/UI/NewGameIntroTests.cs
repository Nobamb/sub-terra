using System.IO;
using NUnit.Framework;
using SubTerra.App.State;
using SubTerra.App.UI.Intro;

namespace SubTerra.App.Tests.UI
{
    public sealed class NewGameIntroTests
    {
        private const float Frame = 1f / 60f;

        [Test]
        public void IsFirstMineEntry_TrueOnlyWhenObjectiveProgressIsEmpty()
        {
            Assert.That(NewGameIntroPolicy.IsFirstMineEntry(new ProgressState(0)), Is.True);
            Assert.That(NewGameIntroPolicy.IsFirstMineEntry(GameState.CreateNew().Progress), Is.True);
            Assert.That(NewGameIntroPolicy.IsFirstMineEntry(new ProgressState(0, false, "mine_block")), Is.False);
            Assert.That(NewGameIntroPolicy.IsFirstMineEntry(new ProgressState(2)), Is.False);
            Assert.That(NewGameIntroPolicy.IsFirstMineEntry(new ProgressState(0, false, null, true)), Is.False);
            Assert.That(NewGameIntroPolicy.IsFirstMineEntry(null), Is.False);
        }

        [Test]
        public void ShouldPlayVideo_RequiresFirstEntryAndNoReduceMotionAndFile()
        {
            Assert.That(NewGameIntroPolicy.ShouldPlayVideo(true, false, true, false), Is.True);
            Assert.That(NewGameIntroPolicy.ShouldPlayVideo(false, false, true, false), Is.False, "이어하기·재진입");
            Assert.That(NewGameIntroPolicy.ShouldPlayVideo(true, true, true, false), Is.False, "모션 감소");
            Assert.That(NewGameIntroPolicy.ShouldPlayVideo(true, false, false, false), Is.False, "영상 없음");
            Assert.That(NewGameIntroPolicy.ShouldPlayVideo(true, false, true, true), Is.False, "이미 재생");
        }

        [Test]
        public void ShouldPlayTurnOnOnly_StillTrueWhenVideoIsSkipped()
        {
            Assert.That(NewGameIntroPolicy.ShouldPlayTurnOnOnly(true, false), Is.True);
            Assert.That(NewGameIntroPolicy.ShouldPlayTurnOnOnly(false, false), Is.False);
            Assert.That(NewGameIntroPolicy.ShouldPlayTurnOnOnly(true, true), Is.False);
        }

        [Test]
        public void TurnOn_StartsFullyCoveredWithNoLine()
        {
            var frame = new NewGameTurnOnTimeline().Evaluate();

            Assert.That(frame.Open, Is.Zero);
            Assert.That(frame.LineWidth, Is.Zero);
            Assert.That(frame.IsComplete, Is.False);
        }

        [Test]
        public void TurnOn_LineGrowsThenShuttersOpenMonotonically()
        {
            var timeline = new NewGameTurnOnTimeline();
            var lastOpen = 0f;
            var lastLine = 0f;
            var sawLineBeforeOpen = false;
            NewGameTurnOnFrame frame;
            do
            {
                frame = timeline.Advance(Frame);
                Assert.That(frame.Open, Is.GreaterThanOrEqualTo(lastOpen));
                Assert.That(frame.LineWidth, Is.GreaterThanOrEqualTo(lastLine - 0.0001f));
                if (frame.Open <= 0.05f && frame.LineWidth >= 0.95f) sawLineBeforeOpen = true;
                lastOpen = frame.Open;
                lastLine = frame.LineWidth;
            }
            while (!frame.IsComplete && timeline.Elapsed < 5f);

            Assert.That(sawLineBeforeOpen, Is.True, "셔터가 열리기 전에 가로선이 먼저 끝까지 그어진다.");
            Assert.That(frame.IsComplete, Is.True);
            Assert.That(frame.Open, Is.EqualTo(1f));
            Assert.That(frame.NoiseAlpha, Is.Zero);
            Assert.That(frame.FlashAlpha, Is.Zero);
            Assert.That(frame.LineAlpha, Is.Zero);
        }

        [Test]
        public void TurnOn_StaysWithinTwoSeconds()
        {
            Assert.That(new NewGameTurnOnTimeline().TotalDuration, Is.LessThanOrEqualTo(2f));
        }

        [Test]
        public void TurnOn_SkipCompletesImmediately()
        {
            var timeline = new NewGameTurnOnTimeline();
            timeline.Advance(Frame);
            timeline.Skip();

            var frame = timeline.Evaluate();

            Assert.That(timeline.IsComplete, Is.True);
            Assert.That(frame.IsComplete, Is.True);
            Assert.That(frame.Open, Is.EqualTo(1f));
        }

        [Test]
        public void TurnOn_LongFrameIsClampedSoLoadHitchDoesNotEatTheEffect()
        {
            var timeline = new NewGameTurnOnTimeline();

            timeline.Advance(3f);

            Assert.That(timeline.Elapsed, Is.LessThanOrEqualTo(NewGameTurnOnTimeline.MaxStep + 0.0001f));
            Assert.That(timeline.IsComplete, Is.False);
        }

        [Test]
        public void IntroVideo_ShippedInStreamingAssets()
        {
            // 인트로 파일명을 바꾸면 코드 상수와 함께 바꿔야 한다.
            Assert.That(NewGameIntroPlayer.VideoRelativePath, Is.EqualTo("Video/sub-terra-intro.mp4"));
            Assert.That(
                File.Exists(Path.Combine(UnityEngine.Application.dataPath, "StreamingAssets", NewGameIntroPlayer.VideoRelativePath)),
                Is.True);
        }
    }
}
