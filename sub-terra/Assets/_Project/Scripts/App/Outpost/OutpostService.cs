using System;
using System.Collections.Generic;
using SubTerra.App.Core.Data;
using SubTerra.App.Inventory;
using SubTerra.App.State;
using SubTerra.Shared;

namespace SubTerra.App.Outpost
{
    /// <summary>
    /// A가 판정한 전진기지 상태를 소비해 충전·보관·정산을 원자적으로 처리한다.
    /// 연결, 거리, 활성 여부를 자체 계산하지 않는다.
    /// </summary>
    public sealed class OutpostService
    {
        private readonly InventoryService inventory;
        private readonly IMineralCatalogLookup catalog;
        private readonly GameState gameState;
        private readonly OutpostState state;
        private readonly HashSet<string> completedSettlementIds = new HashSet<string>();

        private OutpostStatusDto runtimeStatus;
        private int settlementSequence;

        public OutpostState State => state;
        public bool IsPanelOpen => IsFacilityInteraction;
        public string InteractionFacilityInstanceId =>
            runtimeStatus?.interactionFacilityInstanceId ?? string.Empty;
        public string InteractionFacilityBuildingId =>
            runtimeStatus?.interactionFacilityBuildingId ?? string.Empty;
        public bool IsFacilityInteraction =>
            runtimeStatus != null
            && runtimeStatus.isInInteractionRange
            && !string.IsNullOrEmpty(runtimeStatus.interactionFacilityBuildingId);

        /// <summary>
        /// 전진기지 코어 전용 정보 모드가 유효한 상호작용인지 나타낸다.
        /// </summary>
        public bool IsOutpostCoreInteraction =>
            runtimeStatus != null
            && runtimeStatus.isInInteractionRange
            && runtimeStatus.interactionFacilityBuildingId == DataIds.Buildings.OutpostCoreBasic;

        public event Action<OutpostSnapshot> SnapshotChanged;
        public event Action<OutpostOperationResult> OperationCompleted;
        public event Action<OutpostAutoSaveRequest> AutoSaveRequested;
        public event Action TutorialRequested;

        public OutpostService(
            InventoryService inventory,
            IMineralCatalogLookup catalog,
            GameState gameState,
            OutpostState state = null)
        {
            this.inventory = inventory;
            this.catalog = catalog;
            this.gameState = gameState;
            this.state = state ?? gameState?.Outpost ?? new OutpostState();
        }

        public void ApplyRuntimeStatus(OutpostStatusDto status)
        {
            runtimeStatus = status;
            RaiseSnapshotChanged();
        }

        public void ClearRuntimeStatus()
        {
            if (runtimeStatus == null)
            {
                return;
            }

            runtimeStatus = null;
            RaiseSnapshotChanged();
        }

        public bool TryGetPowerDisconnectedInteractionMessage(out string message)
        {
            message = string.Empty;
            if (runtimeStatus == null || !runtimeStatus.isInInteractionRange)
            {
                return false;
            }

            var buildingId = runtimeStatus.interactionFacilityBuildingId;
            var facilityName = buildingId == DataIds.Buildings.ChargerBasic
                ? "충전기"
                : buildingId == DataIds.Buildings.SettlementBasic
                    ? "정산 콘솔"
                    : string.Empty;
            if (string.IsNullOrEmpty(facilityName) || runtimeStatus.connectedFacilities == null)
            {
                return false;
            }

            for (var i = 0; i < runtimeStatus.connectedFacilities.Count; i++)
            {
                var facility = runtimeStatus.connectedFacilities[i];
                if (facility == null
                    || facility.buildingId != buildingId
                    || !IsCurrentInteractionFacility(facility))
                {
                    continue;
                }

                if (facility.isActive || facility.inactiveReasonId != "power_disconnected")
                {
                    return false;
                }

                message = facilityName
                    + " 사용불가, 전력망 미연결\n"
                    + " 엘레베이터 또는 전진기지 코어 근처에서 전력망 연결이 가능합니다.";
                return true;
            }

            return false;
        }

