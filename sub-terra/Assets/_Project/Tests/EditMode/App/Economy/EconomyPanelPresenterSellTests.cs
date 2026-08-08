using System.Collections.Generic;
using NUnit.Framework;
using SubTerra.App.Economy;
using SubTerra.App.Inventory;
using SubTerra.App.State;
using SubTerra.App.UI.Economy;
using SubTerra.Shared;

namespace SubTerra.App.Tests.Economy
{
    /// <summary>
    /// Surface Base 판매 패널 Presenter: 목록/선택/수량/미리보기/단건·전체 판매.
    /// 실제 EconomyService + InventoryService + 카탈로그 경로를 구동한다.
    /// </summary>
    public sealed class EconomyPanelPresenterSellTests
    {
        private const string Copper = "mineral.copper";
        private const string Iron = "mineral.iron";
        private const string Lithium = "mineral.lithium";

        private sealed class RecordingSellView : IEconomyPanelView
        {
            public string StatusMessage { get; private set; } = string.Empty;
            public string StatusDetail { get; private set; } = string.Empty;
            public int BusyTrueCount { get; private set; }
            public int BusyFalseCount { get; private set; }
            public bool LastBusy { get; private set; }
            public IReadOnlyList<SellMineralRowReadModel> Rows { get; private set; } =
                System.Array.Empty<SellMineralRowReadModel>();
            public string SelectedId { get; private set; } = string.Empty;
            public int SellQty { get; private set; }
            public int Owned { get; private set; }
            public int UnitPrice { get; private set; }
            public int QtyMin { get; private set; }
            public int QtyMax { get; private set; }
            public int PreviewCredits { get; private set; }
            public string PreviewLabel { get; private set; } = string.Empty;
            public int CreditsLabel { get; private set; } = -1;
            public bool SellSelectedEnabled { get; private set; }
            public bool SellAllEnabled { get; private set; }
            public bool IsEmpty { get; private set; }
            public string EmptyMessage { get; private set; } = string.Empty;

            public void SetStatusMessage(string message) => StatusMessage = message ?? string.Empty;
            public void SetStatusDetail(string detail) => StatusDetail = detail ?? string.Empty;

            public void SetBusy(bool busy)
            {
                LastBusy = busy;
                if (busy)
                {
                    BusyTrueCount++;
                }
                else
                {
                    BusyFalseCount++;
                }
            }

            public void SetVisible(bool visible) { }

            public void SetSellRows(IReadOnlyList<SellMineralRowReadModel> rows)
            {
                Rows = rows ?? System.Array.Empty<SellMineralRowReadModel>();
            }

            public void SetSelectedMineral(string mineralId, int sellQuantity, int owned, int unitPrice)
            {
                SelectedId = mineralId ?? string.Empty;
                SellQty = sellQuantity;
                Owned = owned;
                UnitPrice = unitPrice;
            }

            public void SetSellQuantityControls(int sellQuantity, int min, int max)
            {
                SellQty = sellQuantity;
                QtyMin = min;
                QtyMax = max;
            }

            public void SetPreviewCredits(int previewCredits, string previewLabel)
            {
                PreviewCredits = previewCredits;
                PreviewLabel = previewLabel ?? string.Empty;
            }

            public void SetCreditsLabel(int credits) => CreditsLabel = credits;

            public void SetSellActionsEnabled(bool sellSelected, bool sellAll)
            {
                SellSelectedEnabled = sellSelected;
                SellAllEnabled = sellAll;
            }

            public void SetEmptySellState(bool isEmpty, string emptyMessage)
            {
                IsEmpty = isEmpty;
                EmptyMessage = emptyMessage ?? string.Empty;
            }
        }

        private static (EconomyService economy, InventoryService inventory, GameState state, InMemoryMineralCatalog catalog)
            CreateSystem(int startGold = 0)
        {
            var catalog = new InMemoryMineralCatalog();
            catalog.Register(Copper, 1.5f, 10, "Copper");
            catalog.Register(Iron, 2f, 15, "Iron");
            catalog.Register(Lithium, 0.8f, 40, "Lithium");
            var state = GameState.CreateNew();
            if (startGold != 0)
            {
                state.SetGold(startGold);
            }

            var inventory = new InventoryService(catalog, maxCapacity: 100f, state);
            var economy = new EconomyService(inventory, catalog, state);
            return (economy, inventory, state, catalog);
        }

