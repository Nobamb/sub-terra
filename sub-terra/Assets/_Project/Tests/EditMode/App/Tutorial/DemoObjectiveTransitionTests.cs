using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.Inventory;
using SubTerra.App.Outpost;
using SubTerra.App.Progression;
using SubTerra.App.State;
using SubTerra.App.Tutorial;
using SubTerra.App.UI.Tutorial;
using SubTerra.Shared;

namespace SubTerra.App.Tests.Tutorial
{
    /// <summary>N-S01/N-S02/N-F01/N-F02 목표 표·허용/금지 전이·성공 후 전진.</summary>
    public sealed class DemoObjectiveTransitionTests
    {
        [Test]
        public void N_S01_CatalogContainsExactlyThirteenRequiredObjectives()
        {
            Assert.That(DemoObjectiveIds.Ordered.Length, Is.EqualTo(DemoObjectiveIds.RequiredCount));
            Assert.That(DemoObjectiveCatalog.All.Count, Is.EqualTo(13));

            var ids = new HashSet<string>();
            foreach (var definition in DemoObjectiveCatalog.All)
            {
                Assert.That(definition.Id, Is.Not.Null.And.Not.Empty);
                Assert.That(definition.Title, Is.Not.Null.And.Not.Empty);
                Assert.That(definition.Description, Is.Not.Null.And.Not.Empty);
                Assert.That(definition.RequiredSignal, Is.Not.EqualTo(DemoProgressSignal.None));
                Assert.That(ids.Add(definition.Id), Is.True, "duplicate " + definition.Id);
            }

            Assert.That(ids.Contains(DemoObjectiveIds.ExploreStart), Is.True);
            Assert.That(ids.Contains(DemoObjectiveIds.MineCopperIron), Is.True);
            Assert.That(ids.Contains(DemoObjectiveIds.PathGuide), Is.True);
            Assert.That(ids.Contains(DemoObjectiveIds.MineLithium), Is.True);
            Assert.That(ids.Contains(DemoObjectiveIds.StructuralCrack), Is.True);
            Assert.That(ids.Contains(DemoObjectiveIds.PlaceSupport), Is.True);
            Assert.That(ids.Contains(DemoObjectiveIds.GasEncounter), Is.True);
            Assert.That(ids.Contains(DemoObjectiveIds.OutpostInstall), Is.True);
            Assert.That(ids.Contains(DemoObjectiveIds.ReturnRecommend), Is.True);
            Assert.That(ids.Contains(DemoObjectiveIds.Settlement), Is.True);
            Assert.That(ids.Contains(DemoObjectiveIds.BatteryUpgrade), Is.True);
            Assert.That(ids.Contains(DemoObjectiveIds.DeepSignal), Is.True);
            Assert.That(ids.Contains(DemoObjectiveIds.DemoEnd), Is.True);
        }

        [Test]
        public void N_S01_OrderedChainLinksEveryNextObjective()
        {
            for (var i = 0; i < DemoObjectiveCatalog.All.Count; i++)
            {
                var current = DemoObjectiveCatalog.All[i];
                if (current.IsTerminal)
                {
                    Assert.That(current.NextObjectiveId, Is.Empty);
                    continue;
                }

                Assert.That(
                    DemoObjectiveCatalog.TryGet(current.NextObjectiveId, out _),
                    Is.True,
                    current.Id + " -> " + current.NextObjectiveId);
                Assert.That(
                    current.NextObjectiveId,
                    Is.EqualTo(DemoObjectiveIds.Ordered[i + 1]));
            }
        }

        [Test]
        public void N_F01_FullSequenceAdvancesExactlyOncePerMatchingSignal()
        {
            var engine = new DemoObjectiveTransitionEngine();
            Assert.That(engine.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.ExploreStart));

            var sequence = new[]
            {
                DemoProgressSignal.ExplorationStarted,
                DemoProgressSignal.CopperAndIronCollected,
                DemoProgressSignal.PathGuidanceAcknowledged,
                DemoProgressSignal.LithiumCollected,
                DemoProgressSignal.StructuralHazardObserved,
                DemoProgressSignal.SupportPlaced,
                DemoProgressSignal.GasHazardObserved,
                DemoProgressSignal.OutpostInstalled,
                DemoProgressSignal.ReturnRecommendationPresented,
                DemoProgressSignal.SettlementSucceeded,
                DemoProgressSignal.BatteryUpgradeSucceeded,
                DemoProgressSignal.DeepZoneUnlocked,
                DemoProgressSignal.DemoCompleted
            };