        public OutpostOperationResult TryCharge()
        {
            if (!TryValidateFacility(
                    DataIds.Buildings.ChargerBasic,
                    OutpostOperationKind.Charge,
                    out var failure))
            {
                return Complete(failure);
            }

            var before = gameState.Player.Energy;
            var target = gameState.Player.MaxEnergy;
            gameState.SetCurrentEnergy(target);
            var result = Success(
                OutpostOperationKind.Charge,
                string.Empty,
                target - before,
                0,
                target == before ? "이미 완전히 충전되었습니다." : "충전이 완료되었습니다.");
            RaiseSnapshotChanged();
            return Complete(result);
        }

        public OutpostOperationResult TryDeposit(string mineralId, int quantity)
        {
            if (!TryValidateFacility(
                    DataIds.Buildings.StorageBasic,
                    OutpostOperationKind.Deposit,
                    out var failure))
            {
                return Complete(failure);
            }

            if (!TryValidateMineralRequest(
                    mineralId,
                    quantity,
                    OutpostOperationKind.Deposit,
                    out _,
                    out failure))
            {
                return Complete(failure);
            }

            var playerQuantity = inventory.State.GetQuantity(mineralId);
            var storageQuantity = state.GetStorageQuantity(mineralId);
            // 10개를 요청해도 8개만 있으면 남은 8개 전부를 보관한다.
            var transferQuantity = OutpostTransferQuantity.ClampToAvailable(quantity, playerQuantity);
            if (transferQuantity <= 0)
            {
                return Complete(Fail(
                    OutpostOperationStatus.InsufficientQuantity,
                    OutpostOperationKind.Deposit,
                    "플레이어 화물이 부족합니다."));
            }

            if (storageQuantity > int.MaxValue - transferQuantity)
            {
                return Complete(Fail(
                    OutpostOperationStatus.OverflowRisk,
                    OutpostOperationKind.Deposit,
                    "보관함 수량 한도를 초과합니다."));
            }

            // 출발지 검증을 모두 마친 뒤 두 상태를 한 경로에서 변경한다.
            var reduction = inventory.TryReduceMineral(mineralId, transferQuantity);
            if (reduction.Status != InventoryMutationStatus.Success)
            {
                return Complete(Fail(
                    OutpostOperationStatus.InsufficientQuantity,
                    OutpostOperationKind.Deposit,
                    "화물 이동에 실패했습니다."));
            }

            state.SetStorageQuantity(mineralId, storageQuantity + transferQuantity);
            var result = Success(
                OutpostOperationKind.Deposit,
                mineralId,
                transferQuantity,
                0,
                "보관함에 " + transferQuantity + "개를 옮겼습니다.");
            RaiseSnapshotChanged();
            return Complete(result);
        }

        public OutpostOperationResult TryWithdraw(string mineralId, int quantity)
        {
            if (!TryValidateFacility(
                    DataIds.Buildings.StorageBasic,
                    OutpostOperationKind.Withdraw,
                    out var failure))
            {
                return Complete(failure);
            }

            if (!TryValidateMineralRequest(
                    mineralId,
                    quantity,
                    OutpostOperationKind.Withdraw,
                    out var info,
                    out failure))
            {
                return Complete(failure);
            }

            var storageQuantity = state.GetStorageQuantity(mineralId);
            // 10개를 꺼내도 보관이 8개면 남은 8개 전부를 꺼낸다.
            var transferQuantity = OutpostTransferQuantity.ClampToAvailable(quantity, storageQuantity);
            if (transferQuantity <= 0)
            {
                return Complete(Fail(
                    OutpostOperationStatus.InsufficientQuantity,
                    OutpostOperationKind.Withdraw,
                    "보관함 수량이 부족합니다."));
            }

            var playerQuantity = inventory.State.GetQuantity(mineralId);
            if (playerQuantity > int.MaxValue - transferQuantity)
            {
                return Complete(Fail(
                    OutpostOperationStatus.OverflowRisk,
                    OutpostOperationKind.Withdraw,
                    "플레이어 화물 수량 한도를 초과합니다."));
            }

            var addedWeight = info.UnitWeight * transferQuantity;
            if (addedWeight < 0f
                || inventory.CurrentWeight + addedWeight > inventory.MaxCapacity + 0.0001f)
            {
                return Complete(Fail(
                    OutpostOperationStatus.CapacityExceeded,
                    OutpostOperationKind.Withdraw,
                    "플레이어 화물 한도를 초과합니다."));
            }

            // 정확히 전량을 수용할 수 있음을 확인한 뒤 이동한다.
            var addition = inventory.TryAddMineral(mineralId, transferQuantity);
            if (addition.Status != InventoryMutationStatus.Success
                || addition.AcceptedQuantity != transferQuantity)
            {
                return Complete(Fail(
                    OutpostOperationStatus.CapacityExceeded,
                    OutpostOperationKind.Withdraw,
                    "플레이어 화물 이동에 실패했습니다."));
            }

            state.SetStorageQuantity(mineralId, storageQuantity - transferQuantity);
            var result = Success(
                OutpostOperationKind.Withdraw,
                mineralId,
                transferQuantity,
                0,
                "화물로 " + transferQuantity + "개를 옮겼습니다.");
            RaiseSnapshotChanged();
            return Complete(result);
        }

