using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.Economy;
using SubTerra.App.Inventory;
using SubTerra.App.State;
using SubTerra.App.UI.Economy;

namespace SubTerra.App.Tests.Economy
{
    public sealed class EngineFuelSellTests
    {
        private const string Copper = DataIds.Minerals.Copper;
        private const string EngineFuel = DataIds.RareItems.EngineFuel;

        [Test]
        public void SelectedSell_PaysOneHundredGold_AndKeepsRareLabel()
        {
            var (economy, inventory, state, presenter, view) = CreateBound();
            inventory.TryAddMineral(EngineFuel, 1);
            presenter.RefreshSellList();
            presenter.SelectMineral(EngineFuel);

            Assert.That(view.Rows.Count, Is.EqualTo(1));
            Assert.That(view.Rows[0].IsRare, Is.True);
            Assert.That(view.Rows[0].DisplayName, Does.Contain("희귀"));
            Assert.That(view.Rows[0].UnitPrice, Is.EqualTo(100));
            Assert.That(view.SellAllEnabled, Is.False);

            var result = presenter.RequestSellSelected();
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(state.Player.Gold, Is.EqualTo(100));
            Assert.That(inventory.State.GetQuantity(EngineFuel), Is.EqualTo(0));
        }

        [Test]
        public void SellAll_SkipsEngineFuel_AndSellsMineralsOnly()
        {
            var (economy, inventory, state, presenter, view) = CreateBound();
            inventory.TryAddMineral(Copper, 2);
            inventory.TryAddMineral(EngineFuel, 1);
            presenter.RefreshSellList();
            presenter.RequestSellAll();

            Assert.That(state.Player.Gold, Is.EqualTo(20));
            Assert.That(inventory.State.GetQuantity(Copper), Is.EqualTo(0));
            Assert.That(inventory.State.GetQuantity(EngineFuel), Is.EqualTo(1));
            Assert.That(view.Rows.Count, Is.EqualTo(1));
            Assert.That(view.Rows[0].MineralId, Is.EqualTo(EngineFuel));
            Assert.That(view.Rows[0].IsRare, Is.True);
        }

        private static (
            EconomyService economy,
            InventoryService inventory,
            GameState state,
            EconomyPanelPresenter presenter,
            RecordingView view)
            CreateBound()
        {
            var catalog = new InMemoryMineralCatalog();
            catalog.Register(Copper, 1.5f, 10, "구리");
            catalog.Register(EngineFuel, 1f, 100, "엔진 연료");
            var state = GameState.CreateNew();
            var inventory = new InventoryService(catalog, maxCapacity: 100f, state);
            var economy = new EconomyService(inventory, catalog, state);
            var view = new RecordingView();
            var presenter = new EconomyPanelPresenter(view);
            presenter.Bind(economy, null, inventory, state);
            return (economy, inventory, state, presenter, view);
        }

        private sealed class RecordingView : IEconomyPanelView
        {
            public System.Collections.Generic.IReadOnlyList<SellMineralRowReadModel> Rows { get; private set; } =
                System.Array.Empty<SellMineralRowReadModel>();
            public bool SellAllEnabled { get; private set; }

            public void SetStatusMessage(string message) { }
            public void SetStatusDetail(string detail) { }
            public void SetBusy(bool busy) { }
            public void SetVisible(bool visible) { }
            public void SetSellRows(System.Collections.Generic.IReadOnlyList<SellMineralRowReadModel> rows)
            {
                Rows = rows ?? System.Array.Empty<SellMineralRowReadModel>();
            }

            public void SetEmptySellState(bool isEmpty, string message) { }
            public void SetSelectedMineral(string mineralId, int sellQuantity, int owned, int unitPrice) { }
            public void SetSellQuantityControls(int sellQuantity, int min, int max) { }
            public void SetPreviewCredits(int credits, string label) { }
            public void SetCreditsLabel(int gold) { }
            public void SetSellActionsEnabled(bool sellSelected, bool sellAll)
            {
                SellAllEnabled = sellAll;
            }
        }
    }
}
