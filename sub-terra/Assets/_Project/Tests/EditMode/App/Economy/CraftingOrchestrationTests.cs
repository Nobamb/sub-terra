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
    /// E-F04 설치 실패 시 차감 없음, E-F05 중복 요청 1건, 성공 시 이벤트 1회.
    /// </summary>
    public sealed class CraftingOrchestrationTests
    {
        private const string Copper = "mineral.copper";
        private const string Iron = "mineral.iron";
        private const string SupportBuilding = "building.support.basic";

        private sealed class PlacementStub : IBuildingPlacementGate
        {
            public bool NextResult { get; set; } = true;
            public int CallCount { get; private set; }
            public string LastBuildingId { get; private set; }

            public bool TryPlace(string buildingId)
            {
                CallCount++;
                LastBuildingId = buildingId;
                return NextResult;
            }
        }

        private sealed class RecordingEconomyView : IEconomyPanelView
        {
            public string StatusMessage { get; private set; } = string.Empty;
            public string StatusDetail { get; private set; } = string.Empty;
            public int BusyTrueCount { get; private set; }
            public int MessageCount { get; private set; }

            public void SetStatusMessage(string message)
            {
                StatusMessage = message ?? string.Empty;
                MessageCount++;
            }

            public void SetStatusDetail(string detail)
            {
                StatusDetail = detail ?? string.Empty;
            }

            public void SetBusy(bool busy)
            {
                if (busy)
                {
                    BusyTrueCount++;
                }
            }

            public void SetVisible(bool visible) { }
        }

        private sealed class CallbackPlacement : IBuildingPlacementGate
        {
            private readonly System.Func<string, bool> callback;

            public CallbackPlacement(System.Func<string, bool> callback)
            {
                this.callback = callback;
            }

            public bool TryPlace(string buildingId) => callback(buildingId);
        }

        private static (EconomyService economy, InventoryService inventory, GameState state, CraftingService crafting)
            CreateCraftSystem()
        {
            var catalog = new InMemoryMineralCatalog();
            catalog.Register(Copper, 1.5f, 10, "Copper");
            catalog.Register(Iron, 2f, 15, "Iron");
            var state = GameState.CreateNew();
            var inventory = new InventoryService(catalog, 100f, state);
            var economy = new EconomyService(inventory, catalog, state);
            var crafting = new CraftingService(economy);
            return (economy, inventory, state, crafting);
        }

        [Test]
        public void E_F04_PlacementFailure_DoesNotSpend_InventoryUnchanged()
        {
            var (economy, inventory, state, crafting) = CreateCraftSystem();
            inventory.TryAddMineral(Copper, 10);
            inventory.TryAddMineral(Iron, 10);

            var costs = new List<ItemCostDto>
            {
                new ItemCostDto(Copper, 5),
                new ItemCostDto(Iron, 5)
            };

            var placement = new PlacementStub { NextResult = false };
            var spendSave = 0;
            economy.AutoSaveRequested += _ => spendSave++;

            var beforeFp = inventory.State.CaptureFingerprint();
            var beforeGold = state.Player.Gold;

            var result = crafting.TryCraftBuilding(SupportBuilding, costs, placement);

            Assert.That(result.Status, Is.EqualTo(EconomyTransactionStatus.PlacementFailed));
            Assert.That(placement.CallCount, Is.EqualTo(1));
            Assert.That(inventory.State.CaptureFingerprint().Equals(beforeFp), Is.True);
            Assert.That(state.Player.Gold, Is.EqualTo(beforeGold));
            Assert.That(spendSave, Is.Zero, "배치 실패 시 AutoSave/차감 없음");
            Assert.That(inventory.State.GetQuantity(Copper), Is.EqualTo(10));
            Assert.That(inventory.State.GetQuantity(Iron), Is.EqualTo(10));
        }

        [Test]
        public void E_F04_PlacementSuccess_ThenSpend_DeductsOnce()
        {
            var (economy, inventory, _, crafting) = CreateCraftSystem();
            inventory.TryAddMineral(Copper, 10);
            inventory.TryAddMineral(Iron, 10);

            var costs = new List<ItemCostDto>
            {
                new ItemCostDto(Copper, 5),
                new ItemCostDto(Iron, 5)
            };

            var placement = new PlacementStub { NextResult = true };
            var saveCount = 0;
            economy.AutoSaveRequested += _ => saveCount++;

            var result = crafting.TryCraftBuilding(SupportBuilding, costs, placement);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Status, Is.EqualTo(EconomyTransactionStatus.Success));
            Assert.That(placement.CallCount, Is.EqualTo(1));
            Assert.That(inventory.State.GetQuantity(Copper), Is.EqualTo(5));
            Assert.That(inventory.State.GetQuantity(Iron), Is.EqualTo(5));
            Assert.That(saveCount, Is.EqualTo(1));
        }

        [Test]
        public void E_F05_SequentialDuplicateSell_OnlyOneFullStackApplied()
        {
            var (economy, inventory, state, crafting) = CreateCraftSystem();
            inventory.TryAddMineral(Copper, 4);

            var view = new RecordingEconomyView();
            var presenter = new EconomyPanelPresenter(view);
            presenter.Bind(economy, crafting);

            var saveCount = 0;
            economy.AutoSaveRequested += _ => saveCount++;

            // 같은 스택을 연속 판매 — 첫 건만 성공, 두 번째는 부족.
            var r1 = presenter.RequestSell(Copper, 4);
            var r2 = presenter.RequestSell(Copper, 4);

            Assert.That(r1.IsSuccess, Is.True);
            Assert.That(r2.Status, Is.EqualTo(EconomyTransactionStatus.InsufficientResources));
            Assert.That(inventory.State.GetQuantity(Copper), Is.EqualTo(0));
            Assert.That(state.Player.Gold, Is.EqualTo(4 * 10));
            Assert.That(saveCount, Is.EqualTo(1), "성공 거래 자동 저장은 1회만");
        }

        [Test]
        public void E_F05_PresenterReentryDuringSell_BlockedAsBusy()
        {
            var (economy, inventory, _, crafting) = CreateCraftSystem();
            inventory.TryAddMineral(Copper, 5);

            var view = new RecordingEconomyView();
            var presenter = new EconomyPanelPresenter(view);
            presenter.Bind(economy, crafting);

            EconomyTransactionResult nestedResult = default;
            var nestedCalled = false;

            // TrySell 성공 콜백 시점에 Presenter는 아직 busy=true 이므로 재진입은 Busy.
            economy.TransactionCompleted += result =>
            {
                if (nestedCalled || !result.IsSuccess)
                {
                    return;
                }

                nestedCalled = true;
                nestedResult = presenter.RequestSell(Copper, 1);
            };

            var outer = presenter.RequestSell(Copper, 2);
            Assert.That(outer.IsSuccess, Is.True);
            Assert.That(nestedCalled, Is.True);
            Assert.That(nestedResult.Status, Is.EqualTo(EconomyTransactionStatus.Busy));
            // 외곽 판매 2개만 반영
            Assert.That(inventory.State.GetQuantity(Copper), Is.EqualTo(3));
        }

        [Test]
        public void Craft_InsufficientBeforePlacement_DoesNotPlace()
        {
            var (_, inventory, _, crafting) = CreateCraftSystem();
            inventory.TryAddMineral(Copper, 1);

            var costs = new List<ItemCostDto> { new ItemCostDto(Copper, 5) };
            var placement = new PlacementStub { NextResult = true };

            var result = crafting.TryCraftBuilding(SupportBuilding, costs, placement);

            Assert.That(result.Status, Is.EqualTo(EconomyTransactionStatus.InsufficientResources));
            Assert.That(placement.CallCount, Is.Zero, "자원 부족 시 배치 시도 안 함");
            Assert.That(inventory.State.GetQuantity(Copper), Is.EqualTo(1));
        }

        [Test]
        public void Craft_BusyReentry_SecondRejected()
        {
            var (economy, inventory, _, crafting) = CreateCraftSystem();
            inventory.TryAddMineral(Copper, 10);

            var costs = new List<ItemCostDto> { new ItemCostDto(Copper, 1) };
            var reentryCount = 0;
            IBuildingPlacementGate gate = null;
            gate = new CallbackPlacement(_ =>
            {
                reentryCount++;
                var nested = crafting.TryCraftBuilding(SupportBuilding, costs, gate);
                Assert.That(nested.Status, Is.EqualTo(EconomyTransactionStatus.Busy));
                return true;
            });

            var result = crafting.TryCraftBuilding(SupportBuilding, costs, gate);
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(reentryCount, Is.EqualTo(1));
            Assert.That(inventory.State.GetQuantity(Copper), Is.EqualTo(9));
        }

        [Test]
        public void Failure_DoesNotRaiseAutoSave_SuccessRaisesOnce()
        {
            var (economy, inventory, _, crafting) = CreateCraftSystem();
            inventory.TryAddMineral(Copper, 5);

            var saves = 0;
            economy.AutoSaveRequested += _ => saves++;

            var costs = new List<ItemCostDto> { new ItemCostDto(Copper, 99) };
            crafting.TryCraftBuilding(SupportBuilding, costs, new PlacementStub());
            Assert.That(saves, Is.Zero);

            var okCosts = new List<ItemCostDto> { new ItemCostDto(Copper, 1) };
            crafting.TryCraftBuilding(SupportBuilding, okCosts, new PlacementStub { NextResult = true });
            Assert.That(saves, Is.EqualTo(1));
        }
    }
}
