using System.Collections.Generic;
using NUnit.Framework;
using SubTerra.App.Economy;
using SubTerra.App.Inventory;
using SubTerra.App.State;
using SubTerra.Shared;

namespace SubTerra.App.Tests.Economy
{
    /// <summary>
    /// E-F01~F03, 오버플로·이벤트 횟수. 실제 EconomyService + InventoryService + 카탈로그 경로.
    /// </summary>
    public sealed class EconomyServiceTests
    {
        private const string Copper = "mineral.copper";
        private const string Iron = "mineral.iron";
        private const string Lithium = "mineral.lithium";

        private static InMemoryMineralCatalog CreateMvpCatalog()
        {
            var catalog = new InMemoryMineralCatalog();
            // unitPrice: copper 10, iron 15, lithium 40 — 판매 골드는 이 값만 사용
            catalog.Register(Copper, 1.5f, 10, "Copper");
            catalog.Register(Iron, 2f, 15, "Iron");
            catalog.Register(Lithium, 0.8f, 40, "Lithium");
            return catalog;
        }

        private static (EconomyService economy, InventoryService inventory, GameState state, InMemoryMineralCatalog catalog)
            CreateSystem(int startGold = 0)
        {
            var catalog = CreateMvpCatalog();
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
        public void E_F01_PartialSell_OnlySelectedMineralDecreases_GoldIsCatalogPriceTimesQty()
        {
            var (economy, inventory, state, _) = CreateSystem();
            inventory.TryAddMineral(Copper, 5);
            inventory.TryAddMineral(Iron, 3);

            var txCount = 0;
            var saveCount = 0;
            EconomyTransactionResult last = default;
            economy.TransactionCompleted += r =>
            {
                txCount++;
                last = r;
            };
            economy.AutoSaveRequested += _ => saveCount++;

            // 구리 2만 판매. 단가 10 → 골드 +20. 철은 유지.
            var result = economy.TrySellMineral(Copper, 2);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Status, Is.EqualTo(EconomyTransactionStatus.Success));
            Assert.That(result.GoldDelta, Is.EqualTo(2 * 10));
            Assert.That(inventory.State.GetQuantity(Copper), Is.EqualTo(3));
            Assert.That(inventory.State.GetQuantity(Iron), Is.EqualTo(3));
            Assert.That(state.Player.Gold, Is.EqualTo(20));
            Assert.That(txCount, Is.EqualTo(1));
            Assert.That(saveCount, Is.EqualTo(1));
            Assert.That(last.Kind, Is.EqualTo(EconomyTransactionKind.Sell));
            Assert.That(last.ChangedItemId, Is.EqualTo(Copper));
            Assert.That(last.ChangedQuantity, Is.EqualTo(2));
        }

        [Test]
        public void E_F02_CanAfford_NoMutation_ThenTrySpend_DeductsAllCostsOnce()
        {
            var (economy, inventory, state, _) = CreateSystem();
            inventory.TryAddMineral(Copper, 10);
            inventory.TryAddMineral(Iron, 5);

            var costs = new List<ItemCostDto>
            {
                new ItemCostDto(Copper, 3),
                new ItemCostDto(Iron, 2)
            };

            var invEvents = 0;
            var goldEvents = 0;
            var saveCount = 0;
            inventory.InventoryChanged += _ => invEvents++;
            state.CreditsChanged += _ => goldEvents++;
            economy.AutoSaveRequested += _ => saveCount++;

            var beforeCopper = inventory.State.GetQuantity(Copper);
            var beforeIron = inventory.State.GetQuantity(Iron);
            var beforeGold = state.Player.Gold;
            var beforeFp = inventory.State.CaptureFingerprint();

            // CanAfford: 상태 불변
            Assert.That(economy.CanAfford(costs), Is.True);
            Assert.That(inventory.State.CaptureFingerprint().Equals(beforeFp), Is.True);
            Assert.That(state.Player.Gold, Is.EqualTo(beforeGold));
            Assert.That(invEvents, Is.Zero);
            Assert.That(goldEvents, Is.Zero);
            Assert.That(saveCount, Is.Zero);

            // TrySpend: 복수 비용 일괄 차감, 이벤트 1회(인벤)
            invEvents = 0;
            Assert.That(economy.TrySpend(costs), Is.True);
            Assert.That(inventory.State.GetQuantity(Copper), Is.EqualTo(beforeCopper - 3));
            Assert.That(inventory.State.GetQuantity(Iron), Is.EqualTo(beforeIron - 2));
            Assert.That(state.Player.Gold, Is.EqualTo(beforeGold));
            Assert.That(invEvents, Is.EqualTo(1), "일괄 차감 시 InventoryChanged 1회");
            Assert.That(saveCount, Is.EqualTo(1));
        }

