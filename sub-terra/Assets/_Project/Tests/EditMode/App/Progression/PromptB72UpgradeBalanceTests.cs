using System.Linq;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.Progression;
using SubTerra.Gameplay.Mining;
using UnityEditor;

namespace SubTerra.App.Tests.Progression
{
    public sealed class PromptB72UpgradeBalanceTests
    {
        private const string CatalogPath =
            "Assets/_Project/Data/Catalog/GameDataCatalog.asset";

        [Test]
        public void Catalog_UsesReworkedEffectsAndTieredMineralCosts()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameDataCatalog>(CatalogPath);
            Assert.That(catalog, Is.Not.Null);

            AssertUpgrade(catalog, DataIds.Upgrades.DrillSpeed,
                new[] { 0.25f, 2f / 3f, 11f / 9f },
                "mineral.copper:8", "mineral.copper:10,mineral.iron:6", "mineral.iron:12,mineral.lithium:8");
            AssertUpgrade(catalog, DataIds.Upgrades.DrillEfficiency,
                new[] { 0.2f, 0.35f, 0.5f },
                "mineral.copper:6", "mineral.copper:8,mineral.iron:5", "mineral.iron:10,mineral.lithium:6");
            AssertUpgrade(catalog, DataIds.Upgrades.MaximumEnergy,
                new[] { 50f, 110f, 180f },
                "mineral.copper:8", "mineral.copper:8,mineral.iron:6", "mineral.iron:10,mineral.lithium:8");
            AssertUpgrade(catalog, DataIds.Upgrades.MaximumCargo,
                new[] { 30f, 70f, 120f },
                "mineral.copper:6", "mineral.copper:8,mineral.iron:5", "mineral.iron:8,mineral.lithium:6");
            AssertUpgrade(catalog, DataIds.Upgrades.GasResistance,
                new[] { 0.25f, 0.5f, 0.75f },
                "mineral.copper:6", "mineral.copper:6,mineral.iron:4", "mineral.iron:8,mineral.lithium:6");
            AssertUpgrade(catalog, DataIds.Upgrades.DroneScan,
                new[] { 3f, 7f },
                "mineral.copper:6", "mineral.copper:6,mineral.iron:5");
            AssertUpgrade(catalog, DataIds.Upgrades.DroneRescue,
                new[] { 0.35f, 0.7f },
                "mineral.copper:8", "mineral.iron:8,mineral.lithium:6");
            AssertUpgrade(catalog, DataIds.Upgrades.MaximumHealth,
                new[] { 40f, 80f, 130f },
                "mineral.copper:6", "mineral.copper:6,mineral.iron:4", "mineral.iron:8,mineral.lithium:5");
            AssertUpgrade(catalog, DataIds.Upgrades.HealthRegeneration,
                new[] { 1f, 2f, 3f },
                "mineral.copper:6", "mineral.copper:6,mineral.iron:4", "mineral.iron:6,mineral.lithium:5");
        }

        [Test]
        public void DeepZone_RequiresOnlyProgressAndDrillLevelTwo()
        {
            var requirements = DeepZoneUnlockRule.Mvp.UpgradeRequirements;

            Assert.That(requirements.Count, Is.EqualTo(1));
            Assert.That(requirements[0].UpgradeId, Is.EqualTo(DataIds.Upgrades.DrillSpeed));
            Assert.That(requirements[0].RequiredLevel, Is.EqualTo(2));
        }

        [TestCase(1.25f, 16)]
        [TestCase(1f / 0.65f, 13)]
        [TestCase(2f, 10)]
        public void DrillEfficiency_TenMiningActionsReduceActualTotalEnergy(
            float efficiencyMultiplier,
            int expectedTotal)
        {
            var remainder = 0f;
            var total = 0;
            for (var i = 0; i < 10; i++)
            {
                total += MiningEnergyCostCalculator.Calculate(
                    2,
                    efficiencyMultiplier,
                    remainder,
                    out remainder);
            }

            Assert.That(total, Is.EqualTo(expectedTotal));
        }

        private static void AssertUpgrade(
            GameDataCatalog catalog,
            string id,
            float[] effects,
            params string[] costs)
        {
            Assert.That(catalog.TryGetUpgrade(id, out var upgrade), Is.True, id);
            Assert.That(upgrade.Levels.Count, Is.EqualTo(effects.Length), id);
            Assert.That(costs.Length, Is.EqualTo(effects.Length), id);

            for (var i = 0; i < effects.Length; i++)
            {
                Assert.That(upgrade.Levels[i].EffectValue, Is.EqualTo(effects[i]).Within(0.0001f), $"{id} Lv.{i + 1}");
                var actualCosts = string.Join(",", upgrade.Levels[i].Costs.Select(
                    cost => $"{cost.ItemId}:{cost.Quantity}"));
                Assert.That(actualCosts, Is.EqualTo(costs[i]), $"{id} Lv.{i + 1}");
            }
        }
    }
}
