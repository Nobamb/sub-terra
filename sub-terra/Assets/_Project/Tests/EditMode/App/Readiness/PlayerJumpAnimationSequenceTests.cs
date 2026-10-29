using System.Collections.Generic;
using NUnit.Framework;
using SubTerra.Gameplay.Player;

namespace SubTerra.App.Tests.EditMode.Readiness
{
    public sealed class PlayerJumpAnimationSequenceTests
    {
        private const float Gravity = 9.81f * 3f;
        private const float Step = 1f / 120f;

        private static PlayerJumpAnimationSequence NewSequence() => new(new PlayerJumpAnimationSettings());

        private static string Frame(PlayerJumpAnimationSequence sequence) =>
            sequence.Clip + " " + (sequence.FrameIndex + 1);

        [Test]
        public void FullJump_TakeOff40Fps_RiseByProgress_ApexOnce_FallLoop_LandEightToOne()
        {
            var sequence = NewSequence();
            var phases = new List<PlayerJumpPhase>();
            sequence.PhaseChanged += (_, next) => phases.Add(next);
            sequence.StartJump(11f);
            sequence.Tick(0f, true, 11f, false);
            for (int i = 1; i <= 5; i++)
            {
                Assert.That(Frame(sequence), Is.EqualTo("Rise " + i), "도약 1~5는 40fps");
                Assert.That(sequence.Phase, Is.EqualTo(PlayerJumpPhase.TakeOff));
                if (i < 5) sequence.Tick(1f / 40f, true, 11f - Gravity * i / 40f, false);
            }

            // 실제 상승: 위 속도가 줄어드는 만큼 6→24로 되돌아가지 않고 진행한다.
            float time = 5f / 40f;
            sequence.Tick(1f / 40f, true, 11f - Gravity * time, false);
            Assert.That(sequence.Phase, Is.EqualTo(PlayerJumpPhase.Rise));
            Assert.That(sequence.FrameIndex + 1, Is.EqualTo(6), "도약이 끝나면 6번에서 이어 간다");
            int previous = sequence.FrameIndex;
            while (11f - Gravity * time > 0f)
            {
                sequence.Tick(Step, true, 11f - Gravity * time, false);
                Assert.That(sequence.FrameIndex, Is.GreaterThanOrEqualTo(previous));
                previous = sequence.FrameIndex;
                time += Step;
            }
            Assert.That(previous + 1, Is.EqualTo(24), "정점 직전에 24에 닿는다");

            sequence.Tick(Step, true, -0.01f, false);
            for (int i = 1; i <= 15; i++)
            {
                Assert.That(Frame(sequence), Is.EqualTo("Fall " + i), "정점 이후 fall 1~15 한 번");
                sequence.Tick(1f / 40f, true, -5f, false);
            }
            foreach (int number in new[] { 18, 19, 20, 21, 22, 23, 24, 18, 19 })
            {
                Assert.That(sequence.Phase, Is.EqualTo(PlayerJumpPhase.FallLoop));
                Assert.That(Frame(sequence), Is.EqualTo("Fall " + number), "fall 18~24 12fps 반복");
                sequence.Tick(1f / 12f, true, -10f, false);
            }

            sequence.Tick(Step, false, 0f, false);
            for (int i = 8; i >= 1; i--)
            {
                Assert.That(Frame(sequence), Is.EqualTo("Ready " + i), "착지는 ready 8→1 30fps");
                sequence.Tick(1f / 30f, false, 0f, false);
            }
            Assert.That(sequence.IsActive, Is.False);
            Assert.That(phases, Is.EqualTo(new[]
            {
                PlayerJumpPhase.TakeOff, PlayerJumpPhase.Rise, PlayerJumpPhase.Apex,
                PlayerJumpPhase.FallLoop, PlayerJumpPhase.Land, PlayerJumpPhase.None
            }));
        }

        // 화물 최대(점프 75%)와 그보다 낮은 점프, 60fps·120fps 표시 간격.
        [TestCase(11f, 1f / 60f)]
        [TestCase(8.25f, 1f / 60f)]
        [TestCase(5.5f, 1f / 60f)]
        [TestCase(8.25f, 1f / 120f)]
        public void LowJump_StillReachesRise24AtApex(float launchSpeed, float step)
        {
            var sequence = NewSequence();
            sequence.StartJump(launchSpeed);
            sequence.Tick(0f, true, launchSpeed, false);
            float time = 0f;
            int last = 0;
            for (int i = 0; i < 600 && sequence.Clip == PlayerJumpClip.Rise; i++)
            {
                last = sequence.FrameIndex + 1;
                time += step;
                sequence.Tick(step, true, launchSpeed - Gravity * time, false);
            }
            Assert.That(last, Is.EqualTo(24));
            Assert.That(Frame(sequence), Is.EqualTo("Fall 1"));
            Assert.That(sequence.Phase, Is.EqualTo(PlayerJumpPhase.Apex));
            Assert.That(launchSpeed - Gravity * time, Is.LessThanOrEqualTo(0f), "정점 이후에만 떨어지는 그림");
        }

