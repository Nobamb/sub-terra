using UnityEngine;

namespace SubTerra.Gameplay.Building
{
    public sealed class BuildingInstance : MonoBehaviour
    {
        [SerializeField] private string instanceId;
        [SerializeField] private string buildingId;

        public string InstanceId => instanceId;
        public string BuildingId => buildingId;

        private void Awake()
        {
            FacilityGroundedVisual.Apply(transform, buildingId,
                buildingId == "building.charger.basic" ? new Vector2Int(2, 2) : default);
            AttachIdleAnimation();
        }

        public void Initialize(string nextInstanceId, string nextBuildingId, Vector2Int footprint = default)
        {
            instanceId = nextInstanceId;
            buildingId = nextBuildingId;
            FacilityGroundedVisual.Apply(transform, buildingId, footprint);
            AttachIdleAnimation();
        }

        // B-214: 주기 연출·포탈 홀 회전은 프리팹이 아니라 시설이 생길 때(설치·복원·이동) 런타임에서 붙인다.
        private void AttachIdleAnimation()
        {
            try
            {
                FacilityIdleAnimation.EnsureFor(gameObject, buildingId);
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }
    }
}
