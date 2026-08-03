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

namespace SubTerra.Gameplay.Snapshot
{
    /// <summary>Records only gameplay changes and restores the world before derived systems recalculate.</summary>
    public sealed class WorldSnapshotSystem : MonoBehaviour, IWorldSnapshotProvider
    {
        [SerializeField] private Tilemap foregroundTilemap;
        [SerializeField] private MiningSystem miningSystem;
        [SerializeField] private StructuralIntegritySystem structuralSystem;
        [SerializeField] private GasHazardSystem gasHazardSystem;
        [SerializeField] private BuildingPlacementSystem buildingPlacementSystem;
        [SerializeField] private PowerNetworkSystem powerNetworkSystem;
        [SerializeField] private MonoBehaviour baseWorldGeneratorBehaviour;
        [SerializeField] private long worldSeed;
        [SerializeField, Min(1)] private int generatorVersion = 1;

        private readonly Dictionary<Vector3Int, MiningSnapshotDto> minedCells = new();
        private readonly Dictionary<Vector3Int, CollapseSnapshotDto> collapsedCells = new();
        private readonly Dictionary<string, BuildingSnapshotDto> buildings = new();
        private IWorldBaseGenerator baseWorldGenerator;

        private void Awake() => ResolveBaseWorldGenerator();

        private void OnEnable()
        {
            if (miningSystem != null) miningSystem.TileMined += OnTileMined;
            if (structuralSystem != null) structuralSystem.CollapseTriggered += OnStructuralCollapse;
            if (buildingPlacementSystem != null) buildingPlacementSystem.BuildingPlaced += OnBuildingPlaced;
        }

        private void OnDisable()
        {
            if (miningSystem != null) miningSystem.TileMined -= OnTileMined;
            if (structuralSystem != null) structuralSystem.CollapseTriggered -= OnStructuralCollapse;
            if (buildingPlacementSystem != null) buildingPlacementSystem.BuildingPlaced -= OnBuildingPlaced;
        }

        public WorldSnapshotDto CaptureSnapshot()
        {
            var snapshot = new WorldSnapshotDto
            {
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                worldSeed = worldSeed,
                generatorVersion = generatorVersion,
                miningChanges = new List<MiningSnapshotDto>(minedCells.Values),
                collapseChanges = new List<CollapseSnapshotDto>(collapsedCells.Values),
                buildings = new List<BuildingSnapshotDto>(buildings.Values),
                gasChanges = CaptureGasZones(),
                // 합의한 단순 복원 방식: 시설 위치를 복원한 뒤 전력망을 다시 계산한다.
                powerState = new PowerSnapshotDto { cableConnections = new List<PowerConnectionSnapshotDto>() }
            };
            return snapshot;
        }

        public void RestoreSnapshot(WorldSnapshotDto snapshot)
        {
            if (snapshot == null) return;
            ResolveBaseWorldGenerator();
            int restoredVersion = snapshot.generatorVersion > 0
                ? snapshot.generatorVersion
                : 1;
            if (baseWorldGenerator != null
                && !baseWorldGenerator.Regenerate(snapshot.worldSeed, restoredVersion))
            {
                Debug.LogError(
                    $"Cannot restore world seed {snapshot.worldSeed}: generator version {restoredVersion} is unavailable.",
                    this);
                return;
            }

            worldSeed = snapshot.worldSeed;
            generatorVersion = restoredVersion;
            ApplyRemovedTiles(snapshot.miningChanges);
            ApplyCollapsedTiles(snapshot.collapseChanges);
            RestoreBuildings(snapshot.buildings);
            RestoreGasZones(snapshot.gasChanges);
            powerNetworkSystem?.RequestRebuild();
        }

        public void ConfigureBaseWorldIdentity(long seed, int version)
        {
            worldSeed = seed;
            generatorVersion = Mathf.Max(1, version);
        }

        private void OnTileMined(Vector3Int cell, MiningTileDto _)
        {
            minedCells[cell] = new MiningSnapshotDto { x = cell.x, y = cell.y, isDestroyed = true, remainingDurability = 0f };
        }

        private void OnStructuralCollapse(StructuralCollapseEventDto collapse)
        {
            if (collapse?.cells == null) return;
            foreach (CollapseCellDto cell in collapse.cells)
            {
                var key = new Vector3Int(cell.x, cell.y, 0);
                collapsedCells[key] = new CollapseSnapshotDto { x = cell.x, y = cell.y, isCollapsed = true, structuralIntegrity = 0f };
            }
        }

        private void OnBuildingPlaced(BuildingPlacementResult result)
        {
            if (!result.IsSuccess) return;
            buildings[result.InstanceId] = new BuildingSnapshotDto
            {
                instanceId = result.InstanceId,
                buildingTypeId = result.BuildingId,
                x = result.Cell.x,
                y = result.Cell.y,
                rotation = 0,
                level = 1,
                health = 1f
            };
        }

        private List<GasSnapshotDto> CaptureGasZones()
        {
            var snapshots = new List<GasSnapshotDto>();
            if (gasHazardSystem == null) return snapshots;
            foreach (GasZone zone in gasHazardSystem.ActiveZones)
            {
                if (zone == null || !zone.IsActive) continue;
                snapshots.Add(new GasSnapshotDto
                {
                    gasZoneId = zone.GasZoneId,
                    gasTypeId = zone.GasType.ToString(),
                    x = Mathf.RoundToInt(zone.transform.position.x),
                    y = Mathf.RoundToInt(zone.transform.position.y),
                    concentrationLevel = zone.Intensity,
                    remainingDuration = zone.RemainingDuration,
                    isActive = true,
                    isNeutralized = false
                });
            }
            return snapshots;
        }

        private void ApplyRemovedTiles(IEnumerable<MiningSnapshotDto> changes)
        {
            if (foregroundTilemap == null || changes == null) return;
            foreach (MiningSnapshotDto change in changes)
            {
                if (change.isDestroyed) foregroundTilemap.SetTile(new Vector3Int(change.x, change.y, 0), null);
            }
        }

        private void ApplyCollapsedTiles(IEnumerable<CollapseSnapshotDto> changes)
        {
            if (foregroundTilemap == null || changes == null) return;
            bool tileChanged = false;
            foreach (CollapseSnapshotDto change in changes)
            {
                if (!change.isCollapsed) continue;
                foregroundTilemap.SetTile(new Vector3Int(change.x, change.y, 0), null);
                tileChanged = true;
            }

            if (!tileChanged) return;
            foregroundTilemap.RefreshAllTiles();
            TilemapCollider2D tilemapCollider = foregroundTilemap.GetComponent<TilemapCollider2D>();
            if (tilemapCollider != null && tilemapCollider.hasTilemapChanges)
                tilemapCollider.ProcessTilemapChanges();
        }

        private void RestoreBuildings(IEnumerable<BuildingSnapshotDto> snapshots)
        {
            if (buildingPlacementSystem == null || snapshots == null) return;
            buildings.Clear();
            foreach (BuildingSnapshotDto building in snapshots)
            {
                if (!buildingPlacementSystem.TryRestoreBuilding(building)) continue;
                buildings[building.instanceId] = building;
            }
        }

        private void RestoreGasZones(IEnumerable<GasSnapshotDto> snapshots)
        {
            if (gasHazardSystem == null || snapshots == null) return;
            foreach (GasSnapshotDto gas in snapshots) gasHazardSystem.RestoreGasZone(gas);
        }

        private void ResolveBaseWorldGenerator()
        {
            baseWorldGenerator = baseWorldGeneratorBehaviour as IWorldBaseGenerator;
        }
    }
}
