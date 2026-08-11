using NUnit.Framework;
using SubTerra.App.Run;
using SubTerra.App.State;

namespace SubTerra.App.Tests.Run
{
    public sealed class EmergencyEscapeServiceTests
    {
        [Test]
        public void PromptB46_Success_Spends100GoldAndTenPercentMaximumEnergy()
        {
            var state = GameState.FromParts(
                new PlayerState(100, 135, 150, 0f, 0f, 0f),
                new ProgressState(0),
                new RunState(0, true));
            var service = new EmergencyEscapeService(state);

            Assert.That(service.TrySpend(out var cost, out var failure), Is.True);
            Assert.That(failure, Is.EqualTo(EmergencyEscapePaymentFailure.None));
            Assert.That(cost.Gold, Is.EqualTo(100));
            Assert.That(cost.Energy, Is.EqualTo(14));
            Assert.That(state.Player.Gold, Is.EqualTo(50));
            Assert.That(state.Player.Energy, Is.EqualTo(86));
        }

        [TestCase(99, 100, EmergencyEscapePaymentFailure.InsufficientGold)]
        [TestCase(100, 9, EmergencyEscapePaymentFailure.InsufficientEnergy)]
        public void PromptB46_Failure_DoesNotPartiallySpend(
            int gold,
            int energy,
            EmergencyEscapePaymentFailure expected)
        {
            var state = GameState.FromParts(
                new PlayerState(energy, 100, gold, 0f, 0f, 0f),
                new ProgressState(0),
                new RunState(0, true));
            var service = new EmergencyEscapeService(state);

            Assert.That(service.TrySpend(out _, out var failure), Is.False);
            Assert.That(failure, Is.EqualTo(expected));
            Assert.That(state.Player.Gold, Is.EqualTo(gold));
            Assert.That(state.Player.Energy, Is.EqualTo(energy));
        }
    }
}
