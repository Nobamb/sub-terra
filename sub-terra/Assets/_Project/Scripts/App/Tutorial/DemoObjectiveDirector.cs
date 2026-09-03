using System;
using SubTerra.App.Core.Data;
using SubTerra.App.Economy;
using SubTerra.App.Outpost;
using SubTerra.App.Progression;
using SubTerra.App.State;
using SubTerra.Shared;

namespace SubTerra.App.Tutorial
{
    /// <summary>
    /// 실제 채굴·건설·Service 성공 이벤트를 prompt-B 60 퀘스트 신호로 변환한다.
    /// 과거에 충족한 상태는 다음 퀘스트에 재사용하지 않아 한 행동이 여러 단계를 넘기지 않는다.
    /// </summary>
    public sealed class DemoObjectiveDirector
    {
        private const int MinimumLightDepth = 10;
        private const int GasCoreRange = 5;
        private const int FacilityCoreRange = 10;

        private readonly DemoObjectiveTransitionEngine engine = new();
        private GameState gameState;
        private StructuralRiskLevel structuralRisk;
        private bool storagePlaced;
        private bool chargerPlacedNearCore;
        private bool clinicPlacedNearCore;
        private bool settlementPlacedNearCore;
        private bool hasTrackedOutpostCore;
        private int outpostCoreX;
        private int outpostCoreY;
        private bool gasTargetMinedNearCore;
        private bool gasActivatedNearCore;
        private string activatedGasZoneId = string.Empty;

        public string CurrentObjectiveId => engine.CurrentObjectiveId;
        public int CompletedCount => engine.CompletedCount;
        public bool IsDemoComplete => engine.IsDemoComplete;
        public DemoObjectiveReadModel ReadModel => engine.GetReadModel();

        public event Action<DemoObjectiveReadModel> ProgressChanged;

        public void BindGameState(GameState state)
        {
            gameState = state;
            structuralRisk = state?.Run?.StructuralRisk ?? StructuralRiskLevel.Safe;
        }

        public void ResetNewGame()
        {
            engine.Reset();
            ResetStepState();
            structuralRisk = gameState?.Run?.StructuralRisk ?? StructuralRiskLevel.Safe;
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
            ResetStepState();
            structuralRisk = gameState?.Run?.StructuralRisk ?? StructuralRiskLevel.Safe;
            PushToGameState();
            RaiseChanged();
        }

        public DemoTransitionResult HandleSignal(DemoProgressSignal signal)
        {
            var result = engine.TryAdvance(signal);
            if (!result.Advanced)
            {
                return result;
            }

            PrepareEnteredObjective(result.CurrentObjectiveId);
            PushToGameState();
            RaiseChanged();
            return result;
        }

        /// <summary>Scene 전환처럼 Director가 파괴되는 성공 경로에서 진행 State를 직접 한 단계만 갱신한다.</summary>
        public static DemoTransitionResult AdvancePersistedState(
            GameState state,
            DemoProgressSignal signal)
        {
            if (state?.Progress == null)
            {
                return DemoTransitionResult.Rejected(string.Empty, 0, false, false, "state-missing");
            }

            var transition = new DemoObjectiveTransitionEngine();
            transition.Restore(
                state.Progress.CurrentObjectiveId,
                state.Progress.CompletedObjectives,
                state.Progress.IsDemoComplete);
            var result = transition.TryAdvance(signal);
            if (result.Advanced)
            {
                state.SetDemoProgress(
                    transition.CurrentObjectiveId,
                    transition.CompletedCount,
                    transition.IsDemoComplete);
            }

            return result;
        }

