using System;
using System.Collections.Generic;
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
        [SerializeField] private Transform interactionOrigin;
        [SerializeField, Min(0.1f)] private float facilityInteractionRange = 2f;
        [SerializeField, Min(0.1f)] private float facilityPowerConnectionRange = 10f;
        [SerializeField] private Transform elevatorPowerOrigin;
        [SerializeField] private string outpostInstanceId = "outpost.demo";

        private IGameplayEventSink eventSink;
        private PowerNetworkSnapshot latestPowerSnapshot;
        private bool hasPowerSnapshot;
        private bool lastInteractionRange;
        private string lastInteractionFacilityInstanceId;
        private string lastInteractionFacilityBuildingId;
        private ICollapseDamageReceiver collapseDamageReceiver;

        private void Awake() => eventSink = eventSinkBehaviour as IGameplayEventSink;

        private void OnEnable()
        {
            if (miningSystem != null) miningSystem.TileMined += OnTileMined;
            if (structuralSystem != null)
            {
                structuralSystem.BindCollapseDamageReceiver(collapseDamageReceiver);
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

        public void SetCollapseDamageReceiver(ICollapseDamageReceiver receiver)
        {
            collapseDamageReceiver = receiver;
            if (structuralSystem != null)
            {
                structuralSystem.BindCollapseDamageReceiver(receiver);
            }
        }

        public void SetInteractionOrigin(Transform origin)
        {
            interactionOrigin = origin;
            PublishOutpostStatusIfAvailable();
        }

        public void SetElevatorPowerOrigin(Transform origin)
        {
            elevatorPowerOrigin = origin;
            PublishOutpostStatusIfAvailable();
        }

        private void Start()
        {
            powerNetworkSystem?.RequestRebuild();
        }

        private void Update()
        {
            if (!hasPowerSnapshot)
            {
                return;
            }

            var interactionFacility = FindInteractionFacility();
            var isInInteractionRange = interactionFacility != null;
            var interactionFacilityInstanceId = interactionFacility != null
                ? interactionFacility.InstanceId
                : string.Empty;
            var interactionFacilityBuildingId = interactionFacility != null
                ? interactionFacility.BuildingId
                : string.Empty;
            if (lastInteractionRange != isInInteractionRange
                || lastInteractionFacilityInstanceId != interactionFacilityInstanceId
                || lastInteractionFacilityBuildingId != interactionFacilityBuildingId)
            {
                PublishOutpostStatusIfAvailable();
            }
        }

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
            latestPowerSnapshot = snapshot;
            hasPowerSnapshot = true;
            PublishOutpostStatusIfAvailable();
        }

        private void PublishOutpostStatusIfAvailable()
        {
            if (!hasPowerSnapshot)
            {
                return;
            }

            var interactionFacility = FindInteractionFacility();
            var isInInteractionRange = interactionFacility != null;
            lastInteractionRange = isInInteractionRange;
            lastInteractionFacilityInstanceId = interactionFacility != null
                ? interactionFacility.InstanceId
                : string.Empty;
            lastInteractionFacilityBuildingId = interactionFacility != null
                ? interactionFacility.BuildingId
                : string.Empty;
            var status = new OutpostStatusDto
            {
                outpostInstanceId = outpostInstanceId,
                isActive = latestPowerSnapshot.Supply > 0,
                isInInteractionRange = isInInteractionRange,
                interactionFacilityInstanceId = lastInteractionFacilityInstanceId,
                interactionFacilityBuildingId = lastInteractionFacilityBuildingId,
                totalPowerSupply = latestPowerSnapshot.Supply,
                totalPowerConsumption = latestPowerSnapshot.Demand,
                connectedFacilities = BuildFacilityStatuses()
            };
            Publish(new GameplayEventDto { type = GameplayEventType.OutpostStatusChanged, instanceId = outpostInstanceId, outpostStatus = status });
        }

        private BuildingInstance FindInteractionFacility()
        {
            if (interactionOrigin == null || powerNetworkSystem == null)
            {
                return null;
            }

            var squaredRange = facilityInteractionRange * facilityInteractionRange;
            BuildingInstance nearestFacility = null;
            var nearestSquaredDistance = float.MaxValue;
            foreach (PowerNode node in powerNetworkSystem.Nodes)
            {
                if (node == null)
                {
                    continue;
                }

                var instance = node.GetComponent<BuildingInstance>();
                if (instance == null || !IsInteractionFacility(instance.BuildingId))
                {
                    continue;
                }

                var delta = (Vector2)(node.transform.position - interactionOrigin.position);
                var squaredDistance = delta.sqrMagnitude;
                if (squaredDistance > squaredRange || squaredDistance >= nearestSquaredDistance)
                {
                    continue;
                }

                nearestFacility = instance;
                nearestSquaredDistance = squaredDistance;
            }

            return nearestFacility;
        }

        private static bool IsInteractionFacility(string buildingId)
        {
            return buildingId == "building.charger.basic"
                || buildingId == "building.storage.basic"
                || buildingId == "building.settlement.basic"
                || buildingId == "building.outpost_core.basic";
        }

        private List<ConnectedFacilityStatusDto> BuildFacilityStatuses()
        {
            var statuses = new List<ConnectedFacilityStatusDto>();
            if (powerNetworkSystem == null)
            {
                return statuses;
            }

            foreach (PowerNode node in powerNetworkSystem.Nodes)
            {
                if (node == null || node.IsPowerSource)
                {
                    continue;
                }

                BuildingInstance instance = node.GetComponent<BuildingInstance>();
                if (instance == null || string.IsNullOrWhiteSpace(instance.BuildingId))
                {
                    continue;
                }

                var usesProximityPower = IsProximityPoweredFacility(instance.BuildingId);
                var isActive = usesProximityPower
                    ? IsWithinPowerSupplyRange(node.transform.position)
                    : node.IsPowered;

                statuses.Add(new ConnectedFacilityStatusDto
                {
                    instanceId = instance.InstanceId,
                    buildingId = instance.BuildingId,
                    isActive = isActive,
                    inactiveReasonId = isActive
                        ? string.Empty
                        : usesProximityPower || !powerNetworkSystem.IsReachable(node)
                            ? "power_disconnected"
                            : "insufficient_power"
                });
            }

            statuses.Sort((left, right) => string.CompareOrdinal(left.instanceId, right.instanceId));
            return statuses;
        }

        private bool IsWithinPowerSupplyRange(Vector3 facilityPosition)
        {
            var squaredRange = facilityPowerConnectionRange * facilityPowerConnectionRange;
            if (elevatorPowerOrigin != null)
            {
                var elevatorDelta = (Vector2)(facilityPosition - elevatorPowerOrigin.position);
                if (elevatorDelta.sqrMagnitude <= squaredRange)
                {
                    return true;
                }
            }

            foreach (PowerNode node in powerNetworkSystem.Nodes)
            {
                if (node == null || !node.IsPowerSource)
                {
                    continue;
                }

                var sourceDelta = (Vector2)(facilityPosition - node.transform.position);
                if (sourceDelta.sqrMagnitude <= squaredRange)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsProximityPoweredFacility(string buildingId)
        {
            return buildingId == "building.charger.basic"
                || buildingId == "building.settlement.basic";
        }

        private void Publish(GameplayEventDto gameplayEvent) => eventSink?.Publish(gameplayEvent);
    }
}