        [Test]
        public void RefreshSellList_OnlyOwnedPositive_ShowsUnitPriceAndDisplayName()
        {
            var (economy, inventory, state, _) = CreateSystem(startGold: 5);
            inventory.TryAddMineral(Copper, 3);
            inventory.TryAddMineral(Iron, 0); // 0 수량은 스냅샷에 없을 수 있음
            inventory.TryAddMineral(Lithium, 1);

            var view = new RecordingSellView();
            var presenter = new EconomyPanelPresenter(view);
            presenter.Bind(economy, null, inventory, state);

            Assert.That(view.Rows.Count, Is.EqualTo(2));
            Assert.That(view.Rows[0].MineralId, Is.EqualTo(Copper));
            Assert.That(view.Rows[0].DisplayName, Is.EqualTo("Copper"));
            Assert.That(view.Rows[0].OwnedQuantity, Is.EqualTo(3));
            Assert.That(view.Rows[0].UnitPrice, Is.EqualTo(10));
            Assert.That(view.Rows[1].MineralId, Is.EqualTo(Lithium));
            Assert.That(view.Rows[1].UnitPrice, Is.EqualTo(40));
            Assert.That(view.IsEmpty, Is.False);
            Assert.That(view.CreditsLabel, Is.EqualTo(5));
            Assert.That(view.SellAllEnabled, Is.True);
        }

        [Test]
        public void SelectMineral_SetsQty1_AndPreviewUnitPriceTimesQty()
        {
            var (economy, inventory, state, _) = CreateSystem();
            inventory.TryAddMineral(Copper, 5);

            var view = new RecordingSellView();
            var presenter = new EconomyPanelPresenter(view);
            presenter.Bind(economy, null, inventory, state);
            presenter.SelectMineral(Copper);

            Assert.That(presenter.SelectedMineralId, Is.EqualTo(Copper));
            Assert.That(presenter.SellQuantity, Is.EqualTo(1));
            Assert.That(view.SellQty, Is.EqualTo(1));
            Assert.That(view.PreviewCredits, Is.EqualTo(10));
            Assert.That(view.UnitPrice, Is.EqualTo(10));
            Assert.That(view.Owned, Is.EqualTo(5));
        }

        [Test]
        public void SetSellQuantity_AndAdjust_ClampToOwned()
        {
            var (economy, inventory, state, _) = CreateSystem();
            inventory.TryAddMineral(Iron, 4);

            var view = new RecordingSellView();
            var presenter = new EconomyPanelPresenter(view);
            presenter.Bind(economy, null, inventory, state);
            presenter.SelectMineral(Iron);

            presenter.SetSellQuantity(99);
            Assert.That(presenter.SellQuantity, Is.EqualTo(4));
            Assert.That(view.PreviewCredits, Is.EqualTo(4 * 15));

            presenter.SetSellQuantity(0);
            Assert.That(presenter.SellQuantity, Is.EqualTo(1));

            presenter.AdjustSellQuantity(1);
            Assert.That(presenter.SellQuantity, Is.EqualTo(2));
            presenter.AdjustSellQuantity(-10);
            Assert.That(presenter.SellQuantity, Is.EqualTo(1));
        }

        [Test]
        public void RequestSellSelected_CallsTrySellOnce_GoldAndInventoryUpdate_BusyClears()
        {
            var (economy, inventory, state, _) = CreateSystem();
            inventory.TryAddMineral(Copper, 5);

            var sellCalls = 0;
            economy.TransactionCompleted += r =>
            {
                if (r.Kind == EconomyTransactionKind.Sell && r.IsSuccess)
                {
                    sellCalls++;
                }
            };

            var view = new RecordingSellView();
            var presenter = new EconomyPanelPresenter(view);
            presenter.Bind(economy, null, inventory, state);
            presenter.SelectMineral(Copper);
            presenter.SetSellQuantity(2);

            var result = presenter.RequestSellSelected();

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(sellCalls, Is.EqualTo(1));
            Assert.That(state.Player.Gold, Is.EqualTo(20));
            Assert.That(inventory.State.GetQuantity(Copper), Is.EqualTo(3));
            Assert.That(presenter.IsBusy, Is.False);
            Assert.That(view.LastBusy, Is.False);
            Assert.That(view.BusyTrueCount, Is.GreaterThanOrEqualTo(1));
            // 목록 갱신: 아직 3개 보유
            Assert.That(view.Rows.Count, Is.EqualTo(1));
            Assert.That(view.Rows[0].OwnedQuantity, Is.EqualTo(3));
            Assert.That(view.CreditsLabel, Is.EqualTo(20));
        }

