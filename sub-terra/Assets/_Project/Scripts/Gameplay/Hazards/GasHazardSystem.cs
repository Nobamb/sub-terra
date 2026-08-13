using System;
using System.Collections.Generic;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SubTerra.Gameplay.Hazards
{
    /// <summary>Creates gas zones from mined tiles and publishes the player's current exposure state.</summary>
    public sealed class GasHazardSystem : MonoBehaviour
    {
        [SerializeField] private Tilemap foregroundTilemap;
        [SerializeField] private Transform playerTransform;
        [SerializeField] private GasZone gasZonePrefab;
        [SerializeField, Range(0f, 1f)] private float defaultIntensity = 0.8f;
        [SerializeField, Min(0.1f)] private float defaultRadius = GasVisualRules.GasRadiusBlocks;
        [SerializeField, Min(0.1f)] private float defaultDuration = 12f;

        private readonly List<GasZone> zones = new();
        private int nextZoneSequence = 1;

        public GasExposureState CurrentExposure { get; private set; } = new(false, GasRiskLevel.Safe, GasType.Unknown, string.Empty, 0f);
        public IReadOnlyList<GasZone> ActiveZones => zones;
        public event Action<GasExposureState> ExposureChanged;
        public event Action<GasZone> GasZoneActivated;
        public event Action<string> GasZoneDeactivated;

        private void Update()
        {
            TickZones(Time.deltaTime);
            ReevaluateExposure();
        }

        public void SetPlayerTransform(Transform target)
        {
            playerTransform = target;
            ReevaluateExposure();
        }

        public GasZone ActivateAt(Vector3Int cell, MiningTileDto tile)
        {
            if (foregroundTilemap == null || !tile.containsGas) return null;
            Vector3 position = foregroundTilemap.GetCellCenterWorld(cell);
            GasZone zone = CreateZone(position);
            string id = $"gas-{nextZoneSequence++:D4}";
            float intensity = Mathf.Max(defaultIntensity, tile.structuralImpact);
            zone.Activate(id, GasType.Toxic, intensity, defaultRadius, defaultDuration);
            zones.Add(zone);
            GasZoneActivated?.Invoke(zone);
            ReevaluateExposure();
            return zone;
        }

        /// <summary>
        /// 저장된 위치·농도·남은 시간을 복원한다.
        /// remainingDuration이 0 이하면 기본 지속시간을 사용한다.
        /// </summary>
        public GasZone RestoreGasZone(GasSnapshotDto snapshot)
        {
            if (!snapshot.isActive || snapshot.isNeutralized) return null;
            foreach (GasZone existing in zones)
            {
                if (existing != null && existing.IsActive && existing.GasZoneId == snapshot.gasZoneId) return existing;
            }

            string id = string.IsNullOrWhiteSpace(snapshot.gasZoneId) ? $"gas-{nextZoneSequence++:D4}" : snapshot.gasZoneId;
            if (Enum.TryParse(snapshot.gasTypeId, out GasType parsedType) == false) parsedType = GasType.Toxic;
            float duration = snapshot.remainingDuration > 0f
                ? snapshot.remainingDuration
                : defaultDuration;
            GasZone zone = CreateZone(new Vector3(snapshot.x, snapshot.y, 0f));
            zone.Activate(id, parsedType, snapshot.concentrationLevel, defaultRadius, duration, false);
            zones.Add(zone);
            GasZoneActivated?.Invoke(zone);
            ReevaluateExposure();
            return zone;
        }

        /// <summary>월드 스냅샷 복원 전 활성 가스 구역을 제거한다.</summary>
        public void ClearRestoredZones()
        {
            for (int index = zones.Count - 1; index >= 0; index--)
            {
                GasZone zone = zones[index];
                if (zone != null)
                {
                    if (Application.isPlaying) Destroy(zone.gameObject);
                    else DestroyImmediate(zone.gameObject);
                }
            }

            zones.Clear();
            nextZoneSequence = 1;
            SetExposure(new GasExposureState(false, GasRiskLevel.Safe, GasType.Unknown, string.Empty, 0f));
        }

        private GasZone CreateZone(Vector3 position)
        {
            if (gasZonePrefab != null)
            {
                GasZone zone = Instantiate(gasZonePrefab, position, Quaternion.identity, transform);
                zone.gameObject.SetActive(false);
                return zone;
            }

            GameObject fallback = new("GasZone_Runtime");
            fallback.transform.SetParent(transform);
            fallback.transform.position = position;
            fallback.transform.localScale = Vector3.one;
            var runtimeZone = fallback.AddComponent<GasZone>();
            fallback.AddComponent<GasZoneVisual>();
            var trigger = fallback.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = defaultRadius;
            return runtimeZone;
        }

        private void TickZones(float deltaTime)
        {
            for (int index = zones.Count - 1; index >= 0; index--)
            {
                GasZone zone = zones[index];
                if (zone == null) { zones.RemoveAt(index); continue; }
                bool wasActive = zone.IsActive;
                zone.Tick(deltaTime);
                if (wasActive && !zone.IsActive)
                    GasZoneDeactivated?.Invoke(zone.GasZoneId);
            }
        }

        private void ReevaluateExposure()
        {
            if (playerTransform == null)
            {
                SetExposure(new GasExposureState(false, GasRiskLevel.Safe, GasType.Unknown, string.Empty, 0f));
                return;
            }

            GasZone selected = null;
            foreach (GasZone zone in zones)
            {
                if (zone == null || !zone.Contains(playerTransform.position)) continue;
                if (selected == null || zone.Intensity > selected.Intensity) selected = zone;
            }

            if (selected == null)
            {
                SetExposure(new GasExposureState(false, GasRiskLevel.Safe, GasType.Unknown, string.Empty, 0f));
                return;
            }

            SetExposure(new GasExposureState(
                true,
                GasRiskEvaluator.Evaluate(selected.Intensity),
                selected.GasType,
                selected.GasZoneId,
                selected.RemainingDuration,
                selected.Intensity));
        }

        private void SetExposure(GasExposureState next)
        {
            if (CurrentExposure.Equals(next)) return;
            CurrentExposure = next;
            ExposureChanged?.Invoke(CurrentExposure);
        }
    }
}
