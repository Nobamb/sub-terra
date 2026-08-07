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
    /// <summary>
    /// 기본 월드는 Seed+generatorVersion으로 재생성하고,
    /// 채굴·변경 타일·건물·가스·붕괴·발견 구역·케이블만 변경점으로 저장·복원한다.
    /// 전력/구조/가스 노출 등 파생값은 복원 뒤 재계산한다.
    /// </summary>
    public sealed class WorldSnapshotSystem : MonoBehaviour, IWorldSnapshotProvider
    {
        [SerializeField] private Tilemap foregroundTilemap;
        [SerializeField] private MiningSystem miningSystem;
        [SerializeField] private MiningTileResolver tileResolver;
        [SerializeField] private StructuralIntegritySystem structuralSystem;
        [SerializeField] private GasHazardSystem gasHazardSystem;
        [SerializeField] private BuildingPlacementSystem buildingPlacementSystem;
        [SerializeField] private PowerNetworkSystem powerNetworkSystem;
        [SerializeField] private MonoBehaviour baseWorldGeneratorBehaviour;
        [SerializeField] private long worldSeed;
        [SerializeField, Min(1)] private int generatorVersion = 1;

        private readonly Dictionary<Vector3Int, MiningSnapshotDto> minedCells = new();
        private readonly Dictionary<Vector3Int, ChangedTileSnapshotDto> changedCells = new();
        private readonly Dictionary<Vector3Int, CollapseSnapshotDto> collapsedCells = new();
        private readonly Dictionary<string, BuildingSnapshotDto> buildings = new();
        private readonly HashSet<string> discoveredChunkIds = new(StringComparer.Ordinal);
        private IWorldBaseGenerator baseWorldGenerator;

        /// <summary>마지막 RestoreSnapshot 결과. Continue 경로가 비호환 생성을 감지할 때 사용한다.</summary>
        public bool LastRestoreSucceeded { get; private set; } = true;

        /// <summary>마지막 복원 실패 사유(로그·테스트용). 세이브 원문/절대 경로는 넣지 않는다.</summary>
        public string LastRestoreFailureReason { get; private set; } = string.Empty;

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
                version = "1.2",
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                worldSeed = worldSeed,
                generatorVersion = generatorVersion,
                miningChanges = new List<MiningSnapshotDto>(minedCells.Values),
                changedTiles = new List<ChangedTileSnapshotDto>(changedCells.Values),
                collapseChanges = new List<CollapseSnapshotDto>(collapsedCells.Values),
                buildings = new List<BuildingSnapshotDto>(buildings.Values),
                gasChanges = CaptureGasZones(),
                discoveredChunkIds = new List<string>(discoveredChunkIds),
                // 케이블 토폴로지만 저장. 공급/소비 등 파생 전력값은 복원 후 재계산한다.
                powerState = new PowerSnapshotDto
                {
                    cableConnections = CapturePowerConnections()
                }
            };
            snapshot.discoveredChunkIds.Sort(StringComparer.Ordinal);
            return snapshot;
        }

        public bool RestoreSnapshot(WorldSnapshotDto snapshot)
        {
            LastRestoreSucceeded = false;
            LastRestoreFailureReason = string.Empty;
            if (snapshot == null)
            {
                LastRestoreSucceeded = true;
                return true;
            }

            ResolveBaseWorldGenerator();
            int restoredVersion = snapshot.generatorVersion > 0
                ? snapshot.generatorVersion
                : 1;

            // 기본 월드 재생성. generatorVersion 불일치 시 명시적 실패 신호.
            if (baseWorldGenerator != null
                && !baseWorldGenerator.Regenerate(snapshot.worldSeed, restoredVersion))
            {
                LastRestoreFailureReason =
                    "generator_version_unavailable:" + restoredVersion;
                Debug.LogError(
                    "Cannot restore world: generator version "
                    + restoredVersion
                    + " is unavailable for seed restore.",
                    this);
                return false;
            }

            worldSeed = snapshot.worldSeed;
            generatorVersion = restoredVersion;
            structuralSystem?.ConfigureWorldSeed(worldSeed);

            // 런타임 변경점 버퍼와 배치된 시설을 비운 뒤 변경점을 다시 적용한다.
            ClearRuntimeChangeBuffers();
            buildingPlacementSystem?.PrepareForWorldRestore();
            gasHazardSystem?.ClearRestoredZones();
            ClearRuntimePowerCables();

            ApplyRemovedTiles(snapshot.miningChanges);
            ApplyChangedTiles(snapshot.changedTiles);
            ApplyCollapsedTiles(snapshot.collapseChanges);
            // 버팀목 등 지지물이 먼저 복원된 뒤 구조 위험을 재계산해야 한다.
            RestoreBuildings(snapshot.buildings);
            RebuildStructuralRiskAfterRestore(snapshot);
            RestoreGasZones(snapshot.gasChanges);
            RestoreDiscoveredChunks(snapshot.discoveredChunkIds);
            RestorePowerConnections(snapshot.powerState);
            powerNetworkSystem?.RequestRebuild();

            LastRestoreSucceeded = true;
            return true;
        }

        /// <summary>
        /// prompt-B 36-1: 타일 변경점 적용 후 전체 맵 기하 기준으로 구조 위험을 복원한다.
        /// 런타임 충격 버퍼는 세이브에 없으므로 파괴된 채굴/붕괴 셀을 충격 원천으로 재주입한다.
        /// </summary>
        private void RebuildStructuralRiskAfterRestore(WorldSnapshotDto snapshot)
        {
            if (structuralSystem == null)
            {
                return;
            }

            var minedCells = new List<Vector3Int>();
            if (snapshot?.miningChanges != null)
            {
                foreach (MiningSnapshotDto change in snapshot.miningChanges)
                {
                    if (!change.isDestroyed)
                    {
                        continue;
                    }

                    minedCells.Add(new Vector3Int(change.x, change.y, 0));
                }
            }

            // 붕괴로 사라진 셀도 비지지 기하에 기여하는 채굴 흔적으로 취급한다.
            if (snapshot?.collapseChanges != null)
            {
                foreach (CollapseSnapshotDto change in snapshot.collapseChanges)
                {
                    if (!change.isCollapsed)
                    {
                        continue;
                    }

                    var key = new Vector3Int(change.x, change.y, 0);
                    if (!minedCells.Contains(key))
                    {
                        minedCells.Add(key);
                    }
                }
            }

            // remainingDurability 0 변경 타일도 실질적으로 제거된 것으로 본다.
            if (snapshot?.changedTiles != null)
            {
                foreach (ChangedTileSnapshotDto change in snapshot.changedTiles)
                {
                    if (change.remainingDurability > 0f && !string.IsNullOrEmpty(change.tileId))
                    {
                        continue;
                    }

                    var key = new Vector3Int(change.x, change.y, 0);
                    if (!minedCells.Contains(key))
                    {
                        minedCells.Add(key);
                    }
                }
            }

            structuralSystem.RebuildRiskFromMinedCells(minedCells);
        }

        public void ConfigureBaseWorldIdentity(long seed, int version)
        {
            worldSeed = seed;
            generatorVersion = Mathf.Max(1, version);
        }

        /// <summary>부분 채굴·타일 교체 등 파괴가 아닌 변경점을 기록한다.</summary>
        public void RecordChangedTile(int x, int y, string tileId, float remainingDurability)
        {
            var key = new Vector3Int(x, y, 0);
            changedCells[key] = new ChangedTileSnapshotDto
            {
                x = x,
                y = y,
                tileId = tileId ?? string.Empty,
                remainingDurability = remainingDurability
            };
        }

        /// <summary>플레이어가 발견한 구역(청크) ID를 영구 변경점으로 기록한다.</summary>
        public void RecordDiscoveredChunk(string chunkId)
        {
            if (string.IsNullOrWhiteSpace(chunkId)) return;
            discoveredChunkIds.Add(chunkId);
        }

        /// <summary>테스트·외부 시스템이 채굴 변경점을 직접 주입할 때 사용한다.</summary>
        public void RecordMinedCell(int x, int y, bool isDestroyed, float remainingDurability)
        {
            var key = new Vector3Int(x, y, 0);
            minedCells[key] = new MiningSnapshotDto
            {
                x = x,
                y = y,
                isDestroyed = isDestroyed,
                remainingDurability = remainingDurability
            };
            if (isDestroyed)
            {
                changedCells.Remove(key);
            }
        }

        /// <summary>좌표별 타일 존재 여부를 해시해 저장 전후 비교에 사용한다.</summary>
        public long ComputeOccupiedTileHash()
        {
            if (foregroundTilemap == null) return 0L;
            long hash = 17L;
            var bounds = foregroundTilemap.cellBounds;
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                for (int x = bounds.xMin; x < bounds.xMax; x++)
                {
                    if (!foregroundTilemap.HasTile(new Vector3Int(x, y, 0))) continue;
                    unchecked
                    {
                        hash = (hash * 31L) + x;
                        hash = (hash * 31L) + y;
                    }
                }
            }

            return hash;
        }

        private void OnTileMined(Vector3Int cell, MiningTileDto _)
        {
            minedCells[cell] = new MiningSnapshotDto
            {
                x = cell.x,
                y = cell.y,
                isDestroyed = true,
                remainingDurability = 0f
            };
            changedCells.Remove(cell);
        }

        private void OnStructuralCollapse(StructuralCollapseEventDto collapse)
        {
            if (collapse?.cells == null) return;
            foreach (CollapseCellDto cell in collapse.cells)
            {
                var key = new Vector3Int(cell.x, cell.y, 0);
                collapsedCells[key] = new CollapseSnapshotDto
                {
                    x = cell.x,
                    y = cell.y,
                    isCollapsed = true,
                    structuralIntegrity = 0f
                };
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

        private List<PowerConnectionSnapshotDto> CapturePowerConnections()
        {
            var connections = new List<PowerConnectionSnapshotDto>();
            if (powerNetworkSystem == null) return connections;
            foreach (PowerCable cable in powerNetworkSystem.Cables)
            {
                if (cable == null || !cable.IsValid) continue;
                string a = cable.EndpointA != null ? cable.EndpointA.EntityId : string.Empty;
                string b = cable.EndpointB != null ? cable.EndpointB.EntityId : string.Empty;
                if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) continue;
                connections.Add(new PowerConnectionSnapshotDto
                {
                    nodeAInstanceId = a,
                    nodeBInstanceId = b
                });
            }

            connections.Sort((left, right) =>
            {
                int compare = string.CompareOrdinal(left.nodeAInstanceId, right.nodeAInstanceId);
                return compare != 0
                    ? compare
                    : string.CompareOrdinal(left.nodeBInstanceId, right.nodeBInstanceId);
            });
            return connections;
        }

        private void ApplyRemovedTiles(IEnumerable<MiningSnapshotDto> changes)
        {
            if (foregroundTilemap == null || changes == null) return;
            foreach (MiningSnapshotDto change in changes)
            {
                var key = new Vector3Int(change.x, change.y, 0);
                minedCells[key] = change;
                if (change.isDestroyed)
                {
                    foregroundTilemap.SetTile(key, null);
                }
            }
        }

        private void ApplyChangedTiles(IEnumerable<ChangedTileSnapshotDto> changes)
        {
            if (changes == null) return;
            foreach (ChangedTileSnapshotDto change in changes)
            {
                var key = new Vector3Int(change.x, change.y, 0);
                changedCells[key] = change;
                if (foregroundTilemap == null) continue;

                if (string.IsNullOrEmpty(change.tileId) || change.remainingDurability <= 0f)
                {
                    foregroundTilemap.SetTile(key, null);
                    continue;
                }

                // 타일 ID → TileBase 역조회가 가능할 때만 교체. 없으면 버퍼만 복원해 해시/후속 시스템이 사용한다.
                if (tileResolver != null
                    && tileResolver.TryFindTileById(change.tileId, out TileBase tile)
                    && tile != null)
                {
                    foregroundTilemap.SetTile(key, tile);
                }
            }
        }

        private void ApplyCollapsedTiles(IEnumerable<CollapseSnapshotDto> changes)
        {
            if (changes == null) return;
            bool tileChanged = false;
            foreach (CollapseSnapshotDto change in changes)
            {
                var key = new Vector3Int(change.x, change.y, 0);
                collapsedCells[key] = change;
                if (!change.isCollapsed || foregroundTilemap == null) continue;
                foregroundTilemap.SetTile(key, null);
                tileChanged = true;
            }

            if (!tileChanged || foregroundTilemap == null) return;
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

        private void RestoreDiscoveredChunks(IEnumerable<string> chunkIds)
        {
            discoveredChunkIds.Clear();
            if (chunkIds == null) return;
            foreach (string id in chunkIds)
            {
                if (!string.IsNullOrWhiteSpace(id)) discoveredChunkIds.Add(id);
            }
        }

        private void RestorePowerConnections(PowerSnapshotDto powerState)
        {
            if (powerNetworkSystem == null
                || powerState.cableConnections == null
                || powerState.cableConnections.Count == 0)
            {
                return;
            }

            Dictionary<string, PowerNode> nodesById = BuildNodeLookup();
            foreach (PowerConnectionSnapshotDto connection in powerState.cableConnections)
            {
                if (string.IsNullOrEmpty(connection.nodeAInstanceId)
                    || string.IsNullOrEmpty(connection.nodeBInstanceId))
                {
                    continue;
                }

                if (!nodesById.TryGetValue(connection.nodeAInstanceId, out PowerNode nodeA)
                    || !nodesById.TryGetValue(connection.nodeBInstanceId, out PowerNode nodeB))
                {
                    continue;
                }

                if (HasCableBetween(nodeA, nodeB)) continue;

                var cableObject = new GameObject(
                    "PowerCable_Restore_" + connection.nodeAInstanceId + "_" + connection.nodeBInstanceId);
                cableObject.transform.SetParent(powerNetworkSystem.transform, false);
                PowerCable cable = cableObject.AddComponent<PowerCable>();
                cable.Configure(powerNetworkSystem, nodeA, nodeB);
                powerNetworkSystem.RegisterCable(cable);
            }
        }

        private Dictionary<string, PowerNode> BuildNodeLookup()
        {
            var lookup = new Dictionary<string, PowerNode>(StringComparer.Ordinal);
            if (powerNetworkSystem == null) return lookup;
            foreach (PowerNode node in powerNetworkSystem.Nodes)
            {
                if (node == null || string.IsNullOrEmpty(node.EntityId)) continue;
                lookup[node.EntityId] = node;
            }

            return lookup;
        }

        private bool HasCableBetween(PowerNode a, PowerNode b)
        {
            if (powerNetworkSystem == null) return false;
            foreach (PowerCable cable in powerNetworkSystem.Cables)
            {
                if (cable == null || !cable.IsValid) continue;
                if ((cable.EndpointA == a && cable.EndpointB == b)
                    || (cable.EndpointA == b && cable.EndpointB == a))
                {
                    return true;
                }
            }

            return false;
        }

        private void ClearRuntimeChangeBuffers()
        {
            minedCells.Clear();
            changedCells.Clear();
            collapsedCells.Clear();
            buildings.Clear();
            discoveredChunkIds.Clear();
        }

        private void ClearRuntimePowerCables()
        {
            if (powerNetworkSystem == null) return;
            var existing = new List<PowerCable>(powerNetworkSystem.Cables);
            foreach (PowerCable cable in existing)
            {
                if (cable == null) continue;
                powerNetworkSystem.UnregisterCable(cable);
                if (Application.isPlaying) Destroy(cable.gameObject);
                else DestroyImmediate(cable.gameObject);
            }
        }

        private void ResolveBaseWorldGenerator()
        {
            baseWorldGenerator = baseWorldGeneratorBehaviour as IWorldBaseGenerator;
        }
    }
}
