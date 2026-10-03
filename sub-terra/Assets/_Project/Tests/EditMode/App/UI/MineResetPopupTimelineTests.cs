using NUnit.Framework;
using SubTerra.App.UI.SurfaceBase;

namespace SubTerra.App.Tests.UI
{
    /// <summary>prompt-B 123-2: '새 광산 구역' 팝업 등장·유지·닫기 시간표.</summary>
    public sealed class MineResetPopupTimelineTests
    {
        [Test]
        public void Open_StartsAsThinLineThenWidthThenHeight()
        {
            var start = MineResetPopupTimeline.EvaluateOpen(0f);
            Assert.That(start.ScanWidth, Is.Zero);
            Assert.That(start.Reveal, Is.Zero);
            Assert.That(start.Content, Is.Zero);
            Assert.That(start.HexAlpha, Is.Zero);
            Assert.That(start.Interactable, Is.False);

            // 가로로 먼저 뻗고(ScanEnd), 그 직후부터 위아래로 펼친다.
            Assert.That(MineResetPopupTimeline.RevealStart, Is.LessThan(MineResetPopupTimeline.ScanEnd));
            Assert.That(MineResetPopupTimeline.RevealStart, Is.GreaterThan(MineResetPopupTimeline.ScanStart));
            var widthDone = MineResetPopupTimeline.EvaluateOpen(MineResetPopupTimeline.ScanEnd);
            Assert.That(widthDone.ScanWidth, Is.EqualTo(1f));
            Assert.That(widthDone.Reveal, Is.LessThan(0.2f));
        }

        [Test]
        public void Open_SettlesWithinRequestedWindowWithoutOvershoot()
        {
            Assert.That(MineResetPopupTimeline.SettleTime, Is.InRange(0.45f, 0.65f));
            var settled = MineResetPopupTimeline.EvaluateOpen(MineResetPopupTimeline.SettleTime);
            Assert.That(settled.Reveal, Is.EqualTo(1f));
            Assert.That(settled.Content, Is.EqualTo(1f));
            Assert.That(settled.HexScale, Is.EqualTo(1f));
            Assert.That(settled.Interactable, Is.True);

            // 탄성 없이 감속: 펼침·육각 배율은 단조 증가하고 1을 넘지 않는다.
            var lastReveal = 0f;
            var lastHex = 0f;
            for (var t = 0f; t <= 1.2f; t += 0.005f)
            {
                var pose = MineResetPopupTimeline.EvaluateOpen(t);
                Assert.That(pose.Reveal, Is.GreaterThanOrEqualTo(lastReveal - 1e-5f));
                Assert.That(pose.HexScale, Is.GreaterThanOrEqualTo(lastHex - 1e-5f));
                Assert.That(pose.Reveal, Is.LessThanOrEqualTo(1f));
                Assert.That(pose.HexScale, Is.LessThanOrEqualTo(1f));
                lastReveal = pose.Reveal;
                lastHex = pose.HexScale;
            }
        }

        [Test]
        public void Open_ContentFadesInLateHalfAndHexOverlapsPanel()
        {
            Assert.That(MineResetPopupTimeline.ContentStart, Is.GreaterThan(MineResetPopupTimeline.RevealStart));
            Assert.That(MineResetPopupTimeline.ContentStart, Is.LessThan(MineResetPopupTimeline.RevealEnd));
            Assert.That(MineResetPopupTimeline.ContentEnd - MineResetPopupTimeline.ContentStart, Is.LessThanOrEqualTo(0.2f));
            // 육각형은 패널이 다 펼쳐지기 전에 시작해 겹쳐 진행한다.
            Assert.That(MineResetPopupTimeline.HexStart, Is.LessThan(MineResetPopupTimeline.RevealEnd));
            Assert.That(MineResetPopupTimeline.CoreStart, Is.LessThan(MineResetPopupTimeline.HexStart));
            Assert.That(MineResetPopupTimeline.HexStartScale, Is.GreaterThan(0f));
        }

        [Test]
        public void Open_FlashesOnceThenSettlesToIdle()
        {
            var peak = MineResetPopupTimeline.EvaluateOpen(MineResetPopupTimeline.FrameFlashPeak);
            Assert.That(peak.FrameFlash, Is.EqualTo(1f));
            Assert.That(MineResetPopupTimeline.EvaluateOpen(MineResetPopupTimeline.FrameFlashEnd).FrameFlash, Is.Zero.Within(1e-5f));

            var hexPeak = MineResetPopupTimeline.EvaluateOpen(MineResetPopupTimeline.HexEnd);
            Assert.That(hexPeak.HexFlash, Is.EqualTo(1f));
            var after = MineResetPopupTimeline.EvaluateOpen(MineResetPopupTimeline.HexFlashEnd);
            Assert.That(after.HexFlash, Is.Zero.Within(1e-5f));
            Assert.That(after.Idle, Is.EqualTo(1f));
            Assert.That(after.CoreAlpha, Is.Zero);
            Assert.That(after.ScanAlpha, Is.Zero);
            Assert.That(after.EdgeAlpha, Is.Zero);
        }

        [Test]
        public void Close_GlowFadesBeforePanelAndFinishes()
        {
            var early = MineResetPopupTimeline.EvaluateClose(MineResetPopupTimeline.GlowFadeDuration);
            Assert.That(early.Glow, Is.Zero.Within(1e-5f));
            Assert.That(early.Alpha, Is.GreaterThan(0.3f));
            Assert.That(early.Finished, Is.False);
            var done = MineResetPopupTimeline.EvaluateClose(MineResetPopupTimeline.CloseDuration);
            Assert.That(done.Alpha, Is.Zero.Within(1e-5f));
            Assert.That(done.Finished, Is.True);
            Assert.That(MineResetPopupTimeline.CloseDuration, Is.LessThanOrEqualTo(0.35f));
        }

        [Test]
        public void Breath_IsPeriodicAndBounded()
        {
            for (var t = 0f; t < 8f; t += 0.1f)
            {
                var v = MineResetPopupTimeline.Breath(t, 3f, 0.2f);
                Assert.That(v, Is.InRange(0f, 1f));
                Assert.That(MineResetPopupTimeline.Breath(t + 3f, 3f, 0.2f), Is.EqualTo(v).Within(1e-4f));
            }
        }

        [Test]
        public void FitScale_ShrinksOnlyWhenScreenIsTooShort()
        {
            Assert.That(MineResetPopupTimeline.FitScale(1920f, 1080f, 1332f, 1021f, 12f), Is.EqualTo(1f));
            Assert.That(MineResetPopupTimeline.FitScale(1920f, 1536f, 1332f, 1021f, 12f), Is.EqualTo(1f));
            // 21:9(너비 기준 스케일) → 캔버스 높이 810
            var ultrawide = MineResetPopupTimeline.FitScale(1920f, 810f, 1332f, 1021f, 12f);
            Assert.That(ultrawide, Is.LessThan(1f));
            Assert.That(1021f * ultrawide, Is.LessThanOrEqualTo(810f - 24f + 0.01f));
        }
    }
}
