using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.Editor.DataValidation;
using SubTerra.App.Progression;
using SubTerra.Gameplay.Player;
using SubTerra.Shared;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Tests.Progression
{
    /// <summary>prompt-B 200: 최대 산소·산소 소모 감소 업그레이드.</summary>
    public sealed class PromptB200OxygenUpgradeTests
    {
        private const string CatalogPath = "Assets/_Project/Data/Catalog/GameDataCatalog.asset";

        // 현재 설정 기본값(0.5 / 0.02)과 #334 이동·만재 배율. 검사기 값과 무관하게 공식만 확인한다.
        private const float BaseRate = 0.5f;
        private const float PerMeter = 0.02f;

        private readonly List<Object> created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (var i = 0; i < created.Count; i++)
            {
                if (created[i] != null)
                {
                    Object.DestroyImmediate(created[i]);
                }
            }

            created.Clear();
        }

        // ------------------------------------------------------------------ 데이터·트리

        [Test]
        public void ShippedCatalog_ContainsBothOxygenUpgradesWithDraftValues()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameDataCatalog>(CatalogPath);
            Assert.That(catalog, Is.Not.Null);

            AssertUpgrade(catalog, DataIds.Upgrades.MaximumOxygen,
                new[] { 40f, 80f, 130f },
                "mineral.copper:6", "mineral.copper:6,mineral.iron:4", "mineral.iron:8,mineral.lithium:5");
            AssertUpgrade(catalog, DataIds.Upgrades.OxygenEfficiency,
                new[] { 0.15f, 0.30f, 0.45f },
                "mineral.copper:6", "mineral.copper:8,mineral.iron:5", "mineral.iron:10,mineral.lithium:6");

            var validation = catalog.ValidateAll();
            Assert.That(validation.IsValid, Is.True, validation.FormatAll());
        }

        [Test]
        public void ShippedTree_ChainsOxygenUnderMaximumEnergyInCapacityTab()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameDataCatalog>(CatalogPath);
            Assert.That(catalog.TryGetUpgrade(DataIds.Upgrades.MaximumOxygen, out var maximum), Is.True);
            Assert.That(catalog.TryGetUpgrade(DataIds.Upgrades.OxygenEfficiency, out var efficiency), Is.True);

            Assert.That(maximum.TreeParentId, Is.EqualTo(DataIds.Upgrades.MaximumEnergy));
            Assert.That(maximum.UnlockRequiredLevel, Is.EqualTo(1));
            Assert.That(efficiency.TreeParentId, Is.EqualTo(DataIds.Upgrades.MaximumOxygen));
            Assert.That(efficiency.UnlockRequiredLevel, Is.EqualTo(1));

            Assert.That(UpgradeCategoryRules.Resolve(DataIds.Upgrades.MaximumOxygen), Is.EqualTo(UpgradeCategory.Capacity));
            Assert.That(UpgradeCategoryRules.Resolve(DataIds.Upgrades.OxygenEfficiency), Is.EqualTo(UpgradeCategory.Capacity));
        }

        [Test]
        public void TreeNodes_DoNotOverlapEachOther()
        {
            var nodes = PromptB118UpgradeTreeBuilder.Nodes;
            for (var i = 0; i < nodes.Length; i++)
            {
                for (var j = i + 1; j < nodes.Length; j++)
                {
                    Assert.That(nodes[i].Position, Is.Not.EqualTo(nodes[j].Position),
                        nodes[i].Id + " / " + nodes[j].Id);
                }
            }
        }

        [Test]
        public void ExistingUpgradeIds_AreUnchanged()
        {
            Assert.That(DataIds.Upgrades.MaximumEnergy, Is.EqualTo("upgrade.energy.maximum"));
            Assert.That(DataIds.Upgrades.MaximumHealth, Is.EqualTo("upgrade.health.maximum"));
            Assert.That(DataIds.Upgrades.GasResistance, Is.EqualTo("upgrade.gas.resistance"));
            Assert.That(DataIds.Upgrades.MaximumOxygen, Is.EqualTo("upgrade.oxygen.maximum"));
            Assert.That(DataIds.Upgrades.OxygenEfficiency, Is.EqualTo("upgrade.oxygen.efficiency"));
        }

        // ------------------------------------------------------------------ 표시 문구

        [Test]
        public void Formatter_ShowsMaximumAsPlusAndEfficiencyAsMinusPercent()
        {
            Assert.That(UpgradeEffectFormatter.FormatValue(DataIds.Upgrades.MaximumOxygen, 40f), Is.EqualTo("+40"));
            Assert.That(UpgradeEffectFormatter.FormatValue(DataIds.Upgrades.OxygenEfficiency, 0.15f), Is.EqualTo("-15%"));
            Assert.That(UpgradeEffectFormatter.FormatValue(DataIds.Upgrades.OxygenEfficiency, 0f), Is.EqualTo("없음"));
            Assert.That(ItemDisplayNames.Upgrade(DataIds.Upgrades.MaximumOxygen), Is.EqualTo("최대 산소"));
            Assert.That(ItemDisplayNames.Upgrade(DataIds.Upgrades.OxygenEfficiency), Is.EqualTo("산소 소모 감소"));
            Assert.That(ItemDisplayNames.UpgradeDescription(DataIds.Upgrades.MaximumOxygen), Is.Not.Empty);
            Assert.That(ItemDisplayNames.UpgradeDescription(DataIds.Upgrades.OxygenEfficiency), Is.Not.Empty);
        }

        // ------------------------------------------------------------------ 제공자

        [Test]
        public void Provider_AddsMaximumBonusAndCapsReduction()
        {
            var provider = CreateProvider((DataIds.Upgrades.MaximumOxygen, 40f), (DataIds.Upgrades.OxygenEfficiency, 0.15f));

            Assert.That(provider.GetMaximumOxygen(100f), Is.EqualTo(140f).Within(0.0001f));
            Assert.That(provider.GetOxygenDrainReduction(), Is.EqualTo(0.15f).Within(0.0001f));

            var broken = CreateProvider((DataIds.Upgrades.OxygenEfficiency, 5f));
            Assert.That(broken.GetOxygenDrainReduction(), Is.LessThan(1f));
            Assert.That(broken.GetOxygenDrainReduction(), Is.EqualTo(UpgradeEffectProvider.MaximumOxygenDrainReduction));
        }

        [Test]
        public void Provider_WithoutPurchases_KeepsBaseValues()
        {
            var provider = CreateProvider();

            Assert.That(provider.GetMaximumOxygen(100f), Is.EqualTo(100f));
            Assert.That(provider.GetOxygenDrainReduction(), Is.Zero);
        }

        // ------------------------------------------------------------------ 산소 상태

        [Test]
        public void ApplyMaximum_RaisesCurrentByIncreaseOnly_AndClampsOnDecrease()
        {
            var oxygen = new PlayerOxygenState(100f);
            oxygen.Advance(40f, 12f, 1f, 0f, 1f, 1f);
            Assert.That(oxygen.Remaining, Is.EqualTo(60d).Within(0.0001d));

            Assert.That(oxygen.ApplyMaximum(140f), Is.True);
            Assert.That(oxygen.Maximum, Is.EqualTo(140f));
            Assert.That(oxygen.Remaining, Is.EqualTo(100d).Within(0.0001d), "증가분(+40)만큼만 오르고 전부 채우지 않는다");

            Assert.That(oxygen.ApplyMaximum(140f), Is.False);

            Assert.That(oxygen.ApplyMaximum(80f), Is.True);
            Assert.That(oxygen.Remaining, Is.EqualTo(80d).Within(0.0001d));
        }

        [Test]
        public void Controller_MaximumUpgrade_UpdatesHudMaximumWithoutRefilling()
        {
            var host = new GameObject("PromptB200_MaxOxygen");
            created.Add(host);
            var survival = host.AddComponent<PlayerSurvivalController>();
            survival.BindOxygenDepth(() => 12f);
            survival.RestoreOxygen(50f);

            var events = new List<(float current, float max)>();
            survival.OxygenChanged += (current, max) => events.Add((current, max));
            survival.BindOxygenUpgradeEffects(new FixedOxygenUpgrades(40f, 0f));

            Assert.That(survival.OxygenMaximum, Is.EqualTo(140f));
            Assert.That(survival.OxygenRemaining, Is.EqualTo(90f).Within(0.0001f));
            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events[0].max, Is.EqualTo(140f));

            survival.RefillOxygen();
            Assert.That(survival.OxygenRemaining, Is.EqualTo(140f));
        }

        [Test]
        public void Controller_UpgradeBoundBeforeOxygenState_StartsAtUpgradedMaximum()
        {
            var host = new GameObject("PromptB200_MaxOxygenFirst");
            created.Add(host);
            var survival = host.AddComponent<PlayerSurvivalController>();
            survival.BindOxygenUpgradeEffects(new FixedOxygenUpgrades(80f, 0f));
            survival.BindOxygenDepth(() => 12f);

            Assert.That(survival.OxygenMaximum, Is.EqualTo(180f));
            Assert.That(survival.OxygenRemaining, Is.EqualTo(180f));
        }

        // ------------------------------------------------------------------ 소모 감소

        [Test]
        public void Controller_Reduction_AppliesAfterWorkloadAndGasMultipliers()
        {
            const float seconds = 10f;
            const float depth = 12f;
            var baseline = DrainOver(seconds, depth, cargo: 1f, gas: true, reduction: 0f);
            var reduced = DrainOver(seconds, depth, cargo: 1f, gas: true, reduction: 0.3f);

            Assert.That(baseline, Is.GreaterThan(0f));
            Assert.That(reduced, Is.EqualTo(baseline * 0.7f).Within(0.001f));

            // 가스 2배·만재 배율이 그대로 남은 상태에서 곱해졌는지 직접 계산과 비교한다.
            var rate = (BaseRate + PerMeter * Mathf.Round(depth)) * 1.5f * 2f;
            Assert.That(baseline, Is.EqualTo(rate * seconds).Within(0.001f));
        }

        [Test]
        public void Controller_Reduction_DoesNotChangeShallowRecovery()
        {
            var plain = RecoveredOver(5f, reduction: 0f);
            var reduced = RecoveredOver(5f, reduction: 0.45f);

            Assert.That(plain, Is.GreaterThan(0f));
            Assert.That(reduced, Is.EqualTo(plain).Within(0.0001f));
        }

        [Test]
        public void Controller_MaxedReduction_StillDrainsDeeperFasterAndNeverStops()
        {
            const float seconds = 10f;
            var shallow = DrainOver(seconds, 12f, cargo: 0f, gas: false, reduction: 0.45f);
            var deep = DrainOver(seconds, 200f, cargo: 0f, gas: false, reduction: 0.45f);

            Assert.That(shallow, Is.GreaterThan(0f), "만렙이어도 소모가 0이 되지 않는다");
            Assert.That(deep, Is.GreaterThan(shallow), "만렙이어도 더 깊은 곳이 더 빨리 닳는다");
            // 만렙 45% 감소여도 깊이 200M의 소모는 감소 없는 깊이 12M보다 크다.
            var unreducedShallow = DrainOver(seconds, 12f, cargo: 0f, gas: false, reduction: 0f);
            Assert.That(deep, Is.GreaterThan(unreducedShallow));
        }

        [Test]
        public void Controller_ReductionFromProvider_IsCappedBelowOne()
        {
            var absurd = DrainOver(10f, 50f, cargo: 0f, gas: false, reduction: 5f);
            Assert.That(absurd, Is.GreaterThan(0f));
        }

        // ------------------------------------------------------------------ 보조

        private static float DrainOver(float seconds, float depth, float cargo, bool gas, float reduction)
        {
            var host = new GameObject("PromptB200_Drain");
            try
            {
                var survival = host.AddComponent<PlayerSurvivalController>();
                var exposed = gas;
                survival.BindOxygenDepth(() => depth, () => exposed);
                survival.BindOxygenUpgradeEffects(new FixedOxygenUpgrades(300f, reduction));
                survival.RefillOxygen();
                survival.SetOxygenCargoLoadRatio(cargo);

                var start = survival.OxygenRemaining;
                Tick(survival, seconds);
                return start - survival.OxygenRemaining;
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        private static float RecoveredOver(float seconds, float reduction)
        {
            var host = new GameObject("PromptB200_Recover");
            try
            {
                var survival = host.AddComponent<PlayerSurvivalController>();
                survival.BindOxygenDepth(() => 5f);
                survival.BindOxygenUpgradeEffects(new FixedOxygenUpgrades(0f, reduction));
                survival.RestoreOxygen(10f);

                var start = survival.OxygenRemaining;
                Tick(survival, seconds);
                return survival.OxygenRemaining - start;
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        private static void Tick(PlayerSurvivalController survival, float seconds)
        {
            var tick = typeof(PlayerSurvivalController).GetMethod("AdvanceOxygen",
                BindingFlags.Instance | BindingFlags.NonPublic);
            tick.Invoke(survival, new object[] { seconds });
        }

        private UpgradeEffectProvider CreateProvider(params (string id, float effect)[] purchased)
        {
            var upgrades = new List<UpgradeData>();
            var entries = new List<UpgradeLevelState>();
            for (var i = 0; i < purchased.Length; i++)
            {
                var data = ScriptableObject.CreateInstance<UpgradeData>();
                created.Add(data);
                data.EditorSet(purchased[i].id, purchased[i].id, 1, new List<UpgradeLevelDefinition>
                {
                    new UpgradeLevelDefinition(1, purchased[i].effect, new List<ItemCostEntry>())
                });
                upgrades.Add(data);
                entries.Add(new UpgradeLevelState(purchased[i].id, 1));
            }

            var state = new UpgradeState();
            Assert.That(state.TryRestore(entries), Is.True);
            return new UpgradeEffectProvider(state, new ListCatalog(upgrades));
        }

        private static void AssertUpgrade(GameDataCatalog catalog, string id, float[] effects, params string[] costs)
        {
            Assert.That(catalog.TryGetUpgrade(id, out var upgrade), Is.True, id);
            Assert.That(upgrade.Levels.Count, Is.EqualTo(effects.Length), id);
            for (var i = 0; i < effects.Length; i++)
            {
                Assert.That(upgrade.Levels[i].EffectValue, Is.EqualTo(effects[i]).Within(0.0001f), $"{id} Lv.{i + 1}");
                var actual = string.Join(",", upgrade.Levels[i].Costs.Select(c => $"{c.ItemId}:{c.Quantity}"));
                Assert.That(actual, Is.EqualTo(costs[i]), $"{id} Lv.{i + 1}");
            }
        }

        private sealed class FixedOxygenUpgrades : IPlayerOxygenUpgradeProvider
        {
            private readonly float bonus;
            private readonly float reduction;

            public FixedOxygenUpgrades(float bonus, float reduction)
            {
                this.bonus = bonus;
                this.reduction = reduction;
            }

            public float GetMaximumOxygen(float baseMaximum) => baseMaximum + bonus;
            public float GetOxygenDrainReduction() => reduction;
        }

        private sealed class ListCatalog : IUpgradeCatalog
        {
            private readonly IReadOnlyList<UpgradeData> upgrades;

            public ListCatalog(IReadOnlyList<UpgradeData> upgrades)
            {
                this.upgrades = upgrades;
            }

            public IReadOnlyList<UpgradeData> Upgrades => upgrades;

            public bool TryGetUpgrade(string upgradeId, out UpgradeData data)
            {
                data = upgrades.FirstOrDefault(u => u != null && u.Id == upgradeId);
                return data != null;
            }
        }
    }
}
