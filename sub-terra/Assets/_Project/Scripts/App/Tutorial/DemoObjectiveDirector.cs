using System;
using SubTerra.App.Core.Data;
using SubTerra.App.Economy;
using SubTerra.App.Inventory;
using SubTerra.App.Outpost;
using SubTerra.App.Progression;
using SubTerra.App.State;
using SubTerra.Shared;

namespace SubTerra.App.Tutorial
{
    /// <summary>
    /// 기존 Gameplay/App 이벤트·Service 성공 결과만으로 목표 State를 전진시킨다.
    /// 자원·골드·위험 State를 직접 조작하지 않는다.
    /// </summary>
    public sealed class DemoObjectiveDirector
    {
        private readonly DemoObjectiveTransitionEngine engine = new DemoObjectiveTransitionEngine();
        private GameState gameState;
        private bool explorationStarted;
        private bool structuralHazardObserved;
        private bool supportPlacedAfterHazard;
        private bool gasHazardEntered;
        private bool gasHazardResolved;
        private bool outpostInstalled;
        private bool settlementSucceeded;
        private InventorySnapshot latestInventory;

        public string CurrentObjectiveId => engine.CurrentObjectiveId;
        public int CompletedCount => engine.CompletedCount;
        public bool IsDemoComplete => engine.IsDemoComplete;
        public DemoObjectiveReadModel ReadModel => engine.GetReadModel();

        public event Action<DemoObjectiveReadModel> ProgressChanged;

        public void BindGameState(GameState state)
        {
            // 바인딩만 연결한다. 여기서 engine 기본값을 State에 밀어 넣으면
            // 이어하기 Progress(CurrentObjectiveId)를 덮어쓴다.
            gameState = state;
        }

        public void ResetNewGame()
        {
            engine.Reset();
            explorationStarted = false;
            structuralHazardObserved = false;
            supportPlacedAfterHazard = false;
            gasHazardEntered = false;
            gasHazardResolved = false;
            outpostInstalled = false;
            settlementSucceeded = false;
            latestInventory = null;
            PushToGameState();
            RaiseChanged();
        }

        public void RestoreFromProgress(ProgressState progress)
        {
            if (progress == null)
            {
                ResetNewGame();
                return;
            }

            engine.Restore(
                progress.CurrentObjectiveId,
                progress.CompletedObjectives,
                progress.IsDemoComplete);
            explorationStarted = engine.CompletedCount > 0
                || engine.CurrentObjectiveId != DemoObjectiveIds.ExploreStart;
            structuralHazardObserved = engine.CompletedCount
                > DemoObjectiveCatalog.IndexOf(DemoObjectiveIds.StructuralCrack);
            supportPlacedAfterHazard = engine.CompletedCount
                > DemoObjectiveCatalog.IndexOf(DemoObjectiveIds.PlaceSupport);
            gasHazardEntered = false;
            gasHazardResolved = engine.CompletedCount
                > DemoObjectiveCatalog.IndexOf(DemoObjectiveIds.GasEncounter);
            outpostInstalled = engine.CompletedCount
                > DemoObjectiveCatalog.IndexOf(DemoObjectiveIds.OutpostInstall);
            settlementSucceeded = engine.CompletedCount
                > DemoObjectiveCatalog.IndexOf(DemoObjectiveIds.Settlement);
            latestInventory = null;
            PushToGameState();
            RaiseChanged();
        }

        public DemoTransitionResult HandleSignal(DemoProgressSignal signal)
        {
            var result = engine.TryAdvance(signal);
            if (result.Advanced)
            {
                PushToGameState();
                RaiseChanged();
            }

            AdvanceRememberedObjectives();

            return result;
        }

        /// <summary>탐사 세션이 준비되면 1회 호출. 시작 목표를 완료한다.</summary>
        public DemoTransitionResult NotifyExplorationReady()
        {
            if (explorationStarted)
            {
                return DemoTransitionResult.Rejected(
                    engine.CurrentObjectiveId,
                    engine.CompletedCount,
                    false,
                    engine.IsDemoComplete,
                    "already-started");
            }

            explorationStarted = true;
            return HandleSignal(DemoProgressSignal.ExplorationStarted);
        }

        public DemoTransitionResult NotifyGuidanceAcknowledged()
        {
            return HandleSignal(DemoProgressSignal.PathGuidanceAcknowledged);
        }

        public DemoTransitionResult NotifyReturnRecommendationAcknowledged()
        {
            return HandleSignal(DemoProgressSignal.ReturnRecommendationPresented);
        }

        public DemoTransitionResult NotifyDemoEndAcknowledged()
        {
            return HandleSignal(DemoProgressSignal.DemoCompleted);
        }

        public void OnInventoryChanged(InventorySnapshot snapshot)
        {
            if (snapshot == null || engine.IsDemoComplete)
            {
                return;
            }

            latestInventory = snapshot;
            AdvanceRememberedObjectives();
        }

