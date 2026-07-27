using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.Progression;
using SubTerra.App.UI.Progression;
using SubTerra.Shared;
using UnityEditor;
using UnityEngine;

namespace SubTerra.App.Tests.Progression
{
    /// <summary>F-S01~F-S05 정적/소유권 검증.</summary>
    public sealed class ProgressionStaticStructureTests
    {
        [Test]
        public void F_S01_CatalogContainsAllSevenMvpUpgradeDefinitions()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameDataCatalog>(
                "Assets/_Project/Data/Catalog/GameDataCatalog.asset");
            Assert.That(catalog, Is.Not.Null);

            var validation = catalog.ValidateAll();
            Assert.That(validation.IsValid, Is.True, validation.FormatAll());

            var required = new[]
            {
                DataIds.Upgrades.DrillSpeed,
                DataIds.Upgrades.DrillEfficiency,
                DataIds.Upgrades.MaximumEnergy,
                DataIds.Upgrades.MaximumCargo,
                DataIds.Upgrades.DroneScan,
                DataIds.Upgrades.DroneRescue,
                DataIds.Upgrades.GasResistance
            };
            Assert.That(catalog.Upgrades.Count, Is.GreaterThanOrEqualTo(required.Length));
            foreach (var id in required)
            {
                Assert.That(catalog.TryGetUpgrade(id, out var data), Is.True, id);
                Assert.That(data.Levels.Count, Is.EqualTo(data.MaxLevel), id);
            }

            Assert.That(required.Distinct().Count(), Is.EqualTo(7));
        }

        [Test]
        public void F_S02_DefinitionAndStateAreSeparated()
        {
            var definitionFields = typeof(UpgradeData)
                .GetFields(System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.Public
                    | System.Reflection.BindingFlags.NonPublic)
                .Select(field => field.Name)
                .ToArray();
            Assert.That(definitionFields, Does.Not.Contain("currentLevel"));
            Assert.That(definitionFields, Does.Not.Contain("unlockedZoneIds"));

            Assert.That(typeof(UpgradeState).IsSerializable, Is.True);
            Assert.That(typeof(UpgradeState).GetMethod(nameof(UpgradeState.GetLevel)), Is.Not.Null);
            Assert.That(typeof(UpgradeState).GetMethod(nameof(UpgradeState.IsZoneUnlocked)), Is.Not.Null);
        }

        [Test]
        public void F_S03_SharedProviderBoundaryHasNoAppConcreteType()
        {
            Assert.That(typeof(IUpgradeEffectProvider).Assembly.GetName().Name, Is.EqualTo("SubTerra.Shared"));
            var methods = typeof(IUpgradeEffectProvider).GetMethods();
            Assert.That(methods.Length, Is.EqualTo(7));
            foreach (var method in methods)
            {
                Assert.That(method.ReturnType.Namespace, Does.Not.StartWith("SubTerra.App"));
                foreach (var parameter in method.GetParameters())
                {
                    Assert.That(parameter.ParameterType.Namespace, Does.Not.StartWith("SubTerra.App"));
                }
            }

            var gameplayRoot = Path.Combine(Application.dataPath, "_Project", "Scripts", "Gameplay");
            foreach (var path in Directory.GetFiles(gameplayRoot, "*.cs", SearchOption.AllDirectories))
            {
                var source = File.ReadAllText(path);
                Assert.That(source, Does.Not.Contain("SubTerra.App.Progression"), path);
                Assert.That(source, Does.Not.Contain("ProgressionService"), path);
            }
        }

        [Test]
        public void F_S04_PurchaseSourceValidatesBeforeSpendAndThenCommitsLevel()
        {
            var path = Path.Combine(
                Application.dataPath,
                "_Project",
                "Scripts",
                "App",
                "Progression",
                "ProgressionService.cs");
            var source = File.ReadAllText(path);
            var validateIndex = source.IndexOf("CostAggregator.TryNormalize");
            var affordIndex = source.IndexOf("wallet.CanAfford");
            var spendIndex = source.IndexOf("wallet.TrySpend");
            var levelIndex = source.IndexOf("state.ApplyPurchasedLevel");

            Assert.That(validateIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(affordIndex, Is.GreaterThan(validateIndex));
            Assert.That(spendIndex, Is.GreaterThan(affordIndex));
            Assert.That(levelIndex, Is.GreaterThan(spendIndex));
        }

        [Test]
        public void F_S05_UiPresenterOnlyCallsProgressionService()
        {
            var path = Path.Combine(
                Application.dataPath,
                "_Project",
                "Scripts",
                "App",
                "UI",
                "Progression",
                "ProgressionPanelPresenter.cs");
            var source = File.ReadAllText(path);
            Assert.That(source, Does.Contain("service.TryPurchase"));
            Assert.That(source, Does.Not.Contain("ApplyPurchasedLevel"));
            Assert.That(source, Does.Not.Contain("TrySpend"));
            Assert.That(source, Does.Not.Contain("SetQuantity"));
            Assert.That(typeof(IProgressionPanelView).GetMethods().All(m => !m.Name.Contains("Spend")), Is.True);
        }
    }
}
