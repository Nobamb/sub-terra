using System.Collections.Generic;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.Integration;
using SubTerra.App.Outpost;
using SubTerra.App.Progression;
using SubTerra.App.State;
using SubTerra.App.Tutorial;
using SubTerra.App.UI.Tutorial;
using SubTerra.Shared;

namespace SubTerra.App.Tests.Tutorial
{
    /// <summary>prompt-B 60/86의 18단계 순서와 실제 행동 판정 회귀 검증.</summary>
    public sealed class DemoObjectiveTransitionTests
    {
        [Test]
        public void PromptB86_CatalogContainsExactlyEighteenOrderedQuests()
        {
            Assert.That(DemoObjectiveIds.Ordered.Length, Is.EqualTo(18));
            Assert.That(DemoObjectiveCatalog.All.Count, Is.EqualTo(18));
            Assert.That(DemoObjectiveIds.RequiredCount, Is.EqualTo(18));

            var ids = new HashSet<string>();
            for (var i = 0; i < DemoObjectiveCatalog.All.Count; i++)
            {
                var definition = DemoObjectiveCatalog.All[i];
                Assert.That(definition.Id, Is.EqualTo(DemoObjectiveIds.Ordered[i]));
                Assert.That(ids.Add(definition.Id), Is.True);
                Assert.That(definition.RequiredSignal, Is.Not.EqualTo(DemoProgressSignal.None));
                Assert.That(definition.Title, Is.Not.Empty);
                Assert.That(definition.Description, Is.Not.Empty);
                Assert.That(definition.NextActionHint, Is.Not.Empty);
            }

            Assert.That(DemoObjectiveCatalog.All[17].IsTerminal, Is.True);
            Assert.That(
                DemoObjectiveCatalog.GetRequired(DemoObjectiveIds.ReturnToMine).Description,
                Is.EqualTo("다시 광산으로 돌아온 뒤, 엘리베이터에서 벗어나 채굴을 이어가주세요."));
            Assert.That(
                DemoObjectiveCatalog.GetRequired(DemoObjectiveIds.ReturnToMine).NextActionHint,
                Does.Contain("검은색 블록 3칸에서 벗어나기"));
            Assert.That(DemoObjectiveCatalog.GetRequired(DemoObjectiveIds.PlaceLightAtDepth).Description, Does.Contain("10m"));
            Assert.That(DemoObjectiveCatalog.GetRequired(DemoObjectiveIds.PurifyGasWithOutpost).Description, Does.Contain("리튬 또는 가스"));
        }

        [Test]
        public void PromptB60_2_FirstGuidanceUsesRequestedIntroductionWithoutReplacingQuestText()
        {
            var first = DemoObjectiveCatalog.GetRequired(DemoObjectiveIds.MineBlock);

            Assert.That(first.Description, Does.Contain("블록 하나를 제거하세요"));
            Assert.That(first.GuidanceTitle, Is.EqualTo("생존자 브리핑"));
            Assert.That(first.GuidanceBody, Is.EqualTo(DemoObjectiveCatalog.IntroductionGuidanceBody));
            Assert.That(first.GuidanceBody, Does.Contain("당신은 재앙 이후, 얼마 남지 않은 생존자입니다."));
            Assert.That(first.GuidanceBody, Does.Contain("[조작 안내]"));
        }

        [Test]
        public void PromptB60_1_MineReturnCompletesOnlyAfterLeavingElevatorProtectedArea()
        {
            var gate = new MineReturnDepartureGate();

            Assert.That(gate.Observe(false, -6.5f, -6.5f, 1.5f), Is.False);
            Assert.That(gate.Observe(true, -6.5f, -6.5f, 1.5f), Is.False);
            Assert.That(gate.Observe(true, -5f, -6.5f, 1.5f), Is.False);
            Assert.That(gate.Observe(true, -4.99f, -6.5f, 1.5f), Is.True);
        }

        [Test]
        public void PromptB60_TransitionRejectsOutOfOrderAndAdvancesOnlyOneStep()
        {
            var engine = new DemoObjectiveTransitionEngine();

            Assert.That(engine.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.MineBlock));
            Assert.That(engine.TryAdvance(DemoProgressSignal.CopperMined).Advanced, Is.False);
            Assert.That(engine.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.MineBlock));

