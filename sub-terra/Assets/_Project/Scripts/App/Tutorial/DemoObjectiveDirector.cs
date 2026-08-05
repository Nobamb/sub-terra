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

            var id = engine.CurrentObjectiveId;
            if (id == DemoObjectiveIds.MineCopperIron
                && snapshot.GetQuantity(DataIds.Minerals.Copper) > 0
                && snapshot.GetQuantity(DataIds.Minerals.Iron) > 0)
            {
                HandleSignal(DemoProgressSignal.CopperAndIronCollected);
            }
            else if (id == DemoObjectiveIds.MineLithium
                && snapshot.GetQuantity(DataIds.Minerals.Lithium) > 0)
            {
                HandleSignal(DemoProgressSignal.LithiumCollected);
            }
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
                        HandleSignal(DemoProgressSignal.StructuralHazardObserved);
                    }

                    break;
                case GameplayEventType.GasTriggered:
                    HandleSignal(DemoProgressSignal.GasHazardObserved);
                    break;
                case GameplayEventType.BuildingPlaced:
                {
                    var placedId = gameplayEvent.entityId;
                    if (string.IsNullOrEmpty(placedId) && gameplayEvent.buildingPlacement != null)
                    {
                        placedId = gameplayEvent.buildingPlacement.buildingId;
                    }

                    if (IsSupportBuilding(placedId))
                    {
                        HandleSignal(DemoProgressSignal.SupportPlaced);
                    }

                    break;
                }
                case GameplayEventType.OutpostActivated:
                    HandleSignal(DemoProgressSignal.OutpostInstalled);
                    break;
            }
        }

        public void OnStructuralRiskChanged(StructuralRiskLevel level)
        {
            if (level == StructuralRiskLevel.Caution || level == StructuralRiskLevel.Critical)
            {
                HandleSignal(DemoProgressSignal.StructuralHazardObserved);
            }
        }

        public void OnGasExposureChanged(GasRiskLevel level)
        {
            if (level == GasRiskLevel.Elevated || level == GasRiskLevel.Hazard)
            {
                HandleSignal(DemoProgressSignal.GasHazardObserved);
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
                HandleSignal(DemoProgressSignal.OutpostInstalled);
            }
            else if (result.Kind == OutpostOperationKind.SettlePlayerCargo
                || result.Kind == OutpostOperationKind.SettleStorage)
            {
                // 정산은 Service 성공 이후에만 전진한다.
                HandleSignal(DemoProgressSignal.SettlementSucceeded);
            }
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
        /// 업그레이드 목표에 있을 때 잠금이 커밋되면 선행 목표를 닫은 뒤 심층 신호까지 전진한다.
        /// </summary>
        public void OnDeepZoneAccessChanged(ZoneAccessResult result)
        {
            if (!result.IsUnlocked || !result.DidUnlockNow)
            {
                return;
            }

            // Presenter가 TryUnlockDeepZone만 호출해도 battery → deep → end 순서가 깨지지 않게 한다.
            if (engine.CurrentObjectiveId == DemoObjectiveIds.BatteryUpgrade)
            {
                HandleSignal(DemoProgressSignal.BatteryUpgradeSucceeded);
            }

            HandleSignal(DemoProgressSignal.DeepZoneUnlocked);
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

        private static bool IsSupportBuilding(string buildingId)
        {
            return buildingId == DataIds.Buildings.SupportBasic;
        }
    }
}
