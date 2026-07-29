using System.Collections.Generic;
using SubTerra.Gameplay.Drone;
using SubTerra.Gameplay.Hazards;
using SubTerra.Gameplay.Structural;
using UnityEngine;
using SharedContext = SubTerra.Shared.DroneContextDto;
using SharedProvider = SubTerra.Shared.IDroneContextProvider;

namespace SubTerra.App.Integration
{
    /// <summary>
    /// A의 Runtime Sensor를 수정하지 않고 B가 합의된 Shared DTO로 읽게 하는 경계 어댑터.
    /// </summary>
    public sealed class DroneContextProviderAdapter : MonoBehaviour, SharedProvider
    {
        [SerializeField] private DroneSensor sensor;

        public SharedContext CreateContext()
        {
            if (sensor == null)
            {
                return null;
            }

            var source = sensor.CaptureContext();
            return new SharedContext
            {
                depth = source.Depth,
                currentEnergy = source.CurrentEnergy,
                returnEnergyEstimate = source.ReturnEnergyEstimate,
                structuralIntegrity = MapStructuralIntegrity(source.StructuralRisk),
                gasRisk = MapGasRisk(source.GasRisk),
                unsettledCargoValue = source.UnsettledCargoValue,
                cargoWeight = source.CargoWeight,
                nearestBaseDistance = source.NearestBaseDistance,
                nearbyMineralIds = source.NearbyMineralIds != null
                    ? new List<string>(source.NearbyMineralIds)
                    : new List<string>(),
                returnPathAvailable = source.ReturnPathAvailable
            };
        }

        public void BindTo(DroneSensor droneSensor)
        {
            sensor = droneSensor;
        }

        public bool HasRequiredReferences()
        {
            return sensor != null;
        }

        private static float MapStructuralIntegrity(StructuralRiskLevel risk)
        {
            switch (risk)
            {
                case StructuralRiskLevel.Critical:
                    return 0f;
                case StructuralRiskLevel.Caution:
                    return 0.5f;
                default:
                    return 1f;
            }
        }

        private static float MapGasRisk(GasRiskLevel risk)
        {
            switch (risk)
            {
                case GasRiskLevel.Critical:
                    return 1f;
                case GasRiskLevel.Caution:
                    return 0.5f;
                default:
                    return 0f;
            }
        }
    }
}