        public OutpostOperationResult TrySettle(OutpostSettlementSource source)
        {
            settlementSequence++;
            return TrySettle(source, "outpost-settlement-" + settlementSequence);
        }

        public OutpostOperationResult TrySettlePlayerCargo(string mineralId, int quantity)
        {
            settlementSequence++;
            return TrySettlePlayerCargo(
                mineralId,
                quantity,
                "outpost-settlement-" + settlementSequence);
        }

        public OutpostOperationResult TrySettlePlayerCargo(
            string mineralId,
            int quantity,
            string settlementId)
        {
            const OutpostOperationKind kind = OutpostOperationKind.SettlePlayerCargo;
            if (!TryValidateFacility(DataIds.Buildings.SettlementBasic, kind, out var failure))
            {
                return Complete(failure);
            }

            if (!TryValidateMineralRequest(
                    mineralId,
                    quantity,
                    kind,
                    out var info,
                    out failure))
            {
                return Complete(failure);
            }

            if (string.IsNullOrEmpty(settlementId))
            {
                return Complete(Fail(
                    OutpostOperationStatus.InvalidRequest,
                    kind,
                    "정산 ID가 필요합니다."));
            }

            if (completedSettlementIds.Contains(settlementId))
            {
                return Complete(Fail(
                    OutpostOperationStatus.AlreadyProcessed,
                    kind,
                    "이미 처리된 정산입니다."));
            }

            if (inventory.State.GetQuantity(mineralId) < quantity)
            {
                return Complete(Fail(
                    OutpostOperationStatus.InsufficientQuantity,
                    kind,
                    "보유 수량이 부족합니다."));
            }

            if (info.UnitPrice > 0 && quantity > int.MaxValue / info.UnitPrice)
            {
                return Complete(Fail(
                    OutpostOperationStatus.OverflowRisk,
                    kind,
                    "정산 금액 한도를 초과합니다."));
            }

            var goldGain = info.UnitPrice * quantity;
            if (gameState.Player.Gold > int.MaxValue - goldGain)
            {
                return Complete(Fail(
                    OutpostOperationStatus.OverflowRisk,
                    kind,
                    "골드 한도를 초과합니다."));
            }

            var reduction = inventory.TryReduceMineral(mineralId, quantity);
            if (reduction.Status != InventoryMutationStatus.Success)
            {
                return Complete(Fail(
                    OutpostOperationStatus.InsufficientQuantity,
                    kind,
                    "플레이어 화물 정산에 실패했습니다."));
            }

            gameState.AddGold(goldGain);
            completedSettlementIds.Add(settlementId);
            var result = Success(
                kind,
                mineralId,
                quantity,
                goldGain,
                "정산이 완료되었습니다. +" + goldGain + "G");
            RaiseSnapshotChanged();
            Complete(result);
            AutoSaveRequested?.Invoke(
                new OutpostAutoSaveRequest(OutpostAutoSaveReason.Settlement, settlementId));
            return result;
        }

