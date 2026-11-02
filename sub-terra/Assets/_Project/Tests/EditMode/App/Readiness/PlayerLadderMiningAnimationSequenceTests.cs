using NUnit.Framework;
using SubTerra.Gameplay.Player;

namespace SubTerra.App.Tests.EditMode.Readiness
{
    public sealed class PlayerLadderMiningAnimationSequenceTests
    {
        private PlayerLadderMiningAnimationSequence NewSequence() =>
            new(new PlayerLadderMiningAnimationSettings());

        [Test]
        public void StartAndWork_Use24FpsThenPingPongAt12Fps()
        {
            var sequence = NewSequence();
            sequence.Start(1);
            for (int i = 0; i < 12; i++)
            {
                Assert.That(sequence.FrameIndex, Is.EqualTo(i));
                sequence.Tick(1f / 24f);
            }
            foreach (int index in new[] { 12, 13, 14, 15, 14, 13, 12, 13 })
            {
                Assert.That(sequence.FrameIndex, Is.EqualTo(index));
                sequence.Tick(1f / 12f);
            }
        }

        [TestCase(0.05f)]
        [TestCase(0.6f)]
        [TestCase(0.85f)]
        public void Completed_Starts17ImmediatelyAndFinishes24At60Fps(float time)
        {
            var sequence = NewSequence();
            sequence.Start(-1);
            sequence.Tick(time);
            sequence.Complete();
            sequence.Stop();
            for (int i = 16; i < 24; i++)
            {
                Assert.That(sequence.IsActive, Is.True);
                Assert.That(sequence.FrameIndex, Is.EqualTo(i));
                sequence.Tick(1f / 60f);
            }
            Assert.That(sequence.IsActive, Is.False);
        }

        [Test]
        public void Cancel_ReversesToOneWithinPointThreeSeconds_InterruptIsImmediate()
        {
            var sequence = NewSequence();
            sequence.Start(1);
            sequence.Tick(0.75f);
            sequence.Stop();
            int previous = sequence.FrameIndex;
            for (int i = 0; i < 299; i++)
            {
                sequence.Tick(0.001f);
                Assert.That(sequence.FrameIndex, Is.LessThanOrEqualTo(previous));
                previous = sequence.FrameIndex;
            }
            Assert.That(sequence.FrameIndex, Is.Zero);
            sequence.Tick(0.002f);
            Assert.That(sequence.IsActive, Is.False);
            sequence.Start(1);
            sequence.Interrupt();
            Assert.That(sequence.IsActive, Is.False);
        }

        [TestCase(1, 0.149f, 12)]
        [TestCase(-1, 0.149f, 0)]
        [TestCase(1, 0.151f, 0)]
        public void RestartAfterCancellation_SkipsStartOnlyForSameSideWithinGrace(int side, float delay, int frame)
        {
            var sequence = NewSequence();
            sequence.Start(1);
            sequence.Tick(0.6f);
            sequence.Stop();
            sequence.Tick(delay);
            sequence.Start(side);
            Assert.That(sequence.FrameIndex, Is.EqualTo(frame));
            Assert.That(sequence.Direction, Is.EqualTo(side));
        }

        [Test]
        public void RestartDuringFinish_StartsOne_DamageResumesThirteen_ResetClearsGrace()
        {
            var sequence = NewSequence();
            sequence.Start(1);
            sequence.Complete();
            sequence.Start(1);
            Assert.That(sequence.FrameIndex, Is.Zero);
            sequence.ResumeAfterDamage();
            Assert.That(sequence.FrameIndex, Is.EqualTo(12));
            sequence.Interrupt();
            sequence.Start(1);
            Assert.That(sequence.FrameIndex, Is.EqualTo(12));
            sequence.Reset();
            sequence.Start(1);
            Assert.That(sequence.FrameIndex, Is.Zero);
        }

        [Test]
        public void SettingsAndTickSize_ControlSequenceWithoutUnityTime()
        {
            var settings = new PlayerLadderMiningAnimationSettings { startFps = 12f, workFirstFrame = 10 };
            var sequence = new PlayerLadderMiningAnimationSequence(settings);
            sequence.Start(1);
            sequence.Tick(0.5f);
            Assert.That(sequence.FrameIndex, Is.EqualTo(6));
            sequence.Tick(0.5f);
            Assert.That(sequence.FrameIndex, Is.EqualTo(9));
            var large = NewSequence();
            var small = NewSequence();
            large.Start(1);
            small.Start(1);
            large.Tick(1.25f);
            for (int i = 0; i < 125; i++) small.Tick(0.01f);
            Assert.That(large.FrameIndex, Is.EqualTo(small.FrameIndex));
        }
    }
}
