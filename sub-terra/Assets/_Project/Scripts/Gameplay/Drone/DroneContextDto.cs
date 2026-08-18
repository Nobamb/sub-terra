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
        public StructuralRiskCause StructuralCause { get; }
        public bool StructuralTelegraphing { get; }
        public GasRiskLevel GasRisk { get; }
        public int UnsettledCargoValue { get; }
        public float CargoWeight { get; }
        public float MaxCargoWeight { get; }
        public float NearestBaseDistance { get; }
        public IReadOnlyList<string> NearbyMineralIds { get; }
        public bool ReturnPathAvailable { get; }

        public DroneContextDto(int depth, int currentEnergy, int returnEnergyEstimate, StructuralRiskLevel structuralRisk, GasRiskLevel gasRisk, int unsettledCargoValue, float cargoWeight, float maxCargoWeight, float nearestBaseDistance, IReadOnlyList<string> nearbyMineralIds, bool returnPathAvailable, StructuralRiskCause structuralCause = StructuralRiskCause.None, bool structuralTelegraphing = false)
        {
            Depth = depth;
            CurrentEnergy = currentEnergy;
            ReturnEnergyEstimate = returnEnergyEstimate;
            StructuralRisk = structuralRisk;
            StructuralCause = structuralCause;
            StructuralTelegraphing = structuralTelegraphing;
            GasRisk = gasRisk;
            UnsettledCargoValue = unsettledCargoValue;
            CargoWeight = cargoWeight;
            MaxCargoWeight = maxCargoWeight;
            NearestBaseDistance = nearestBaseDistance;
            NearbyMineralIds = nearbyMineralIds ?? Array.Empty<string>();
            ReturnPathAvailable = returnPathAvailable;
        }
    }
}