        public void OnGameplayEvent(GameplayEventDto gameplayEvent)
        {
            if (gameplayEvent == null || engine.IsDemoComplete)
            {
                return;
            }

            switch (gameplayEvent.type)
            {
                case GameplayEventType.StructuralRiskChanged:
                    // integrity는 A가 확정한 값. 위험 구간이면 관찰 신호만 보낸다.
                    if (gameplayEvent.structuralIntegrity < 0.66f)
                    {
                        ObserveStructuralHazard();
                    }

                    break;
                case GameplayEventType.GasTriggered:
                    // 생성 신호만으로 완료하지 않는다. 실제 노출 후 Safe 복귀를 기다린다.
                    break;
                case GameplayEventType.BuildingPlaced:
                {
                    var placedId = gameplayEvent.entityId;
                    if (string.IsNullOrEmpty(placedId) && gameplayEvent.buildingPlacement != null)
                    {
                        placedId = gameplayEvent.buildingPlacement.buildingId;
                    }

                    if (IsSupportBuilding(placedId)
                        && (structuralHazardObserved
                            || engine.CurrentObjectiveId == DemoObjectiveIds.PlaceSupport))
                    {
                        supportPlacedAfterHazard = true;
                        HandleSignal(DemoProgressSignal.SupportPlaced);
                    }

                    break;
                }
                case GameplayEventType.OutpostActivated:
                    outpostInstalled = true;
                    HandleSignal(DemoProgressSignal.OutpostInstalled);
                    break;
                case GameplayEventType.PlayerRescued:
                    // 실패 결과를 다시 계산하지 않고 Shared 결과를 다음 행동 안내에 반영한다.
                    var rescue = gameplayEvent.playerRescue;
                    gameState?.SetInteractionPrompt(
                        rescue != null && rescue.usedCheckpoint
                            ? "드론 구조 완료: 전진기지에서 탐사를 재개하세요."
                            : "드론 구조 완료: Surface Base에서 장비를 정비하세요.");
                    break;
            }
        }

        public void OnStructuralRiskChanged(StructuralRiskLevel level)
        {
            if (level == StructuralRiskLevel.Caution
                || level == StructuralRiskLevel.Critical
                || level == StructuralRiskLevel.Imminent)
            {
                ObserveStructuralHazard();
            }
        }

        /// <summary>
        /// 경로 안내 중 균열을 보면 안내를 닫지 않아도 한 단계 넘긴다.
        /// 이어하기에서 현재 위험이 다시 통지될 때도 같은 경로다.
        /// </summary>
        private void ObserveStructuralHazard()
        {
            structuralHazardObserved = true;
            if (engine.CurrentObjectiveId == DemoObjectiveIds.PathGuide)
            {
                HandleSignal(DemoProgressSignal.PathGuidanceAcknowledged);
            }

            HandleSignal(DemoProgressSignal.StructuralHazardObserved);
        }

        public void OnGasExposureChanged(GasRiskLevel level)
        {
            if (level == GasRiskLevel.Elevated || level == GasRiskLevel.Hazard)
            {
                gasHazardEntered = true;
                return;
            }

            if (level == GasRiskLevel.Safe && gasHazardEntered)
            {
                gasHazardResolved = true;
                HandleSignal(DemoProgressSignal.GasHazardResolved);
            }
        }

        public void OnOutpostOperationCompleted(OutpostOperationResult result)
        {
            if (!result.IsSuccess)
            {
                return;
            }

            if (result.Kind == OutpostOperationKind.Install)
            {
                outpostInstalled = true;
                HandleSignal(DemoProgressSignal.OutpostInstalled);
            }
            else if (result.Kind == OutpostOperationKind.SettlePlayerCargo
                || result.Kind == OutpostOperationKind.SettleStorage)
            {
                // 정산은 Service 성공 이후에만 전진한다.
                settlementSucceeded = true;
                HandleSignal(DemoProgressSignal.SettlementSucceeded);
            }
        }

        /// <summary>복원된 OutpostState에 설치 이력이 있을 때 완료 조건을 재평가한다.</summary>
        public void NotifyOutpostAlreadyInstalled()
        {
            outpostInstalled = true;
            AdvanceRememberedObjectives();
        }

        public void OnEconomyTransactionCompleted(EconomyTransactionResult result)
        {
            // 경제 실패만으로는 목표를 넘기지 않는다. 정산은 Outpost 경로를 우선한다.
            if (!result.IsSuccess)
            {
                return;
            }
        }

        public void OnProgressionPurchaseCompleted(ProgressionPurchaseResult result)
        {
            // 구매 성공만으로는 목표를 넘기지 않는다.
            // 심층 조건 충족·잠금 해제는 Binder가 ProgressionService 경로로 평가한다.
            if (!result.IsSuccess)
            {
                return;
            }
        }