        [Test]
        public void E_F03_InsufficientOneMineral_TrySpendFalse_AllStacksAndGoldUnchanged()
        {
            var (economy, inventory, state, _) = CreateSystem(startGold: 50);
            inventory.TryAddMineral(Copper, 10);
            inventory.TryAddMineral(Iron, 1); // iron 부족

            var costs = new List<ItemCostDto>
            {
                new ItemCostDto(Copper, 3),
                new ItemCostDto(Iron, 5)
            };

            var beforeFp = inventory.State.CaptureFingerprint();
            var beforeGold = state.Player.Gold;
            var invEvents = 0;
            var saveCount = 0;
            inventory.InventoryChanged += _ => invEvents++;
            economy.AutoSaveRequested += _ => saveCount++;

            Assert.That(economy.CanAfford(costs), Is.False);
            Assert.That(economy.TrySpend(costs), Is.False);
            Assert.That(inventory.State.CaptureFingerprint().Equals(beforeFp), Is.True);
            Assert.That(state.Player.Gold, Is.EqualTo(beforeGold));
            Assert.That(invEvents, Is.Zero);
            Assert.That(saveCount, Is.Zero);
            Assert.That(economy.LastResult.Status, Is.EqualTo(EconomyTransactionStatus.InsufficientResources));
        }

        [Test]
        public void E_S02_SellPrice_UsesCatalogOnly_NotCallerPrice()
        {
            var (economy, inventory, state, catalog) = CreateSystem();
            inventory.TryAddMineral(Copper, 4);

            // 호출자는 수량을 넘길 뿐 단가를 넘기지 않는다. 카탈로그 10 * 3 = 30.
            var result = economy.TrySellMineral(Copper, 3);
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.GoldDelta, Is.EqualTo(3 * 10));
            Assert.That(state.Player.Gold, Is.EqualTo(30));

            // 카탈로그 단가를 바꾼 뒤 추가 판매하면 새 단가가 적용된다(UI 가격 필드 없음).
            catalog.Register(Copper, 1.5f, 99, "Copper");
            inventory.TryAddMineral(Copper, 1);
            var second = economy.TrySellMineral(Copper, 1);
            Assert.That(second.GoldDelta, Is.EqualTo(99));
            Assert.That(state.Player.Gold, Is.EqualTo(30 + 99));
        }

        [Test]
        public void E_S04_DuplicateCostIds_AreSummedBeforeCheck()
        {
            var (economy, inventory, _, _) = CreateSystem();
            inventory.TryAddMineral(Copper, 5);

            // 동일 ID가 목록에 여러 번: 2+3=5 합산 후 검사·차감.
            var costs = new List<ItemCostDto>
            {
                new ItemCostDto(Copper, 2),
                new ItemCostDto(Copper, 3)
            };

            Assert.That(economy.CanAfford(costs), Is.True);
            Assert.That(economy.TrySpend(costs), Is.True);
            Assert.That(inventory.State.GetQuantity(Copper), Is.EqualTo(0));

            // 합산 후 부족이면 전부 거부
            inventory.TryAddMineral(Copper, 4);
            var over = new List<ItemCostDto>
            {
                new ItemCostDto(Copper, 2),
                new ItemCostDto(Copper, 3)
            };
            var fp = inventory.State.CaptureFingerprint();
            Assert.That(economy.CanAfford(over), Is.False);
            Assert.That(economy.TrySpend(over), Is.False);
            Assert.That(inventory.State.CaptureFingerprint().Equals(fp), Is.True);
        }

