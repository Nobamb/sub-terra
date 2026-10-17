using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Core;
using SubTerra.App.Core.Data;
using SubTerra.App.Integration;
using SubTerra.App.Inventory;
using SubTerra.App.Progression;
using SubTerra.App.Save;
using SubTerra.App.State;
using SubTerra.App.UI.Progression;
using SubTerra.Gameplay.Player;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.TestTools;

namespace SubTerra.App.Tests.PlayMode.Integration.Progression
{
    /// <summary>구매 성공 프레임에 UI와 Gameplay Provider가 같은 레벨을 읽는지 검증.</summary>
    public sealed class ProgressionPlayModeTests
    {
        private sealed class Catalog : IUpgradeCatalog
        {
            private readonly UpgradeData[] entries;

            public Catalog(params UpgradeData[] data)
            {
                entries = data;
            }

            public IReadOnlyList<UpgradeData> Upgrades => entries;

            public bool TryGetUpgrade(string upgradeId, out UpgradeData found)
            {
                foreach (var entry in entries)
                {
                    if (entry != null && entry.Id == upgradeId)
                    {
                        found = entry;
                        return true;
                    }
                }

                found = null;
                return false;
            }
        }

        private sealed class Wallet : IResourceWallet
        {
            public int Copper = 3;

            public bool CanAfford(IReadOnlyList<ItemCostDto> costs)
            {
                return costs != null && costs.Count == 1 && Copper >= costs[0].Quantity;
            }

            public bool TrySpend(IReadOnlyList<ItemCostDto> costs)
            {
                if (!CanAfford(costs))
                {
                    return false;
                }

                Copper -= costs[0].Quantity;
                return true;
            }
        }

        private sealed class View : IProgressionPanelView, IProgressionPurchaseFeedbackView
        {
            public UpgradeSnapshot Selected;
            public UpgradeSnapshot SelectedAtFeedback;
            public string Message;
            public bool Busy;

            public void SetUpgradeList(IReadOnlyList<UpgradeSnapshot> upgrades) { }
            public void SetSelectedUpgrade(UpgradeSnapshot upgrade) => Selected = upgrade;
            public void SetPurchaseResult(string message, string detail) => Message = message;
            public void SetDeepZoneAccess(ZoneAccessResult access) { }
            public void SetBusy(bool busy) => Busy = busy;
            public void SetVisible(bool visible) { }
            public void OnPurchaseCompleted(ProgressionPurchaseResult result) => SelectedAtFeedback = Selected;
        }

