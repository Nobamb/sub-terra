using UnityEngine;

namespace SubTerra.Gameplay.Building
{
    /// <summary>Gameplay placement data. App data can be adapted to this without the placement system depending on App.</summary>
    [CreateAssetMenu(fileName = "BuildingPlacementDefinition", menuName = "SubTerra/Gameplay/Building Placement", order = 10)]
    public sealed class BuildingPlacementDefinition : ScriptableObject
    {
        [SerializeField] private string buildingId = "building.support";
        [SerializeField] private GameObject runtimePrefab;
        [SerializeField] private Vector2Int footprint = Vector2Int.one;
        [SerializeField] private bool requiresGround = true;

        public string BuildingId => buildingId;
        public GameObject RuntimePrefab => runtimePrefab;
        public Vector2Int Footprint => new(Mathf.Max(1, footprint.x), Mathf.Max(1, footprint.y));
        public bool RequiresGround => requiresGround;

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
