using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.Economy;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.Progression;
using SubTerra.App.UI.Progression;
using SubTerra.Shared;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Tests.Progression
{
    /// <summary>prompt-B 118: 업그레이드 트리 해금 규칙, 구매 검증, 표시 수치 검증.</summary>
    public sealed class PromptB118UpgradeTreeTests
    {
        private const string CatalogPath = "Assets/_Project/Data/Catalog/GameDataCatalog.asset";
        private const string SurfacePrefabPath = "Assets/_Project/Prefabs/UI/SurfaceBasePanel.prefab";

        private sealed class Wallet : IResourceWallet, IResourceBalanceProvider
        {
            public readonly Dictionary<string, int> Amounts = new Dictionary<string, int>();
            public int SpendCalls { get; private set; }

            public int GetOwnedQuantity(string itemId)
            {
                return Amounts.TryGetValue(itemId, out var value) ? value : 0;
            }

            public bool CanAfford(IReadOnlyList<ItemCostDto> costs)
            {
                for (var i = 0; i < costs.Count; i++)
                {
                    if (GetOwnedQuantity(costs[i].ItemId) < costs[i].Quantity)
                    {
                        return false;
                    }
                }

                return true;
            }

            public bool TrySpend(IReadOnlyList<ItemCostDto> costs)
            {
                SpendCalls++;
                if (!CanAfford(costs))
                {
                    return false;
                }

                for (var i = 0; i < costs.Count; i++)
                {
                    Amounts[costs[i].ItemId] = GetOwnedQuantity(costs[i].ItemId) - costs[i].Quantity;
                }

                return true;
            }
        }

        private static GameDataUpgradeCatalog LoadCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameDataCatalog>(CatalogPath);
            Assert.That(catalog, Is.Not.Null);
            return new GameDataUpgradeCatalog(catalog);
        }

        private static UpgradeState StateWithDrill(int level)
        {
            var state = new UpgradeState();
            if (level > 0)
            {
                Assert.That(
                    state.TryRestore(new[] { new UpgradeLevelState(DataIds.Upgrades.DrillSpeed, level) }),
                    Is.True);
            }

            return state;
        }

        private static List<string> UnlockedIds(IUpgradeCatalog catalog, UpgradeState state)
        {
            var set = new HashSet<string>();
            UpgradeUnlockRules.CollectUnlocked(catalog, state, set);
            return set.OrderBy(id => id, System.StringComparer.Ordinal).ToList();
        }

        [Test]
        public void NewGame_OnlyDrillSpeedIsUnlocked()
        {
            var catalog = LoadCatalog();

            var unlocked = UnlockedIds(catalog, new UpgradeState());

            Assert.That(unlocked, Is.EqualTo(new[] { DataIds.Upgrades.DrillSpeed }));
        }

        [TestCase(0, 1)]
        [TestCase(1, 4)]
        [TestCase(2, 8)]
        [TestCase(3, 11)]
        public void DrillSpeedLevel_UnlocksNeighborsInStages(int drillLevel, int expectedUnlockedCount)
        {
            var catalog = LoadCatalog();

            var unlocked = UnlockedIds(catalog, StateWithDrill(drillLevel));

            Assert.That(unlocked.Count, Is.EqualTo(expectedUnlockedCount));
        }

        [Test]
        public void EveryExistingUpgrade_IsReachableThroughDrillSpeedOnly()
        {
            var catalog = LoadCatalog();
            var maxDrill = 0;
            Assert.That(catalog.TryGetUpgrade(DataIds.Upgrades.DrillSpeed, out var drill), Is.True);
            maxDrill = drill.MaxLevel;

            var unlocked = UnlockedIds(catalog, StateWithDrill(maxDrill));

            Assert.That(unlocked.Count, Is.EqualTo(catalog.Upgrades.Count));
            // 드릴 속도 자체는 조건이 없고, 비용에도 다른 업그레이드/골드가 들어가지 않는다.
            Assert.That(UpgradeUnlockRules.HasRequirement(drill), Is.False);
            for (var i = 0; i < drill.Levels.Count; i++)
            {
                for (var c = 0; c < drill.Levels[i].Costs.Count; c++)
                {
                    Assert.That(drill.Levels[i].Costs[c].ItemId, Does.StartWith("mineral."));
                }
            }
        }

        [Test]
        public void ParentsNeverUnlockLaterThanTheirChildren()
        {
            var catalog = LoadCatalog();
            var byId = catalog.Upgrades.ToDictionary(u => u.Id);
            foreach (var data in catalog.Upgrades)
            {
                if (string.IsNullOrEmpty(data.TreeParentId))
                {
                    continue;
                }

                Assert.That(byId.ContainsKey(data.TreeParentId), Is.True, data.Id);
                Assert.That(
                    byId[data.TreeParentId].UnlockRequiredLevel,
                    Is.LessThanOrEqualTo(data.UnlockRequiredLevel),
                    data.Id);
            }
        }

        [Test]
        public void ShippedCatalog_PassesUnlockValidation()
        {
            var catalog = LoadCatalog();

            Assert.That(UpgradeUnlockRules.Validate(catalog.Upgrades), Is.Empty);
        }

        [Test]
        public void BuilderTable_MatchesShippedData()
        {
            var catalog = LoadCatalog();
            foreach (var def in PromptB118UpgradeTreeBuilder.Nodes)
            {
                Assert.That(catalog.TryGetUpgrade(def.Id, out var data), Is.True, def.Id);
                Assert.That(data.UnlockRequiredLevel, Is.EqualTo(def.DrillLevelRequired), def.Id);
                Assert.That(data.TreeParentId, Is.EqualTo(def.ParentId), def.Id);
            }

            Assert.That(PromptB118UpgradeTreeBuilder.Nodes.Length, Is.EqualTo(catalog.Upgrades.Count));
        }

        [Test]
        public void PurchasedUpgrade_InOldSave_StaysUnlockedWithoutDrillLevel()
        {
            var catalog = LoadCatalog();
            var state = new UpgradeState();
            Assert.That(
                state.TryRestore(new[] { new UpgradeLevelState(DataIds.Upgrades.CargoGold, 2) }),
                Is.True);

            var unlocked = UnlockedIds(catalog, state);

            Assert.That(unlocked, Does.Contain(DataIds.Upgrades.CargoGold));
        }

        [Test]
        public void TryPurchase_LockedUpgrade_FailsBeforeSpendingAnything()
        {
            var catalog = LoadCatalog();
            var wallet = new Wallet();
            wallet.Amounts[DataIds.Minerals.Copper] = 100;
            wallet.Amounts[DataIds.Minerals.Iron] = 100;
            wallet.Amounts[DataIds.Currency.Gold] = 9999;
            var state = new UpgradeState();
            var service = new ProgressionService(state, catalog, wallet);

            var result = service.TryPurchase(DataIds.Upgrades.DrillEfficiency);

            Assert.That(result.Status, Is.EqualTo(ProgressionPurchaseStatus.Locked));
            Assert.That(result.UserMessage, Is.EqualTo("드릴 속도 Lv.1 필요"));
            Assert.That(wallet.SpendCalls, Is.Zero);
            Assert.That(wallet.GetOwnedQuantity(DataIds.Minerals.Copper), Is.EqualTo(100));
            Assert.That(state.GetLevel(DataIds.Upgrades.DrillEfficiency), Is.Zero);
        }

        [Test]
        public void TryPurchase_DrillLevelUp_ReportsNewlyUnlockedUpgrades()
        {
            var catalog = LoadCatalog();
            var wallet = new Wallet();
            wallet.Amounts[DataIds.Minerals.Copper] = 8;
            var service = new ProgressionService(new UpgradeState(), catalog, wallet);

            var result = service.TryPurchase(DataIds.Upgrades.DrillSpeed);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(
                result.NewlyUnlockedUpgradeIds.OrderBy(id => id, System.StringComparer.Ordinal),
                Is.EquivalentTo(new[]
                {
                    DataIds.Upgrades.DrillEfficiency,
                    DataIds.Upgrades.MaximumEnergy,
                    DataIds.Upgrades.MaximumCargo
                }));
            Assert.That(wallet.GetOwnedQuantity(DataIds.Minerals.Copper), Is.Zero);
        }

        [Test]
        public void TryPurchase_NonUnlockingUpgrade_ReportsNoNewUnlocks()
        {
            var catalog = LoadCatalog();
            var wallet = new Wallet();
            wallet.Amounts[DataIds.Minerals.Copper] = 100;
            var state = StateWithDrill(1);
            var service = new ProgressionService(state, catalog, wallet);

            var result = service.TryPurchase(DataIds.Upgrades.DrillEfficiency);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.NewlyUnlockedUpgradeIds, Is.Empty);
        }

        [Test]
        public void Snapshot_Locked_IsNotAffordableAndOnlyCarriesRequirement()
        {
            var catalog = LoadCatalog();
            var wallet = new Wallet();
            wallet.Amounts[DataIds.Minerals.Copper] = 999;
            wallet.Amounts[DataIds.Minerals.Iron] = 999;
            var service = new ProgressionService(new UpgradeState(), catalog, wallet);

            Assert.That(service.TryGetSnapshot(DataIds.Upgrades.DroneScan, out var snapshot), Is.True);

            Assert.That(snapshot.IsUnlocked, Is.False);
            Assert.That(snapshot.CanAffordNextLevel, Is.False);
            Assert.That(snapshot.LockedReason, Is.EqualTo("드릴 속도 Lv.2 필요"));
            Assert.That(snapshot.UnlockRequirementUpgradeId, Is.EqualTo(DataIds.Upgrades.DrillSpeed));
            Assert.That(snapshot.UnlockRequiredLevel, Is.EqualTo(2));
        }

        [Test]
        public void Snapshot_ShortfallListsMissingAmountPerResource()
        {
            var catalog = LoadCatalog();
            var wallet = new Wallet();
            wallet.Amounts[DataIds.Minerals.Copper] = 3;
            var service = new ProgressionService(new UpgradeState(), catalog, wallet);

            Assert.That(service.TryGetSnapshot(DataIds.Upgrades.DrillSpeed, out var snapshot), Is.True);

            Assert.That(snapshot.CanAffordNextLevel, Is.False);
            Assert.That(snapshot.NextCostShortages.Count, Is.EqualTo(1));
            Assert.That(snapshot.NextCostShortages[0].ItemId, Is.EqualTo(DataIds.Minerals.Copper));
            Assert.That(snapshot.NextCostShortages[0].Quantity, Is.EqualTo(5));
        }

        [Test]
        public void TryPurchase_InsufficientResources_DoesNotSpendAndKeepsLevel()
        {
            var catalog = LoadCatalog();
            var wallet = new Wallet();
            wallet.Amounts[DataIds.Minerals.Copper] = 7;
            var state = new UpgradeState();
            var service = new ProgressionService(state, catalog, wallet);

            var result = service.TryPurchase(DataIds.Upgrades.DrillSpeed);

            Assert.That(result.Status, Is.EqualTo(ProgressionPurchaseStatus.InsufficientResources));
            Assert.That(wallet.SpendCalls, Is.Zero);
            Assert.That(wallet.GetOwnedQuantity(DataIds.Minerals.Copper), Is.EqualTo(7));
            Assert.That(state.GetLevel(DataIds.Upgrades.DrillSpeed), Is.Zero);
        }

        [Test]
        public void Validation_RejectsCycleAndUnknownRequirement()
        {
            var a = Create("upgrade.test.a", "upgrade.test.b", 1);
            var b = Create("upgrade.test.b", "upgrade.test.a", 1);
            var c = Create("upgrade.test.c", "upgrade.test.missing", 1);
            try
            {
                Assert.That(UpgradeUnlockRules.Validate(new[] { a, b }), Does.Contain("순환"));
                Assert.That(UpgradeUnlockRules.Validate(new[] { c }), Does.Contain("알 수 없는"));
            }
            finally
            {
                Object.DestroyImmediate(a);
                Object.DestroyImmediate(b);
                Object.DestroyImmediate(c);
            }
        }

        private static UpgradeData Create(string id, string requirementId, int requiredLevel)
        {
            var data = ScriptableObject.CreateInstance<UpgradeData>();
            data.EditorSet(
                id,
                id,
                1,
                new List<UpgradeLevelDefinition>
                {
                    new UpgradeLevelDefinition(1, 1f, new List<ItemCostEntry>
                    {
                        new ItemCostEntry(DataIds.Minerals.Copper, 1)
                    })
                });
            data.EditorSetUnlock(requirementId, requiredLevel, string.Empty);
            return data;
        }

        [TestCase(DataIds.Upgrades.DrillSpeed, 0.25f, "+25%")]
        [TestCase(DataIds.Upgrades.DrillSpeed, 1.2222222f, "+122%")]
        [TestCase(DataIds.Upgrades.DrillEfficiency, 0.35f, "-35%")]
        [TestCase(DataIds.Upgrades.MaximumEnergy, 110f, "+110")]
        [TestCase(DataIds.Upgrades.HealthRegeneration, 0.3f, "+0.3/초")]
        [TestCase(DataIds.Upgrades.MaximumCargo, 70f, "+70kg")]
        [TestCase(DataIds.Upgrades.CargoGold, 75f, "+75%")]
        [TestCase(DataIds.Upgrades.DroneScan, 7f, "7칸")]
        [TestCase(DataIds.Upgrades.DroneRescue, 0.7f, "+70%")]
        [TestCase(DataIds.Upgrades.GasResistance, 0.5f, "-50%")]
        public void EffectFormatter_UsesRealUnits(string id, float value, string expected)
        {
            Assert.That(UpgradeEffectFormatter.FormatValue(id, value), Is.EqualTo(expected));
        }

        [Test]
        public void SurfaceBasePrefab_HasTreeWithOneNodePerUpgrade()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SurfacePrefabPath);
            var tree = prefab.GetComponentInChildren<UpgradeTreeView>(true);
            Assert.That(tree, Is.Not.Null);

            var ids = tree.Nodes.Select(n => n.UpgradeId).ToList();
            Assert.That(ids.Count, Is.EqualTo(11));
            Assert.That(ids.Distinct().Count(), Is.EqualTo(11));
            Assert.That(tree.Connectors.Count, Is.EqualTo(10));
            var view = prefab.GetComponentInChildren<ProgressionPanelView>(true);
            Assert.That(view.IsTreeMode, Is.True);
            // 노드 하나만 입력을 받고 장식은 레이캐스트를 받지 않는다.
            var node = tree.Nodes[1];
            var graphics = node.GetComponentsInChildren<UnityEngine.UI.Graphic>(true);
            var raycastTargets = graphics.Where(g => g.raycastTarget).ToArray();
            Assert.That(raycastTargets.Length, Is.EqualTo(1));
            Assert.That(raycastTargets[0].gameObject, Is.EqualTo(node.gameObject));
        }

        [Test]
        public void LockedNode_HidesNameIconAndLevel()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SurfacePrefabPath);
            var instance = Object.Instantiate(prefab);
            try
            {
                var tree = instance.GetComponentInChildren<UpgradeTreeView>(true);
                var node = tree.FindNode(DataIds.Upgrades.DroneScan);
                var snapshot = new UpgradeSnapshot(
                    DataIds.Upgrades.DroneScan,
                    "드론 스캔 범위",
                    0,
                    2,
                    0f,
                    3f,
                    new[] { new ItemCostDto(DataIds.Minerals.Copper, 6) },
                    false,
                    null,
                    null,
                    false,
                    "드릴 속도 Lv.2 필요",
                    DataIds.Upgrades.DrillSpeed);

                node.Apply(snapshot);

                var labels = node.GetComponentsInChildren<TMPro.TMP_Text>(true);
                foreach (var label in labels)
                {
                    if (label.name == "NameLabel" || label.name == "LevelLabel")
                    {
                        Assert.That(label.text, Is.Empty, label.name);
                    }
                }

                Assert.That(node.IsBlindVisible, Is.True);
                Assert.That(node.VisualState, Is.EqualTo(UpgradeNodeVisualState.Locked));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }
    }
}