        public OutpostOperationResult TrySettle(
            OutpostSettlementSource source,
            string settlementId)
        {
            var kind = source == OutpostSettlementSource.PlayerCargo
                ? OutpostOperationKind.SettlePlayerCargo
                : OutpostOperationKind.SettleStorage;

            if (!TryValidateFacility(
                    DataIds.Buildings.SettlementBasic,
                    kind,
                    out var failure))
            {
                return Complete(failure);
            }

            if (string.IsNullOrEmpty(settlementId))
            {
                return Complete(Fail(
                    OutpostOperationStatus.InvalidRequest,
                    kind,
                    "정산 ID가 필요합니다."));
            }

            if (completedSettlementIds.Contains(settlementId))
            {
                return Complete(Fail(
                    OutpostOperationStatus.AlreadyProcessed,
                    kind,
                    "이미 처리된 정산입니다."));
            }

            if (!TryBuildSettlement(
                    source,
                    out var reductions,
                    out var goldGain,
                    out failure))
            {
                return Complete(failure);
            }

            var beforeGold = gameState.Player.Gold;
            if (beforeGold > int.MaxValue - goldGain)
            {
                return Complete(Fail(
                    OutpostOperationStatus.OverflowRisk,
                    kind,
                    "골드 한도를 초과합니다."));
            }

            if (source == OutpostSettlementSource.PlayerCargo)
            {
                var reduction = inventory.TryReduceMany(reductions);
                if (reduction.Status != InventoryMutationStatus.Success)
                {
                    return Complete(Fail(
                        OutpostOperationStatus.InsufficientQuantity,
                        kind,
                        "플레이어 화물 정산에 실패했습니다."));
                }
            }
            else
            {
                for (var i = 0; i < reductions.Count; i++)
                {
                    state.SetStorageQuantity(reductions[i].Key, 0);
                }
            }

            gameState.AddGold(goldGain);
            completedSettlementIds.Add(settlementId);
            var result = Success(kind, string.Empty, SumQuantities(reductions), goldGain, "정산이 완료되었습니다.");
            RaiseSnapshotChanged();
            Complete(result);
            AutoSaveRequested?.Invoke(
                new OutpostAutoSaveRequest(OutpostAutoSaveReason.Settlement, settlementId));
            return result;
        }

        public OutpostOperationResult HandleOutpostInstalled(
            string instanceId,
            string checkpointId,
            int checkpointX,
            int checkpointY)
        {
            if (string.IsNullOrEmpty(instanceId))
            {
                return Complete(Fail(
                    OutpostOperationStatus.InvalidRequest,
                    OutpostOperationKind.Install,
                    "전진기지 인스턴스 ID가 필요합니다."));
            }

            if (state.HasInstalledOutpost(instanceId))
            {
                return Complete(Fail(
                    OutpostOperationStatus.AlreadyProcessed,
                    OutpostOperationKind.Install,
                    "이미 처리된 전진기지입니다."));
            }

            state.RecordInstallation(instanceId, checkpointId, checkpointX, checkpointY);
            var showTutorial = gameState != null
                && gameState.Progress != null
                && !gameState.Progress.HasSeenOutpostTutorial;
            if (showTutorial)
            {
                gameState.MarkOutpostTutorialSeen();
                TutorialRequested?.Invoke();
            }

            var result = Success(
                OutpostOperationKind.Install,
                string.Empty,
                1,
                0,
                "전진기지 체크포인트가 등록되었습니다.");
            RaiseSnapshotChanged();
            Complete(result);
            AutoSaveRequested?.Invoke(
                new OutpostAutoSaveRequest(OutpostAutoSaveReason.Installation, instanceId));
            return result;
        }

        public OutpostSnapshot GetSnapshot()
        {
            var status = runtimeStatus;
            var facilities = CreateFacilityModels(status);
            return new OutpostSnapshot(
                status?.outpostInstanceId,
                status != null && status.isActive,
                status != null && status.isInInteractionRange,
                status?.inactiveReasonId,
                status?.totalPowerSupply ?? 0f,
                status?.totalPowerConsumption ?? 0f,
                facilities,
                inventory?.GetSnapshot(),
                CreateStorageSnapshot(),
                state.CheckpointId,
                state.CheckpointX,
                state.CheckpointY);
        }

