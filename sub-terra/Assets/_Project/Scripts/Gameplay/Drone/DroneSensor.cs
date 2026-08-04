using System;
using System.Collections.Generic;
using SubTerra.Gameplay.Hazards;
using SubTerra.Gameplay.Mining;
using SubTerra.Gameplay.Power;
using SubTerra.Gameplay.Structural;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SubTerra.Gameplay.Drone
{
    /// <summary>Samples real gameplay facts periodically; it never chooses a recommendation or updates UI.</summary>
    public sealed class DroneSensor : MonoBehaviour, IDroneContextProvider, SubTerra.Shared.IDroneContextProvider
    {
        [SerializeField] private Transform playerTransform;
        [SerializeField] private Tilemap foregroundTilemap;
        [SerializeField] private MiningTileResolver tileResolver;
        [SerializeField] private StructuralIntegritySystem structuralSystem;
        [SerializeField] private GasHazardSystem gasHazardSystem;
        [SerializeField] private PowerNetworkSystem powerNetworkSystem;
        [SerializeField] private Transform[] outpostCores = Array.Empty<Transform>();
        [SerializeField, Min(1)] private int mineralScanRadius = 4;
        [SerializeField, Min(0.1f)] private float scanInterval = 0.5f;
        [SerializeField] private float surfaceY;

        private float nextScanTime;
        private int currentEnergy;
        private int returnEnergyEstimate;
        private int unsettledCargoValue;
        private float cargoWeight;
        private float maxCargoWeight;
        private bool returnPathAvailable = true;
        private GasRiskLevel? appliedGasRisk;

        public DroneContextDto CurrentContext { get; private set; }
        public event Action<DroneContextDto> ContextUpdated;

        private void Update()
        {
            if (Time.time < nextScanTime) return;
            nextScanTime = Time.time + scanInterval;
            CaptureAndNotify();
        }

        public void SetPlayerTransform(Transform target) => playerTransform = target;

        /// <summary>효과 적용 계층이 확정한 저항·대피소 반영 위험도를 Drone Context와 공유한다.</summary>
        public void SetAppliedGasRisk(GasRiskLevel risk)
        {
            appliedGasRisk = risk;
        }

        public void ClearAppliedGasRisk()
        {
            appliedGasRisk = null;
        }

        public void SetAppReadings(
            int energy,
            int returnEstimate,
            int cargoValue,
            float nextCargoWeight,
            float nextMaxCargoWeight,
            bool hasReturnPath)
        {
            currentEnergy = Mathf.Max(0, energy);
            returnEnergyEstimate = Mathf.Max(0, returnEstimate);
            unsettledCargoValue = Mathf.Max(0, cargoValue);
            cargoWeight = Mathf.Max(0f, nextCargoWeight);
            maxCargoWeight = Mathf.Max(0f, nextMaxCargoWeight);
            returnPathAvailable = hasReturnPath;
        }

        /// <summary>App이 소유한 현재 전력·인벤토리 수치만 갱신하고 Gameplay 귀환 판정은 보존한다.</summary>
        public void SetAppStateReadings(
            int energy,
            int cargoValue,
            float nextCargoWeight,
            float nextMaxCargoWeight)
        {
            currentEnergy = Mathf.Max(0, energy);
            unsettledCargoValue = Mathf.Max(0, cargoValue);
            cargoWeight = Mathf.Max(0f, nextCargoWeight);
            maxCargoWeight = Mathf.Max(0f, nextMaxCargoWeight);
        }

        public DroneContextDto CaptureContext()
        {
            Vector2 playerPosition = playerTransform != null ? playerTransform.position : transform.position;
            int depth = DroneContextCalculator.CalculateDepth(surfaceY, playerPosition.y);
            StructuralRiskLevel structuralRisk = structuralSystem != null ? structuralSystem.CurrentRisk : StructuralRiskLevel.Stable;
            GasRiskLevel gasRisk = appliedGasRisk
                ?? (gasHazardSystem != null
                    ? gasHazardSystem.CurrentExposure.Risk
                    : GasRiskLevel.Safe);
            float baseDistance = DroneContextCalculator.FindNearestDistance(playerPosition, outpostCores);
            IReadOnlyList<string> minerals = ScanNearbyMinerals(playerPosition);
            return new DroneContextDto(depth, currentEnergy, returnEnergyEstimate, structuralRisk, gasRisk, unsettledCargoValue, cargoWeight, maxCargoWeight, baseDistance, minerals, returnPathAvailable);
        }

        SubTerra.Shared.DroneContextDto SubTerra.Shared.IDroneContextProvider.CreateContext()
        {
            DroneContextDto context = CaptureContext();
            return new SubTerra.Shared.DroneContextDto
            {
                depth = context.Depth,
                currentEnergy = context.CurrentEnergy,
                returnEnergyEstimate = context.ReturnEnergyEstimate,
                structuralIntegrity = ToIntegrityValue(context.StructuralRisk),
                gasRisk = ToRiskValue(context.GasRisk),
                unsettledCargoValue = context.UnsettledCargoValue,
                cargoWeight = context.CargoWeight,
                maxCargoWeight = context.MaxCargoWeight,
                nearestBaseDistance = context.NearestBaseDistance,
                nearbyMineralIds = new List<string>(context.NearbyMineralIds),
                returnPathAvailable = context.ReturnPathAvailable
            };
        }

        private void CaptureAndNotify()
        {
            CurrentContext = CaptureContext();
            ContextUpdated?.Invoke(CurrentContext);
        }

        private IReadOnlyList<string> ScanNearbyMinerals(Vector2 playerPosition)
        {
            var mineralIds = new HashSet<string>();
            if (foregroundTilemap == null || tileResolver == null) return new List<string>();
            Vector3Int center = foregroundTilemap.WorldToCell(playerPosition);
            for (int x = center.x - mineralScanRadius; x <= center.x + mineralScanRadius; x++)
            for (int y = center.y - mineralScanRadius; y <= center.y + mineralScanRadius; y++)
            {
                TileBase tile = foregroundTilemap.GetTile(new Vector3Int(x, y, center.z));
                if (tile == null || !tileResolver.TryResolve(tile, out MiningTileDto definition) || string.IsNullOrWhiteSpace(definition.mineralId)) continue;
                mineralIds.Add(definition.mineralId);
            }
            return new List<string>(mineralIds);
        }

        private static float ToIntegrityValue(StructuralRiskLevel risk)
        {
            return risk switch
            {
                StructuralRiskLevel.Stable => 1f,
                StructuralRiskLevel.Caution => 0.65f,
                StructuralRiskLevel.Danger => 0.3f,
                _ => 0f
            };
        }

        private static float ToRiskValue(GasRiskLevel risk)
        {
            return risk switch { GasRiskLevel.Safe => 0f, GasRiskLevel.Caution => 0.5f, _ => 1f };
        }
    }
}
