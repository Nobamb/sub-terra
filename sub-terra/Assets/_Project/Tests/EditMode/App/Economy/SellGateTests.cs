using NUnit.Framework;
using SubTerra.App.Economy;
using SubTerra.App.Inventory;
using SubTerra.App.State;
using SubTerra.Shared;

namespace SubTerra.App.Tests.Economy
{
    /// <summary>PR-4 ISellGate 매트릭스: null allow / false deny / true allow.</summary>
    public sealed class SellGateTests
    {
        private const string Copper = "mineral.copper";

        private static (EconomyService economy, InventoryService inventory, GameState state)
            Create(ISellGate gate)
        {
            var catalog = new InMemoryMineralCatalog();
            catalog.Register(Copper, 1f, 10, "Copper");
            var state = GameState.CreateNew();
            var inventory = new InventoryService(catalog, 50f, state);
            inventory.TryAddMineral(Copper, 5);
            var economy = new EconomyService(inventory, catalog, state, gate);
            return (economy, inventory, state);
        }

        [Test]
        public void NullGate_AllowsSell()
        {
            var (economy, inventory, state) = Create(null);
            var result = economy.TrySellMineral(Copper, 1);
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(state.Player.Gold, Is.EqualTo(10));
            Assert.That(inventory.State.GetQuantity(Copper), Is.EqualTo(4));
        }

        [Test]
        public void GateFalse_DeniesSell_ClearMessage_NoMutation()
        {
            var gate = new SceneSellGate { IsSellAllowed = false };
            var (economy, inventory, state) = Create(gate);
            var beforeFp = inventory.State.CaptureFingerprint();

            var result = economy.TrySellMineral(Copper, 1);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Status, Is.EqualTo(EconomyTransactionStatus.InvalidRequest));
            Assert.That(result.UserMessage, Does.Contain("Surface Base"));
            Assert.That(state.Player.Gold, Is.EqualTo(0));
            Assert.That(inventory.State.CaptureFingerprint().Equals(beforeFp), Is.True);
        }

        [Test]
        public void GateTrue_AllowsSell()
        {
            var gate = new SceneSellGate { IsSellAllowed = true };
            var (economy, inventory, state) = Create(gate);

            var result = economy.TrySellMineral(Copper, 2);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(state.Player.Gold, Is.EqualTo(20));
            Assert.That(inventory.State.GetQuantity(Copper), Is.EqualTo(3));
        }

        [Test]
        public void GateToggle_ViaInterfaceSetter_Only()
        {
            ISellGate gate = new SceneSellGate { IsSellAllowed = false };
            var (economy, _, state) = Create(gate);

            Assert.That(economy.TrySellMineral(Copper, 1).IsSuccess, Is.False);

            // concrete cast 없이 인터페이스 setter로 토글
            gate.IsSellAllowed = true;
            Assert.That(economy.TrySellMineral(Copper, 1).IsSuccess, Is.True);
            Assert.That(state.Player.Gold, Is.EqualTo(10));
        }
    }
}
