using NUnit.Framework;
using SubTerra.App.Inventory;
using SubTerra.App.State;
using SubTerra.Shared;

namespace SubTerra.App.Tests.Progression
{
    public sealed class PromptB100GoldCommitTests
    {
        [TestCase("", 0, 20, 0)]
        [TestCase("mineral.copper", 1, 50, 1)]
        [TestCase("", 0, 0, 0)]
        [TestCase("", 0, -5, 0)]
        public void SuccessGrantsGoldWithoutCargo(string mineral, int quantity, int gold, int cargo)
        {
            var inventory = Inventory(50);
            var state = GameState.CreateNew();
            int energy = state.Player.Energy;
            var result = MiningYieldCommit.TryCommit(inventory, state, null, mineral, quantity, 1, gold);
            Assert.That(result.Succeeded, Is.True);
            Assert.That(state.Player.Gold, Is.EqualTo(System.Math.Max(0, gold)));
            Assert.That(inventory.State.GetQuantity("mineral.copper"), Is.EqualTo(cargo));
            Assert.That(state.Player.Energy, Is.EqualTo(energy - 1));
        }

        [TestCase(0f, 1, MiningCommitStatus.InventoryFull)]
        [TestCase(50f, 1000, MiningCommitStatus.InsufficientEnergy)]
        public void FailedCommitChangesNothing(float capacity, int cost, MiningCommitStatus status)
        {
            var inventory = Inventory(capacity);
            var state = GameState.CreateNew();
            int energy = state.Player.Energy;
            var result = MiningYieldCommit.TryCommit(inventory, state, null, "mineral.copper", 1, cost, 50);
            Assert.That(result.Status, Is.EqualTo(status));
            Assert.That(state.Player.Gold, Is.Zero);
            Assert.That(inventory.State.GetQuantity("mineral.copper"), Is.Zero);
            Assert.That(state.Player.Energy, Is.EqualTo(energy));
        }

        [Test]
        public void GoldSaturatesAndHudShowsAcceptedAmount()
        {
            var state = GameState.CreateNew();
            state.AddGold(int.MaxValue - 3);
            var result = MiningYieldCommit.TryCommit(Inventory(0), state, null, "", 0, 1, 20);
            Assert.That(state.Player.Gold, Is.EqualTo(int.MaxValue));
            Assert.That(result.AcceptedGold, Is.EqualTo(3));
            Assert.That(MiningYieldCommit.FormatHudFeedback("", 0, 0, 3), Is.EqualTo("골드 +3G"));
            Assert.That(MiningYieldCommit.FormatHudFeedback("mineral.copper", 1, 1, 50), Is.EqualTo("구리 +1 (+1 수확)  골드 +50G"));
        }

        private static InventoryService Inventory(float capacity)
        {
            var minerals = new InMemoryMineralCatalog();
            minerals.Register("mineral.copper", 1.5f, 10, "구리");
            return new InventoryService(minerals, capacity);
        }
    }
}