        public void OnGameplayEvent(GameplayEventDto gameplayEvent)
        {
            if (gameplayEvent == null || engine.IsDemoComplete)
            {
                return;
            }

            switch (gameplayEvent.type)
            {
                case GameplayEventType.TileMined:
                    OnTileMined(gameplayEvent);
                    break;
                case GameplayEventType.BuildingPlaced:
                    OnBuildingPlaced(gameplayEvent);
                    break;
                case GameplayEventType.StructuralRiskChanged:
                    structuralRisk = gameplayEvent.structuralIntegrity < 0.66f
                        ? StructuralRiskLevel.Critical
                        : StructuralRiskLevel.Safe;
                    break;
                case GameplayEventType.GasTriggered:
                    OnGasTriggered(gameplayEvent);
                    break;
                case GameplayEventType.GasPurified:
                    OnGasPurified(gameplayEvent);
                    break;
                case GameplayEventType.PlayerRescued:
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
            structuralRisk = level;
        }

        public void OnGasExposureChanged(GasRiskLevel level)
        {
            // 가스 정화 완료는 노출 여부와 전진기지 보호가 함께 확정된 GasPurified 이벤트만 사용한다.
        }

        public void OnOutpostOperationCompleted(OutpostOperationResult result)
        {
            if (!result.IsSuccess)
            {
                return;
            }

            if (CurrentObjectiveId == DemoObjectiveIds.StoreMineral
                && storagePlaced
                && result.Kind == OutpostOperationKind.Deposit
                && result.Quantity > 0)
            {
                HandleSignal(DemoProgressSignal.MineralStored);
            }
            else if (CurrentObjectiveId == DemoObjectiveIds.ChargeNearOutpost
                && chargerPlacedNearCore
                && result.Kind == OutpostOperationKind.Charge)
            {
                HandleSignal(DemoProgressSignal.ChargedNearOutpost);
            }
            else if (CurrentObjectiveId == DemoObjectiveIds.HealNearOutpost
                && clinicPlacedNearCore
                && result.Kind == OutpostOperationKind.Heal)
            {
                HandleSignal(DemoProgressSignal.HealedNearOutpost);
            }
            else if (CurrentObjectiveId == DemoObjectiveIds.SellAtSettlement
                && settlementPlacedNearCore
                && (result.Kind == OutpostOperationKind.SettlePlayerCargo
                    || result.Kind == OutpostOperationKind.SettleStorage)
                && result.Quantity > 0
                && result.GoldDelta > 0)
            {
                HandleSignal(DemoProgressSignal.MineralSoldAtSettlement);
            }
        }

        public void OnEconomyTransactionCompleted(EconomyTransactionResult result)
        {
            // 지상 판매는 퀘스트 16의 전진기지 정산 콘솔 판매로 인정하지 않는다.
        }

        public void OnProgressionPurchaseCompleted(ProgressionPurchaseResult result)
        {
            if (CurrentObjectiveId == DemoObjectiveIds.UpgradeDrillSpeed
                && result.IsSuccess
                && result.UpgradeId == DataIds.Upgrades.DrillSpeed
                && result.CurrentLevel > result.PreviousLevel)
            {
                HandleSignal(DemoProgressSignal.DrillSpeedUpgraded);
            }
        }

        public void OnDeepZoneAccessChanged(ZoneAccessResult result)
        {
            if (CurrentObjectiveId == DemoObjectiveIds.UnlockDeepZone
                && result.IsUnlocked
                && result.DidUnlockNow)
            {
                HandleSignal(DemoProgressSignal.DeepZoneUnlocked);
            }
        }

        public DemoTransitionResult NotifyDeepZoneAlreadyUnlocked()
        {
            return CurrentObjectiveId == DemoObjectiveIds.UnlockDeepZone
                ? HandleSignal(DemoProgressSignal.DeepZoneUnlocked)
                : DemoTransitionResult.Rejected(
                    CurrentObjectiveId,
                    CompletedCount,
                    false,
                    IsDemoComplete,
                    "not-on-deep-zone-quest");
        }

        public DemoTransitionResult NotifyEmergencyEscapeSucceeded()
        {
            return HandleSignal(DemoProgressSignal.EmergencyEscapeSucceeded);
        }

        public DemoTransitionResult NotifyDemoEndAcknowledged()
        {
            return DemoTransitionResult.Rejected(
                CurrentObjectiveId,
                CompletedCount,
                true,
                IsDemoComplete,
                "completion-window-only");
        }

        /// <summary>복원 후 코어 존재는 서비스가 충전·정산 성공을 검증하므로 위치 없는 폴백만 허용한다.</summary>
        public void NotifyOutpostAlreadyInstalled()
        {
            if (CurrentObjectiveId == DemoObjectiveIds.ChargeNearOutpost
                || CurrentObjectiveId == DemoObjectiveIds.HealNearOutpost
                || CurrentObjectiveId == DemoObjectiveIds.SellAtSettlement)
            {
                hasTrackedOutpostCore = false;
            }
        }

#if UNITY_EDITOR || SUBTERRA_BUILD_DEVELOPMENT
        public DemoTransitionResult DebugForceAdvance()
        {
            var result = engine.DebugForceAdvance();
            if (result.Advanced)
            {
                PrepareEnteredObjective(result.CurrentObjectiveId);
                PushToGameState();
                RaiseChanged();
            }

            return result;
        }
#endif

        private void OnTileMined(GameplayEventDto gameplayEvent)
        {
            if (CurrentObjectiveId == DemoObjectiveIds.MineBlock)
            {
                HandleSignal(DemoProgressSignal.BlockMined);
            }
            else if (CurrentObjectiveId == DemoObjectiveIds.MineCopper
                && gameplayEvent.reasonId == DataIds.Minerals.Copper
                && gameplayEvent.quantity > 0)
            {
                HandleSignal(DemoProgressSignal.CopperMined);
            }
            else if (CurrentObjectiveId == DemoObjectiveIds.MineIron
                && gameplayEvent.reasonId == DataIds.Minerals.Iron
                && gameplayEvent.quantity > 0)
            {
                HandleSignal(DemoProgressSignal.IronMined);
            }
            else if (CurrentObjectiveId == DemoObjectiveIds.MineLithium
                && gameplayEvent.reasonId == DataIds.Minerals.Lithium
                && gameplayEvent.quantity > 0)
            {
                HandleSignal(DemoProgressSignal.LithiumMined);
            }
            else if (CurrentObjectiveId == DemoObjectiveIds.PurifyGasWithOutpost
                && hasTrackedOutpostCore
                && (gameplayEvent.entityId == "tile.gas-pocket"
                    || gameplayEvent.reasonId == DataIds.Minerals.Lithium)
                && IsWithinRange(
                    outpostCoreX,
                    outpostCoreY,
                    gameplayEvent.x,
                    gameplayEvent.y,
                    GasCoreRange))
            {
                gasTargetMinedNearCore = true;
            }
        }

        private void OnBuildingPlaced(GameplayEventDto gameplayEvent)
        {
            var buildingId = gameplayEvent.entityId;
            if (string.IsNullOrEmpty(buildingId) && gameplayEvent.buildingPlacement != null)
            {
                buildingId = gameplayEvent.buildingPlacement.buildingId;
            }

            if (CurrentObjectiveId == DemoObjectiveIds.PlaceSupportInDanger
                && buildingId == DataIds.Buildings.SupportBasic
                && gameplayEvent.buildingPlacement != null
                && gameplayEvent.buildingPlacement.reducedStructuralRisk)
            {
                HandleSignal(DemoProgressSignal.SupportPlacedInDanger);
            }
            else if (CurrentObjectiveId == DemoObjectiveIds.PlaceLadder
                && buildingId == DataIds.Buildings.LadderBasic)
            {
                HandleSignal(DemoProgressSignal.LadderPlaced);
            }
            else if (CurrentObjectiveId == DemoObjectiveIds.PlaceLightAtDepth
                && buildingId == DataIds.Buildings.LightBasic
                && (gameState?.Run?.Depth ?? 0) >= MinimumLightDepth)
            {
                HandleSignal(DemoProgressSignal.LightPlacedAtDepth);
            }
            else if (CurrentObjectiveId == DemoObjectiveIds.StoreMineral
                && buildingId == DataIds.Buildings.StorageBasic)
            {
                storagePlaced = true;
            }
            else if (CurrentObjectiveId == DemoObjectiveIds.InstallOutpostCore
                && buildingId == DataIds.Buildings.OutpostCoreBasic)
            {
                TrackOutpostCore(gameplayEvent.x, gameplayEvent.y);
                HandleSignal(DemoProgressSignal.OutpostCoreInstalled);
            }
            else if (CurrentObjectiveId == DemoObjectiveIds.ChargeNearOutpost
                && buildingId == DataIds.Buildings.ChargerBasic
                && (!hasTrackedOutpostCore
                    || IsWithinRange(
                        outpostCoreX,
                        outpostCoreY,
                        gameplayEvent.x,
                        gameplayEvent.y,
                        FacilityCoreRange)))
            {
                chargerPlacedNearCore = true;
            }
            else if (CurrentObjectiveId == DemoObjectiveIds.HealNearOutpost
                && buildingId == DataIds.Buildings.ClinicBasic
                && (!hasTrackedOutpostCore
                    || IsWithinRange(
                        outpostCoreX,
                        outpostCoreY,
                        gameplayEvent.x,
                        gameplayEvent.y,
                        FacilityCoreRange)))
            {
                clinicPlacedNearCore = true;
            }
            else if (CurrentObjectiveId == DemoObjectiveIds.PurifyGasWithOutpost
                && buildingId == DataIds.Buildings.OutpostCoreBasic)
            {
                TrackOutpostCore(gameplayEvent.x, gameplayEvent.y);
            }
            else if (CurrentObjectiveId == DemoObjectiveIds.SellAtSettlement
                && buildingId == DataIds.Buildings.SettlementBasic
                && (!hasTrackedOutpostCore
                    || IsWithinRange(
                        outpostCoreX,
                        outpostCoreY,
                        gameplayEvent.x,
                        gameplayEvent.y,
                        FacilityCoreRange)))
            {
                settlementPlacedNearCore = true;
            }
        }

        private void OnGasTriggered(GameplayEventDto gameplayEvent)
        {
            if (CurrentObjectiveId != DemoObjectiveIds.PurifyGasWithOutpost
                || !gasTargetMinedNearCore
                || !hasTrackedOutpostCore
                || !IsWithinRange(
                    outpostCoreX,
                    outpostCoreY,
                    gameplayEvent.x,
                    gameplayEvent.y,
                    GasCoreRange))
            {
                return;
            }

            gasActivatedNearCore = true;
            activatedGasZoneId = gameplayEvent.entityId ?? string.Empty;
        }

        private void OnGasPurified(GameplayEventDto gameplayEvent)
        {
            if (CurrentObjectiveId != DemoObjectiveIds.PurifyGasWithOutpost
                || !gasTargetMinedNearCore
                || !gasActivatedNearCore)
            {
                return;
            }

            if (!string.IsNullOrEmpty(activatedGasZoneId)
                && gameplayEvent.instanceId != activatedGasZoneId)
            {
                return;
            }

            HandleSignal(DemoProgressSignal.GasPurifiedByOutpost);
        }

        private void TrackOutpostCore(int x, int y)
        {
            hasTrackedOutpostCore = true;
            outpostCoreX = x;
            outpostCoreY = y;
        }

        private void PrepareEnteredObjective(string objectiveId)
        {
            if (objectiveId == DemoObjectiveIds.StoreMineral)
            {
                storagePlaced = false;
            }
            else if (objectiveId == DemoObjectiveIds.ChargeNearOutpost)
            {
                chargerPlacedNearCore = false;
            }
            else if (objectiveId == DemoObjectiveIds.HealNearOutpost)
            {
                clinicPlacedNearCore = false;
            }
            else if (objectiveId == DemoObjectiveIds.PurifyGasWithOutpost)
            {
                hasTrackedOutpostCore = false;
                gasTargetMinedNearCore = false;
                gasActivatedNearCore = false;
                activatedGasZoneId = string.Empty;
            }
            else if (objectiveId == DemoObjectiveIds.SellAtSettlement)
            {
                settlementPlacedNearCore = false;
            }
        }

        private void ResetStepState()
        {
            storagePlaced = false;
            chargerPlacedNearCore = false;
            clinicPlacedNearCore = false;
            settlementPlacedNearCore = false;
            hasTrackedOutpostCore = false;
            outpostCoreX = 0;
            outpostCoreY = 0;
            gasTargetMinedNearCore = false;
            gasActivatedNearCore = false;
            activatedGasZoneId = string.Empty;
        }

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

        private static bool IsWithinRange(
            int originX,
            int originY,
            int targetX,
            int targetY,
            int range)
        {
            var deltaX = targetX - originX;
            var deltaY = targetY - originY;
            return deltaX * deltaX + deltaY * deltaY <= range * range;
        }
    }
}
