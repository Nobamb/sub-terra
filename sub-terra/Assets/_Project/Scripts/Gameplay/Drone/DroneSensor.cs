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
        [SerializeField, Min(0)] private int mineralScanRadius;
        [SerializeField, Min(0.1f)] private float scanInterval = 0.5f;
        [SerializeField, Min(0.1f)] private float pulseInterval = 30f;
        [SerializeField, Min(0.1f)] private float pulseDuration = 10f;
        [SerializeField] private float surfaceY;

        private float nextScanTime;
        private float nextPulseTime;
        private int currentEnergy;
        private int returnEnergyEstimate;
        private int unsettledCargoValue;
        private float cargoWeight;
        private float maxCargoWeight;
        private bool returnPathAvailable = true;
        private GasRiskLevel? appliedGasRisk;
        private IUpgradeEffectProvider upgradeEffects;
        private DroneScanPulseView pulseView;
        private readonly List<DroneScanTarget> lastPulseTargets = new();

        public DroneContextDto CurrentContext { get; private set; }
        public int EffectiveMineralScanRadius => ResolveMineralScanRadius();
        public float ContextScanInterval => scanInterval;
        public float PulseInterval => pulseInterval;
        public float PulseDuration => pulseDuration;
        public IReadOnlyList<DroneScanTarget> LastPulseTargets => lastPulseTargets;
        public DroneScanPulseView ScanPulseView => pulseView;
        /// <summary>지표면 기준 Y. HUD 깊이 브리지와 동일 값을 공유할 때 사용한다.</summary>
        public float SurfaceY
        {
            get => surfaceY;
            set => surfaceY = value;
        }

        public event Action<DroneContextDto> ContextUpdated;

        private void Update()
        {
            float now = Time.time;
            if (now >= nextScanTime)
            {
                nextScanTime = now + scanInterval;
                CaptureAndNotify();
            }

            TickScanPulse(now);
        }

        public void SetPlayerTransform(Transform target) => playerTransform = target;

        public void SetUpgradeEffects(IUpgradeEffectProvider effects)
        {
            upgradeEffects = effects;
            nextPulseTime = 0f;
            if (ResolveMineralScanRadius() <= 0)
            {
                ClearScanPulse();
            }
        }

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
            StructuralRiskStatus structuralStatus = structuralSystem != null
                ? structuralSystem.EvaluateStatusAtWorld(playerPosition)
                : StructuralRiskStatus.Stable(Vector3Int.zero);
            GasRiskLevel gasRisk = appliedGasRisk
                ?? (gasHazardSystem != null
                    ? gasHazardSystem.CurrentExposure.Risk
                    : GasRiskLevel.Safe);
            float baseDistance = DroneContextCalculator.FindNearestDistance(playerPosition, outpostCores);
            IReadOnlyList<string> minerals = ScanNearbyMinerals(playerPosition);
            return new DroneContextDto(depth, currentEnergy, returnEnergyEstimate, structuralStatus.Level, gasRisk, unsettledCargoValue, cargoWeight, maxCargoWeight, baseDistance, minerals, returnPathAvailable, structuralStatus.Cause, structuralStatus.IsTelegraphing);
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
                structuralCauseId = ToCauseId(context.StructuralCause),
                structuralTelegraphing = context.StructuralTelegraphing,
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
            var radius = ResolveMineralScanRadius();
            if (radius <= 0) return new List<string>();
            for (int x = center.x - radius; x <= center.x + radius; x++)
            for (int y = center.y - radius; y <= center.y + radius; y++)
            {
                TileBase tile = foregroundTilemap.GetTile(new Vector3Int(x, y, center.z));
                if (tile == null || !tileResolver.TryResolve(tile, out MiningTileDto definition) || string.IsNullOrWhiteSpace(definition.mineralId)) continue;
                mineralIds.Add(definition.mineralId);
            }
            return new List<string>(mineralIds);
        }

        private int ResolveMineralScanRadius()
        {
            var baseRadius = Mathf.Max(0, mineralScanRadius);
            var effectiveRadius = upgradeEffects != null
                ? upgradeEffects.GetDroneScanRadius(baseRadius)
                : 0f;
            if (float.IsNaN(effectiveRadius) || float.IsInfinity(effectiveRadius))
            {
                return 0;
            }

            return Mathf.Max(0, Mathf.CeilToInt(effectiveRadius));
        }

        /// <summary>Context 갱신과 독립된 30초 월드 스캔 펄스를 진행한다.</summary>
        public void TickScanPulse(float currentTime)
        {
            if (pulseView != null)
            {
                pulseView.Tick(currentTime);
            }

            int radius = ResolveMineralScanRadius();
            if (radius <= 0)
            {
                if (lastPulseTargets.Count > 0 || pulseView != null)
                {
                    ClearScanPulse();
                }
                return;
            }

            if (currentTime < nextPulseTime) return;
            nextPulseTime = currentTime + Mathf.Max(0.1f, pulseInterval);

            CollectPulseTargets(radius, lastPulseTargets);
            EnsurePulseView();
            Vector3 center = foregroundTilemap != null
                ? foregroundTilemap.GetCellCenterWorld(foregroundTilemap.WorldToCell(GetPlayerPosition()))
                : GetPlayerPosition();
            pulseView.Show(
                lastPulseTargets,
                center,
                radius,
                currentTime + Mathf.Max(0.1f, pulseDuration),
                currentTime);
        }

        private void CollectPulseTargets(int radius, List<DroneScanTarget> results)
        {
            results.Clear();
            if (foregroundTilemap == null || radius <= 0) return;

            Vector3Int center = foregroundTilemap.WorldToCell(GetPlayerPosition());
            var kinds = new Dictionary<Vector3Int, DroneScanTargetKind>();
            for (int x = center.x - radius; x <= center.x + radius; x++)
            for (int y = center.y - radius; y <= center.y + radius; y++)
            {
                var cell = new Vector3Int(x, y, center.z);
                TileBase tile = foregroundTilemap.GetTile(cell);
                if (tile != null
                    && tileResolver != null
                    && tileResolver.TryResolve(tile, out MiningTileDto definition)
                    && !string.IsNullOrWhiteSpace(definition.mineralId))
                {
                    kinds[cell] = DroneScanTargetKind.Mineral;
                }

                // Lv.2의 절대 반경 7부터 활성 가스 구역이 덮는 셀도 위험 표식으로 우선한다.
                if (radius >= 7 && IsActiveGasCell(cell))
                {
                    kinds[cell] = DroneScanTargetKind.GasHazard;
                }
            }

            for (int x = center.x - radius; x <= center.x + radius; x++)
            for (int y = center.y - radius; y <= center.y + radius; y++)
            {
                var cell = new Vector3Int(x, y, center.z);
                if (kinds.TryGetValue(cell, out DroneScanTargetKind kind))
                {
                    results.Add(new DroneScanTarget(
                        cell,
                        foregroundTilemap.GetCellCenterWorld(cell),
                        kind));
                }
            }
        }

        private bool IsActiveGasCell(Vector3Int cell)
        {
            if (gasHazardSystem == null) return false;
            Vector2 cellCenter = foregroundTilemap.GetCellCenterWorld(cell);
            IReadOnlyList<GasZone> zones = gasHazardSystem.ActiveZones;
            for (int index = 0; index < zones.Count; index++)
            {
                GasZone zone = zones[index];
                if (zone != null
                    && zone.IsActive
                    && Vector2.Distance(zone.transform.position, cellCenter) <= zone.Radius)
                {
                    return true;
                }
            }

            return false;
        }

        private Vector3 GetPlayerPosition()
        {
            return playerTransform != null ? playerTransform.position : transform.position;
        }

        private void EnsurePulseView()
        {
            if (pulseView != null) return;
            pulseView = GetComponent<DroneScanPulseView>();
            if (pulseView == null)
            {
                pulseView = gameObject.AddComponent<DroneScanPulseView>();
            }
        }

        private void ClearScanPulse()
        {
            lastPulseTargets.Clear();
            if (pulseView != null)
            {
                pulseView.Clear();
            }
        }

        private void OnDisable()
        {
            ClearScanPulse();
            nextScanTime = 0f;
            nextPulseTime = 0f;
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

        private static string ToCauseId(StructuralRiskCause cause)
        {
            return cause == StructuralRiskCause.Unsupported ? "unsupported"
                : cause == StructuralRiskCause.MiningImpact ? "mining_impact"
                : cause == StructuralRiskCause.SupportRemoved ? "support_removed"
                : string.Empty;
        }
    }
}