        [Test]
        public void CargoYieldPurchase_UpdatesSelectionBeforeSuccessFeedback()
        {
            var data = ScriptableObject.CreateInstance<UpgradeData>();
            var view = new View();
            var presenter = new ProgressionPanelPresenter(view);
            try
            {
                data.EditorSet(DataIds.Upgrades.CargoYield, "채굴 수확량", 1,
                    new List<UpgradeLevelDefinition>
                    {
                        new UpgradeLevelDefinition(1, 1f,
                            new List<ItemCostEntry> { new ItemCostEntry(DataIds.Minerals.Copper, 1) },
                            new List<MineralBonusEntry> { new MineralBonusEntry(DataIds.Minerals.Copper, 1) })
                    });
                var wallet = new Wallet();
                var service = new ProgressionService(new UpgradeState(), new Catalog(data), wallet);
                presenter.Bind(service);
                Assert.That(presenter.SelectUpgrade(data.Id), Is.True);

                var result = presenter.RequestPurchase();

                Assert.That(result.IsSuccess, Is.True);
                Assert.That(view.SelectedAtFeedback.CurrentLevel, Is.EqualTo(1));
                Assert.That(view.Selected.CurrentLevel, Is.EqualTo(1));
                Assert.That(service.Effects.GetMiningYieldBonus(DataIds.Minerals.Copper), Is.EqualTo(1));
                Assert.That(wallet.Copper, Is.EqualTo(2));
            }
            finally
            {
                presenter.Unbind();
                Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void PurchaseCompleted_ObservesRuntimeHealthAndRegeneration_WithoutSceneReload()
        {
            GameBootstrapper.ResetInstanceForTests();
            Assert.That(SaveRuntimeController.Instance, Is.Null);
            var bootstrapObject = new GameObject("Issue147_Bootstrap");
            var bootstrap = bootstrapObject.AddComponent<GameBootstrapper>();
            bootstrap.enabled = false;
            var state = GameState.CreateNew();
            bootstrap.TryReplaceState(state);
            var runtimeObject = new GameObject("Issue147_Runtime");
            var runtime = runtimeObject.AddComponent<SaveRuntimeController>();
            runtime.enabled = false;
            var host = new GameObject("Issue147_Survival");
            var survival = host.AddComponent<PlayerSurvivalController>();
            var controller = host.AddComponent<RunFailureRuntimeController>();
            var settings = ScriptableObject.CreateInstance<PlayerSurvivalSettings>();
            var health = CreateUpgrade(DataIds.Upgrades.MaximumHealth, 30f);
            var regeneration = CreateUpgrade(DataIds.Upgrades.HealthRegeneration, 0.3f);
            try
            {
                survival.Configure(settings, host.transform);
                var service = new ProgressionService(
                    new UpgradeState(), new Catalog(health, regeneration), new Wallet());
                typeof(SaveRuntimeController).GetField("progression", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(runtime, service);
                typeof(SaveRuntimeController).GetField("inventoryService", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(runtime, new InventoryService(new InMemoryMineralCatalog(), 50f, state));
                typeof(RunFailureRuntimeController).GetField("survivalController", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(controller, survival);
                controller.Bind(runtime, state);
                var completed = 0;
                service.PurchaseCompleted += result =>
                {
                    completed++;
                    Assert.That(result.IsSuccess, Is.True);
                    Assert.That(survival.State.MaximumHealth, Is.EqualTo(130));
                    if (result.UpgradeId == DataIds.Upgrades.HealthRegeneration)
                    {
                        Assert.That(survival.State.AdvanceRegeneration(1f), Is.True);
                        Assert.That(survival.State.Health, Is.EqualTo(120.3f).Within(0.001f));
                    }
                };

                Assert.That(service.TryPurchase(health.Id).IsSuccess, Is.True);
                Assert.That(survival.State.Health, Is.EqualTo(130f));
                survival.State.TryApplyDamage(RunFailureCause.Fall, 10, 0f, 0f, false, out _);
                Assert.That(service.TryPurchase(regeneration.Id).IsSuccess, Is.True);
                Assert.That(completed, Is.EqualTo(2));
            }
            finally
            {
                controller.Unbind();
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(runtimeObject);
                Object.DestroyImmediate(bootstrapObject);
                Object.DestroyImmediate(settings);
                Object.DestroyImmediate(health);
                Object.DestroyImmediate(regeneration);
                GameBootstrapper.ResetInstanceForTests();
            }
        }

        private static UpgradeData CreateUpgrade(string id, float effect)
        {
            var data = ScriptableObject.CreateInstance<UpgradeData>();
            data.EditorSet(id, id, 1, new List<UpgradeLevelDefinition>
            {
                new UpgradeLevelDefinition(1, effect, new List<ItemCostEntry>
                {
                    new ItemCostEntry(DataIds.Minerals.Copper, 1)
                })
            });
            return data;
        }

        [UnityTest]
        public IEnumerator Purchase_UpdatesUiAndProviderInSameFrame()
        {
            var data = ScriptableObject.CreateInstance<UpgradeData>();
            data.EditorSet(
                DataIds.Upgrades.DrillSpeed,
                "드릴 속도",
                1,
                new List<UpgradeLevelDefinition>
                {
                    new UpgradeLevelDefinition(
                        1,
                        0.25f,
                        new List<ItemCostEntry>
                        {
                            new ItemCostEntry(DataIds.Minerals.Copper, 2)
                        })
                });
            var state = new UpgradeState();
            var wallet = new Wallet();
            var service = new ProgressionService(state, new Catalog(data), wallet);
            var view = new View();
            var presenter = new ProgressionPanelPresenter(view);
            presenter.Bind(service);
            Assert.That(presenter.SelectUpgrade(DataIds.Upgrades.DrillSpeed), Is.True);

            var result = presenter.RequestPurchase();

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(view.Selected.CurrentLevel, Is.EqualTo(1));
            Assert.That(view.Selected.CurrentEffectValue, Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(service.Effects.GetDrillSpeedMultiplier(), Is.EqualTo(1.25f).Within(0.0001f));
            Assert.That(wallet.Copper, Is.EqualTo(1));
            Assert.That(view.Message, Does.Contain("완료"));
            Assert.That(view.Busy, Is.False);

            Object.Destroy(data);
            yield return null;
        }
    }
}
