using System;
using System.Collections.Generic;
using SubTerra.Gameplay.Building;
using SubTerra.Gameplay.Hazards;
using SubTerra.Gameplay.Mining;
using SubTerra.Gameplay.Power;
using SubTerra.Gameplay.Structural;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.Tilemaps;

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
        [SerializeField] private Transform elevatorPowerOrigin;
        [SerializeField] private string outpostInstanceId = "outpost.demo";

        private IGameplayEventSink eventSink;
        private PowerNetworkSnapshot latestPowerSnapshot;
        private bool hasPowerSnapshot;
        private bool lastInteractionRange;
        private bool lastPurificationRange;
        private string lastInteractionFacilityInstanceId;
        private string lastInteractionFacilityBuildingId;
        private ICollapseDamageReceiver collapseDamageReceiver;
        private Func<TileBase, float> collapseDamageMultiplier;

        private void Awake() => eventSink = eventSinkBehaviour as IGameplayEventSink;

        private void OnEnable()
        {
            if (miningSystem != null) miningSystem.TileMined += OnTileMined;
            if (structuralSystem != null)
            {
                structuralSystem.BindCollapseDamageReceiver(collapseDamageReceiver, collapseDamageMultiplier);
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
            // 꺼져 있던 동안(복원 등) 바뀐 범위 상태는 연출 없이 맞춰 둔다.
            RefreshFacilityPowerRange(false);
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

        public void SetCollapseDamageReceiver(
            ICollapseDamageReceiver receiver,
            Func<TileBase, float> resolveDamageMultiplier = null)
        {
            collapseDamageReceiver = receiver;
            collapseDamageMultiplier = resolveDamageMultiplier;
            if (structuralSystem != null)
            {
                structuralSystem.BindCollapseDamageReceiver(receiver, collapseDamageMultiplier);
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
            // 엘리베이터 연결은 씬 배선 단계라 켜짐 연출 없이 상태만 맞춘다.
            RefreshFacilityPowerRange(false);
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
            var isInPurificationRange = interactionOrigin != null
                && IsWithinOutpostSupplyRange(interactionOrigin.position);
            var interactionFacilityInstanceId = interactionFacility != null
                ? interactionFacility.InstanceId
                : string.Empty;
            var interactionFacilityBuildingId = interactionFacility != null
                ? interactionFacility.BuildingId
                : string.Empty;
            if (lastInteractionRange != isInInteractionRange
                || lastInteractionFacilityInstanceId != interactionFacilityInstanceId
                || lastInteractionFacilityBuildingId != interactionFacilityBuildingId
                || lastPurificationRange != isInPurificationRange)
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

        private void OnBuildingPlaced(BuildingPlacementResult result)
        {
            PublishBuildingResult(result, BuildingPlacementState.Placed);

            if (result.IsSuccess && result.BuildingId == "building.outpost_core.basic")
            {
                Publish(new GameplayEventDto
                {
                    type = GameplayEventType.OutpostActivated,
                    entityId = result.InstanceId,
                    instanceId = result.InstanceId,
                    x = result.Cell.x,
                    y = result.Cell.y
                });
            }
        }
        private void OnBuildingRejected(BuildingPlacementResult result) => PublishBuildingResult(result, BuildingPlacementState.Failed);

        private void PublishBuildingResult(BuildingPlacementResult result, BuildingPlacementState state)
        {
            var placement = new BuildingPlacementResultDto
            {
                state = state,
                buildingId = result.BuildingId,
                instanceId = result.InstanceId,
                reasonId = result.Failure.ToString(),
                x = result.Cell.x,
                y = result.Cell.y,
                reducedStructuralRisk = result.ReducedStructuralRisk
            };
            Publish(new GameplayEventDto { type = result.IsSuccess ? GameplayEventType.BuildingPlaced : GameplayEventType.BuildingPlacementChanged, entityId = result.BuildingId, instanceId = result.InstanceId, x = result.Cell.x, y = result.Cell.y, buildingPlacement = placement });
        }

        private void OnPowerNetworkRebuilt(PowerNetworkSnapshot snapshot)
        {
            EnsureOutpostRangeIndicators();
            RefreshFacilityPowerRange(true);
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
            lastPurificationRange = interactionOrigin != null
                && IsWithinOutpostSupplyRange(interactionOrigin.position);
            var status = new OutpostStatusDto
            {
                outpostInstanceId = outpostInstanceId,
                isActive = latestPowerSnapshot.Supply > 0,
                isInInteractionRange = isInInteractionRange,
                isInPurificationRange = lastPurificationRange,
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
                || buildingId == "building.clinic.basic"
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

                if (!IsProximityPoweredFacility(instance.BuildingId))
                {
                    continue;
                }

                var isActive = IsWithinPowerSupplyRange(node.transform.position);

                statuses.Add(new ConnectedFacilityStatusDto
                {
                    instanceId = instance.InstanceId,
                    buildingId = instance.BuildingId,
                    isActive = isActive,
                    inactiveReasonId = isActive
                        ? string.Empty
                        : "power_disconnected"
                });
            }

            statuses.Sort((left, right) => string.CompareOrdinal(left.instanceId, right.instanceId));
            return statuses;
        }

        /// <summary>
        /// B-185-1: 켜짐 연출을 실제 게임 판정(BuildFacilityStatuses와 같은 공급 범위)에 맞춘다.
        /// 설치·이동·코어 설치·엘리베이터 범위 모두 Rebuild를 거치므로 여기서 한 번에 판정한다.
        /// 복원 중에는 PowerOnEffect.IsSuppressed라 상태만 맞추고, 판정 끝에 BatchStarted를 한 번 알린다.
        /// </summary>
        private void RefreshFacilityPowerRange(bool allowEffect)
        {
            if (powerNetworkSystem == null)
            {
                return;
            }

            try
            {
                foreach (PowerNode node in powerNetworkSystem.Nodes)
                {
                    if (node == null || node.IsPowerSource)
                    {
                        continue;
                    }

                    var instance = node.GetComponent<BuildingInstance>();
                    if (instance == null || !IsProximityPoweredFacility(instance.BuildingId))
                    {
                        continue;
                    }

                    var facility = node.GetComponent<PowerFacility>();
                    if (facility == null)
                    {
                        continue;
                    }

                    facility.SetSupplyRangePowered(
                        IsWithinPowerSupplyRange(node.transform.position),
                        allowEffect);
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
            finally
            {
                PowerOnEffect.FlushPendingStarts();
            }
        }

        private bool IsWithinPowerSupplyRange(Vector3 facilityPosition)
        {
            var squaredRange = PowerSupplyRangeIndicator.DefaultRadius
                * PowerSupplyRangeIndicator.DefaultRadius;
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

                var range = node.GetComponent<PowerSupplyRangeIndicator>();
                if (range != null
                    ? range.Contains(facilityPosition)
                    : ((Vector2)(facilityPosition - node.transform.position)).sqrMagnitude <= squaredRange)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsWithinOutpostSupplyRange(Vector3 worldPosition)
        {
            if (powerNetworkSystem == null)
            {
                return false;
            }

            var squaredRange = PowerSupplyRangeIndicator.DefaultRadius
                * PowerSupplyRangeIndicator.DefaultRadius;
            foreach (PowerNode node in powerNetworkSystem.Nodes)
            {
                if (!IsOutpostCoreSource(node))
                {
                    continue;
                }

                var range = node.GetComponent<PowerSupplyRangeIndicator>();
                if (range != null
                    ? range.Contains(worldPosition)
                    : ((Vector2)(worldPosition - node.transform.position)).sqrMagnitude <= squaredRange)
                {
                    return true;
                }
            }

            return false;
        }

        private void EnsureOutpostRangeIndicators()
        {
            if (powerNetworkSystem == null)
            {
                return;
            }

            foreach (PowerNode node in powerNetworkSystem.Nodes)
            {
                if (!IsOutpostCoreSource(node)
                    || node.GetComponent<PowerSupplyRangeIndicator>() != null)
                {
                    continue;
                }

                node.gameObject.AddComponent<PowerSupplyRangeIndicator>()
                    .Configure(PowerSupplyRangeIndicator.DefaultRadius);
            }
        }

        private static bool IsOutpostCoreSource(PowerNode node)
        {
            if (node == null || !node.IsPowerSource)
            {
                return false;
            }

            var instance = node.GetComponent<BuildingInstance>();
            return instance != null
                && instance.BuildingId == "building.outpost_core.basic";
        }

        /// <summary>
        /// 전진기지 범위가 켜짐 판정 그 자체인 시설. 켜짐 연출도 이 시설에만 나온다.
        /// 조명(가스 시야)·보관함(근접 사용)은 범위와 무관하게 동작하므로 연출을 내지 않는다.
        /// </summary>
        private static bool IsProximityPoweredFacility(string buildingId)
        {
            return buildingId == "building.charger.basic"
                || buildingId == "building.clinic.basic"
                || buildingId == "building.settlement.basic";
        }

        private void Publish(GameplayEventDto gameplayEvent) => eventSink?.Publish(gameplayEvent);
    }
}