        private bool TryValidateFacility(
            string buildingId,
            OutpostOperationKind kind,
            out OutpostOperationResult failure)
        {
            if (inventory == null || catalog == null || gameState == null)
            {
                failure = Fail(
                    OutpostOperationStatus.DependencyMissing,
                    kind,
                    "필수 서비스가 연결되지 않았습니다.");
                return false;
            }

            if (buildingId == DataIds.Buildings.StorageBasic
                && runtimeStatus != null
                && runtimeStatus.isInInteractionRange
                && (string.IsNullOrEmpty(runtimeStatus.interactionFacilityBuildingId)
                    || runtimeStatus.interactionFacilityBuildingId == buildingId))
            {
                failure = default;
                return true;
            }

            if (runtimeStatus == null
                || !runtimeStatus.isInInteractionRange
                || (!IsProximityPoweredFacility(buildingId) && !runtimeStatus.isActive))
            {
                failure = Fail(
                    OutpostOperationStatus.OutpostUnavailable,
                    kind,
                    runtimeStatus?.inactiveReasonId ?? "전진기지를 사용할 수 없습니다.");
                return false;
            }

            if (runtimeStatus.connectedFacilities != null)
            {
                for (var i = 0; i < runtimeStatus.connectedFacilities.Count; i++)
                {
                    var facility = runtimeStatus.connectedFacilities[i];
                    if (facility == null || facility.buildingId != buildingId)
                    {
                        continue;
                    }

                    if (!IsCurrentInteractionFacility(facility))
                    {
                        continue;
                    }

                    if (facility != null && facility.buildingId == buildingId)
                    {
                        if (facility.isActive)
                        {
                            failure = default;
                            return true;
                        }

                        failure = Fail(
                            OutpostOperationStatus.FacilityUnavailable,
                            kind,
                            string.IsNullOrEmpty(facility.inactiveReasonId)
                                ? "시설이 비활성 상태입니다."
                                : facility.inactiveReasonId);
                        return false;
                    }
                }
            }

            if (!runtimeStatus.isActive
                && (runtimeStatus.connectedFacilities == null
                    || runtimeStatus.connectedFacilities.Count == 0))
            {
                failure = Fail(
                    OutpostOperationStatus.OutpostUnavailable,
                    kind,
                    string.IsNullOrEmpty(runtimeStatus.inactiveReasonId)
                        ? "전진기지를 사용할 수 없습니다."
                        : runtimeStatus.inactiveReasonId);
                return false;
            }

            failure = Fail(
                OutpostOperationStatus.FacilityUnavailable,
                kind,
                "연결된 시설이 없습니다.");
            return false;
        }

        private static bool IsProximityPoweredFacility(string buildingId)
        {
            return buildingId == DataIds.Buildings.ChargerBasic
                || buildingId == DataIds.Buildings.SettlementBasic;
        }

        private bool IsCurrentInteractionFacility(ConnectedFacilityStatusDto facility)
        {
            if (string.IsNullOrEmpty(runtimeStatus.interactionFacilityInstanceId)
                && string.IsNullOrEmpty(runtimeStatus.interactionFacilityBuildingId))
            {
                return true;
            }

            return facility.instanceId == runtimeStatus.interactionFacilityInstanceId
                && facility.buildingId == runtimeStatus.interactionFacilityBuildingId;
        }

        private bool TryValidateMineralRequest(
            string mineralId,
            int quantity,
            OutpostOperationKind kind,
            out MineralUnitInfo info,
            out OutpostOperationResult failure)
        {
            info = default;
            if (string.IsNullOrEmpty(mineralId)
                || quantity <= 0
                || !catalog.TryGetMineral(mineralId, out info)
                || info.UnitWeight <= 0f
                || info.UnitPrice < 0)
            {
                failure = Fail(
                    OutpostOperationStatus.InvalidRequest,
                    kind,
                    "광물 ID와 수량을 확인해 주세요.");
                return false;
            }

            failure = default;
            return true;
        }