        [Test]
        public void TakeOffAlwaysFinishesBeforeFallingEvenIfApexComesEarly()
        {
            var sequence = NewSequence();
            sequence.StartJump(1f);
            sequence.Tick(1f / 40f, true, -0.5f, false);
            Assert.That(Frame(sequence), Is.EqualTo("Rise 2"));
            sequence.Tick(3f / 40f, true, -1f, false);
            Assert.That(Frame(sequence), Is.EqualTo("Rise 5"));
            sequence.Tick(1f / 40f, true, -1.5f, false);
            Assert.That(Frame(sequence), Is.EqualTo("Fall 1"));
        }

        [Test]
        public void WalkingOffLedge_StartsAtFallOne_CoyoteJumpSwitchesToTakeOff()
        {
            var sequence = NewSequence();
            sequence.Tick(Step, true, -0.2f, true);
            Assert.That(Frame(sequence), Is.EqualTo("Fall 1"));
            Assert.That(sequence.Phase, Is.EqualTo(PlayerJumpPhase.Apex));
            sequence.Tick(1f / 40f, true, -0.5f, true);
            Assert.That(Frame(sequence), Is.EqualTo("Fall 2"));

            sequence.StartJump(11f);
            Assert.That(Frame(sequence), Is.EqualTo("Rise 1"));
            Assert.That(sequence.Phase, Is.EqualTo(PlayerJumpPhase.TakeOff));
        }

        [Test]
        public void Landing_MoveInputEndsImmediately_JumpInputRestarts()
        {
            var sequence = NewSequence();
            sequence.Tick(Step, true, -3f, false);
            sequence.Tick(Step, false, 0f, false);
            Assert.That(Frame(sequence), Is.EqualTo("Ready 8"));
            sequence.Tick(Step, false, 0f, true);
            Assert.That(sequence.IsActive, Is.False, "좌우 입력이면 바로 걷기");

            sequence.Tick(Step, true, -3f, false);
            sequence.Tick(Step, false, 0f, false);
            sequence.Tick(1f / 30f, false, 0f, false);
            Assert.That(Frame(sequence), Is.EqualTo("Ready 7"));
            sequence.StartJump(11f);
            sequence.Tick(0f, true, 11f, false);
            Assert.That(Frame(sequence), Is.EqualTo("Rise 1"), "착지 중 점프는 바로 새 도약");
        }

        [Test]
        public void BufferedJumpOnLandingTick_SkipsLanding()
        {
            var sequence = NewSequence();
            var phases = new List<PlayerJumpPhase>();
            sequence.Tick(Step, true, -3f, false);
            sequence.PhaseChanged += (_, next) => phases.Add(next);
            // 같은 물리 틱에 착지와 버퍼 점프가 처리되면 그림 쪽에서는 접지를 보지 못한다.
            sequence.StartJump(11f);
            sequence.Tick(0f, true, 11f, false);
            Assert.That(phases, Is.EqualTo(new[] { PlayerJumpPhase.TakeOff }));
            Assert.That(Frame(sequence), Is.EqualTo("Rise 1"));
        }

        [Test]
        public void BobCompensation_DefaultOff_UsesMeasuredTableOnlyForApexFrames()
        {
            var settings = new PlayerJumpAnimationSettings { fallBobOffsets = new[] { 0f, 19f, 32f } };
            var sequence = new PlayerJumpAnimationSequence(settings);
            sequence.Tick(Step, true, -1f, false);
            sequence.Tick(1f / 40f, true, -1f, false);
            Assert.That(sequence.BobOffsetPixels(), Is.EqualTo(0f), "기본은 끔");
            settings.fallBobCompensation = 1f;
            Assert.That(sequence.BobOffsetPixels(), Is.EqualTo(19f));
            settings.fallBobCompensation = 0.5f;
            Assert.That(sequence.BobOffsetPixels(), Is.EqualTo(9.5f));
            sequence.Tick(Step, false, 0f, false);
            Assert.That(sequence.BobOffsetPixels(), Is.EqualTo(0f));
        }
    }
}