            Assert.That(engine.TryAdvance(DemoProgressSignal.BlockMined).Advanced, Is.True);
            Assert.That(engine.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.MineCopper));
            Assert.That(engine.CompletedCount, Is.EqualTo(1));
        }

        [Test]
        public void PromptB60_MiningEventsCountOnlyAtTheirOwnQuest()
        {
            var director = new DemoObjectiveDirector();
            director.ResetNewGame();

            director.OnGameplayEvent(Mined(DataIds.Minerals.Copper, "tile.copper"));
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.MineCopper));
            Assert.That(director.CompletedCount, Is.EqualTo(1), "첫 구리 블록은 블록 제거만 완료해야 한다.");

            director.OnGameplayEvent(Mined(DataIds.Minerals.Iron, "tile.iron"));
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.MineCopper));

            director.OnGameplayEvent(Mined(DataIds.Minerals.Copper, "tile.copper"));
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.UpgradeDrillSpeed));
        }

        [Test]
        public void PromptB60_DrillQuestAcceptsOnlySuccessfulDrillSpeedIncrease()
        {
            var director = At(DemoObjectiveIds.UpgradeDrillSpeed);

            director.OnProgressionPurchaseCompleted(ProgressionPurchaseResult.Success(
                DataIds.Upgrades.DroneScan,
                0,
                1,
                0.1f));
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.UpgradeDrillSpeed));

            director.OnProgressionPurchaseCompleted(ProgressionPurchaseResult.Success(
                DataIds.Upgrades.DrillSpeed,
                0,
                1,
                0.1f));
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.TravelToSurface));
        }

        [Test]
        public void PromptB60_ElevatorSignalsPersistOnlyForMatchingDirection()
        {
            var state = GameState.CreateNew();
            state.SetDemoProgress(DemoObjectiveIds.TravelToSurface, 3, false);

            var wrong = DemoObjectiveDirector.AdvancePersistedState(
                state,
                DemoProgressSignal.MineReachedByElevator);
            Assert.That(wrong.Advanced, Is.False);
            Assert.That(state.Progress.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.TravelToSurface));

            Assert.That(DemoObjectiveDirector.AdvancePersistedState(
                state,
                DemoProgressSignal.SurfaceReachedByElevator).Advanced, Is.True);
            Assert.That(state.Progress.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.ReturnToMine));

            Assert.That(DemoObjectiveDirector.AdvancePersistedState(
                state,
                DemoProgressSignal.MineReachedByElevator).Advanced, Is.True);
            Assert.That(state.Progress.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.MineIron));
        }

        [Test]
        public void PromptB60_3_SupportRequiresConfirmedStructuralRiskReduction()
        {
            var director = At(DemoObjectiveIds.PlaceSupportInDanger);
            director.OnGameplayEvent(Placed(DataIds.Buildings.SupportBasic, 2, -8));
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.PlaceSupportInDanger));

            // 실제 배치 순서처럼 위험 상태가 먼저 안전으로 바뀐 뒤 배치 이벤트가 와도 완료되어야 한다.
            director.OnStructuralRiskChanged(StructuralRiskLevel.Caution);
            director.OnStructuralRiskChanged(StructuralRiskLevel.Safe);
            director.OnGameplayEvent(Placed(DataIds.Buildings.SupportBasic, 2, -8, true));
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.PlaceLadder));
        }

        [Test]
        public void PromptB60_LightRequiresDepthTenOrMore()
        {
            var state = GameState.CreateNew();
            var director = At(DemoObjectiveIds.PlaceLightAtDepth, state);

            state.SetDepth(9);
            director.OnGameplayEvent(Placed(DataIds.Buildings.LightBasic, 0, -9));
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.PlaceLightAtDepth));

            state.SetDepth(10);
            director.OnGameplayEvent(Placed(DataIds.Buildings.LightBasic, 0, -10));
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.StoreMineral));
        }

        [Test]
        public void PromptB60_StorageRequiresPlacementThenSuccessfulDeposit()
        {
            var director = At(DemoObjectiveIds.StoreMineral);
            director.OnOutpostOperationCompleted(Operation(OutpostOperationKind.Deposit, 1, 0));
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.StoreMineral));

            director.OnGameplayEvent(Placed(DataIds.Buildings.StorageBasic, 0, -10));
            director.OnOutpostOperationCompleted(new OutpostOperationResult(
                OutpostOperationStatus.InsufficientQuantity,
                OutpostOperationKind.Deposit,
                DataIds.Minerals.Copper,
                0,
                0,
                "failed"));
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.StoreMineral));

            director.OnOutpostOperationCompleted(Operation(OutpostOperationKind.Deposit, 1, 0));
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.InstallOutpostCore));
        }

        [Test]
        public void PromptB60_ChargerMustBeNearTrackedCoreAndChargeMustSucceed()
        {
            var director = At(DemoObjectiveIds.InstallOutpostCore);
            director.OnGameplayEvent(Placed(DataIds.Buildings.OutpostCoreBasic, 0, -15));
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.ChargeNearOutpost));

            director.OnGameplayEvent(Placed(DataIds.Buildings.ChargerBasic, 11, -15));
            director.OnOutpostOperationCompleted(Operation(OutpostOperationKind.Charge, 0, 0));
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.ChargeNearOutpost));

            director.OnGameplayEvent(Placed(DataIds.Buildings.ChargerBasic, 10, -15));
            director.OnOutpostOperationCompleted(Operation(OutpostOperationKind.Charge, 0, 0));
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.HealNearOutpost));
        }

        [Test]
        public void PromptB60_DeepAndLithiumRequireTheirCurrentSuccessfulEvents()
        {
            var director = At(DemoObjectiveIds.UnlockDeepZone);
            director.OnDeepZoneAccessChanged(new ZoneAccessResult(true, false, "already"));
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.UnlockDeepZone));

            director.OnDeepZoneAccessChanged(new ZoneAccessResult(true, true, string.Empty));
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.MineLithium));

            director.OnGameplayEvent(Mined(DataIds.Minerals.Copper, "tile.copper"));
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.MineLithium));
            director.OnGameplayEvent(Mined(DataIds.Minerals.Lithium, "tile.lithium"));
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.PurifyGasWithOutpost));
        }

        [Test]
        public void PromptB60_GasQuestRequiresCoreMineActivateAndShelteredExposureInOrder()
        {
            var director = At(DemoObjectiveIds.PurifyGasWithOutpost);
            director.OnGameplayEvent(new GameplayEventDto
            {
                type = GameplayEventType.GasPurified,
                instanceId = "gas.1"
            });
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.PurifyGasWithOutpost));

            director.OnGameplayEvent(Placed(DataIds.Buildings.OutpostCoreBasic, 0, -20));
            director.OnGameplayEvent(Mined(string.Empty, "tile.gas-pocket", 6, -20, 0));
            director.OnGameplayEvent(new GameplayEventDto
            {
                type = GameplayEventType.GasTriggered,
                entityId = "gas.far",
                x = 6,
                y = -20
            });
            director.OnGameplayEvent(new GameplayEventDto
            {
                type = GameplayEventType.GasPurified,
                instanceId = "gas.far"
            });
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.PurifyGasWithOutpost));

            director.OnGameplayEvent(Mined(DataIds.Minerals.Lithium, "tile.lithium", 5, -20, 1));
            director.OnGameplayEvent(new GameplayEventDto
            {
                type = GameplayEventType.GasTriggered,
                entityId = "gas.near",
                x = 5,
                y = -20
            });
            director.OnGameplayEvent(new GameplayEventDto
            {
                type = GameplayEventType.GasPurified,
                instanceId = "gas.near"
            });
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.SellAtSettlement));
        }

        [Test]
        public void PromptB60_SettlementRequiresNearbyConsoleAndPositiveSale()
        {
            var director = At(DemoObjectiveIds.PurifyGasWithOutpost);
            director.OnGameplayEvent(Placed(DataIds.Buildings.OutpostCoreBasic, 0, -20));
            CompleteGasQuest(director, 1, -20, "gas.1");
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.SellAtSettlement));

            director.OnGameplayEvent(Placed(DataIds.Buildings.SettlementBasic, 11, -20));
            director.OnOutpostOperationCompleted(Operation(OutpostOperationKind.SettlePlayerCargo, 1, 5));
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.SellAtSettlement));

            director.OnGameplayEvent(Placed(DataIds.Buildings.SettlementBasic, 10, -20));
            director.OnOutpostOperationCompleted(Operation(OutpostOperationKind.SettlePlayerCargo, 1, 5));
            Assert.That(director.CurrentObjectiveId, Is.EqualTo(DemoObjectiveIds.EmergencyEscapeReturn));
        }

        [Test]
        public void PromptB60_EmergencyEscapeIsTheOnlyTerminalCompletionSignal()
        {
            var director = At(DemoObjectiveIds.EmergencyEscapeReturn);
            Assert.That(director.HandleSignal(DemoProgressSignal.MineralSoldAtSettlement).Advanced, Is.False);
            Assert.That(director.IsDemoComplete, Is.False);

            Assert.That(director.NotifyEmergencyEscapeSucceeded().Advanced, Is.True);
            Assert.That(director.IsDemoComplete, Is.True);
            Assert.That(director.CompletedCount, Is.EqualTo(18));
        }

        [Test]
        public void PromptB73_EmergencyEscapeQuest_NeverShowsCompletionPopup()
        {
            var director = At(DemoObjectiveIds.EmergencyEscapeReturn);
            var view = new FakeObjectiveView();
            using var presenter = new DemoObjectivePresenter(view);

            presenter.Bind(director);

            Assert.That(view.DemoCompleteVisible, Is.False,
                "긴급 탈출 포탈 퀘스트 진입만으로 완료 팝업이 나타나면 안 된다.");

            director.NotifyEmergencyEscapeSucceeded();

            Assert.That(view.DemoCompleteVisible, Is.False,
                "포탈 사용에 성공한 뒤에도 불필요한 완료 팝업을 표시하지 않는다.");
        }

        [Test]
        public void PromptB74_EmergencyEscapeSuccess_ReplacesActiveQuestTextWithCompletionState()
        {
            var director = At(DemoObjectiveIds.EmergencyEscapeReturn);
            var view = new FakeObjectiveView();
            using var presenter = new DemoObjectivePresenter(view);
            presenter.Bind(director);

            Assert.That(view.Objective.Title, Is.EqualTo("긴급 탈출 귀환"));

            director.NotifyEmergencyEscapeSucceeded();

            Assert.That(view.Objective.IsDemoComplete, Is.True);
            Assert.That(view.Objective.CompletedCount, Is.EqualTo(DemoObjectiveIds.RequiredCount));
            Assert.That(view.Objective.Title, Is.EqualTo(DemoObjectiveCatalog.DemoCompleteTitle));
            Assert.That(view.Objective.NextActionHint, Is.Empty);
            Assert.That(view.DemoCompleteVisible, Is.False,
                "완료 상태는 HUD에 반영하되 제거된 완료 팝업은 다시 표시하지 않는다.");
        }

        private static DemoObjectiveDirector At(string objectiveId, GameState state = null)
        {
            var director = new DemoObjectiveDirector();
            if (state != null)
            {
                director.BindGameState(state);
            }

            director.ResetNewGame();
            var guard = 0;
            while (director.CurrentObjectiveId != objectiveId && guard++ < 20)
            {
                var definition = DemoObjectiveCatalog.GetRequired(director.CurrentObjectiveId);
                Assert.That(definition, Is.Not.Null);
                Assert.That(director.HandleSignal(definition.RequiredSignal).Advanced, Is.True);
            }

            Assert.That(director.CurrentObjectiveId, Is.EqualTo(objectiveId));
            return director;
        }

        private static GameplayEventDto Mined(
            string mineralId,
            string tileId,
            int x = 0,
            int y = 0,
            int quantity = 1)
        {
            return new GameplayEventDto
            {
                type = GameplayEventType.TileMined,
                entityId = tileId,
                reasonId = mineralId,
                x = x,
                y = y,
                quantity = quantity
            };
        }

        private static GameplayEventDto Placed(
            string buildingId,
            int x,
            int y,
            bool reducedStructuralRisk = false)
        {
            return new GameplayEventDto
            {
                type = GameplayEventType.BuildingPlaced,
                entityId = buildingId,
                x = x,
                y = y,
                buildingPlacement = new BuildingPlacementResultDto
                {
                    state = BuildingPlacementState.Placed,
                    buildingId = buildingId,
                    x = x,
                    y = y,
                    reducedStructuralRisk = reducedStructuralRisk
                }
            };
        }

        private static OutpostOperationResult Operation(
            OutpostOperationKind kind,
            int quantity,
            int gold)
        {
            return new OutpostOperationResult(
                OutpostOperationStatus.Success,
                kind,
                DataIds.Minerals.Copper,
                quantity,
                gold,
                "success");
        }

        private static void CompleteGasQuest(
            DemoObjectiveDirector director,
            int x,
            int y,
            string gasId)
        {
            director.OnGameplayEvent(Mined(string.Empty, "tile.gas-pocket", x, y, 0));
            director.OnGameplayEvent(new GameplayEventDto
            {
                type = GameplayEventType.GasTriggered,
                entityId = gasId,
                x = x,
                y = y
            });
            director.OnGameplayEvent(new GameplayEventDto
            {
                type = GameplayEventType.GasPurified,
                instanceId = gasId
            });
        }

        private sealed class FakeObjectiveView : IDemoObjectiveView
        {
            public bool DemoCompleteVisible { get; private set; }
            public DemoObjectiveReadModel Objective { get; private set; }

            public void SetObjective(DemoObjectiveReadModel model)
            {
                Objective = model;
            }
            public void SetGuidanceVisible(bool visible) { }
            public void SetGuidanceText(string title, string body) { }
            public void SetInputLocked(bool locked) { }
            public void SetHazardYield(bool yieldToHazard) { }
            public void SetDetailsVisible(bool visible) { }
            public void SetDetailsText(string title, string body, string nextAction) { }

            public void SetDemoCompleteVisible(bool visible, string summary)
            {
                DemoCompleteVisible = visible;
            }
        }
    }
}
