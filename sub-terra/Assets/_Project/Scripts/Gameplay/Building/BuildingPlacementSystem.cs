using System;
using System.Collections.Generic;
using SubTerra.Gameplay.Structural;
using SubTerra.Gameplay.Power;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SubTerra.Gameplay.Building
{
    /// <summary>Owns grid validation and runtime creation; inventory spending stays behind an adapter.</summary>
    public sealed class BuildingPlacementSystem : MonoBehaviour
    {
        [SerializeField] private Tilemap terrainTilemap;
        [SerializeField] private Transform buildingRoot;
        [SerializeField] private MonoBehaviour resourceWalletBehaviour;
        [SerializeField] private StructuralIntegritySystem structuralIntegritySystem;
        [SerializeField] private PowerNetworkSystem powerNetworkSystem;
        [SerializeField] private Transform placementOrigin;
        [SerializeField, Min(0f)] private float maximumPlacementDistance = 6f;
        [SerializeField] private Collider2D allowedPlacementArea;
        [SerializeField] private BuildingPlacementDefinition[] restoreDefinitions = Array.Empty<BuildingPlacementDefinition>();

        private readonly HashSet<Vector3Int> occupiedCells = new();
        private IBuildingResourceWallet resourceWallet;
        private IResourceWallet sharedResourceWallet;
        private BuildingPlacementDefinition selection;
        private int nextInstanceSequence = 1;
        private readonly HashSet<string> restoredInstanceIds = new();

        public BuildingPlacementDefinition Selection => selection;
        public event Action<BuildingPlacementResult> BuildingPlaced;
        public event Action<BuildingPlacementResult> PlacementRejected;

        private void Awake()
        {
            resourceWallet = resourceWalletBehaviour as IBuildingResourceWallet;
            if (buildingRoot == null) buildingRoot = transform;
            if (powerNetworkSystem == null) powerNetworkSystem = GetComponent<PowerNetworkSystem>();
        }

        private void OnDisable()
        {
            // 비활성 시 잔여 Preview가 Enter 채굴 게이트를 붙잡지 않게 한다.
            if (selection != null)
            {
                selection = null;
                BuildingPlacementActivity.End();
            }
        }

        public void Select(BuildingPlacementDefinition definition)
        {
            if (selection != null)
            {
                BuildingPlacementActivity.End();
            }

            selection = definition;
            if (selection != null)
            {
                BuildingPlacementActivity.Begin();
            }
        }

        public void ClearSelection()
        {
            if (selection != null)
            {
                BuildingPlacementActivity.End();
            }

            selection = null;
        }

        /// <summary>Called by App bootstrap to connect the Shared economy contract without a Unity object reference.</summary>
        public void SetResourceWallet(IResourceWallet wallet) => sharedResourceWallet = wallet;

        public Vector3Int WorldToCell(Vector3 worldPosition)
        {
            return terrainTilemap != null ? terrainTilemap.WorldToCell(worldPosition) : Vector3Int.RoundToInt(worldPosition);
        }

        /// <summary>
        /// 커서 칸을 하단 앵커로 보고, 플레이어 대비 좌/우로 footprint 가로를 펼친 뒤 위로 높이를 펼친다.
        /// 반환 origin은 항상 발자국 좌하단(EnumerateFootprint 기준).
        /// 오른쪽 설치: 커서가 왼쪽 열 → 오른쪽으로 확장. 왼쪽 설치: 커서가 오른쪽 열 → 왼쪽으로 확장.
        /// </summary>
        public static Vector3Int ResolveFootprintOrigin(
            Vector3Int cursorCell,
            Vector2Int footprint,
            float playerWorldX,
            float cursorWorldX)
        {
            int width = Mathf.Max(1, footprint.x);
            int originX = cursorWorldX >= playerWorldX
                ? cursorCell.x
                : cursorCell.x - (width - 1);
            return new Vector3Int(originX, cursorCell.y, cursorCell.z);
        }

        /// <summary>현재 선택의 footprint 칸 목록. Preview 점 표시용.</summary>
        public void GetFootprintCells(Vector3Int origin, List<Vector3Int> results)
        {
            results.Clear();
            if (selection == null)
            {
                return;
            }

            foreach (Vector3Int cell in EnumerateFootprint(origin, selection.Footprint))
            {
                results.Add(cell);
            }
        }

        public bool CanPlaceAt(Vector3Int origin, out BuildingPlacementFailure failure)
        {
            if (selection == null) { failure = BuildingPlacementFailure.NoSelection; return false; }
            if (string.IsNullOrWhiteSpace(selection.BuildingId) || selection.RuntimePrefab == null)
            {
                failure = BuildingPlacementFailure.InvalidDefinition;
                return false;
            }

            Vector2Int footprint = selection.Footprint;
            // 거리·허용 구역은 footprint 중심 기준으로 검사해 2x2 좌하단 origin 편향을 줄인다.
            Vector3 worldPosition = FootprintWorldCenter(origin, footprint);
            if (allowedPlacementArea != null && !allowedPlacementArea.OverlapPoint(worldPosition))
            {
                failure = BuildingPlacementFailure.OutsideAllowedArea;
                return false;
            }

            if (placementOrigin != null
                && maximumPlacementDistance > 0f
                && Vector2.Distance(placementOrigin.position, worldPosition) > maximumPlacementDistance)
            {
                failure = BuildingPlacementFailure.OutOfRange;
                return false;
            }

            foreach (Vector3Int cell in EnumerateFootprint(origin, footprint))
            {
                if (occupiedCells.Contains(cell) || (terrainTilemap != null && terrainTilemap.HasTile(cell)))
                {
                    failure = BuildingPlacementFailure.Occupied;
                    return false;
                }

                // 지면은 footprint 하단 행만 검사한다.
                // 높이 2 이상에서 윗칸에 cell+down 타일을 요구하면 빈 공간이 필요한 윗칸이 영원히 MissingGround가 된다.
                bool isBottomRow = cell.y == origin.y;
                if (selection.RequiresGround
                    && isBottomRow
                    && (terrainTilemap == null || !terrainTilemap.HasTile(cell + Vector3Int.down)))
                {
                    failure = BuildingPlacementFailure.MissingGround;
                    return false;
                }
            }

            if (sharedResourceWallet != null)
            {
                if (!sharedResourceWallet.CanAfford(selection.Costs))
                {
                    failure = BuildingPlacementFailure.CannotAfford;
                    return false;
                }
            }
            else if (resourceWallet == null)
            {
                failure = BuildingPlacementFailure.ResourceWalletUnavailable;
                return false;
            }
            else if (!resourceWallet.CanAfford(selection.BuildingId))
            {
                failure = BuildingPlacementFailure.CannotAfford;
                return false;
            }

            failure = BuildingPlacementFailure.None;
            return true;
        }

        public BuildingPlacementResult TryPlaceAt(Vector3Int origin)
        {
            if (!CanPlaceAt(origin, out BuildingPlacementFailure failure)) return Reject(failure, origin);
            BuildingPlacementDefinition definition = selection;
            Vector2Int footprint = definition.Footprint;
            // 2x2 등 다중 칸 시설은 footprint 중심에 생성해 점 표시 공간과 시각 위치를 맞춘다.
            GameObject instanceObject = Instantiate(
                definition.RuntimePrefab,
                FootprintWorldCenter(origin, footprint),
                Quaternion.identity,
                buildingRoot);
            if (instanceObject == null) return Reject(BuildingPlacementFailure.InstantiateFailed, origin);

            // 비용은 생성에 성공한 뒤에만 차감한다. 실패하면 생성물을 되돌린다.
            bool spent = sharedResourceWallet != null
                ? sharedResourceWallet.TrySpend(definition.Costs)
                : resourceWallet.TrySpend(definition.BuildingId);
            if (!spent)
            {
                Destroy(instanceObject);
                return Reject(BuildingPlacementFailure.SpendFailed, origin);
            }

            string instanceId = $"{definition.BuildingId}-{nextInstanceSequence++:D4}";
            BuildingInstance instance = instanceObject.GetComponent<BuildingInstance>() ?? instanceObject.AddComponent<BuildingInstance>();
            instance.Initialize(instanceId, definition.BuildingId);
            BindPowerNode(instanceObject, instanceId);
            foreach (Vector3Int cell in EnumerateFootprint(origin, footprint)) occupiedCells.Add(cell);
            StructuralSupport support = instanceObject.GetComponent<StructuralSupport>();
            if (support != null) structuralIntegritySystem?.RegisterSupport(support);

            // 한 번의 선택은 한 시설만 확정한다. 이벤트 재진입과 같은 프레임 중복 확정을 함께 막는다.
            ClearSelection();
            var result = new BuildingPlacementResult(true, BuildingPlacementFailure.None, instanceId, definition.BuildingId, origin);
            BuildingPlaced?.Invoke(result);
            return result;
        }

        /// <summary>
        /// Enter 확정용: 플레이어 기준 배치 반경 안에서 CanPlaceAt을 통과하는 칸 중
        /// 가장 가까운 칸을 고른다. 거리 동률이면 발밑 → 전방 → 아래/옆 순.
        /// 후보가 없으면 false와 대표 실패 사유를 반환한다.
        /// </summary>
        /// <param name="facingDirection">플레이어 전방 부호(+1 오른쪽, -1 왼쪽).</param>
        public bool TryFindBestPlacementCell(
            float facingDirection,
            out Vector3Int bestCell,
            out BuildingPlacementFailure failure)
        {
            bestCell = default;
            if (selection == null)
            {
                failure = BuildingPlacementFailure.NoSelection;
                return false;
            }

            Vector3 originWorld = placementOrigin != null
                ? placementOrigin.position
                : transform.position;
            Vector3Int playerCell = WorldToCell(originWorld);
            int facingSign = facingDirection > 0.01f ? 1 : facingDirection < -0.01f ? -1 : 1;
            int radius = Mathf.Max(1, Mathf.CeilToInt(maximumPlacementDistance > 0f
                ? maximumPlacementDistance
                : 6f));

            bool hasCandidate = false;
            Vector3Int candidate = default;
            float bestDistance = float.PositiveInfinity;
            int bestPreference = int.MaxValue;
            int bestManhattan = int.MaxValue;

            BuildingPlacementFailure nearestFailure = BuildingPlacementFailure.OutOfRange;
            float nearestFailureDistance = float.PositiveInfinity;
            int nearestFailurePreference = int.MaxValue;

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    var cell = new Vector3Int(playerCell.x + dx, playerCell.y + dy, playerCell.z);
                    float distance = Vector2.Distance(originWorld, CellToWorld(cell));
                    if (maximumPlacementDistance > 0f && distance > maximumPlacementDistance + 0.001f)
                    {
                        continue;
                    }

                    int preference = RankPlacementPreference(cell, playerCell, facingSign);
                    int manhattan = Mathf.Abs(dx) + Mathf.Abs(dy);

                    if (CanPlaceAt(cell, out BuildingPlacementFailure cellFailure))
                    {
                        if (!hasCandidate
                            || distance < bestDistance - 0.0001f
                            || (Mathf.Abs(distance - bestDistance) <= 0.0001f
                                && (preference < bestPreference
                                    || (preference == bestPreference && manhattan < bestManhattan))))
                        {
                            hasCandidate = true;
                            candidate = cell;
                            bestDistance = distance;
                            bestPreference = preference;
                            bestManhattan = manhattan;
                        }
                    }
                    else if (cellFailure != BuildingPlacementFailure.None
                             && (distance < nearestFailureDistance - 0.0001f
                                 || (Mathf.Abs(distance - nearestFailureDistance) <= 0.0001f
                                     && (preference < nearestFailurePreference
                                         || (preference == nearestFailurePreference
                                             && PreferFailure(cellFailure, nearestFailure))))))
                    {
                        nearestFailure = cellFailure;
                        nearestFailureDistance = distance;
                        nearestFailurePreference = preference;
                    }
                }
            }

            if (hasCandidate)
            {
                bestCell = candidate;
                failure = BuildingPlacementFailure.None;
                return true;
            }

            failure = nearestFailure;
            bestCell = playerCell;
            return false;
        }

        /// <summary>
        /// Enter 확정: 최적 칸에 1회 설치한다. 성공 시 비용 1회 차감·선택 해제(좌클릭과 동일).
        /// 후보가 없을 때는 PlacementRejected를 올리지 않고 실패 결과만 반환한다(선택 유지·사유 표시용).
        /// </summary>
        public BuildingPlacementResult TryPlaceNearest(float facingDirection)
        {
            if (!TryFindBestPlacementCell(facingDirection, out Vector3Int cell, out BuildingPlacementFailure failure))
            {
                return new BuildingPlacementResult(
                    false,
                    failure,
                    string.Empty,
                    selection != null ? selection.BuildingId : string.Empty,
                    cell);
            }

            return TryPlaceAt(cell);
        }

        /// <summary>
        /// 거리 동률 시 우선순위. 값이 작을수록 우선.
        /// 0 발밑(동일 칸) → 1 발밑 축(동일 X) → 2 전방 수평 → 3 전방 기타 → 4 아래 → 5 옆 → 6 기타.
        /// </summary>
        public static int RankPlacementPreference(
            Vector3Int cell,
            Vector3Int playerCell,
            int facingSign)
        {
            int dx = cell.x - playerCell.x;
            int dy = cell.y - playerCell.y;
            if (dx == 0 && dy == 0)
            {
                return 0;
            }

            if (dx == 0)
            {
                return 1;
            }

            bool facing = facingSign != 0 && Mathf.Sign(dx) == facingSign;
            if (facing && dy == 0)
            {
                return 2;
            }

            if (facing)
            {
                return 3;
            }

            if (dy < 0)
            {
                return 4;
            }

            if (dx != 0)
            {
                return 5;
            }

            return 6;
        }

        /// <summary>
        /// 후보 부재 시 플레이어에게 더 유용한 실패 사유를 고른다.
        /// 자원 부족 &gt; 지면 없음 &gt; 점유 &gt; 허용 구역 밖 &gt; 거리 초과 순.
        /// </summary>
        private static bool PreferFailure(
            BuildingPlacementFailure candidate,
            BuildingPlacementFailure current)
        {
            return FailurePriority(candidate) < FailurePriority(current);
        }

        private static int FailurePriority(BuildingPlacementFailure failure)
        {
            switch (failure)
            {
                case BuildingPlacementFailure.CannotAfford:
                    return 0;
                case BuildingPlacementFailure.MissingGround:
                    return 1;
                case BuildingPlacementFailure.Occupied:
                    return 2;
                case BuildingPlacementFailure.OutsideAllowedArea:
                    return 3;
                case BuildingPlacementFailure.OutOfRange:
                    return 4;
                case BuildingPlacementFailure.ResourceWalletUnavailable:
                    return 5;
                case BuildingPlacementFailure.InvalidDefinition:
                    return 6;
                default:
                    return 10;
            }
        }

        /// <summary>
        /// 월드 스냅샷 복원 직전 호출. 기존 Runtime 시설을 제거하고 점유/멱등 상태를 초기화한다.
        /// 지갑·선택 상태는 건드리지 않는다.
        /// </summary>
        public void PrepareForWorldRestore()
        {
            Transform root = buildingRoot != null ? buildingRoot : transform;
            for (int index = root.childCount - 1; index >= 0; index--)
            {
                Transform child = root.GetChild(index);
                if (child == null) continue;
                BuildingInstance building = child.GetComponent<BuildingInstance>();
                if (building == null) continue;
                StructuralSupport support = child.GetComponent<StructuralSupport>();
                if (support != null) structuralIntegritySystem?.UnregisterSupport(support);
                DestroyRuntime(child.gameObject);
            }

            occupiedCells.Clear();
            restoredInstanceIds.Clear();
            nextInstanceSequence = 1;
        }

        /// <summary>Restores a previously placed building without querying or spending the App-owned wallet.</summary>
        public bool TryRestoreBuilding(BuildingSnapshotDto snapshot)
        {
            if (string.IsNullOrWhiteSpace(snapshot.instanceId) || restoredInstanceIds.Contains(snapshot.instanceId)) return false;
            BuildingPlacementDefinition definition = FindDefinition(snapshot.buildingTypeId);
            if (definition == null || definition.RuntimePrefab == null) return false;

            var cell = new Vector3Int(snapshot.x, snapshot.y, 0);
            Vector2Int footprint = definition.Footprint;
            GameObject instanceObject = Instantiate(
                definition.RuntimePrefab,
                FootprintWorldCenter(cell, footprint),
                Quaternion.identity,
                buildingRoot != null ? buildingRoot : transform);
            BuildingInstance instance = instanceObject.GetComponent<BuildingInstance>() ?? instanceObject.AddComponent<BuildingInstance>();
            instance.Initialize(snapshot.instanceId, snapshot.buildingTypeId);
            BindPowerNode(instanceObject, snapshot.instanceId);
            foreach (Vector3Int occupied in EnumerateFootprint(cell, footprint)) occupiedCells.Add(occupied);
            StructuralSupport support = instanceObject.GetComponent<StructuralSupport>();
            if (support != null) structuralIntegritySystem?.RegisterSupport(support);
            restoredInstanceIds.Add(snapshot.instanceId);

            // 멱등 복원 시 이후 배치 시퀀스가 저장 인스턴스 ID와 겹치지 않게 시퀀스를 전진시킨다.
            int separator = snapshot.instanceId.LastIndexOf('-');
            if (separator >= 0
                && int.TryParse(snapshot.instanceId.Substring(separator + 1), out int sequence)
                && sequence >= nextInstanceSequence)
            {
                nextInstanceSequence = sequence + 1;
            }

            return true;
        }

        private void BindPowerNode(GameObject instanceObject, string instanceId)
        {
            PowerNode powerNode = instanceObject.GetComponent<PowerNode>();
            if (powerNode == null)
            {
                return;
            }

            powerNode.SetEntityId(instanceId);
            powerNode.SetNetwork(powerNetworkSystem);
        }

        private BuildingPlacementResult Reject(BuildingPlacementFailure failure, Vector3Int cell)
        {
            var result = new BuildingPlacementResult(false, failure, string.Empty, selection != null ? selection.BuildingId : string.Empty, cell);
            PlacementRejected?.Invoke(result);
            return result;
        }

        private BuildingPlacementDefinition FindDefinition(string buildingId)
        {
            if (selection != null && selection.BuildingId == buildingId) return selection;
            foreach (BuildingPlacementDefinition definition in restoreDefinitions)
            {
                if (definition != null && definition.BuildingId == buildingId) return definition;
            }
            return null;
        }

        private Vector3 CellToWorld(Vector3Int cell)
        {
            return terrainTilemap != null ? terrainTilemap.GetCellCenterWorld(cell) : cell;
        }

        /// <summary>발자국 좌하단~우상단 셀 중심의 중간점. 1x1이면 단일 셀 중심과 동일.</summary>
        private Vector3 FootprintWorldCenter(Vector3Int origin, Vector2Int size)
        {
            int width = Mathf.Max(1, size.x);
            int height = Mathf.Max(1, size.y);
            if (width == 1 && height == 1)
            {
                return CellToWorld(origin);
            }

            Vector3 min = CellToWorld(origin);
            Vector3 max = CellToWorld(origin + new Vector3Int(width - 1, height - 1, 0));
            return (min + max) * 0.5f;
        }

        private static IEnumerable<Vector3Int> EnumerateFootprint(Vector3Int origin, Vector2Int size)
        {
            int width = Mathf.Max(1, size.x);
            int height = Mathf.Max(1, size.y);
            for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                yield return origin + new Vector3Int(x, y, 0);
        }

        private static void DestroyRuntime(UnityEngine.Object target)
        {
            if (target == null) return;
            if (Application.isPlaying) Destroy(target);
            else DestroyImmediate(target);
        }
    }
}
