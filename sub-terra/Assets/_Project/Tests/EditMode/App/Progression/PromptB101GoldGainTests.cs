using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.Economy;
using SubTerra.App.Integration;
using SubTerra.App.Inventory;
using SubTerra.App.Outpost;
using SubTerra.App.Progression;
using SubTerra.App.State;
using SubTerra.Shared;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Tests.Progression
{
    public sealed class PromptB101GoldGainTests
    {
        private GameDataCatalog catalog;
        private GameState state;
        private InventoryService inventory;
        private UpgradeState upgrades;
        private UpgradeEffectProvider effects;
        private EconomyService economy;
        private ProgressionService progression;

        [SetUp]
        public void SetUp()
        {
            catalog = AssetDatabase.LoadAssetAtPath<GameDataCatalog>("Assets/_Project/Data/Catalog/GameDataCatalog.asset");
            state = GameState.CreateNew();
            var minerals = new GameDataCatalogMineralLookup(catalog);
            inventory = new InventoryService(minerals, 10000f);
            upgrades = new UpgradeState();
            var data = new GameDataUpgradeCatalog(catalog);
            effects = new UpgradeEffectProvider(upgrades, data);
            economy = new EconomyService(inventory, minerals, state, effects: effects);
            progression = new ProgressionService(upgrades, data, economy);
        }

        [Test]
        public void CatalogHasConfirmedTableAndCurrencyIsNotMineral()
        {
            Assert.That(catalog.TryGetUpgrade(DataIds.Upgrades.CargoGold, out var data), Is.True);
            Assert.That(data.Levels.Select(l => l.EffectValue), Is.EqualTo(new[] { 50f, 75f, 100f }));
            Assert.That(data.Levels.Select(l => l.Costs[0].Quantity), Is.EqualTo(new[] { 500, 1000, 3000 }));
            Assert.That(data.Levels.Select(l => l.Costs[1].ItemId), Is.EqualTo(new[] { DataIds.Minerals.Copper, DataIds.Minerals.Iron, DataIds.Minerals.Lithium }));
            Assert.That(data.Levels.All(l => l.Costs[0].ItemId == DataIds.Currency.Gold && l.Costs[1].Quantity == 10), Is.True);
            Assert.That(UpgradeCategoryRules.Resolve(data.Id), Is.EqualTo(UpgradeCategory.Capacity));
            Assert.That(catalog.TryGetMineral(DataIds.Currency.Gold, out _), Is.False);
            var validation = catalog.ValidateAll();
            Assert.That(validation.IsValid, Is.True, validation.FormatAll());
            Assert.That(ItemDisplayNames.Cost(DataIds.Currency.Gold, 500), Is.EqualTo("500G"));
        }

        [TestCase(15, 50, 8)]
        [TestCase(30, 50, 15)]
        [TestCase(30, 75, 23)]
        [TestCase(75, 50, 38)]
        [TestCase(75, 75, 56)]
        [TestCase(330, 75, 248)]
        [TestCase(0, 100, 0)]
        public void RoundsOnceOnTransactionTotal(int gold, int percent, int expected)
        {
            Assert.That(EconomyPricing.ComputeGoldBonus(gold, percent), Is.EqualTo(expected));
        }

        [TestCase(499, 10)]
        [TestCase(500, 9)]
        public void FailedPurchaseChangesNeitherWalletNorLevel(int gold, int copper)
        {
            state.AddGold(gold);
            inventory.TryAddMineral(DataIds.Minerals.Copper, copper);
            Assert.That(progression.TryPurchase(DataIds.Upgrades.CargoGold).IsSuccess, Is.False);
            Assert.That(state.Player.Gold, Is.EqualTo(gold));
            Assert.That(inventory.State.GetQuantity(DataIds.Minerals.Copper), Is.EqualTo(copper));
            Assert.That(upgrades.GetLevel(DataIds.Upgrades.CargoGold), Is.Zero);
        }

        [Test]
        public void PurchasesAllLevelsAndJsonRestoresEffectWithoutNewSaveFields()
        {
            state.AddGold(4500);
            foreach (var id in new[] { DataIds.Minerals.Copper, DataIds.Minerals.Iron, DataIds.Minerals.Lithium })
                inventory.TryAddMineral(id, 10);
            foreach (var percent in new[] { 50, 75, 100 })
            {
                Assert.That(progression.TryPurchase(DataIds.Upgrades.CargoGold).IsSuccess, Is.True);
                Assert.That(effects.GetGoldGainBonusPercent(), Is.EqualTo(percent));
            }
            Assert.That(state.Player.Gold, Is.Zero);
            Assert.That(progression.TryPurchase(DataIds.Upgrades.CargoGold).IsSuccess, Is.False);
            var restored = JsonUtility.FromJson<UpgradeState>(JsonUtility.ToJson(upgrades));
            Assert.That(new UpgradeEffectProvider(restored, new GameDataUpgradeCatalog(catalog)).GetGoldGainBonusPercent(), Is.EqualTo(100));
            Assert.That(new UpgradeEffectProvider(JsonUtility.FromJson<UpgradeState>("{}"), new GameDataUpgradeCatalog(catalog)).GetGoldGainBonusPercent(), Is.Zero);
        }

        [TestCase(0, 20)]
        [TestCase(1, 30)]
        [TestCase(2, 35)]
        [TestCase(3, 40)]
        [TestCase(99, 20)]
        public void SaleAndMiningApplySameBonusButDirectRewardsDoNot(int level, int total)
        {
            upgrades.TryRestore(new[] { new UpgradeLevelState(DataIds.Upgrades.CargoGold, level) });
            inventory.TryAddMineral(DataIds.Minerals.Copper, 2);
            Assert.That(economy.TrySellMineral(DataIds.Minerals.Copper, 2).GoldDelta, Is.EqualTo(total));
            var mined = MiningYieldCommit.TryCommit(inventory, state, effects, "", 0, 0, 20);
            Assert.That(mined.AcceptedGold, Is.EqualTo(total));
            Assert.That(mined.AcceptedGoldBonus, Is.EqualTo(total - 20));
            state.AddGold(100);
            Assert.That(state.Player.Gold, Is.EqualTo(total * 2 + 100));
        }

        [Test]
        public void SaleRejectsOverflowAndMiningClampsActualBonus()
        {
            upgrades.TryRestore(new[] { new UpgradeLevelState(DataIds.Upgrades.CargoGold, 1) });
            state.AddGold(int.MaxValue - 25);
            inventory.TryAddMineral(DataIds.Minerals.Copper, 2);
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, "[SubTerra] Economy rejected: GoldOverflow Gold balance overflow.");
            Assert.That(economy.TrySellMineral(DataIds.Minerals.Copper, 2).IsSuccess, Is.False);
            Assert.That(inventory.State.GetQuantity(DataIds.Minerals.Copper), Is.EqualTo(2));
            var mined = MiningYieldCommit.TryCommit(inventory, state, effects, "", 0, 0, 20);
            Assert.That(mined.AcceptedGold, Is.EqualTo(25));
            Assert.That(mined.AcceptedGoldBonus, Is.EqualTo(5));
            Assert.That(GoldPickupPresentation.FormatPickupText(25, 5), Is.EqualTo("20G + 5G 보너스!"));
            Assert.That(MiningYieldCommit.FormatHudFeedback("", 0, 0, 25, 5), Is.EqualTo("골드 +25G (+5G 보너스)"));
        }

        [Test]
        public void WalletNormalizesGoldCostsAndRejectsUnknownOrOverflowWithoutSpending()
        {
            state.AddGold(500);
            var duplicate = new[] { new ItemCostDto(DataIds.Currency.Gold, 200), new ItemCostDto(DataIds.Currency.Gold, 300) };
            Assert.That(economy.CanAfford(duplicate), Is.True);
            Assert.That(state.Player.Gold, Is.EqualTo(500));
            Assert.That(economy.TrySpend(new[] { new ItemCostDto(DataIds.Currency.Gold, 100), new ItemCostDto("mineral.unknown", 1) }), Is.False);
            Assert.That(economy.TrySpend(new[] { new ItemCostDto(DataIds.Currency.Gold, int.MaxValue), new ItemCostDto(DataIds.Currency.Gold, 1) }), Is.False);
            Assert.That(state.Player.Gold, Is.EqualTo(500));
            Assert.That(economy.TrySpend(duplicate), Is.True);
            Assert.That(state.Player.Gold, Is.Zero);
        }

        [Test]
        public void FullCargoPreventsBothBaseGoldAndBonus()
        {
            upgrades.TryRestore(new[] { new UpgradeLevelState(DataIds.Upgrades.CargoGold, 3) });
            var full = new InventoryService(new GameDataCatalogMineralLookup(catalog), 0f);
            var result = MiningYieldCommit.TryCommit(full, state, effects, DataIds.Minerals.Copper, 1, 1, 50);
            Assert.That(result.Status, Is.EqualTo(MiningCommitStatus.InventoryFull));
            Assert.That(state.Player.Gold, Is.Zero);
            Assert.That(result.AcceptedGoldBonus, Is.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void OutpostSettlesCargoAndStorageWithBonus(bool storage)
        {
            upgrades.TryRestore(new[] { new UpgradeLevelState(DataIds.Upgrades.CargoGold, 1) });
            inventory.TryAddMineral(DataIds.Minerals.Iron, 2);
            var outpost = new OutpostService(inventory, new GameDataCatalogMineralLookup(catalog), state, effects: effects);
            outpost.ApplyRuntimeStatus(new OutpostStatusDto
            {
                outpostInstanceId = "test.outpost", isActive = true, isInInteractionRange = true,
                connectedFacilities = new List<ConnectedFacilityStatusDto>
                {
                    new ConnectedFacilityStatusDto { instanceId = "test.storage", buildingId = DataIds.Buildings.StorageBasic, isActive = true },
                    new ConnectedFacilityStatusDto { instanceId = "test.settlement", buildingId = DataIds.Buildings.SettlementBasic, isActive = true }
                }
            });
            if (storage) Assert.That(outpost.TryDeposit(DataIds.Minerals.Iron, 2).IsSuccess, Is.True);
            var result = storage ? outpost.TrySettle(OutpostSettlementSource.Storage)
                : outpost.TrySettlePlayerCargo(DataIds.Minerals.Iron, 2);
            Assert.That(result.IsSuccess, Is.True, result.Message);
            Assert.That(state.Player.Gold, Is.EqualTo(45));
        }
    }
}