            for (var i = 0; i < sequence.Length; i++)
            {
                var result = engine.TryAdvance(sequence[i]);
                Assert.That(result.Advanced, Is.True, "step " + i + " signal " + sequence[i]);
                Assert.That(result.CompletedCount, Is.EqualTo(i + 1));
            }

            Assert.That(engine.IsDemoComplete, Is.True);
            Assert.That(engine.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.DemoEnd));
            Assert.That(engine.CompletedCount, Is.EqualTo(13));

            var again = engine.TryAdvance(DemoProgressSignal.ExplorationStarted);
            Assert.That(again.Advanced, Is.False);
        }

        [Test]
        public void N_F02_OutOfOrderSignalsDoNotSkipObjectives()
        {
            var engine = new DemoObjectiveTransitionEngine();
            // 초기 목표(탐사 시작)에서 이후 단계 신호는 거부
            Assert.That(
                engine.TryAdvance(DemoProgressSignal.SettlementSucceeded).Advanced,
                Is.False);
            Assert.That(
                engine.TryAdvance(DemoProgressSignal.DeepZoneUnlocked).Advanced,
                Is.False);
            Assert.That(engine.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.ExploreStart));
            Assert.That(engine.CompletedCount, Is.Zero);

            engine.TryAdvance(DemoProgressSignal.ExplorationStarted);
            Assert.That(engine.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.MineCopperIron));

            // 리튬 신호로 구리·철 단계를 건너뛰지 않는다
            Assert.That(
                engine.TryAdvance(DemoProgressSignal.LithiumCollected).Advanced,
                Is.False);
            Assert.That(engine.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.MineCopperIron));
        }

        [Test]
        public void N_S02_DirectorSourceHasNoGameplayCalculationReplication()
        {
            var path = Path.Combine(
                UnityEngine.Application.dataPath,
                "_Project",
                "Scripts",
                "App",
                "Tutorial",
                "DemoObjectiveDirector.cs");
            var source = File.ReadAllText(path);
            Assert.That(source, Does.Not.Contain("StructuralRiskEvaluator"));
            Assert.That(source, Does.Not.Contain("GasRiskEvaluator"));
            Assert.That(source, Does.Not.Contain("BuildingPlacementSystem"));
            Assert.That(source, Does.Not.Contain("TryMine"));
            Assert.That(source, Does.Contain("OnGameplayEvent"));
            Assert.That(source, Does.Contain("OnProgressionPurchaseCompleted"));
            Assert.That(source, Does.Contain("OnOutpostOperationCompleted"));
        }

        [Test]
        public void N_F02_SettlementAndUpgradeAndDeepUnlockOnlyOnSuccess()
        {
            var director = new DemoObjectiveDirector();
            director.ResetNewGame();
            AdvanceTo(director, DemoObjectiveIds.Settlement);

            director.OnOutpostOperationCompleted(new OutpostOperationResult(
                OutpostOperationStatus.InsufficientQuantity,
                OutpostOperationKind.SettlePlayerCargo,
                string.Empty,
                0,
                0,
                "fail"));
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.Settlement));

            director.OnOutpostOperationCompleted(new OutpostOperationResult(
                OutpostOperationStatus.Success,
                OutpostOperationKind.SettlePlayerCargo,
                "mineral.copper",
                1,
                10,
                "ok"));
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.BatteryUpgrade));

            // 구매 실패·조건 미충족만으로는 전진하지 않는다.
            director.OnProgressionPurchaseCompleted(ProgressionPurchaseResult.Fail(
                ProgressionPurchaseStatus.InsufficientResources,
                DataIds.Upgrades.MaximumEnergy,
                0,
                "no gold",
                "fail"));
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.BatteryUpgrade));

            // MaximumEnergy만으로는 심층 조건(드론 스캔2·가스 저항1)이 아니므로 전진 없음
            director.OnProgressionPurchaseCompleted(ProgressionPurchaseResult.Success(
                DataIds.Upgrades.MaximumEnergy,
                0,
                1,
                10f));
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.BatteryUpgrade));

            // 조건 충족 알림 후에만 업그레이드 목표 완료
            Assert.That(director.NotifyDeepZonePrerequisitesReady().Advanced, Is.True);
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.DeepSignal));

            director.OnDeepZoneAccessChanged(new ZoneAccessResult(false, false, "locked"));
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.DeepSignal));

            // 조건만 충족(DidUnlockNow=false)이면 심층 목표는 유지
            director.OnDeepZoneAccessChanged(new ZoneAccessResult(true, false, "조건 충족"));
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.DeepSignal));

            // 실제 커밋(DidUnlockNow) 후에만 종료 목표로
            director.OnDeepZoneAccessChanged(new ZoneAccessResult(true, true, "unlocked"));
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.DemoEnd));
        }

        [Test]
        public void N_F04_UnknownObjectiveIdFallsBackSafelyAndRestoreIsStable()
        {
            var engine = new DemoObjectiveTransitionEngine();
            engine.Restore("demo.unknown.legacy", 4);
            Assert.That(engine.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.Ordered[4]));
            Assert.That(engine.CompletedCount, Is.EqualTo(4));

            engine.Restore(DemoObjectiveIds.OutpostInstall, 7);
            Assert.That(engine.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.OutpostInstall));

            var state = GameState.CreateNew();
            var director = new DemoObjectiveDirector();
            director.BindGameState(state);
            director.RestoreFromProgress(new ProgressState(
                7,
                false,
                DemoObjectiveIds.OutpostInstall,
                false));
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.OutpostInstall));
            Assert.That(state.Progress.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.OutpostInstall));
            Assert.That(state.Progress.CompletedObjectives, Is.EqualTo(7));

            // 이미 완료한 단계를 다시 성공 처리해도 중복 전진하지 않음
            director.OnOutpostOperationCompleted(new OutpostOperationResult(
                OutpostOperationStatus.Success,
                OutpostOperationKind.Install,
                string.Empty,
                0,
                0,
                "install"));
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.ReturnRecommend));
            Assert.That(director.CompletedCount, Is.EqualTo(8));

            director.OnOutpostOperationCompleted(new OutpostOperationResult(
                OutpostOperationStatus.Success,
                OutpostOperationKind.Install,
                string.Empty,
                0,
                0,
                "duplicate install"));
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.ReturnRecommend));
            Assert.That(director.CompletedCount, Is.EqualTo(8));
        }

        [Test]
        public void N_F03_HazardPriorityBeatsTutorialAndDismissClearsInputLock()
        {
            Assert.That(
                UiLayerPriority.HazardBeatsTutorial(
                    UiLayerPriority.HazardWarning,
                    UiLayerPriority.TutorialGuidance),
                Is.True);
            Assert.That(
                UiLayerPriority.HazardBeatsTutorial(
                    UiLayerPriority.TutorialGuidance,
                    UiLayerPriority.HazardWarning),
                Is.False);
            Assert.That(UiLayerPriority.ShouldYieldTutorialInput(true), Is.True);
            Assert.That(UiLayerPriority.ShouldYieldTutorialInput(false), Is.False);

            var view = new RecordingDemoView();
            var director = new DemoObjectiveDirector();
            director.ResetNewGame();
            var presenter = new DemoObjectivePresenter(view);
            presenter.Bind(director);

            Assert.That(presenter.IsInputLocked, Is.False);
            presenter.SetHazardActive(true);
            Assert.That(view.HazardYield, Is.True);
            Assert.That(view.GuidanceVisible, Is.False);

            presenter.SetHazardActive(false);
            director.NotifyExplorationReady();
            // path guide 단계로 보낸 뒤 안내 dismiss
            AdvanceTo(director, DemoObjectiveIds.PathGuide);
            presenter.Refresh();
            Assert.That(view.GuidanceVisible, Is.True);
            presenter.DismissGuidance();
            Assert.That(presenter.IsInputLocked, Is.False);
            Assert.That(view.InputLocked, Is.False);
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.MineLithium));
        }

        [Test]
        public void N_S04_DebugForceAdvanceIsDevelopmentOnly()
        {
            var path = Path.Combine(
                UnityEngine.Application.dataPath,
                "_Project",
                "Scripts",
                "App",
                "UI",
                "Tutorial",
                "DemoObjectiveDebugTools.cs");
            var source = File.ReadAllText(path);
            Assert.That(source, Does.Contain("#if UNITY_EDITOR || SUBTERRA_BUILD_DEVELOPMENT"));
            Assert.That(source, Does.Contain("DebugForceAdvanceObjective"));

            var enginePath = Path.Combine(
                UnityEngine.Application.dataPath,
                "_Project",
                "Scripts",
                "App",
                "Tutorial",
                "DemoObjectiveTransitionEngine.cs");
            var engineSource = File.ReadAllText(enginePath);
            Assert.That(engineSource, Does.Contain("#if UNITY_EDITOR || SUBTERRA_BUILD_DEVELOPMENT"));
            Assert.That(engineSource, Does.Contain("DebugForceAdvance"));
        }

        [Test]
        public void N_InventorySignalsDriveMineralObjectives()
        {
            var catalog = new InMemoryMineralCatalog();
            catalog.Register(DataIds.Minerals.Copper, 1f, 5);
            catalog.Register(DataIds.Minerals.Iron, 1f, 5);
            catalog.Register(DataIds.Minerals.Lithium, 1f, 8);

            var director = new DemoObjectiveDirector();
            director.ResetNewGame();
            director.NotifyExplorationReady();
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.MineCopperIron));

            var stacks = new[]
            {
                new InventoryStackEntry(DataIds.Minerals.Copper, "Cu", 1, 1f, 5)
            };
            director.OnInventoryChanged(new InventorySnapshot(1f, 100f, 5f, stacks));
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.MineCopperIron));

            stacks = new[]
            {
                new InventoryStackEntry(DataIds.Minerals.Copper, "Cu", 1, 1f, 5),
                new InventoryStackEntry(DataIds.Minerals.Iron, "Fe", 1, 1f, 5)
            };
            director.OnInventoryChanged(new InventorySnapshot(2f, 100f, 10f, stacks));
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.PathGuide));

            director.NotifyGuidanceAcknowledged();
            stacks = new[]
            {
                new InventoryStackEntry(DataIds.Minerals.Lithium, "Li", 1, 1f, 8)
            };
            director.OnInventoryChanged(new InventorySnapshot(1f, 100f, 8f, stacks));
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.StructuralCrack));
        }

        [Test]
        public void N_GameplayEventsDriveSupportGasOutpost()
        {
            var director = new DemoObjectiveDirector();
            AdvanceTo(director, DemoObjectiveIds.StructuralCrack);

            director.OnGameplayEvent(new GameplayEventDto
            {
                type = GameplayEventType.StructuralRiskChanged,
                structuralIntegrity = 0.4f
            });
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.PlaceSupport));

            director.OnGameplayEvent(new GameplayEventDto
            {
                type = GameplayEventType.BuildingPlaced,
                entityId = DataIds.Buildings.SupportBasic
            });
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.GasEncounter));

            director.OnGameplayEvent(new GameplayEventDto
            {
                type = GameplayEventType.GasTriggered,
                gasRisk = 0.8f
            });
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.OutpostInstall));

            director.OnGameplayEvent(new GameplayEventDto
            {
                type = GameplayEventType.OutpostActivated,
                instanceId = "outpost.1"
            });
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.ReturnRecommend));
        }

        private static void AdvanceTo(DemoObjectiveDirector director, string targetId)
        {
            var guard = 0;
            while (director.CurrentObjectiveId != targetId && guard++ < 20)
            {
                if (!DemoObjectiveCatalog.TryGet(director.CurrentObjectiveId, out var def))
                {
                    Assert.Fail("unknown current " + director.CurrentObjectiveId);
                }

                var result = director.HandleSignal(def.RequiredSignal);
                Assert.That(result.Advanced, Is.True, def.Id);
            }

            Assert.That(director.CurrentObjectiveId, Is.EqualTo(targetId));
        }

        private sealed class RecordingDemoView : IDemoObjectiveView
        {
            public DemoObjectiveReadModel Model;
            public bool GuidanceVisible;
            public bool InputLocked;
            public bool HazardYield;
            public bool DemoCompleteVisible;

            public void SetObjective(DemoObjectiveReadModel model) => Model = model;

            public void SetGuidanceVisible(bool visible) => GuidanceVisible = visible;

            public void SetGuidanceText(string title, string body)
            {
            }

            public void SetInputLocked(bool locked) => InputLocked = locked;

            public void SetHazardYield(bool yieldToHazard) => HazardYield = yieldToHazard;

            public void SetDemoCompleteVisible(bool visible, string summary) =>
                DemoCompleteVisible = visible;
        }
    }
}
