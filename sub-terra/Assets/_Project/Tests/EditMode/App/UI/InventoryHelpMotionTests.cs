using NUnit.Framework;
using SubTerra.App.UI.Inventory;

namespace SubTerra.App.Tests.UI
{
    public sealed class InventoryHelpMotionTests
    {
        [Test]
        public void Glow_RisesSlowlyThenBreathesWithoutVanishing()
        {
            var state = new InventoryHelpHoverState();
            state.Enter();
            Assert.That(state.GlowIntensity, Is.EqualTo(0f).Within(0.0001f));

            state.Tick(InventoryHelpMotion.GlowRiseSeconds * 0.5f);
            Assert.That(state.GlowIntensity, Is.InRange(0.2f, 0.8f), "글로우는 천천히 차올라야 한다.");

            var min = 1f;
            for (var i = 0; i < 240; i++)
            {
                state.Tick(0.01f);
                if (state.GlowLevel >= 1f && state.GlowIntensity < min) min = state.GlowIntensity;
            }

            Assert.That(min, Is.GreaterThan(1f - InventoryHelpMotion.GlowPulseDepth - 0.01f));
        }

        [Test]
        public void Glow_FadesOutAfterExit()
        {
            var state = new InventoryHelpHoverState();
            state.Enter();
            state.Tick(1f);
            state.Exit();
            state.Tick(InventoryHelpMotion.GlowFallSeconds + 0.01f);
            Assert.That(state.GlowIntensity, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(state.IsSettled, Is.True);
        }

        [Test]
        public void Tooltip_OpensLineThenFrameThenContent()
        {
            Assert.That(InventoryHelpMotion.ScaleX(InventoryHelpMotion.LineDuration), Is.EqualTo(1f).Within(0.0001f));
            Assert.That(InventoryHelpMotion.ScaleY(InventoryHelpMotion.LineDuration), Is.EqualTo(InventoryHelpMotion.MinScaleY).Within(0.0001f));
            Assert.That(InventoryHelpMotion.ScaleY(InventoryHelpMotion.OpenDuration), Is.EqualTo(1f).Within(0.0001f));
            Assert.That(InventoryHelpMotion.ContentAlpha(0.1f), Is.EqualTo(0f));
            Assert.That(InventoryHelpMotion.ContentAlpha(0.5f), Is.EqualTo(1f).Within(0.0001f));
            Assert.That(InventoryHelpMotion.Alpha((InventoryHelpMotion.FlickerStart + InventoryHelpMotion.FlickerEnd) * 0.5f),
                Is.LessThan(InventoryHelpMotion.Alpha(InventoryHelpMotion.OpenDuration)), "열리는 도중 한 번 깜빡여야 한다.");
            Assert.That(InventoryHelpMotion.VisibleCharacters(0f, 40), Is.EqualTo(0));
            Assert.That(InventoryHelpMotion.VisibleCharacters(1f, 40), Is.EqualTo(40));
        }

        [Test]
        public void Tooltip_ReenterDuringCloseContinuesWithoutRestart()
        {
            var state = new InventoryHelpHoverState();
            state.Enter();
            state.Tick(0.5f);
            var openBefore = state.OpenElapsed;
            state.Exit();
            state.Tick(InventoryHelpMotion.CloseDuration * 0.5f);
            Assert.That(state.IsClosing, Is.True);
            state.Enter();
            Assert.That(state.OpenElapsed, Is.EqualTo(openBefore), "다시 올려도 열림 시간이 처음으로 돌아가면 안 된다.");
            state.Tick(1f);
            Assert.That(state.CloseElapsed, Is.EqualTo(0f));
            Assert.That(state.IsShown, Is.True);
        }

        [Test]
        public void Tooltip_ClosesAfterCloseDuration()
        {
            var state = new InventoryHelpHoverState();
            state.Enter();
            state.Tick(0.5f);
            state.Exit();
            state.Tick(InventoryHelpMotion.CloseDuration + 0.01f);
            Assert.That(state.IsShown, Is.False);
        }
    }
}
