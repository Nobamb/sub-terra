using System;
using System.Collections.Generic;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.Gameplay.Building
{
    /// <summary>Gameplay placement data. App data can be adapted to this without the placement system depending on App.</summary>
    [CreateAssetMenu(fileName = "BuildingPlacementDefinition", menuName = "SubTerra/Gameplay/Building Placement", order = 10)]
    public sealed class BuildingPlacementDefinition : ScriptableObject
    {
        [Serializable]
        private struct CostEntry
        {
            public string itemId;
            public int quantity;
        }

        [SerializeField] private string buildingId = "building.support";
        [SerializeField] private GameObject runtimePrefab;
        [SerializeField] private Vector2Int footprint = Vector2Int.one;
        [SerializeField] private bool requiresGround = true;
        [SerializeField] private List<CostEntry> costs = new();

        public string BuildingId => buildingId;
        public GameObject RuntimePrefab => runtimePrefab;
        public Vector2Int Footprint => new(Mathf.Max(1, footprint.x), Mathf.Max(1, footprint.y));
        public bool RequiresGround => requiresGround;
        public IReadOnlyList<ItemCostDto> Costs
        {
            get
            {
                var result = new List<ItemCostDto>(costs.Count);
                foreach (CostEntry cost in costs)
                {
                    if (!string.IsNullOrWhiteSpace(cost.itemId) && cost.quantity > 0)
                        result.Add(new ItemCostDto(cost.itemId, cost.quantity));
                }
                return result;
            }
        }

#if UNITY_EDITOR
        public void EditorSet(string id, GameObject prefab, Vector2Int size, bool needsGround)
        {
            buildingId = id;
            runtimePrefab = prefab;
            footprint = size;
            requiresGround = needsGround;
        }
#endif
    }
}