        /// <summary>
        /// DeepZoneUnlockRule 조건이 충족됐을 때(GetDeepZoneAccess.IsUnlocked).
        /// 업그레이드 목표(demo.battery_upgrade)만 전진시킨다. 잠금 커밋은 별도.
        /// </summary>
        public DemoTransitionResult NotifyDeepZonePrerequisitesReady()
        {
            return HandleSignal(DemoProgressSignal.BatteryUpgradeSucceeded);
        }

        /// <summary>
        /// ProgressionService.TryUnlockDeepZone 성공(DidUnlockNow) 이벤트.
        /// 조건만 충족(DidUnlockNow=false)한 상태로는 심층 목표를 넘기지 않는다.
        /// 심층 신호 목표에 있을 때만 실제 잠금 커밋 결과를 반영한다.
        /// </summary>
        public void OnDeepZoneAccessChanged(ZoneAccessResult result)
        {
            if (!result.IsUnlocked || !result.DidUnlockNow)
            {
                return;
            }

            if (engine.CurrentObjectiveId == DemoObjectiveIds.DeepSignal)
            {
                HandleSignal(DemoProgressSignal.DeepZoneUnlocked);
            }
        }

        /// <summary>세이브에 이미 zone.deep 이 커밋된 채 심층 목표에 있을 때 전진.</summary>
        public DemoTransitionResult NotifyDeepZoneAlreadyUnlocked()
        {
            if (engine.CurrentObjectiveId != DemoObjectiveIds.DeepSignal)
            {
                return DemoTransitionResult.Rejected(
                    engine.CurrentObjectiveId,
                    engine.CompletedCount,
                    false,
                    engine.IsDemoComplete,
                    "not-on-deep-signal");
            }

            return HandleSignal(DemoProgressSignal.DeepZoneUnlocked);
        }

        public void OnReturnRecommendationPresented()
        {
            HandleSignal(DemoProgressSignal.ReturnRecommendationPresented);
        }

#if UNITY_EDITOR || SUBTERRA_BUILD_DEVELOPMENT
        public DemoTransitionResult DebugForceAdvance()
        {
            var result = engine.DebugForceAdvance();
            if (result.Advanced)
            {
                PushToGameState();
                RaiseChanged();
            }

            return result;
        }
#endif

        private void PushToGameState()
        {
            gameState?.SetDemoProgress(
                engine.CurrentObjectiveId,
                engine.CompletedCount,
                engine.IsDemoComplete);
        }

        private void RaiseChanged()
        {
            ProgressChanged?.Invoke(engine.GetReadModel());
        }

        /// <summary>
        /// 현재 목표보다 먼저 충족된 상태형 완료 조건을 버리지 않는다.
        /// 순서는 TransitionEngine이 그대로 검증하며 만족한 연속 단계만 전진한다.
        /// </summary>
        private void AdvanceRememberedObjectives()
        {
            for (var guard = 0; guard < 7; guard++)
            {
                DemoProgressSignal signal;
                if (engine.CurrentObjectiveId == DemoObjectiveIds.MineCopperIron
                    && latestInventory != null
                    && latestInventory.GetQuantity(DataIds.Minerals.Copper) > 0
                    && latestInventory.GetQuantity(DataIds.Minerals.Iron) > 0)
                {
                    signal = DemoProgressSignal.CopperAndIronCollected;
                }
                else if (engine.CurrentObjectiveId == DemoObjectiveIds.StructuralCrack
                    && structuralHazardObserved)
                {
                    signal = DemoProgressSignal.StructuralHazardObserved;
                }
                else if (engine.CurrentObjectiveId == DemoObjectiveIds.PlaceSupport
                    && supportPlacedAfterHazard)
                {
                    signal = DemoProgressSignal.SupportPlaced;
                }
                else if (engine.CurrentObjectiveId == DemoObjectiveIds.GasEncounter
                    && gasHazardResolved)
                {
                    signal = DemoProgressSignal.GasHazardResolved;
                }
                else if (engine.CurrentObjectiveId == DemoObjectiveIds.OutpostInstall
                    && outpostInstalled)
                {
                    signal = DemoProgressSignal.OutpostInstalled;
                }
                else if (engine.CurrentObjectiveId == DemoObjectiveIds.Settlement
                    && settlementSucceeded)
                {
                    signal = DemoProgressSignal.SettlementSucceeded;
                }
                else if (engine.CurrentObjectiveId == DemoObjectiveIds.MineLithium
                    && latestInventory != null
                    && latestInventory.GetQuantity(DataIds.Minerals.Lithium) > 0)
                {
                    signal = DemoProgressSignal.LithiumCollected;
                }
                else
                {
                    return;
                }

                var result = engine.TryAdvance(signal);
                if (!result.Advanced)
                {
                    return;
                }

                PushToGameState();
                RaiseChanged();
            }
        }

        private static bool IsSupportBuilding(string buildingId)
        {
            return buildingId == DataIds.Buildings.SupportBasic;
        }
    }
}
