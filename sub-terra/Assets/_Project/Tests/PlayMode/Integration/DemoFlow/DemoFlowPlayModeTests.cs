using System.Collections;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.Inventory;
using SubTerra.App.Outpost;
using SubTerra.App.Progression;
using SubTerra.App.Save;
using SubTerra.App.State;
using SubTerra.App.Tutorial;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.TestTools;

namespace SubTerra.App.Tests.PlayMode.DemoFlow
{
    /// <summary>
    /// Phase N Play Mode: 대표 이벤트 순서·중간 로드 동등 검증.
    /// Scene 전체 수동 완주 대신 Director + Save mapper 경로를 실제 코드로 구동한다.
    /// </summary>
    public sealed class DemoFlowPlayModeTests
    {
        [UnityTest]
        public IEnumerator N_F01_EventSequence_CompletesAllThirteenSteps()
        {
            var state = GameState.CreateNew();
            var director = new DemoObjectiveDirector();
            director.BindGameState(state);
            director.ResetNewGame();

            Assert.That(director.NotifyExplorationReady().Advanced, Is.True);

            var catalog = new InMemoryMineralCatalog();
            catalog.Register(DataIds.Minerals.Copper, 1f, 5);
            catalog.Register(DataIds.Minerals.Iron, 1f, 5);
            catalog.Register(DataIds.Minerals.Lithium, 1f, 8);
            var inventory = new InventoryService(catalog, 100f, state);
            inventory.InventoryChanged += director.OnInventoryChanged;

            inventory.TryAddMineral(DataIds.Minerals.Copper, 1);
            inventory.TryAddMineral(DataIds.Minerals.Iron, 1);
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.PathGuide));

            director.NotifyGuidanceAcknowledged();
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.StructuralCrack));

            director.OnGameplayEvent(new GameplayEventDto
            {
                type = GameplayEventType.StructuralRiskChanged,
                structuralIntegrity = 0.2f
            });
            director.OnGameplayEvent(new GameplayEventDto
            {
                type = GameplayEventType.BuildingPlaced,
                entityId = DataIds.Buildings.SupportBasic
            });
            director.OnGameplayEvent(new GameplayEventDto
            {
                type = GameplayEventType.GasTriggered,
                gasRisk = 1f
            });
            director.OnGasExposureChanged(GasRiskLevel.Hazard);
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.GasEncounter));
            director.OnGasExposureChanged(GasRiskLevel.Safe);
            director.OnGameplayEvent(new GameplayEventDto
            {
                type = GameplayEventType.OutpostActivated,
                instanceId = "op.1"
            });
            director.OnReturnRecommendationPresented();
            director.OnOutpostOperationCompleted(new OutpostOperationResult(
                OutpostOperationStatus.Success,
                OutpostOperationKind.SettleStorage,
                DataIds.Minerals.Copper,
                1,
                5,
                "settled"));
            // 심층 조건 충족 알림 → 실제 잠금 커밋 이벤트 (Service DidUnlockNow 경로)
            Assert.That(director.NotifyDeepZonePrerequisitesReady().Advanced, Is.True);
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.MineLithium));
            inventory.TryAddMineral(DataIds.Minerals.Lithium, 1);
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.DeepSignal));
            director.OnDeepZoneAccessChanged(new ZoneAccessResult(true, true, "deep"));
            director.NotifyDemoEndAcknowledged();

            Assert.That(director.IsDemoComplete, Is.True);
            Assert.That(state.Progress.IsDemoComplete, Is.True);
            Assert.That(state.Progress.CompletedObjectives, Is.EqualTo(13));
            yield return null;
        }

        [UnityTest]
        public IEnumerator N_F04_MidRunSaveRestore_KeepsObjectiveWithoutDoubleAdvance()
        {
            var state = GameState.CreateNew();
            var director = new DemoObjectiveDirector();
            director.BindGameState(state);
            director.ResetNewGame();
            director.NotifyExplorationReady();

            // 구리·철까지 진행한 중간 지점
            director.HandleSignal(DemoProgressSignal.CopperAndIronCollected);
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.PathGuide));
            Assert.That(state.Progress.CompletedObjectives, Is.EqualTo(2));

            var catalog = new InMemoryMineralCatalog();
            catalog.Register(DataIds.Minerals.Copper, 1f, 5);
            var inventory = new InventoryService(catalog, 50f, state);
            var upgrades = new UpgradeState();
            Assert.That(upgrades.TryRestore(System.Array.Empty<UpgradeLevelState>()), Is.True);
            Assert.That(upgrades.TryRestoreUnlockedZones(System.Array.Empty<string>()), Is.True);

            var mapper = new SaveDataMapper(new FixedClock(42));
            var data = mapper.Capture(new SaveCaptureContext(
                state,
                inventory.State,
                upgrades,
                null,
                null,
                "Mine_Demo_Integration",
                "playmode-n"));
            Assert.That(data, Is.Not.Null);
            Assert.That(data.progress.currentObjectiveId, Is.EqualTo(DemoObjectiveIds.PathGuide));

            Assert.That(mapper.TryRestore(data, out var restored), Is.True);
            var restoredDirector = new DemoObjectiveDirector();
            restoredDirector.BindGameState(restored.GameState);
            restoredDirector.RestoreFromProgress(restored.GameState.Progress);

            Assert.That(
                restoredDirector.CurrentObjectiveId,
                Is.EqualTo(DemoObjectiveIds.PathGuide));
            Assert.That(restoredDirector.CompletedCount, Is.EqualTo(2));

            // 이미 지나온 신호로 중복 전진하지 않음
            Assert.That(
                restoredDirector.HandleSignal(DemoProgressSignal.CopperAndIronCollected).Advanced,
                Is.False);
            Assert.That(restoredDirector.CompletedCount, Is.EqualTo(2));

            Assert.That(restoredDirector.NotifyGuidanceAcknowledged().Advanced, Is.True);
            Assert.That(
                restoredDirector.CurrentObjectiveId,
                Is.EqualTo(DemoObjectiveIds.StructuralCrack));
            yield return null;
        }

        private sealed class FixedClock : ISaveClock
        {
            private readonly long seconds;

            public FixedClock(long utcSeconds) => seconds = utcSeconds;

            public long UtcNowSeconds => seconds;
        }
    }
}