        [Test]
        public void RequestSellSelected_NoSelection_IsNoOp()
        {
            var (economy, inventory, state, _) = CreateSystem();
            inventory.TryAddMineral(Copper, 2);

            var view = new RecordingSellView();
            var presenter = new EconomyPanelPresenter(view);
            presenter.Bind(economy, null, inventory, state);

            var beforeGold = state.Player.Gold;
            var result = presenter.RequestSellSelected();

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(state.Player.Gold, Is.EqualTo(beforeGold));
            Assert.That(inventory.State.GetQuantity(Copper), Is.EqualTo(2));
        }

        [Test]
        public void RequestSellAll_MultipleStacks_OneBusySpan_AggregateMessage()
        {
            var (economy, inventory, state, _) = CreateSystem();
            inventory.TryAddMineral(Copper, 2); // 20G
            inventory.TryAddMineral(Iron, 1);   // 15G
            inventory.TryAddMineral(Lithium, 1); // 40G

            var view = new RecordingSellView();
            var presenter = new EconomyPanelPresenter(view);
            presenter.Bind(economy, null, inventory, state);

            // busy true 횟수로 단일 스팬 확인 (SellAll 1회 → BusyTrue 1회)
            var beforeBusy = view.BusyTrueCount;
            presenter.RequestSellAll();

            Assert.That(view.BusyTrueCount - beforeBusy, Is.EqualTo(1), "SellAll busy span is single");
            Assert.That(presenter.IsBusy, Is.False);
            Assert.That(state.Player.Gold, Is.EqualTo(20 + 15 + 40));
            Assert.That(inventory.State.GetQuantity(Copper), Is.EqualTo(0));
            Assert.That(inventory.State.GetQuantity(Iron), Is.EqualTo(0));
            Assert.That(inventory.State.GetQuantity(Lithium), Is.EqualTo(0));
            Assert.That(view.Rows.Count, Is.EqualTo(0));
            Assert.That(view.IsEmpty, Is.True);
            Assert.That(view.StatusMessage, Does.Contain("3종").Or.Contain("판매"));
            Assert.That(view.StatusMessage, Does.Contain("75"));
            Assert.That(view.CreditsLabel, Is.EqualTo(75));
        }

        [Test]
        public void Presenter_NeverCallsAddGoldOrTryReduceDirectly_UsesServiceOnly()
        {
            // 구조 검증은 E_S05. 여기서는 서비스 경유 결과만 확인.
            var (economy, inventory, state, _) = CreateSystem();
            inventory.TryAddMineral(Copper, 1);
            var view = new RecordingSellView();
            var presenter = new EconomyPanelPresenter(view);
            presenter.Bind(economy, null, inventory, state);
            presenter.SelectMineral(Copper);
            presenter.RequestSellSelected();
            Assert.That(state.Player.Gold, Is.EqualTo(10));
        }

        [Test]
        public void BindWithoutInventory_SkipsList_RequestSellStillWorks()
        {
            var (economy, inventory, state, _) = CreateSystem();
            inventory.TryAddMineral(Copper, 3);

            var view = new RecordingSellView();
            var presenter = new EconomyPanelPresenter(view);
            // null inventory = 목록 skip (기존 테스트 호환)
            presenter.Bind(economy, null, null, null);

            Assert.That(view.IsEmpty, Is.True);
            var result = presenter.RequestSell(Copper, 1);
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(state.Player.Gold, Is.EqualTo(10));
        }
    }
}
