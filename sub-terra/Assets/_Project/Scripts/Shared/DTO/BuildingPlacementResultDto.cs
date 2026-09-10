using System;

namespace SubTerra.Shared
{
    /// <summary>
    /// A의 건설 Preview·유효성·설치 결과를 B의 UI로 전달하는 상태
    /// </summary>
    public enum BuildingPlacementState
    {
        None = 0,
        Previewing = 1,
        Valid = 2,
        Invalid = 3,
        Placed = 4,
        Failed = 5,
        Cancelled = 6
    }

    /// <summary>
    /// 건설 판정을 다시 계산하지 않고 UI에 표시하기 위한 Unity 비의존 DTO
    /// </summary>
    [Serializable]
    public sealed class BuildingPlacementResultDto
    {
        public BuildingPlacementState state;
        public string buildingId;
        public string instanceId;
        public string reasonId;
        public int x;
        public int y;
        public bool reducedStructuralRisk;
    }
}
