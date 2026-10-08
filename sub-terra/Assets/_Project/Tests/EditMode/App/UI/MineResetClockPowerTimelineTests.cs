using NUnit.Framework;
using SubTerra.App.UI.HUD;

namespace SubTerra.App.Tests.UI
{
    public sealed class MineResetClockPowerTimelineTests
    {
        [Test]
        public void Endpoints_ContainNoResidualEffects()
        {
            var off = MineResetClockPowerTimeline.Evaluate(-1f);
            Assert.That(off.WindowWidth01 + off.WindowHeight01 + off.ReadoutAlpha + off.FrameAlpha + off.SparkAlpha
                + off.BeamAlpha + off.EdgeGlow + off.SettleFlash, Is.Zero);
            var on = MineResetClockPowerTimeline.Evaluate(2f);
            Assert.That(on.WindowWidth01, Is.EqualTo(1f));
            Assert.That(on.WindowHeight01, Is.EqualTo(1f));
            Assert.That(on.ReadoutAlpha, Is.EqualTo(1f));
            Assert.That(on.FrameAlpha, Is.EqualTo(1f));
            Assert.That(on.SparkAlpha + on.BeamAlpha + on.EdgeGlow + on.SettleFlash, Is.Zero);
        }

        [Test]
        public void Stages_StartWithSpark_ThenHorizontalBeam_ThenVerticalExpansion()
        {
            var spark = MineResetClockPowerTimeline.Evaluate(0.1f);
            Assert.That(spark.SparkAlpha, Is.GreaterThan(0f));
            Assert.That(spark.WindowWidth01, Is.Zero);
            var line = MineResetClockPowerTimeline.Evaluate(0.4f);
            Assert.That(line.WindowWidth01, Is.EqualTo(1f));
            Assert.That(line.WindowHeight01, Is.LessThan(0.35f));
            Assert.That(line.BeamAlpha, Is.EqualTo(1f));
            var expanded = MineResetClockPowerTimeline.Evaluate(0.85f);
            Assert.That(expanded.WindowHeight01, Is.EqualTo(1f));
            Assert.That(expanded.ReadoutAlpha, Is.GreaterThan(0.8f));
            Assert.That(expanded.SettleFlash, Is.GreaterThan(0f));
        }

        [Test]
        public void MaskAndContent_AreMonotonic_AndReverseUsesSamePose()
        {
            var previous = MineResetClockPowerTimeline.Evaluate(0f);
            for (var i = 1; i <= 100; i++)
            {
                var pose = MineResetClockPowerTimeline.Evaluate(i / 100f);
                Assert.That(pose.WindowWidth01, Is.GreaterThanOrEqualTo(previous.WindowWidth01));
                Assert.That(pose.WindowHeight01, Is.GreaterThanOrEqualTo(previous.WindowHeight01));
                Assert.That(pose.ReadoutAlpha, Is.InRange(previous.ReadoutAlpha, 1f));
                Assert.That(pose.FrameAlpha, Is.InRange(previous.FrameAlpha, 1f));
                previous = pose;
            }
            var p = MineResetClockPowerTimeline.Advance(0.5f, true, 0.04f);
            p = MineResetClockPowerTimeline.Advance(p, false, 0.028f);
            Assert.That(p, Is.EqualTo(0.5f).Within(1e-6f));
        }

        [Test]
        public void Durations_Clamp_AndOffIsFaster()
        {
            Assert.That(MineResetClockPowerTimeline.Advance(0f, true, 0.40f), Is.EqualTo(1f));
            Assert.That(MineResetClockPowerTimeline.Advance(1f, false, 0.28f), Is.Zero);
            Assert.That(MineResetClockPowerTimeline.Advance(0.5f, true, -1f), Is.EqualTo(0.5f));
            Assert.That(MineResetClockPowerTimeline.Advance(0.5f, true, 10f), Is.EqualTo(1f));
            Assert.That(MineResetClockPowerTimeline.Advance(0.5f, false, 10f), Is.Zero);
            var on = MineResetClockPowerTimeline.Advance(0f, true, 0.05f);
            var off = 1f - MineResetClockPowerTimeline.Advance(1f, false, 0.05f);
            Assert.That(off / on, Is.EqualTo(0.40f / 0.28f).Within(1e-5f));
        }

        [TestCase(0f, 0f, 0f)]
        [TestCase(0.30f, 0f, 0f)]
        [TestCase(0.45f, 1f, 0f)]
        [TestCase(0.55f, 1f, 0f)]
        [TestCase(0.90f, 1f, 1f)]
        [TestCase(1f, 1f, 1f)]
        public void Frame_IsVisibleDuringExpansion_AfterReadoutHasFaded(float p, float frame, float readout)
        {
            var pose = MineResetClockPowerTimeline.Evaluate(p);
            Assert.That(pose.FrameAlpha, Is.EqualTo(frame).Within(1e-6f));
            Assert.That(pose.ReadoutAlpha, Is.EqualTo(readout).Within(1e-6f));
            if (p >= 0.45f && p <= 0.55f)
                Assert.That(pose.WindowHeight01, Is.InRange(0.1f, 0.9f));
        }
    }
}
