using UnityEngine;

namespace SubTerra.Gameplay.Building
{
    public sealed class BuildingInstance : MonoBehaviour
    {
        [SerializeField] private string instanceId;
        [SerializeField] private string buildingId;

        public string InstanceId => instanceId;
        public string BuildingId => buildingId;

        public void Initialize(string nextInstanceId, string nextBuildingId)
        {
            instanceId = nextInstanceId;
            buildingId = nextBuildingId;
        }
    }
}
