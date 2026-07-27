using System;
using System.Collections.Generic;
using SubTerra.Gameplay.Structural;
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

        private readonly HashSet<Vector3Int> occupiedCells = new();
        private IBuildingResourceWallet resourceWallet;
        private BuildingPlacementDefinition selection;
        private int nextInstanceSequence = 1;

        public BuildingPlacementDefinition Selection => selection;
        public event Action<BuildingPlacementResult> BuildingPlaced;
        public event Action<BuildingPlacementResult> PlacementRejected;

        private void Awake()
        {
            resourceWallet = resourceWalletBehaviour as IBuildingResourceWallet;
            if (buildingRoot == null) buildingRoot = transform;
        }

        public void Select(BuildingPlacementDefinition definition) => selection = definition;
        public void ClearSelection() => selection = null;

        public Vector3Int WorldToCell(Vector3 worldPosition)
        {
            return terrainTilemap != null ? terrainTilemap.WorldToCell(worldPosition) : Vector3Int.RoundToInt(worldPosition);
        }

        public bool CanPlaceAt(Vector3Int origin, out BuildingPlacementFailure failure)
        {
            if (selection == null) { failure = BuildingPlacementFailure.NoSelection; return false; }
            if (string.IsNullOrWhiteSpace(selection.BuildingId) || selection.RuntimePrefab == null)
            {
                failure = BuildingPlacementFailure.InvalidDefinition;
                return false;
            }

            foreach (Vector3Int cell in EnumerateFootprint(origin, selection.Footprint))
            {
                if (occupiedCells.Contains(cell) || (terrainTilemap != null && terrainTilemap.HasTile(cell)))
                {
                    failure = BuildingPlacementFailure.Occupied;
                    return false;
                }
                if (selection.RequiresGround && (terrainTilemap == null || !terrainTilemap.HasTile(cell + Vector3Int.down)))
                {
                    failure = BuildingPlacementFailure.MissingGround;
                    return false;
                }
            }

            if (resourceWallet == null)
            {
                failure = BuildingPlacementFailure.ResourceWalletUnavailable;
                return false;
            }
            if (!resourceWallet.CanAfford(selection.BuildingId))
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
            GameObject instanceObject = Instantiate(selection.RuntimePrefab, CellToWorld(origin), Quaternion.identity, buildingRoot);
            if (instanceObject == null) return Reject(BuildingPlacementFailure.InstantiateFailed, origin);

            // 비용은 생성에 성공한 뒤에만 차감한다. 실패하면 생성물을 되돌린다.
            if (!resourceWallet.TrySpend(selection.BuildingId))
            {
                Destroy(instanceObject);
                return Reject(BuildingPlacementFailure.SpendFailed, origin);
            }

            string instanceId = $"{selection.BuildingId}-{nextInstanceSequence++:D4}";
            BuildingInstance instance = instanceObject.GetComponent<BuildingInstance>() ?? instanceObject.AddComponent<BuildingInstance>();
            instance.Initialize(instanceId, selection.BuildingId);
            foreach (Vector3Int cell in EnumerateFootprint(origin, selection.Footprint)) occupiedCells.Add(cell);
            StructuralSupport support = instanceObject.GetComponent<StructuralSupport>();
            if (support != null) structuralIntegritySystem?.RegisterSupport(support);

            var result = new BuildingPlacementResult(true, BuildingPlacementFailure.None, instanceId, selection.BuildingId, origin);
            BuildingPlaced?.Invoke(result);
            return result;
        }

        private BuildingPlacementResult Reject(BuildingPlacementFailure failure, Vector3Int cell)
        {
            var result = new BuildingPlacementResult(false, failure, string.Empty, selection != null ? selection.BuildingId : string.Empty, cell);
            PlacementRejected?.Invoke(result);
            return result;
        }

        private Vector3 CellToWorld(Vector3Int cell)
        {
            return terrainTilemap != null ? terrainTilemap.GetCellCenterWorld(cell) : cell;
        }

        private static IEnumerable<Vector3Int> EnumerateFootprint(Vector3Int origin, Vector2Int size)
        {
            for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
                yield return origin + new Vector3Int(x, y, 0);
        }
    }
}
