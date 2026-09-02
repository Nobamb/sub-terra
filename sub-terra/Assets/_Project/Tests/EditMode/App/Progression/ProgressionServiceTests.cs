using System.Collections.Generic;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.Inventory;
using SubTerra.App.Progression;
using SubTerra.App.State;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.App.Tests.Progression
{
    /// <summary>F-F01~F-F05 구매·효과·잠금 기능 검증.</summary>
    public sealed class ProgressionServiceTests
    {
        private sealed class Catalog : IUpgradeCatalog
        {
            private readonly List<UpgradeData> upgrades;

            public Catalog(params UpgradeData[] upgrades)
            {
                this.upgrades = new List<UpgradeData>(upgrades);
            }

            public IReadOnlyList<UpgradeData> Upgrades => upgrades;

            public bool TryGetUpgrade(string upgradeId, out UpgradeData data)
            {
                for (var i = 0; i < upgrades.Count; i++)
                {
                    if (upgrades[i] != null && upgrades[i].Id == upgradeId)
                    {
                        data = upgrades[i];
                        return true;
                    }
                }

                data = null;
                return false;
            }
        }

        private sealed class Wallet : IResourceWallet
        {
            private readonly Dictionary<string, int> amounts = new Dictionary<string, int>();

            public int CanAffordCalls { get; private set; }
            public int SpendCalls { get; private set; }

            public void Set(string itemId, int quantity)
            {
                amounts[itemId] = quantity;
            }

            public int Get(string itemId)
            {
                return amounts.TryGetValue(itemId, out var quantity) ? quantity : 0;
            }

            public bool CanAfford(IReadOnlyList<ItemCostDto> costs)
            {
                CanAffordCalls++;
                if (costs == null)
                {
                    return false;
                }

                for (var i = 0; i < costs.Count; i++)
                {
                    if (costs[i].Quantity <= 0 || Get(costs[i].ItemId) < costs[i].Quantity)
                    {
                        return false;
                    }
                }

                return true;
            }

            public bool TrySpend(IReadOnlyList<ItemCostDto> costs)
            {
                SpendCalls++;
                if (!CanAffordWithoutCounting(costs))
                {
                    return false;
                }

                for (var i = 0; i < costs.Count; i++)
                {
                    amounts[costs[i].ItemId] = Get(costs[i].ItemId) - costs[i].Quantity;
                }

                return true;
            }

            private bool CanAffordWithoutCounting(IReadOnlyList<ItemCostDto> costs)
            {
                if (costs == null)
                {
                    return false;
                }

                for (var i = 0; i < costs.Count; i++)
                {
                    if (costs[i].Quantity <= 0 || Get(costs[i].ItemId) < costs[i].Quantity)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        private readonly List<Object> created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (var i = created.Count - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(created[i]);
            }

            created.Clear();
        }

        [Test]
        public void F_F01_Purchase_SpendsOnce_RaisesLevel_AndProviderReadsEffect()
        {
            var data = CreateUpgrade(DataIds.Upgrades.DrillSpeed, 2, 0.1f);
            var wallet = new Wallet();
            wallet.Set(DataIds.Minerals.Copper, 10);
            var state = new UpgradeState();
            var service = new ProgressionService(state, new Catalog(data), wallet);
            var purchaseEvents = 0;
            var saveEvents = 0;
            service.PurchaseCompleted += result =>
            {
                if (result.IsSuccess)
                {
                    purchaseEvents++;
                }
            };
            service.AutoSaveRequested += _ => saveEvents++;

            var result = service.TryPurchase(DataIds.Upgrades.DrillSpeed);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.PreviousLevel, Is.Zero);
            Assert.That(result.CurrentLevel, Is.EqualTo(1));
            Assert.That(wallet.Get(DataIds.Minerals.Copper), Is.EqualTo(9));
            Assert.That(wallet.SpendCalls, Is.EqualTo(1));
            Assert.That(state.GetLevel(DataIds.Upgrades.DrillSpeed), Is.EqualTo(1));
            Assert.That(service.Effects.GetDrillSpeedMultiplier(), Is.EqualTo(1.1f).Within(0.0001f));
            Assert.That(purchaseEvents, Is.EqualTo(1));
            Assert.That(saveEvents, Is.EqualTo(1));
        }

        [Test]
        public void F_F02_MaximumLevel_DoesNotSpendOrRaiseSuccessEvents()
        {
            var data = CreateUpgrade(DataIds.Upgrades.DrillSpeed, 1, 0.1f);
            var wallet = new Wallet();
            wallet.Set(DataIds.Minerals.Copper, 10);
            var state = new UpgradeState();
            Assert.That(
                state.TryRestore(new[] { new UpgradeLevelState(DataIds.Upgrades.DrillSpeed, 1) }),
                Is.True);
            var service = new ProgressionService(state, new Catalog(data), wallet);
            var saveEvents = 0;
            service.AutoSaveRequested += _ => saveEvents++;

            var result = service.TryPurchase(DataIds.Upgrades.DrillSpeed);

            Assert.That(result.Status, Is.EqualTo(ProgressionPurchaseStatus.MaximumLevel));
            Assert.That(wallet.CanAffordCalls, Is.Zero);
            Assert.That(wallet.SpendCalls, Is.Zero);
            Assert.That(wallet.Get(DataIds.Minerals.Copper), Is.EqualTo(10));
            Assert.That(state.GetLevel(DataIds.Upgrades.DrillSpeed), Is.EqualTo(1));
            Assert.That(saveEvents, Is.Zero);
        }

        [Test]
        public void F_F03_MissingNextLevelCost_FailsBeforeWallet()
        {
            var data = ScriptableObject.CreateInstance<UpgradeData>();
            created.Add(data);
            data.EditorSet(
                DataIds.Upgrades.DrillSpeed,
                "Broken",
                1,
                new List<UpgradeLevelDefinition>
                {
                    new UpgradeLevelDefinition(1, 0.1f, new List<ItemCostEntry>())
                });
            var wallet = new Wallet();
            wallet.Set(DataIds.Minerals.Copper, 10);
            var state = new UpgradeState();
            var service = new ProgressionService(state, new Catalog(data), wallet);

            var result = service.TryPurchase(DataIds.Upgrades.DrillSpeed);

            Assert.That(result.Status, Is.EqualTo(ProgressionPurchaseStatus.InvalidDefinition));
            Assert.That(wallet.CanAffordCalls, Is.Zero);
            Assert.That(wallet.SpendCalls, Is.Zero);
            Assert.That(state.GetLevel(DataIds.Upgrades.DrillSpeed), Is.Zero);
        }

        [Test]
        public void F_F04_AllNineEffects_UseCurrentLevelData_AndInvalidLevelUsesBase()
        {
            var upgrades = new[]
            {
                CreateUpgrade(DataIds.Upgrades.DrillSpeed, 1, 0.2f),
                CreateUpgrade(DataIds.Upgrades.DrillEfficiency, 1, 0.1f),
                CreateUpgrade(DataIds.Upgrades.MaximumEnergy, 1, 25f),
                CreateUpgrade(DataIds.Upgrades.MaximumHealth, 1, 30f),
                CreateUpgrade(DataIds.Upgrades.HealthRegeneration, 1, 0.3f),
                CreateUpgrade(DataIds.Upgrades.MaximumCargo, 1, 15f),
                CreateUpgrade(DataIds.Upgrades.DroneScan, 1, 3f),
                CreateUpgrade(DataIds.Upgrades.DroneRescue, 1, 0.2f),
                CreateUpgrade(DataIds.Upgrades.GasResistance, 1, 0.3f)
            };
            var entries = new List<UpgradeLevelState>();
            for (var i = 0; i < upgrades.Length; i++)
            {
                entries.Add(new UpgradeLevelState(upgrades[i].Id, 1));
            }

            var state = new UpgradeState();
            Assert.That(state.TryRestore(entries), Is.True);
            var provider = new UpgradeEffectProvider(state, new Catalog(upgrades));

            Assert.That(provider.GetDrillLevel(), Is.EqualTo(1));
            Assert.That(provider.GetDrillSpeedMultiplier(), Is.EqualTo(1.2f).Within(0.0001f));
            Assert.That(provider.GetEnergyEfficiencyMultiplier(), Is.EqualTo(1f / 0.9f).Within(0.0001f));
            Assert.That(provider.GetMaximumEnergy(100), Is.EqualTo(125));
            Assert.That(provider.GetMaximumHealth(100), Is.EqualTo(130));
            Assert.That(provider.GetHealthRegenerationPerSecond(), Is.EqualTo(0.3f).Within(0.0001f));
            Assert.That(provider.GetMaximumCargoWeight(50f), Is.EqualTo(65f).Within(0.0001f));
            Assert.That(provider.GetDroneScanRadius(4f), Is.EqualTo(3f).Within(0.0001f));
            Assert.That(provider.GetDroneScanRadius(0f), Is.EqualTo(3f).Within(0.0001f));
            Assert.That(provider.GetDroneRescuePreservation(0.1f), Is.EqualTo(0.3f).Within(0.0001f));
            Assert.That(provider.GetGasResistance(), Is.EqualTo(0.3f).Within(0.0001f));

            var invalidState = new UpgradeState();
            invalidState.TryRestore(new[] { new UpgradeLevelState(DataIds.Upgrades.DrillSpeed, 99) });
            var invalidProvider = new UpgradeEffectProvider(invalidState, new Catalog(upgrades));
            Assert.That(invalidProvider.GetDrillSpeedMultiplier(), Is.EqualTo(1f));
        }

        [Test]
        public void DroneScanRadius_UsesAbsoluteZeroThreeSevenValues()
        {
            var scan = ScriptableObject.CreateInstance<UpgradeData>();
            created.Add(scan);
            scan.EditorSet(
                DataIds.Upgrades.DroneScan,
                "드론 스캔 범위",
                2,
                new List<UpgradeLevelDefinition>
                {
                    new UpgradeLevelDefinition(1, 3f, new List<ItemCostEntry>()),
                    new UpgradeLevelDefinition(2, 7f, new List<ItemCostEntry>())
                });
            var state = new UpgradeState();
            var provider = new UpgradeEffectProvider(state, new Catalog(scan));

            Assert.That(provider.GetDroneScanRadius(4f), Is.Zero);
            Assert.That(state.TryRestore(new[] { new UpgradeLevelState(DataIds.Upgrades.DroneScan, 1) }), Is.True);
            Assert.That(provider.GetDroneScanRadius(4f), Is.EqualTo(3f));
            Assert.That(state.TryRestore(new[] { new UpgradeLevelState(DataIds.Upgrades.DroneScan, 2) }), Is.True);
            Assert.That(provider.GetDroneScanRadius(4f), Is.EqualTo(7f));
        }

        [Test]
        public void F_F05_DeepZone_UnlocksAtBoundary_AndStateIsJsonSerializable()
        {
            var state = new UpgradeState();
            Assert.That(
                state.TryRestore(
                    new[]
                    {
                        new UpgradeLevelState(DataIds.Upgrades.DrillSpeed, 2)
                    }),
                Is.True);
            var service = new ProgressionService(state, new Catalog(), new Wallet());

            var before = service.GetDeepZoneAccess(11);
            var boundary = service.GetDeepZoneAccess(12);
            var unlocked = service.TryUnlockDeepZone(12);

            Assert.That(before.IsUnlocked, Is.False);
            Assert.That(before.Reason, Does.Contain("목표"));
            Assert.That(boundary.IsUnlocked, Is.True);
            Assert.That(unlocked.DidUnlockNow, Is.True);
            Assert.That(state.IsZoneUnlocked(DataIds.Zones.Deep), Is.True);

            var json = JsonUtility.ToJson(state);
            Assert.That(json, Does.Contain(DataIds.Upgrades.DrillSpeed));
            Assert.That(json, Does.Contain(DataIds.Zones.Deep));
        }

        [Test]
        public void MaximumEnergyAndCargoPurchase_RefreshExistingStateEventsImmediately()
        {
            var energy = CreateUpgrade(DataIds.Upgrades.MaximumEnergy, 1, 20f);
            var cargo = CreateUpgrade(DataIds.Upgrades.MaximumCargo, 1, 10f);
            var wallet = new Wallet();
            wallet.Set(DataIds.Minerals.Copper, 10);
            var upgradeState = new UpgradeState();
            var service = new ProgressionService(upgradeState, new Catalog(energy, cargo), wallet);
            var gameState = GameState.CreateNew();
            var minerals = new InMemoryMineralCatalog();
            minerals.Register(DataIds.Minerals.Copper, 1f, 1, "Copper");
            var inventory = new InventoryService(minerals, 50f, gameState);
            var synchronizer = new ProgressionDerivedStateSynchronizer(gameState, inventory);
            var energyEvents = 0;
            var inventoryEvents = 0;
            gameState.EnergyChanged += _ => energyEvents++;
            inventory.InventoryChanged += _ => inventoryEvents++;
            synchronizer.Bind(service);

            var energyResult = service.TryPurchase(DataIds.Upgrades.MaximumEnergy);
            var cargoResult = service.TryPurchase(DataIds.Upgrades.MaximumCargo);

            Assert.That(energyResult.IsSuccess, Is.True);
            Assert.That(cargoResult.IsSuccess, Is.True);
            Assert.That(gameState.Player.MaxEnergy, Is.EqualTo(120));
            Assert.That(inventory.MaxCapacity, Is.EqualTo(60f).Within(0.0001f));
            Assert.That(energyEvents, Is.EqualTo(1));
            Assert.That(inventoryEvents, Is.EqualTo(1));
            synchronizer.Dispose();
        }

        [Test]
        public void Restore_InvalidEntries_IsAtomic()
        {
            var state = new UpgradeState();
            Assert.That(
                state.TryRestore(new[] { new UpgradeLevelState(DataIds.Upgrades.DrillSpeed, 1) }),
                Is.True);

            var result = state.TryRestore(
                new[]
                {
                    new UpgradeLevelState(DataIds.Upgrades.DroneScan, 1),
                    new UpgradeLevelState(DataIds.Upgrades.DroneScan, 2)
                });

            Assert.That(result, Is.False);
            Assert.That(state.GetLevel(DataIds.Upgrades.DrillSpeed), Is.EqualTo(1));
            Assert.That(state.GetLevel(DataIds.Upgrades.DroneScan), Is.Zero);
        }

        private UpgradeData CreateUpgrade(string id, int maximumLevel, float effectPerLevel)
        {
            var levels = new List<UpgradeLevelDefinition>();
            for (var level = 1; level <= maximumLevel; level++)
            {
                levels.Add(
                    new UpgradeLevelDefinition(
                        level,
                        effectPerLevel * level,
                        new List<ItemCostEntry>
                        {
                            new ItemCostEntry(DataIds.Minerals.Copper, level)
                        }));
            }

            var data = ScriptableObject.CreateInstance<UpgradeData>();
            created.Add(data);
            data.EditorSet(id, id, maximumLevel, levels);
            return data;
        }
    }
}
