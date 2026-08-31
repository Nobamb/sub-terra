using NUnit.Framework;
using SubTerra.App.Inventory;
using SubTerra.App.Run;
using SubTerra.App.State;
using SubTerra.App.UI.EmergencyRescue;

namespace SubTerra.App.Tests.Run
{
    public sealed class EmergencyRescueServiceTests
    {
        [Test]
        public void PromptB81_Cost_UsesCappedGoldAndRoundedEightyPercentCargo()
        {
            Fixture fixture = CreateFixture(500, 10, 15, 7);
            EmergencyRescueCost cost = fixture.Service.GetCurrentCost();

            Assert.That(cost.GoldCharged, Is.EqualTo(250));
            Assert.That(cost.GoldAfter, Is.EqualTo(250));
            Assert.That(cost.Minerals.Count, Is.EqualTo(3));
            Assert.That(Find(cost, "mineral.copper").Charged, Is.EqualTo(8));
            Assert.That(Find(cost, "mineral.iron").Charged, Is.EqualTo(12));
            Assert.That(Find(cost, "mineral.lithium").Charged, Is.EqualTo(6));

            string display = EmergencyRescuePanelView.FormatCost(cost);
            Assert.That(display, Does.Contain("골드 250G  (500→250)"));
            Assert.That(display, Does.Contain("구리 8  (10→2)"));
        }

        [Test]
        public void PromptB81_Rescue_ChargesOnlyAvailableGoldAndPlayerCargo()
        {
            Fixture fixture = CreateFixture(120, 10, 15, 7);

            Assert.That(
                fixture.Service.TryRescue(out EmergencyRescueCost charged, out var failure),
                Is.True);
            Assert.That(failure, Is.EqualTo(EmergencyRescueFailure.None));
            Assert.That(charged.GoldCharged, Is.EqualTo(120));
            Assert.That(fixture.State.Player.Gold, Is.Zero);
            Assert.That(fixture.Inventory.State.GetQuantity("mineral.copper"), Is.EqualTo(2));
            Assert.That(fixture.Inventory.State.GetQuantity("mineral.iron"), Is.EqualTo(3));
            Assert.That(fixture.Inventory.State.GetQuantity("mineral.lithium"), Is.EqualTo(1));
            Assert.That(fixture.State.Run.LifecyclePhase, Is.EqualTo(RunLifecyclePhase.Active));
            Assert.That(fixture.State.Player.Energy, Is.Zero);
        }

        [Test]
        public void PromptB81_ZeroAssets_RescueIsFree()
        {
            Fixture fixture = CreateFixture(0, 0, 0, 0);

            Assert.That(fixture.Service.GetCurrentCost().IsFree, Is.True);
            Assert.That(fixture.Service.TryRescue(out var charged, out var failure), Is.True);
            Assert.That(charged.IsFree, Is.True);
            Assert.That(failure, Is.EqualTo(EmergencyRescueFailure.None));
        }

        [Test]
        public void PromptB81_EnergyAvailable_DisablesRescueWithoutCharging()
        {
            Fixture fixture = CreateFixture(500, 10, 15, 7);
            fixture.State.SetCurrentEnergy(1);
            InventoryFingerprint before = fixture.Inventory.State.CaptureFingerprint();

            Assert.That(fixture.Service.IsAvailable, Is.False);
            Assert.That(fixture.Service.TryRescue(out _, out var failure), Is.False);
            Assert.That(failure, Is.EqualTo(EmergencyRescueFailure.EnergyAvailable));
            Assert.That(fixture.State.Player.Gold, Is.EqualTo(500));
            Assert.That(fixture.Inventory.State.CaptureFingerprint(), Is.EqualTo(before));
        }

        [TestCase(1, 1)]
        [TestCase(2, 2)]
        [TestCase(3, 2)]
        [TestCase(4, 3)]
        [TestCase(5, 4)]
        public void PromptB81_MineralLoss_RoundsHalfUp(int quantity, int expected)
        {
            Assert.That(EmergencyRescueService.CalculateMineralLoss(quantity), Is.EqualTo(expected));
        }

        private static EmergencyRescueMineralCost Find(EmergencyRescueCost cost, string mineralId)
        {
            for (var i = 0; i < cost.Minerals.Count; i++)
            {
                if (cost.Minerals[i].MineralId == mineralId)
                {
                    return cost.Minerals[i];
                }
            }

            Assert.Fail("Missing mineral cost: " + mineralId);
            return default;
        }

        private static Fixture CreateFixture(int gold, int copper, int iron, int lithium)
        {
            var catalog = new InMemoryMineralCatalog();
            catalog.Register("mineral.copper", 1f, 10, "구리");
            catalog.Register("mineral.iron", 1f, 10, "철");
            catalog.Register("mineral.lithium", 1f, 10, "리튬");
            GameState state = GameState.CreateNew();
            state.BeginRun();
            state.SetGold(gold);
            state.SetCurrentEnergy(0);
            var inventory = new InventoryService(catalog, 1000f, state);
            if (copper > 0) inventory.TryAddMineral("mineral.copper", copper);
            if (iron > 0) inventory.TryAddMineral("mineral.iron", iron);
            if (lithium > 0) inventory.TryAddMineral("mineral.lithium", lithium);
            return new Fixture(state, inventory, new EmergencyRescueService(state, inventory));
        }

        private sealed class Fixture
        {
            public GameState State { get; }
            public InventoryService Inventory { get; }
            public EmergencyRescueService Service { get; }

            public Fixture(GameState state, InventoryService inventory, EmergencyRescueService service)
            {
                State = state;
                Inventory = inventory;
                Service = service;
            }
        }
    }
}
