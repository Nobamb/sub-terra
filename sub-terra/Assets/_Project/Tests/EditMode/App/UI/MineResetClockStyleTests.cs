using NUnit.Framework;
using SubTerra.App.UI.HUD;
using UnityEngine;

namespace SubTerra.App.Tests.UI
{
    public sealed class MineResetClockStyleTests
    {
        [TestCase(1801, MineResetClockBand.Normal)]
        [TestCase(1800, MineResetClockBand.Warning)]
        [TestCase(1799, MineResetClockBand.Warning)]
        [TestCase(301, MineResetClockBand.Warning)]
        [TestCase(300, MineResetClockBand.Critical)]
        [TestCase(299, MineResetClockBand.Critical)]
        [TestCase(61, MineResetClockBand.Critical)]
        [TestCase(60, MineResetClockBand.Final)]
        [TestCase(59, MineResetClockBand.Final)]
        [TestCase(1, MineResetClockBand.Final)]
        [TestCase(0, MineResetClockBand.Final)]
        [TestCase(10800, MineResetClockBand.Normal)]
        public void FirstObservation_SnapsToDisplayedBoundary(int seconds, MineResetClockBand band)
        {
            var style = new MineResetClockStyle();
            style.Observe(seconds);
            Assert.That(style.Band, Is.EqualTo(band));
            Assert.That(style.DisplayColor, Is.EqualTo(MineResetClockStyle.GetColor(band)));
            Assert.That(style.IsTransitioning, Is.False);
            Assert.That(style.IsPulsing, Is.EqualTo(band == MineResetClockBand.Final));
            Assert.That(style.PulseFactor, Is.EqualTo(1f));
        }

        [TestCase(1801, 1800)]
        [TestCase(301, 300)]
        [TestCase(61, 60)]
        [TestCase(1802, 1799)]
        public void NaturalBoundary_TransitionsOnceOverOnePointFiveSeconds(int before, int after)
        {
            var style = new MineResetClockStyle();
            style.Observe(before);
            var from = style.DisplayColor;
            style.Observe(after);
            var to = MineResetClockStyle.GetColor(style.Band);
            Assert.That(style.IsTransitioning, Is.True);
            style.Advance(0.75f);
            Assert.That(style.DisplayColor, Is.EqualTo(Color.Lerp(from, to, 0.5f)));
            Assert.That(style.Observe(after), Is.False);
            style.Advance(0.75f);
            Assert.That(style.DisplayColor, Is.EqualTo(to));
            Assert.That(style.IsTransitioning, Is.False);
        }

        [Test]
        public void ResetIncreaseAndJump_ClearOldAnimation()
        {
            var style = new MineResetClockStyle();
            style.Observe(301);
            style.Observe(300);
            style.Advance(0.2f);
            style.Observe(59);
            Assert.That(style.IsTransitioning, Is.False);
            Assert.That(style.PulseFactor, Is.EqualTo(1f));
            style.Advance(0.875f);
            Assert.That(style.PulseFactor, Is.EqualTo(0.45f).Within(1e-5f));
            style.Observe(10800);
            Assert.That(style.IsPulsing, Is.False);
            Assert.That(style.IsTransitioning, Is.False);
            Assert.That(style.PulseFactor, Is.EqualTo(1f));
            style.Reset();
            style.Observe(1800);
            Assert.That(style.DisplayColor, Is.EqualTo(MineResetClockStyle.WarningColor));
            Assert.That(style.IsTransitioning, Is.False);
        }

        [Test]
        public void RepeatedBoundaryAfterInterruptedTransition_UsesCurrentColor()
        {
            var style = new MineResetClockStyle();
            style.Observe(1801);
            style.Observe(1800);
            style.Advance(0.5f);
            style.Observe(1801);
            Assert.That(style.IsTransitioning, Is.False);
            style.Observe(1800);
            style.Advance(1.5f);
            Assert.That(style.DisplayColor, Is.EqualTo(MineResetClockStyle.WarningColor));
            Assert.That(style.IsTransitioning, Is.False);
        }

        [Test]
        public void NewBoundaryDuringTransition_RetargetsFromDisplayedColorWithoutStacking()
        {
            var style = new MineResetClockStyle();
            style.Observe(1801);
            style.Observe(1800);
            style.Advance(0.75f);
            var displayed = style.DisplayColor;
            for (var seconds = 1799; seconds >= 301; seconds--) style.Observe(seconds);
            style.Observe(300);
            Assert.That(style.DisplayColor, Is.EqualTo(displayed));
            style.Advance(0.75f);
            Assert.That(style.DisplayColor, Is.EqualTo(Color.Lerp(displayed, MineResetClockStyle.CriticalColor, 0.5f)));
            style.Advance(0.75f);
            Assert.That(style.IsTransitioning, Is.False);
            Assert.That(style.DisplayColor, Is.EqualTo(MineResetClockStyle.CriticalColor));
        }

        [Test]
        public void FinalPulse_HasOnePointSevenFiveSecondPeriod_AndDoesNotChangeDigits()
        {
            var style = new MineResetClockStyle();
            style.Observe(60);
            var color = style.DisplayColor;
            style.Advance(0.875f);
            Assert.That(style.PulseFactor, Is.EqualTo(0.45f).Within(1e-5f));
            Assert.That(style.DisplayColor, Is.EqualTo(color));
            style.Observe(59);
            style.Advance(0.875f);
            Assert.That(style.PulseFactor, Is.EqualTo(1f).Within(1e-5f));
            style.Observe(1);
            style.Observe(0);
            Assert.That(style.IsPulsing, Is.True);
            style.Observe(61);
            style.Advance(10f);
            Assert.That(style.PulseFactor, Is.EqualTo(1f));
        }

        [TestCase(0, 0x3f)] [TestCase(1, 0x06)] [TestCase(2, 0x5b)]
        [TestCase(3, 0x4f)] [TestCase(4, 0x66)] [TestCase(5, 0x6d)]
        [TestCase(6, 0x7d)] [TestCase(7, 0x07)] [TestCase(8, 0x7f)] [TestCase(9, 0x6f)]
        public void Glyph_UsesStandardSevenSegmentMask(int digit, int mask)
        {
            Assert.That(SevenSegmentGlyph.GetMask(digit), Is.EqualTo(mask));
        }
    }
}
