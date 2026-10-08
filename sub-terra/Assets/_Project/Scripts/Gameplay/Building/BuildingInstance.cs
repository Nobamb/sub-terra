using UnityEngine;

namespace SubTerra.Gameplay.Building
{
    public sealed class BuildingInstance : MonoBehaviour
    {
        [SerializeField] private string instanceId;
        [SerializeField] private string buildingId;

        public string InstanceId => instanceId;
        public string BuildingId => buildingId;

        private void Awake() => FacilityGroundedVisual.Apply(transform, buildingId,
            buildingId == "building.charger.basic" ? new Vector2Int(2, 2) : default);

        public void Initialize(string nextInstanceId, string nextBuildingId, Vector2Int footprint = default)
        {
            instanceId = nextInstanceId;
            buildingId = nextBuildingId;
            FacilityGroundedVisual.Apply(transform, buildingId, footprint);
        }
    }
}