        private bool TryBuildSettlement(
            OutpostSettlementSource source,
            out List<KeyValuePair<string, int>> reductions,
            out int goldGain,
            out OutpostOperationResult failure)
        {
            reductions = new List<KeyValuePair<string, int>>();
            goldGain = 0;
            var kind = source == OutpostSettlementSource.PlayerCargo
                ? OutpostOperationKind.SettlePlayerCargo
                : OutpostOperationKind.SettleStorage;

            if (source == OutpostSettlementSource.PlayerCargo)
            {
                var stacks = inventory.GetSnapshot().Stacks;
                for (var i = 0; i < stacks.Count; i++)
                {
                    reductions.Add(new KeyValuePair<string, int>(
                        stacks[i].MineralId,
                        stacks[i].Quantity));
                }
            }
            else
            {
                var stacks = state.Storage;
                for (var i = 0; i < stacks.Count; i++)
                {
                    reductions.Add(new KeyValuePair<string, int>(
                        stacks[i].MineralId,
                        stacks[i].Quantity));
                }
            }

            if (reductions.Count == 0)
            {
                failure = Fail(
                    OutpostOperationStatus.InsufficientQuantity,
                    kind,
                    "정산할 광물이 없습니다.");
                return false;
            }

            for (var i = 0; i < reductions.Count; i++)
            {
                var entry = reductions[i];
                if (entry.Value <= 0
                    || !catalog.TryGetMineral(entry.Key, out var info)
                    || info.UnitPrice < 0)
                {
                    failure = Fail(
                        OutpostOperationStatus.InvalidRequest,
                        kind,
                        "정산할 수 없는 광물이 있습니다.");
                    return false;
                }

                if (info.UnitPrice > 0 && entry.Value > int.MaxValue / info.UnitPrice)
                {
                    failure = Fail(
                        OutpostOperationStatus.OverflowRisk,
                        kind,
                        "정산 금액 한도를 초과합니다.");
                    return false;
                }

                var value = info.UnitPrice * entry.Value;
                if (goldGain > int.MaxValue - value)
                {
                    failure = Fail(
                        OutpostOperationStatus.OverflowRisk,
                        kind,
                        "정산 금액 한도를 초과합니다.");
                    return false;
                }

                goldGain += value;
            }

            failure = default;
            return true;
        }

        private InventorySnapshot CreateStorageSnapshot()
        {
            var entries = new List<InventoryStackEntry>(state.Storage.Count);
            var weight = 0f;
            var value = 0f;
            for (var i = 0; i < state.Storage.Count; i++)
            {
                var stack = state.Storage[i];
                var displayName = stack.MineralId;
                var unitWeight = 0f;
                var unitPrice = 0;
                if (catalog != null && catalog.TryGetMineral(stack.MineralId, out var info))
                {
                    displayName = info.DisplayName;
                    unitWeight = info.UnitWeight;
                    unitPrice = info.UnitPrice;
                }

                entries.Add(new InventoryStackEntry(
                    stack.MineralId,
                    displayName,
                    stack.Quantity,
                    unitWeight,
                    unitPrice));
                weight += unitWeight * stack.Quantity;
                value += unitPrice * stack.Quantity;
            }

            entries.Sort((a, b) => string.CompareOrdinal(a.MineralId, b.MineralId));
            return new InventorySnapshot(weight, float.MaxValue, value, entries.ToArray());
        }

        private static OutpostFacilityReadModel[] CreateFacilityModels(OutpostStatusDto status)
        {
            if (status?.connectedFacilities == null)
            {
                return Array.Empty<OutpostFacilityReadModel>();
            }

            var result = new List<OutpostFacilityReadModel>(status.connectedFacilities.Count);
            for (var i = 0; i < status.connectedFacilities.Count; i++)
            {
                var facility = status.connectedFacilities[i];
                if (facility == null || !IsProximityPoweredFacility(facility.buildingId))
                {
                    continue;
                }

                result.Add(new OutpostFacilityReadModel(
                    facility.instanceId,
                    facility.buildingId,
                    facility.isActive,
                    facility.inactiveReasonId));
            }

            return result.ToArray();
        }

        private void RaiseSnapshotChanged()
        {
            SnapshotChanged?.Invoke(GetSnapshot());
        }

        private OutpostOperationResult Complete(OutpostOperationResult result)
        {
            OperationCompleted?.Invoke(result);
            return result;
        }

        private static OutpostOperationResult Success(
            OutpostOperationKind kind,
            string mineralId,
            int quantity,
            int goldDelta,
            string message)
        {
            return new OutpostOperationResult(
                OutpostOperationStatus.Success,
                kind,
                mineralId,
                quantity,
                goldDelta,
                message);
        }

        private static OutpostOperationResult Fail(
            OutpostOperationStatus status,
            OutpostOperationKind kind,
            string message)
        {
            return new OutpostOperationResult(status, kind, string.Empty, 0, 0, message);
        }

        private static int SumQuantities(IReadOnlyList<KeyValuePair<string, int>> entries)
        {
            var total = 0;
            for (var i = 0; i < entries.Count; i++)
            {
                if (total > int.MaxValue - entries[i].Value)
                {
                    return int.MaxValue;
                }

                total += entries[i].Value;
            }

            return total;
        }
    }
}
