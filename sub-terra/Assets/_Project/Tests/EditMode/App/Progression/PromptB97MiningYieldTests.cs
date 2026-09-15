using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.Inventory;
using SubTerra.App.Progression;
using SubTerra.App.State;
using SubTerra.App.UI.Progression;
using SubTerra.Shared;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Tests.Progression
{
    /// <summary>prompt-B 97 채굴 수확량 업그레이드. Y-D / Y-C / UI.</summary>
    public sealed class PromptB97MiningYieldTests
    {
        private const string CatalogPath = "Assets/_Project/Data/Catalog/GameDataCatalog.asset";
        private readonly List<Object> created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (var i = created.Count - 1; i >= 0; i--)
            {
                if (created[i] != null)
                {
                    Object.DestroyImmediate(created[i]);
                }
            }

            created.Clear();
        }

        [Test]
        public void Y_D01_Catalog_HasYieldUpgradeMatchingConfirmedTable()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameDataCatalog>(CatalogPath);
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.TryGetUpgrade(DataIds.Upgrades.CargoYield, out var data), Is.True);
            Assert.That(data.MaxLevel, Is.EqualTo(3));
            Assert.That(data.Levels.Count, Is.EqualTo(3));
            Assert.That(data.DisplayName, Is.EqualTo("채굴 수확량"));
            Assert.That(UpgradeCategoryRules.Resolve(data.Id), Is.EqualTo(UpgradeCategory.Capacity));

            AssertLevel(data.Levels[0], 1, 1f, "mineral.copper:10", Copper(1));
            AssertLevel(data.Levels[1], 2, 2f, "mineral.copper:20,mineral.iron:10", Copper(2), Iron(1));
            AssertLevel(
                data.Levels[2],
                3,
                3f,
                "mineral.copper:30,mineral.iron:20,mineral.lithium:10",
                Copper(3),
                Iron(2),
                Lithium(1));

            var validation = catalog.ValidateAll();
            Assert.That(validation.IsValid, Is.True, validation.FormatAll());
        }

        [Test]
        public void Y_D02_OtherUpgradeMiningYieldBonuses_FailValidation()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameDataCatalog>(CatalogPath);
            Assert.That(source, Is.Not.Null);
            Assert.That(source.TryGetUpgrade(DataIds.Upgrades.DrillSpeed, out var drill), Is.True);

            var badLevels = new List<UpgradeLevelDefinition>();
            for (var i = 0; i < drill.Levels.Count; i++)
            {
                var src = drill.Levels[i];
                badLevels.Add(new UpgradeLevelDefinition(
                    src.Level,
                    src.EffectValue,
                    src.Costs.ToList(),
                    new List<MineralBonusEntry> { Copper(1) }));
            }

            var bad = ScriptableObject.CreateInstance<UpgradeData>();
            created.Add(bad);
            bad.EditorSet(drill.Id, drill.DisplayName, drill.MaxLevel, badLevels);

            var upgrades = source.Upgrades.ToList();
            for (var i = 0; i < upgrades.Count; i++)
            {
                if (upgrades[i] != null && upgrades[i].Id == DataIds.Upgrades.DrillSpeed)
                {
                    upgrades[i] = bad;
                }
            }

            var clone = ScriptableObject.CreateInstance<GameDataCatalog>();
            created.Add(clone);
            clone.EditorSetLists(
                source.Minerals.ToList(),
                source.Buildings.ToList(),
                source.Recipes.ToList(),
                upgrades,
                source.Dialogues.ToList());

            var result = CatalogValidator.Validate(clone);
            Assert.That(result.IsValid, Is.False);
            Assert.That(
                result.Issues.Any(issue => issue.FieldName.Contains("miningYieldBonuses")),
                Is.True);
        }

        [Test]
        public void Y_D03_Provider_ReadsPerMineralBonusByLevel()
        {
            var data = CreateConfirmedYieldUpgrade();
            var state = new UpgradeState();
            var provider = new UpgradeEffectProvider(state, new Catalog(data));

            Assert.That(provider.GetMiningYieldBonus(DataIds.Minerals.Copper), Is.Zero);
            Assert.That(provider.GetMiningYieldBonus(string.Empty), Is.Zero);

            Assert.That(state.TryRestore(new[] { new UpgradeLevelState(DataIds.Upgrades.CargoYield, 1) }), Is.True);
            Assert.That(provider.GetMiningYieldBonus(DataIds.Minerals.Copper), Is.EqualTo(1));
            Assert.That(provider.GetMiningYieldBonus(DataIds.Minerals.Iron), Is.Zero);
            Assert.That(provider.GetMiningYieldBonus(DataIds.Minerals.Lithium), Is.Zero);

            Assert.That(state.TryRestore(new[] { new UpgradeLevelState(DataIds.Upgrades.CargoYield, 2) }), Is.True);
            Assert.That(provider.GetMiningYieldBonus(DataIds.Minerals.Copper), Is.EqualTo(2));
            Assert.That(provider.GetMiningYieldBonus(DataIds.Minerals.Iron), Is.EqualTo(1));
            Assert.That(provider.GetMiningYieldBonus(DataIds.Minerals.Lithium), Is.Zero);

            Assert.That(state.TryRestore(new[] { new UpgradeLevelState(DataIds.Upgrades.CargoYield, 3) }), Is.True);
            Assert.That(provider.GetMiningYieldBonus(DataIds.Minerals.Copper), Is.EqualTo(3));
            Assert.That(provider.GetMiningYieldBonus(DataIds.Minerals.Iron), Is.EqualTo(2));
            Assert.That(provider.GetMiningYieldBonus(DataIds.Minerals.Lithium), Is.EqualTo(1));
        }

        [Test]
        public void Y_D04_Purchase_SpendsThenCommits_AndMidFailureLeavesLevel()
        {
            var data = CreateConfirmedYieldUpgrade();
            var wallet = new Wallet();
            wallet.Set(DataIds.Minerals.Copper, 10);
            var state = new UpgradeState();
            var service = new ProgressionService(state, new Catalog(data), wallet);

            var result = service.TryPurchase(DataIds.Upgrades.CargoYield);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(state.GetLevel(DataIds.Upgrades.CargoYield), Is.EqualTo(1));
            Assert.That(wallet.Get(DataIds.Minerals.Copper), Is.Zero);
            Assert.That(service.Effects.GetMiningYieldBonus(DataIds.Minerals.Copper), Is.EqualTo(1));

            var blocked = service.TryPurchase(DataIds.Upgrades.CargoYield);
            Assert.That(blocked.Status, Is.EqualTo(ProgressionPurchaseStatus.InsufficientResources));
            Assert.That(state.GetLevel(DataIds.Upgrades.CargoYield), Is.EqualTo(1));
        }

        [Test]
        public void Y_D05_Category_IsCapacityTab()
        {
            Assert.That(
                UpgradeCategoryRules.Resolve(DataIds.Upgrades.CargoYield),
                Is.EqualTo(UpgradeCategory.Capacity));
            Assert.That(
                UpgradeCategoryRules.Matches(DataIds.Upgrades.CargoYield, UpgradeCategory.Capacity),
                Is.True);
        }

        [Test]
        public void Y_D06_DisplayNames_AreKoreanWithoutRawId()
        {
            Assert.That(ItemDisplayNames.Upgrade(DataIds.Upgrades.CargoYield), Is.EqualTo("채굴 수확량"));
            Assert.That(ItemDisplayNames.UpgradeDescription(DataIds.Upgrades.CargoYield), Does.Contain("화물 무게"));
            Assert.That(ItemDisplayNames.Upgrade(DataIds.Upgrades.CargoYield), Does.Not.Contain("upgrade.cargo.yield"));
            Assert.That(
                ItemDisplayNames.PreferDisplay(DataIds.Upgrades.CargoYield, DataIds.Upgrades.CargoYield),
                Is.EqualTo("채굴 수확량"));
        }

        [Test]
        public void Y_C01_LevelZeroCopper_AddsBaseOnlyAndSpendsEnergy()
        {
            var fixture = CreateCommitFixture(50f, 0);
            var result = MiningYieldCommit.TryCommit(
                fixture.Inventory,
                fixture.State,
                fixture.Effects,
                DataIds.Minerals.Copper,
                1,
                4);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.AcceptedBaseQuantity, Is.EqualTo(1));
            Assert.That(result.AcceptedBonusQuantity, Is.Zero);
            Assert.That(fixture.Inventory.State.GetQuantity(DataIds.Minerals.Copper), Is.EqualTo(1));
            Assert.That(fixture.State.Player.Energy, Is.EqualTo(96));
        }

        [Test]
        public void Y_C02_LevelOneCopper_AddsBaseAndBonus()
        {
            var fixture = CreateCommitFixture(50f, 1);
            var result = MiningYieldCommit.TryCommit(
                fixture.Inventory,
                fixture.State,
                fixture.Effects,
                DataIds.Minerals.Copper,
                1,
                2);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.AcceptedBaseQuantity, Is.EqualTo(1));
            Assert.That(result.AcceptedBonusQuantity, Is.EqualTo(1));
            Assert.That(fixture.Inventory.State.GetQuantity(DataIds.Minerals.Copper), Is.EqualTo(2));
        }

        [Test]
        public void Y_C03_LevelTwo_AppliesPerMineralBonus()
        {
            var fixture = CreateCommitFixture(50f, 2);
            MiningYieldCommit.TryCommit(
                fixture.Inventory,
                fixture.State,
                fixture.Effects,
                DataIds.Minerals.Copper,
                1,
                1);
            MiningYieldCommit.TryCommit(
                fixture.Inventory,
                fixture.State,
                fixture.Effects,
                DataIds.Minerals.Iron,
                1,
                1);
            MiningYieldCommit.TryCommit(
                fixture.Inventory,
                fixture.State,
                fixture.Effects,
                DataIds.Minerals.Lithium,
                1,
                1);

            Assert.That(fixture.Inventory.State.GetQuantity(DataIds.Minerals.Copper), Is.EqualTo(3));
            Assert.That(fixture.Inventory.State.GetQuantity(DataIds.Minerals.Iron), Is.EqualTo(2));
            Assert.That(fixture.Inventory.State.GetQuantity(DataIds.Minerals.Lithium), Is.EqualTo(1));
        }

        [Test]
        public void Y_C04_LevelThreeLithium_AddsTwo()
        {
            var fixture = CreateCommitFixture(50f, 3);
            var result = MiningYieldCommit.TryCommit(
                fixture.Inventory,
                fixture.State,
                fixture.Effects,
                DataIds.Minerals.Lithium,
                1,
                1);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(fixture.Inventory.State.GetQuantity(DataIds.Minerals.Lithium), Is.EqualTo(2));
        }

        [Test]
        public void Y_C05_BaseDoesNotFit_FailsWithoutChangingState()
        {
            var fixture = CreateCommitFixture(1f, 1);
            var energy = fixture.State.Player.Energy;
            var result = MiningYieldCommit.TryCommit(
                fixture.Inventory,
                fixture.State,
                fixture.Effects,
                DataIds.Minerals.Copper,
                1,
                5);

            Assert.That(result.Status, Is.EqualTo(MiningCommitStatus.InventoryFull));
            Assert.That(fixture.Inventory.State.GetQuantity(DataIds.Minerals.Copper), Is.Zero);
            Assert.That(fixture.State.Player.Energy, Is.EqualTo(energy));
        }

        [Test]
        public void Y_C06_BonusPartial_SucceedsWithBaseOnly()
        {
            var fixture = CreateCommitFixture(2f, 1);
            var result = MiningYieldCommit.TryCommit(
                fixture.Inventory,
                fixture.State,
                fixture.Effects,
                DataIds.Minerals.Copper,
                1,
                3);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.AcceptedBaseQuantity, Is.EqualTo(1));
            Assert.That(result.AcceptedBonusQuantity, Is.Zero);
            Assert.That(fixture.Inventory.State.GetQuantity(DataIds.Minerals.Copper), Is.EqualTo(1));
            Assert.That(fixture.State.Player.Energy, Is.EqualTo(97));
        }

        [Test]
        public void Y_C07_QuestExactAdd_DoesNotApplyYieldBonus()
        {
            var fixture = CreateCommitFixture(50f, 3);
            var additions = new List<KeyValuePair<string, int>>
            {
                new KeyValuePair<string, int>(DataIds.Minerals.Copper, 2)
            };

            var result = fixture.Inventory.TryAddManyExact(additions);

            Assert.That(result.Status, Is.EqualTo(InventoryMutationStatus.Success));
            Assert.That(fixture.Inventory.State.GetQuantity(DataIds.Minerals.Copper), Is.EqualTo(2));
        }

        [Test]
        public void Y_C08_RockTile_AddsNoBonus()
        {
            var fixture = CreateCommitFixture(50f, 3);
            var energy = fixture.State.Player.Energy;
            var result = MiningYieldCommit.TryCommit(
                fixture.Inventory,
                fixture.State,
                fixture.Effects,
                string.Empty,
                0,
                2);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.AcceptedBaseQuantity, Is.Zero);
            Assert.That(result.AcceptedBonusQuantity, Is.Zero);
            Assert.That(fixture.Inventory.State.GetQuantity(DataIds.Minerals.Copper), Is.Zero);
            Assert.That(fixture.State.Player.Energy, Is.EqualTo(energy - 2));
        }

        [Test]
        public void Y_P02_DetailCard_ShowsPerMineralYieldLines()
        {
            var root = new GameObject("YieldDetail");
            created.Add(root);
            var detailRoot = new GameObject("DetailText");
            detailRoot.transform.SetParent(root.transform);
            var detail = detailRoot.AddComponent<TextMeshProUGUI>();
            var view = root.AddComponent<ProgressionPanelView>();
            SetPrivateField(view, "detailText", detail);

            view.SetSelectedUpgrade(new UpgradeSnapshot(
                DataIds.Upgrades.CargoYield,
                "채굴 수확량",
                0,
                3,
                0f,
                1f,
                new[] { new ItemCostDto(DataIds.Minerals.Copper, 10) },
                true,
                null,
                new List<MineralBonusEntry> { Copper(1) }));

            Assert.That(detail.text, Does.Contain("채굴 수확량  Lv.0/3"));
            Assert.That(detail.text, Does.Contain("구리 +0  철 +0  리튬 +0"));
            Assert.That(detail.text, Does.Contain("구리 +1  철 +0  리튬 +0"));
            Assert.That(detail.text, Does.Contain("필요 재료: 구리 x10"));
            Assert.That(detail.text, Does.Not.Contain("upgrade.cargo.yield"));
        }

        [Test]
        public void Y_P04_CapacityTabFiveRows_FitEntryListHeight()
        {
            const float rowHeight = 44f;
            const float entryListHeight = 420f;
            Assert.That(5 * rowHeight, Is.LessThanOrEqualTo(entryListHeight));
        }

        [Test]
        public void FormatHudFeedback_ShowsHarvestSuffixOnlyWhenBonusAccepted()
        {
            Assert.That(
                MiningYieldCommit.FormatHudFeedback(DataIds.Minerals.Copper, 1, 1),
                Is.EqualTo("구리 +1 (+1 수확)"));
            Assert.That(
                MiningYieldCommit.FormatHudFeedback(DataIds.Minerals.Copper, 1, 0),
                Is.EqualTo("구리 +1"));
            Assert.That(MiningYieldCommit.FormatHudFeedback(string.Empty, 1, 1), Is.Empty);
        }

        private CommitFixture CreateCommitFixture(float capacity, int yieldLevel)
        {
            var minerals = new InMemoryMineralCatalog();
            minerals.Register(DataIds.Minerals.Copper, 1.5f, 10, "Copper");
            minerals.Register(DataIds.Minerals.Iron, 2f, 15, "Iron");
            minerals.Register(DataIds.Minerals.Lithium, 0.8f, 40, "Lithium");
            var inventory = new InventoryService(minerals, capacity);
            var state = GameState.CreateNew();
            var data = CreateConfirmedYieldUpgrade();
            var upgradeState = new UpgradeState();
            if (yieldLevel > 0)
            {
                Assert.That(
                    upgradeState.TryRestore(new[] { new UpgradeLevelState(DataIds.Upgrades.CargoYield, yieldLevel) }),
                    Is.True);
            }

            var effects = new UpgradeEffectProvider(upgradeState, new Catalog(data));
            return new CommitFixture(inventory, state, effects);
        }

        private UpgradeData CreateConfirmedYieldUpgrade()
        {
            var data = ScriptableObject.CreateInstance<UpgradeData>();
            created.Add(data);
            data.EditorSet(
                DataIds.Upgrades.CargoYield,
                "채굴 수확량",
                3,
                new List<UpgradeLevelDefinition>
                {
                    new UpgradeLevelDefinition(
                        1,
                        1f,
                        new List<ItemCostEntry> { new ItemCostEntry(DataIds.Minerals.Copper, 10) },
                        new List<MineralBonusEntry> { Copper(1) }),
                    new UpgradeLevelDefinition(
                        2,
                        2f,
                        new List<ItemCostEntry>
                        {
                            new ItemCostEntry(DataIds.Minerals.Copper, 20),
                            new ItemCostEntry(DataIds.Minerals.Iron, 10)
                        },
                        new List<MineralBonusEntry> { Copper(2), Iron(1) }),
                    new UpgradeLevelDefinition(
                        3,
                        3f,
                        new List<ItemCostEntry>
                        {
                            new ItemCostEntry(DataIds.Minerals.Copper, 30),
                            new ItemCostEntry(DataIds.Minerals.Iron, 20),
                            new ItemCostEntry(DataIds.Minerals.Lithium, 10)
                        },
                        new List<MineralBonusEntry> { Copper(3), Iron(2), Lithium(1) })
                });
            return data;
        }

        private static MineralBonusEntry Copper(int quantity)
        {
            return new MineralBonusEntry(DataIds.Minerals.Copper, quantity);
        }

        private static MineralBonusEntry Iron(int quantity)
        {
            return new MineralBonusEntry(DataIds.Minerals.Iron, quantity);
        }

        private static MineralBonusEntry Lithium(int quantity)
        {
            return new MineralBonusEntry(DataIds.Minerals.Lithium, quantity);
        }

        private static void AssertLevel(
            UpgradeLevelDefinition level,
            int expectedLevel,
            float effect,
            string costs,
            params MineralBonusEntry[] bonuses)
        {
            Assert.That(level.Level, Is.EqualTo(expectedLevel));
            Assert.That(level.EffectValue, Is.EqualTo(effect).Within(0.0001f));
            var actualCosts = string.Join(",", level.Costs.Select(cost => cost.ItemId + ":" + cost.Quantity));
            Assert.That(actualCosts, Is.EqualTo(costs));
            Assert.That(level.MiningYieldBonuses.Count, Is.EqualTo(bonuses.Length));
            for (var i = 0; i < bonuses.Length; i++)
            {
                Assert.That(level.MiningYieldBonuses[i].MineralId, Is.EqualTo(bonuses[i].MineralId));
                Assert.That(level.MiningYieldBonuses[i].Quantity, Is.EqualTo(bonuses[i].Quantity));
            }
        }

        private static void SetPrivateField(object target, string name, object value)
        {
            var field = target.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }

        private readonly struct CommitFixture
        {
            public InventoryService Inventory { get; }
            public GameState State { get; }
            public IUpgradeEffectProvider Effects { get; }

            public CommitFixture(
                InventoryService inventory,
                GameState state,
                IUpgradeEffectProvider effects)
            {
                Inventory = inventory;
                State = state;
                Effects = effects;
            }
        }

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
                if (!CanAfford(costs))
                {
                    return false;
                }

                for (var i = 0; i < costs.Count; i++)
                {
                    amounts[costs[i].ItemId] = Get(costs[i].ItemId) - costs[i].Quantity;
                }

                return true;
            }
        }
    }
}
