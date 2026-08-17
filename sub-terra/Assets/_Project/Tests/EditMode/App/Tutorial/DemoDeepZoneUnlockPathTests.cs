using System.Collections.Generic;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.Progression;
using SubTerra.App.State;
using SubTerra.App.Tutorial;
using SubTerra.App.UI.Progression;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.App.Tests.Tutorial
{
    /// <summary>
    /// N deep_signal: ProgressionService.TryUnlockDeepZone 실경로로 Director가 전진하는지 검증.
    /// ZoneAccessResult를 수동 주입하지 않는다.
    /// </summary>
    public sealed class DemoDeepZoneUnlockPathTests
    {
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
        public void N_DeepSignal_AdvancesOnlyViaRealTryUnlockDeepZone()
        {
            var drill = CreateUpgrade(DataIds.Upgrades.DrillSpeed, 2, 0.1f);
            var droneScan = CreateUpgrade(DataIds.Upgrades.DroneScan, 2, 1f);
            var gas = CreateUpgrade(DataIds.Upgrades.GasResistance, 1, 0.2f);
            var maxEnergy = CreateUpgrade(DataIds.Upgrades.MaximumEnergy, 1, 20f);
            var wallet = new TestWallet();
            wallet.Set(DataIds.Minerals.Copper, 50);

            var upgradeState = new UpgradeState();
            var service = new ProgressionService(
                upgradeState,
                new TestCatalog(drill, droneScan, gas, maxEnergy),
                wallet);

            var gameState = GameState.CreateNew();
            var director = new DemoObjectiveDirector();
            director.BindGameState(gameState);
            director.ResetNewGame();

            // 정산 직후 업그레이드 목표까지 전진
            AdvanceTo(director, DemoObjectiveIds.BatteryUpgrade);
            gameState.SetDemoProgress(
                director.CurrentObjectiveId,
                director.CompletedCount,
                director.IsDemoComplete);

            // 생산 Presenter와 동일: 구매 성공 시 completedObjectives로 TryUnlockDeepZone
            var view = new RecordingProgressionView();
            var presenter = new ProgressionPanelPresenter(view);
            presenter.Bind(service, () => gameState.Progress.CompletedObjectives);

            // Director는 Service 이벤트를 구독 (Binder와 동일)
            service.DeepZoneAccessChanged += director.OnDeepZoneAccessChanged;
            service.PurchaseCompleted += result =>
            {
                director.OnProgressionPurchaseCompleted(result);
                if (!result.IsSuccess)
                {
                    return;
                }

                // TutorialDirectorBinder.EvaluateDeepZoneProgress 와 동일 순서
                var completed = gameState.Progress.CompletedObjectives;
                var access = service.GetDeepZoneAccess(completed);
                if (access.IsUnlocked)
                {
                    director.NotifyDeepZonePrerequisitesReady();
                    gameState.SetDemoProgress(
                        director.CurrentObjectiveId,
                        director.CompletedCount,
                        director.IsDemoComplete);
                }

                service.TryUnlockDeepZone(completed);
            };

            // 1) MaximumEnergy만 구매 → Mvp 조건 미충족 → 목표·잠금 모두 불변
            Assert.That(presenter.SelectUpgrade(DataIds.Upgrades.MaximumEnergy), Is.True);
            var energyBuy = presenter.RequestPurchase();
            Assert.That(energyBuy.IsSuccess, Is.True);
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.BatteryUpgrade));
            Assert.That(upgradeState.IsZoneUnlocked(DataIds.Zones.Deep), Is.False);

            // 2) 드론 스캔 1 → 아직 부족
            Assert.That(presenter.SelectUpgrade(DataIds.Upgrades.DroneScan), Is.True);
            Assert.That(presenter.RequestPurchase().IsSuccess, Is.True);
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.BatteryUpgrade));
            Assert.That(upgradeState.IsZoneUnlocked(DataIds.Zones.Deep), Is.False);

            // 3) 드론 스캔 2 + 가스 저항 1만으로는 드릴 조건이 부족하다.
            Assert.That(presenter.RequestPurchase().IsSuccess, Is.True); // DroneScan L2
            Assert.That(presenter.SelectUpgrade(DataIds.Upgrades.GasResistance), Is.True);
            var gasBuy = presenter.RequestPurchase();
            Assert.That(gasBuy.IsSuccess, Is.True);
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.BatteryUpgrade));
            Assert.That(upgradeState.IsZoneUnlocked(DataIds.Zones.Deep), Is.False);

            // 4) 리튬 채굴에 필요한 드릴 2레벨까지 구매하면 잠금이 커밋된다.
            Assert.That(presenter.SelectUpgrade(DataIds.Upgrades.DrillSpeed), Is.True);
            Assert.That(presenter.RequestPurchase().IsSuccess, Is.True);
            Assert.That(upgradeState.IsZoneUnlocked(DataIds.Zones.Deep), Is.False);
            Assert.That(presenter.RequestPurchase().IsSuccess, Is.True);

            Assert.That(
                upgradeState.IsZoneUnlocked(DataIds.Zones.Deep),
                Is.True,
                "TryUnlockDeepZone must commit zone.deep");
            Assert.That(
                director.CurrentObjectiveId,
                Is.EqualTo(DemoObjectiveIds.MineLithium),
                "업그레이드 성공이 후반 리튬 목표를 건너뛰면 안 된다.");

            director.HandleSignal(DemoProgressSignal.LithiumCollected);
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.DeepSignal));
            director.NotifyDeepZoneAlreadyUnlocked();
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.DemoEnd));
            Assert.That(director.IsDemoComplete || director.CurrentObjectiveId == DemoObjectiveIds.DemoEnd, Is.True);
        }

        [Test]
        public void N_ProgressionPresenter_CallsTryUnlockOnPurchaseSuccess()
        {
            var drill = CreateUpgrade(DataIds.Upgrades.DrillSpeed, 2, 0.1f);
            var droneScan = CreateUpgrade(DataIds.Upgrades.DroneScan, 2, 1f);
            var gas = CreateUpgrade(DataIds.Upgrades.GasResistance, 1, 0.2f);
            var wallet = new TestWallet();
            wallet.Set(DataIds.Minerals.Copper, 50);
            var upgradeState = new UpgradeState();
            // 미리 조건을 거의 맞춤: 스캔 2, 가스 0
            Assert.That(
                upgradeState.TryRestore(new[]
                {
                    new UpgradeLevelState(DataIds.Upgrades.DrillSpeed, 2),
                    new UpgradeLevelState(DataIds.Upgrades.DroneScan, 2)
                }),
                Is.True);

            var service = new ProgressionService(
                upgradeState,
                new TestCatalog(drill, droneScan, gas),
                wallet);
            var unlockedEvents = 0;
            service.DeepZoneAccessChanged += _ => unlockedEvents++;

            var view = new RecordingProgressionView();
            var presenter = new ProgressionPanelPresenter(view);
            // completedObjectives >= Mvp required (1)
            presenter.Bind(service, () => 10);

            Assert.That(presenter.SelectUpgrade(DataIds.Upgrades.GasResistance), Is.True);
            var result = presenter.RequestPurchase();
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(upgradeState.IsZoneUnlocked(DataIds.Zones.Deep), Is.True);
            Assert.That(unlockedEvents, Is.EqualTo(1), "TryUnlockDeepZone must raise DeepZoneAccessChanged once");
        }

        private static void AdvanceTo(DemoObjectiveDirector director, string targetId)
        {
            var guard = 0;
            while (director.CurrentObjectiveId != targetId && guard++ < 20)
            {
                Assert.That(
                    DemoObjectiveCatalog.TryGet(director.CurrentObjectiveId, out var def),
                    Is.True);
                Assert.That(director.HandleSignal(def.RequiredSignal).Advanced, Is.True, def.Id);
            }

            Assert.That(director.CurrentObjectiveId, Is.EqualTo(targetId));
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

        private sealed class TestCatalog : IUpgradeCatalog
        {
            private readonly List<UpgradeData> upgrades;

            public TestCatalog(params UpgradeData[] items)
            {
                upgrades = new List<UpgradeData>(items);
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

        private sealed class TestWallet : IResourceWallet
        {
            private readonly Dictionary<string, int> amounts = new Dictionary<string, int>();

            public void Set(string itemId, int quantity) => amounts[itemId] = quantity;

            public bool CanAfford(IReadOnlyList<ItemCostDto> costs)
            {
                if (costs == null)
                {
                    return false;
                }

                for (var i = 0; i < costs.Count; i++)
                {
                    if (!amounts.TryGetValue(costs[i].ItemId, out var owned)
                        || owned < costs[i].Quantity)
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
                    amounts[costs[i].ItemId] -= costs[i].Quantity;
                }

                return true;
            }
        }

        private sealed class RecordingProgressionView : IProgressionPanelView
        {
            public void SetBusy(bool busy)
            {
            }

            public void SetVisible(bool visible)
            {
            }

            public void SetPurchaseResult(string message, string detail)
            {
            }

            public void SetUpgradeList(IReadOnlyList<UpgradeSnapshot> upgrades)
            {
            }

            public void SetSelectedUpgrade(UpgradeSnapshot upgrade)
            {
            }

            public void SetDeepZoneAccess(ZoneAccessResult access)
            {
            }
        }
    }
}
