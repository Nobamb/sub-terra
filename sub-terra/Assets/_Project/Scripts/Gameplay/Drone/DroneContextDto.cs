using System;
using System.Collections.Generic;
using SubTerra.Gameplay.Hazards;
using SubTerra.Gameplay.Structural;

namespace SubTerra.Gameplay.Drone
{
    /// <summary>Read-only world facts for B's recommendation and dialogue layer.</summary>
    [Serializable]
    public readonly struct DroneContextDto
    {
        public int Depth { get; }
        public int CurrentEnergy { get; }
        public int ReturnEnergyEstimate { get; }
        public StructuralRiskLevel StructuralRisk { get; }
        public GasRiskLevel GasRisk { get; }
        public int UnsettledCargoValue { get; }
        public float CargoWeight { get; }
        public float NearestBaseDistance { get; }
        public IReadOnlyList<string> NearbyMineralIds { get; }
        public bool ReturnPathAvailable { get; }

        public DroneContextDto(int depth, int currentEnergy, int returnEnergyEstimate, StructuralRiskLevel structuralRisk, GasRiskLevel gasRisk, int unsettledCargoValue, float cargoWeight, float nearestBaseDistance, IReadOnlyList<string> nearbyMineralIds, bool returnPathAvailable)
        {
            Depth = depth;
            CurrentEnergy = currentEnergy;
            ReturnEnergyEstimate = returnEnergyEstimate;
            StructuralRisk = structuralRisk;
            GasRisk = gasRisk;
            UnsettledCargoValue = unsettledCargoValue;
            CargoWeight = cargoWeight;
            NearestBaseDistance = nearestBaseDistance;
            NearbyMineralIds = nearbyMineralIds ?? Array.Empty<string>();
            ReturnPathAvailable = returnPathAvailable;
        }
    }
}