        [Test]
        public void Sell_InvalidAndOverflow_LeaveStateUnchanged_NoAutoSave()
        {
            var (economy, inventory, state, catalog) = CreateSystem(startGold: 7);
            inventory.TryAddMineral(Copper, 2);
            var fp = inventory.State.CaptureFingerprint();
            var gold = state.Player.Gold;
            var saveCount = 0;
            economy.AutoSaveRequested += _ => saveCount++;

            Assert.That(economy.TrySellMineral(string.Empty, 1).Status,
                Is.EqualTo(EconomyTransactionStatus.InvalidRequest));
            Assert.That(economy.TrySellMineral(Copper, 0).Status,
                Is.EqualTo(EconomyTransactionStatus.InvalidRequest));
            Assert.That(economy.TrySellMineral(Copper, -1).Status,
                Is.EqualTo(EconomyTransactionStatus.InvalidRequest));
            Assert.That(economy.TrySellMineral("mineral.unknown", 1).Status,
                Is.EqualTo(EconomyTransactionStatus.InvalidRequest));
            Assert.That(economy.TrySellMineral(Copper, 99).Status,
                Is.EqualTo(EconomyTransactionStatus.InsufficientResources));

            // 골드 오버플로: 잔고를 Max-5, 단가 큰 카탈로그로 판매 시도
            catalog.Register(Copper, 1f, 100, "Copper");
            state.SetGold(int.MaxValue - 5);
            inventory.TryAddMineral(Copper, 1); // 이미 2 있었으면 3
            // 현재 copper 수량 확보 후 1 판매 시 100 골드 필요 → overflow
            var owned = inventory.State.GetQuantity(Copper);
            Assert.That(owned, Is.GreaterThanOrEqualTo(1));
            var overflow = economy.TrySellMineral(Copper, 1);
            Assert.That(overflow.Status, Is.EqualTo(EconomyTransactionStatus.GoldOverflow));
            Assert.That(state.Player.Gold, Is.EqualTo(int.MaxValue - 5));

            Assert.That(saveCount, Is.Zero);
            // 실패 판매로 구리가 줄지 않았는지: overflow 전에 invalid들은 불변, overflow도 불변
            Assert.That(inventory.State.GetQuantity(Copper), Is.EqualTo(owned));
        }

        [Test]
        public void TrySpend_NegativeEmptyOverflowCosts_RejectedWithoutMutation()
        {
            var (economy, inventory, state, _) = CreateSystem();
            inventory.TryAddMineral(Copper, 10);
            var fp = inventory.State.CaptureFingerprint();

            Assert.That(economy.TrySpend(null), Is.False);
            Assert.That(economy.TrySpend(new List<ItemCostDto>
            {
                new ItemCostDto(string.Empty, 1)
            }), Is.False);
            Assert.That(economy.TrySpend(new List<ItemCostDto>
            {
                new ItemCostDto(Copper, 0)
            }), Is.False);
            Assert.That(economy.TrySpend(new List<ItemCostDto>
            {
                new ItemCostDto(Copper, -2)
            }), Is.False);

            // 합산 오버플로
            Assert.That(economy.TrySpend(new List<ItemCostDto>
            {
                new ItemCostDto(Copper, int.MaxValue),
                new ItemCostDto(Copper, 1)
            }), Is.False);

            Assert.That(inventory.State.CaptureFingerprint().Equals(fp), Is.True);
            Assert.That(state.Player.Gold, Is.EqualTo(0));
        }

        [Test]
        public void CostAggregator_MergesDuplicateIds()
        {
            var costs = new List<ItemCostDto>
            {
                new ItemCostDto(Copper, 1),
                new ItemCostDto(Iron, 2),
                new ItemCostDto(Copper, 4)
            };

            Assert.That(CostAggregator.TryNormalize(costs, out var normalized, out _), Is.True);
            Assert.That(normalized.Count, Is.EqualTo(2));
            Assert.That(normalized[0].ItemId, Is.EqualTo(Copper));
            Assert.That(normalized[0].Quantity, Is.EqualTo(5));
            Assert.That(normalized[1].ItemId, Is.EqualTo(Iron));
            Assert.That(normalized[1].Quantity, Is.EqualTo(2));
        }

        [Test]
        public void IResourceWallet_CanAfford_And_TrySpend_ContractSurface()
        {
            var (economy, inventory, _, _) = CreateSystem();
            inventory.TryAddMineral(Copper, 2);
            IResourceWallet wallet = economy;

            var costs = new List<ItemCostDto> { new ItemCostDto(Copper, 2) };
            Assert.That(wallet.CanAfford(costs), Is.True);
            Assert.That(wallet.TrySpend(costs), Is.True);
            Assert.That(inventory.State.GetQuantity(Copper), Is.EqualTo(0));
            Assert.That(wallet.CanAfford(costs), Is.False);
        }
    }
}
