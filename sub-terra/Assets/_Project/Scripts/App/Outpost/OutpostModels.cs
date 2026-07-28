using System;
using System.Collections.Generic;
using SubTerra.App.Inventory;

namespace SubTerra.App.Outpost
{
    public enum OutpostOperationKind
    {
        Charge = 0,
        Deposit = 1,
        Withdraw = 2,
        SettlePlayerCargo = 3,
        SettleStorage = 4,
        Install = 5
    }

    public enum OutpostOperationStatus
    {
        Success = 0,
        InvalidRequest = 1,
        OutpostUnavailable = 2,
        FacilityUnavailable = 3,
        InsufficientQuantity = 4,
        CapacityExceeded = 5,
        OverflowRisk = 6,
        AlreadyProcessed = 7,
        DependencyMissing = 8
    }

    public enum OutpostSettlementSource
    {
        PlayerCargo = 0,
        Storage = 1
    }

    public enum OutpostAutoSaveReason
    {
        Installation = 0,
        Settlement = 1
    }

    public readonly struct OutpostOperationResult
    {
        public OutpostOperationStatus Status { get; }
        public OutpostOperationKind Kind { get; }
        public string MineralId { get; }
        public int Quantity { get; }
        public int GoldDelta { get; }
        public string Message { get; }

        public bool IsSuccess => Status == OutpostOperationStatus.Success;

        public OutpostOperationResult(
            OutpostOperationStatus status,
            OutpostOperationKind kind,
            string mineralId,
            int quantity,
            int goldDelta,
            string message)
        {
            Status = status;
            Kind = kind;
            MineralId = mineralId ?? string.Empty;
            Quantity = quantity;
            GoldDelta = goldDelta;
            Message = message ?? string.Empty;
        }
    }

    public readonly struct OutpostAutoSaveRequest
    {
        public OutpostAutoSaveReason Reason { get; }
        public string OperationId { get; }

        public OutpostAutoSaveRequest(OutpostAutoSaveReason reason, string operationId)
        {
            Reason = reason;
            OperationId = operationId ?? string.Empty;
        }
    }

    public readonly struct OutpostFacilityReadModel
    {
        public string InstanceId { get; }
        public string BuildingId { get; }
        public bool IsActive { get; }
        public string InactiveReasonId { get; }

        public OutpostFacilityReadModel(
            string instanceId,
            string buildingId,
            bool isActive,
            string inactiveReasonId)
        {
            InstanceId = instanceId ?? string.Empty;
            BuildingId = buildingId ?? string.Empty;
            IsActive = isActive;
            InactiveReasonId = inactiveReasonId ?? string.Empty;
        }
    }

    public sealed class OutpostSnapshot
    {
        private readonly OutpostFacilityReadModel[] facilities;

        public string OutpostInstanceId { get; }
        public bool IsActive { get; }
        public bool IsInInteractionRange { get; }
        public string InactiveReasonId { get; }
        public float PowerSupply { get; }
        public float PowerConsumption { get; }
        public IReadOnlyList<OutpostFacilityReadModel> Facilities => facilities;
        public InventorySnapshot PlayerCargo { get; }
        public InventorySnapshot Storage { get; }
        public string CheckpointId { get; }
        public int CheckpointX { get; }
        public int CheckpointY { get; }

        public OutpostSnapshot(
            string outpostInstanceId,
            bool isActive,
            bool isInInteractionRange,
            string inactiveReasonId,
            float powerSupply,
            float powerConsumption,
            OutpostFacilityReadModel[] facilities,
            InventorySnapshot playerCargo,
            InventorySnapshot storage,
            string checkpointId,
            int checkpointX,
            int checkpointY)
        {
            OutpostInstanceId = outpostInstanceId ?? string.Empty;
            IsActive = isActive;
            IsInInteractionRange = isInInteractionRange;
            InactiveReasonId = inactiveReasonId ?? string.Empty;
            PowerSupply = powerSupply < 0f ? 0f : powerSupply;
            PowerConsumption = powerConsumption < 0f ? 0f : powerConsumption;
            this.facilities = facilities ?? Array.Empty<OutpostFacilityReadModel>();
            PlayerCargo = playerCargo;
            Storage = storage;
            CheckpointId = checkpointId ?? string.Empty;
            CheckpointX = checkpointX;
            CheckpointY = checkpointY;
        }
    }
}
