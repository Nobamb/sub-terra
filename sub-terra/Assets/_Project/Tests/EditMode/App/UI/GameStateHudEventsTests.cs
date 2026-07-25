using NUnit.Framework;
using SubTerra.App.State;

namespace SubTerra.App.Tests.UI
{
    public sealed class GameStateHudEventsTests
    {
        [Test]
        public void CreateNew_HasHudSafeDefaults()
        {
            var state = GameState.CreateNew();
            Assert.That(state.Player.Energy, Is.EqualTo(100));
            Assert.That(state.Player.MaxEnergy, Is.EqualTo(100));
            Assert.That(state.Player.Gold, Is.Zero);
            Assert.That(state.Player.Cargo, Is.Zero);
            Assert.That(state.Player.UnsettledValue, Is.Zero);
            Assert.That(state.Run.Depth, Is.Zero);
            Assert.That(state.Run.StructuralRisk, Is.EqualTo(StructuralRiskLevel.Safe));
            Assert.That(state.Run.GasExposure, Is.EqualTo(GasRiskLevel.Safe));
            Assert.That(state.SelectedBuildingId, Is.Empty);
            Assert.That(state.InteractionPrompt, Is.Empty);
        }

        [Test]
        public void IntentSetters_RaiseEventsWithCurrentPayload()
        {
            var state = GameState.CreateNew();
            EnergyReadModel? energy = null;
            int? gold = null;
            InventoryReadModel? inv = null;
            int? depth = null;
            StructuralRiskLevel? structural = null;
            GasRiskLevel? gas = null;
            BuildingSelectionReadModel? building = null;
            string interaction = null;

            state.EnergyChanged += m => energy = m;
            state.CreditsChanged += g => gold = g;
            state.InventoryChanged += i => inv = i;
            state.DepthChanged += d => depth = d;
            state.StructuralRiskChanged += s => structural = s;
            state.GasExposureChanged += g => gas = g;
            state.BuildingSelectionChanged += b => building = b;
            state.InteractionPromptChanged += p => interaction = p;

            state.SetEnergy(30, 80);
            state.SetGold(12);
            state.SetInventory(1.5f, 9f);
            state.SetDepth(4);
            state.SetStructuralRisk(StructuralRiskLevel.Critical);
            state.SetGasExposure(GasRiskLevel.Hazard);
            state.SetBuildingSelection("building.support.basic", "버팀목");
            state.SetInteractionPrompt("설치");

            Assert.That(energy.HasValue, Is.True);
            Assert.That(energy.Value.Current, Is.EqualTo(30));
            Assert.That(energy.Value.Max, Is.EqualTo(80));
            Assert.That(gold, Is.EqualTo(12));
            Assert.That(inv.HasValue, Is.True);
            Assert.That(inv.Value.CargoWeight, Is.EqualTo(1.5f));
            Assert.That(inv.Value.UnsettledValue, Is.EqualTo(9f));
            Assert.That(depth, Is.EqualTo(4));
            Assert.That(structural, Is.EqualTo(StructuralRiskLevel.Critical));
            Assert.That(gas, Is.EqualTo(GasRiskLevel.Hazard));
            Assert.That(building.HasValue, Is.True);
            Assert.That(building.Value.BuildingId, Is.EqualTo("building.support.basic"));
            Assert.That(interaction, Is.EqualTo("설치"));
        }

        [Test]
        public void SameValue_DoesNotRaiseAnyHudEvent()
        {
            var state = GameState.CreateNew();
            var count = 0;
            state.EnergyChanged += _ => count++;
            state.CreditsChanged += _ => count++;
            state.InventoryChanged += _ => count++;
            state.DepthChanged += _ => count++;
            state.StructuralRiskChanged += _ => count++;
            state.GasExposureChanged += _ => count++;
            state.BuildingSelectionChanged += _ => count++;
            state.InteractionPromptChanged += _ => count++;

            state.SetEnergy(100, 100);
            state.SetGold(0);
            state.AddGold(0);
            state.SetCargoWeight(0f);
            state.SetUnsettledValue(0f);
            state.SetDepth(0);
            state.SetStructuralRisk(StructuralRiskLevel.Safe);
            state.SetGasExposure(GasRiskLevel.Safe);
            state.SetBuildingSelection(string.Empty, string.Empty);
            state.SetInteractionPrompt(string.Empty);

            Assert.That(count, Is.Zero);
        }

        [Test]
        public void AddGold_ClampsAtZeroAndRaisesOnlyWhenChanged()
        {
            var state = GameState.CreateNew();
            var events = 0;
            state.CreditsChanged += _ => events++;

            state.AddGold(5);
            Assert.That(state.Player.Gold, Is.EqualTo(5));
            Assert.That(events, Is.EqualTo(1));

            state.AddGold(-100);
            Assert.That(state.Player.Gold, Is.Zero);
            Assert.That(events, Is.EqualTo(2));

            state.AddGold(-1);
            Assert.That(events, Is.EqualTo(2));
        }
    }
}
