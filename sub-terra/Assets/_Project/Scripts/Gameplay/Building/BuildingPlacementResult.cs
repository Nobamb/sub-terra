using System;
using UnityEngine;

namespace SubTerra.Gameplay.Building
{
    public enum BuildingPlacementFailure
    {
        None = 0,
        NoSelection = 1,
        InvalidDefinition = 2,
        Occupied = 3,
        MissingGround = 4,
        ResourceWalletUnavailable = 5,
        CannotAfford = 6,
        SpendFailed = 7,
        InstantiateFailed = 8,
        OutOfRange = 9,
        OutsideAllowedArea = 10
    }

    [Serializable]
    public readonly struct BuildingPlacementResult
    {
        public bool IsSuccess { get; }
        public BuildingPlacementFailure Failure { get; }
        public string InstanceId { get; }
        public string BuildingId { get; }
        public Vector3Int Cell { get; }
        public bool ReducedStructuralRisk { get; }

        public BuildingPlacementResult(
            bool isSuccess,
            BuildingPlacementFailure failure,
            string instanceId,
            string buildingId,
            Vector3Int cell,
            bool reducedStructuralRisk = false)
        {
            IsSuccess = isSuccess;
            Failure = failure;
            InstanceId = instanceId ?? string.Empty;
            BuildingId = buildingId ?? string.Empty;
            Cell = cell;
            ReducedStructuralRisk = reducedStructuralRisk;
        }
    }
}
