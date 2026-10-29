using NUnit.Framework;
using SubTerra.Gameplay.Player;

namespace SubTerra.App.Tests.EditMode.Readiness
{
    public sealed class PlayerMiningAnimationSequenceTests
    {
        private const float Step = 0.001f;

        [Test]
        public void Defaults_UseExpectedTimingAndReturnFrame()
        {
            var settings = new PlayerMiningAnimationSettings();

            Assert.That(settings.startFps, Is.EqualTo(20f));
            Assert.That(settings.workFps, Is.EqualTo(12f));
            Assert.That(settings.returnFps, Is.EqualTo(30f));
            Assert.That(settings.miningReturnFrame, Is.EqualTo(5));
            Assert.That(settings.finishFps, Is.EqualTo(60f));
            Assert.That(settings.cancelMaxSeconds, Is.EqualTo(0.3f));
            Assert.That(settings.restartGraceSeconds, Is.EqualTo(0.15f));
        }

        [Test]
        public void StartAndWork_PlayFramesOneToThirteenThenReturnToFive()
        {
            var sequence = NewSequence();
            sequence.Start();

            Assert.That(sequence.IsActive, Is.True);
            Assert.That(sequence.FrameIndex, Is.EqualTo(0));
            TickUntilFrame(sequence, 4);
            TickUntilFrame(sequence, 12);
            TickUntilFrame(sequence, 11);
            TickUntilFrame(sequence, 4);

            sequence.Tick(1f / 30f + Step);

            Assert.That(sequence.FrameIndex, Is.EqualTo(5), "forward work resumes after return frame 5");
            TickUntilFrame(sequence, 5);
        }

        [TestCase(7, 6)]
        [TestCase(1, 4)]
        [TestCase(99, 11)]
        public void ReturnFrame_UsesSettingAndClampsToFiveThroughTwelve(int configuredFrame, int expectedIndex)
        {
            var settings = new PlayerMiningAnimationSettings { miningReturnFrame = configuredFrame };
            var sequence = NewSequence(settings);
            sequence.Start();
            TickUntilFrame(sequence, 12);
            TickUntilFrame(sequence, 11);
            TickUntilFrame(sequence, expectedIndex);

            Assert.That(sequence.FrameIndex, Is.EqualTo(expectedIndex));
            TickUntilFrame(sequence, expectedIndex + 1);
        }

        [Test]
        public void Complete_FromStartWorkOrReturn_StartsFinishImmediately()
        {
            var fromStart = NewSequence();
            fromStart.Start();
            TickUntilFrame(fromStart, 2);
            fromStart.Complete();
            AssertCompletedFinish(fromStart);

            var fromWork = NewSequence();
            fromWork.Start();
            TickUntilFrame(fromWork, 8);
            fromWork.Complete();
            AssertCompletedFinish(fromWork);

            var fromReturn = NewSequence();
            fromReturn.Start();
            TickUntilFrame(fromReturn, 12);
            TickUntilFrame(fromReturn, 11);
            TickUntilFrame(fromReturn, 10);
            fromReturn.Complete();
            AssertCompletedFinish(fromReturn);
        }

        [Test]
        public void Complete_FromIdleOrCancel_StartsFinishImmediately()
        {
            var sequence = NewSequence();
            sequence.Complete();
            AssertCompletedFinish(sequence);

            sequence.Start();
            TickUntilFrame(sequence, 8);
            sequence.Stop();
            sequence.Complete();
            AssertCompletedFinish(sequence);
        }

        [Test]
        public void Stop_ReversesWithinMaximumAndShortCancelCanResume()
        {
            var sequence = NewSequence();
            sequence.Start();
            TickUntilFrame(sequence, 12);
            sequence.Stop();

            sequence.Tick(0.299f);
            Assert.That(sequence.IsActive, Is.True);
            Assert.That(sequence.FrameIndex, Is.EqualTo(0));
            sequence.Tick(0.002f);
            Assert.That(sequence.IsActive, Is.False);

            sequence.Start();
            TickUntilFrame(sequence, 8);
            sequence.Stop();
            sequence.Start();

            Assert.That(sequence.IsActive, Is.True);
            Assert.That(sequence.FrameIndex, Is.EqualTo(8), "restart inside the grace period resumes the interrupted work frame");
        }

        [Test]
        public void RestartAfterGraceStartsOver_AndStartDuringFinishRestarts()
        {
            var sequence = NewSequence();
            sequence.Start();
            TickUntilFrame(sequence, 8);
            sequence.Stop();
            sequence.Tick(0.151f);
            sequence.Start();
            Assert.That(sequence.FrameIndex, Is.EqualTo(0));

            sequence.Complete();
            Assert.That(sequence.FrameIndex, Is.EqualTo(14));
            sequence.Start();
            Assert.That(sequence.IsActive, Is.True);
            Assert.That(sequence.FrameIndex, Is.EqualTo(0));
        }

        [Test]
        public void InterruptDamageResumeAndReset_ApplyTheirImmediateFrames()
        {
            var sequence = NewSequence();
            sequence.Start();
            TickUntilFrame(sequence, 7);
            sequence.Interrupt();
            Assert.That(sequence.IsActive, Is.False);

            sequence.ResumeAfterDamage();
            Assert.That(sequence.IsActive, Is.True);
            Assert.That(sequence.FrameIndex, Is.EqualTo(4));
            TickUntilFrame(sequence, 5);

            sequence.Reset();
            Assert.That(sequence.IsActive, Is.False);
            Assert.That(sequence.FrameIndex, Is.EqualTo(0));
            sequence.Tick(0.2f);
            Assert.That(sequence.FrameIndex, Is.EqualTo(0));
        }

        [Test]
        public void DifferentTickSizes_ProduceTheSameFrame()
        {
            var largeSteps = NewSequence();
            var smallSteps = NewSequence();
            largeSteps.Start();
            smallSteps.Start();

            largeSteps.Tick(0.501f);
            for (var i = 0; i < 10; i++) smallSteps.Tick(0.05f);
            smallSteps.Tick(0.001f);

            Assert.That(largeSteps.FrameIndex, Is.EqualTo(smallSteps.FrameIndex));
            Assert.That(largeSteps.FrameIndex, Is.EqualTo(7));
        }

        private static PlayerMiningAnimationSequence NewSequence(PlayerMiningAnimationSettings settings = null)
        {
            return new PlayerMiningAnimationSequence(settings ?? new PlayerMiningAnimationSettings());
        }

        private static void TickUntilFrame(PlayerMiningAnimationSequence sequence, int target)
        {
            var ticks = 0;
            while (sequence.FrameIndex != target && ticks++ < 10000)
            {
                Assert.That(sequence.IsActive, Is.True, "sequence became idle before frame " + target);
                sequence.Tick(Step);
            }

            Assert.That(sequence.FrameIndex, Is.EqualTo(target));
        }

        private static void AssertCompletedFinish(PlayerMiningAnimationSequence sequence)
        {
            sequence.Stop();
            Assert.That(sequence.IsActive, Is.True, "Stop is ignored after completion");
            Assert.That(sequence.FrameIndex, Is.EqualTo(14), "completion immediately starts lifting at frame 15");

            sequence.Tick(9f / 60f);
            Assert.That(sequence.FrameIndex, Is.EqualTo(23), "finish reaches frame 24 at 60 fps");
            Assert.That(sequence.IsActive, Is.True);
            sequence.Tick(1f / 60f);
            Assert.That(sequence.IsActive, Is.False);
            Assert.That(sequence.FrameIndex, Is.EqualTo(23));
        }
    }
}
