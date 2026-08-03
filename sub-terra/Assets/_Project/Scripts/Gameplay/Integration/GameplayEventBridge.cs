using System;
using SubTerra.Gameplay.Building;
using SubTerra.Gameplay.Hazards;
using SubTerra.Gameplay.Mining;
using SubTerra.Gameplay.Power;
using SubTerra.Gameplay.Structural;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.Gameplay.Integration
{
    /// <summary>Converts A's gameplay results into Shared events; it never changes App state or UI itself.</summary>
    public sealed class GameplayEventBridge : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour eventSinkBehaviour;
        [SerializeField] private MiningSystem miningSystem;
        [SerializeField] private StructuralIntegritySystem structuralSystem;
        [SerializeField] private GasHazardSystem gasHazardSystem;
        [SerializeField] private BuildingPlacementSystem buildingPlacementSystem;
        [SerializeField] private PowerNetworkSystem powerNetworkSystem;
        [SerializeField] private string outpostInstanceId = "outpost.demo";

        private IGameplayEventSink eventSink;

        private void Awake() => eventSink = eventSinkBehaviour as IGameplayEventSink;

        private void OnEnable()
        {
            if (miningSystem != null) miningSystem.TileMined += OnTileMined;
            if (structuralSystem != null)
            {
                structuralSystem.RiskChanged += OnStructuralRiskChanged;
                structuralSystem.CollapseTriggered += OnStructuralCollapse;
            }
            if (gasHazardSystem != null) gasHazardSystem.GasZoneActivated += OnGasZoneActivated;
            if (buildingPlacementSystem != null)
            {
                buildingPlacementSystem.BuildingPlaced += OnBuildingPlaced;
                buildingPlacementSystem.PlacementRejected += OnBuildingRejected;
            }
            if (powerNetworkSystem != null) powerNetworkSystem.NetworkRebuilt += OnPowerNetworkRebuilt;
        }

        private void OnDisable()
        {
            if (miningSystem != null) miningSystem.TileMined -= OnTileMined;
            if (structuralSystem != null)
            {
                structuralSystem.RiskChanged -= OnStructuralRiskChanged;
                structuralSystem.CollapseTriggered -= OnStructuralCollapse;
            }
            if (gasHazardSystem != null) gasHazardSystem.GasZoneActivated -= OnGasZoneActivated;
            if (buildingPlacementSystem != null)
            {
                buildingPlacementSystem.BuildingPlaced -= OnBuildingPlaced;
                buildingPlacementSystem.PlacementRejected -= OnBuildingRejected;
            }
            if (powerNetworkSystem != null) powerNetworkSystem.NetworkRebuilt -= OnPowerNetworkRebuilt;
        }

        public void SetEventSink(IGameplayEventSink sink) => eventSink = sink;

        private void OnTileMined(Vector3Int cell, MiningTileDto tile)
        {
            Publish(new GameplayEventDto { type = GameplayEventType.TileMined, entityId = tile.tileId, reasonId = tile.mineralId, x = cell.x, y = cell.y, quantity = tile.quantity });
        }

        private void OnStructuralRiskChanged(StructuralRiskLevel risk)
        {
            float integrity = risk switch
            {
                StructuralRiskLevel.Stable => 1f,
                StructuralRiskLevel.Caution => 0.65f,
                StructuralRiskLevel.Danger => 0.3f,
                _ => 0f
            };
            Publish(new GameplayEventDto { type = GameplayEventType.StructuralRiskChanged, structuralIntegrity = integrity });
        }

        private void OnStructuralCollapse(StructuralCollapseEventDto collapse)
        {
            Publish(new GameplayEventDto
            {
                type = GameplayEventType.StructuralCollapse,
                structuralIntegrity = 0f,
                structuralCollapse = collapse
            });
        }

        private void OnGasZoneActivated(GasZone zone)
        {
            Publish(new GameplayEventDto { type = GameplayEventType.GasTriggered, entityId = zone.GasZoneId, reasonId = zone.GasType.ToString(), x = Mathf.RoundToInt(zone.transform.position.x), y = Mathf.RoundToInt(zone.transform.position.y), gasRisk = zone.Intensity });
        }

        private void OnBuildingPlaced(BuildingPlacementResult result) => PublishBuildingResult(result, BuildingPlacementState.Placed);
        private void OnBuildingRejected(BuildingPlacementResult result) => PublishBuildingResult(result, BuildingPlacementState.Failed);

        private void PublishBuildingResult(BuildingPlacementResult result, BuildingPlacementState state)
        {
            var placement = new BuildingPlacementResultDto { state = state, buildingId = result.BuildingId, instanceId = result.InstanceId, reasonId = result.Failure.ToString(), x = result.Cell.x, y = result.Cell.y };
            Publish(new GameplayEventDto { type = result.IsSuccess ? GameplayEventType.BuildingPlaced : GameplayEventType.BuildingPlacementChanged, entityId = result.BuildingId, instanceId = result.InstanceId, x = result.Cell.x, y = result.Cell.y, buildingPlacement = placement });
        }

        private void OnPowerNetworkRebuilt(PowerNetworkSnapshot snapshot)
        {
            var status = new OutpostStatusDto { outpostInstanceId = outpostInstanceId, isActive = snapshot.Supply > 0, totalPowerSupply = snapshot.Supply, totalPowerConsumption = snapshot.Demand };
            Publish(new GameplayEventDto { type = GameplayEventType.OutpostStatusChanged, instanceId = outpostInstanceId, outpostStatus = status });
        }

        private void Publish(GameplayEventDto gameplayEvent) => eventSink?.Publish(gameplayEvent);
    }
}
