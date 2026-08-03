using System;

namespace SubTerra.Shared
{
    /// <summary>
    /// Gameplay에서 App으로 전달하는 확정 이벤트 종류
    /// </summary>
    public enum GameplayEventType
    {
        Unknown = 0,
        TileMined = 1,
        MineralDiscovered = 2,
        GasTriggered = 3,
        StructuralRiskChanged = 4,
        BuildingPlaced = 5,
        OutpostActivated = 6,
        PlayerRescued = 7,
        DepthZoneEntered = 8,
        BuildingPlacementChanged = 9,
        OutpostStatusChanged = 10,
        StructuralCollapse = 11,
        GasExposureThreshold = 12
    }

    /// <summary>
    /// 이벤트 종류에 필요한 영구 ID·좌표·실제 수치만 전달하는 Unity 비의존 DTO
    /// </summary>
    [Serializable]
    public sealed class GameplayEventDto
    {
        public GameplayEventType type;
        public string entityId;
        public string instanceId;
        public string reasonId;
        public int x;
        public int y;
        public int quantity;
        public int depth;
        public float structuralIntegrity;
        public float gasRisk;
        public BuildingPlacementResultDto buildingPlacement;
        public OutpostStatusDto outpostStatus;
        public StructuralCollapseEventDto structuralCollapse;
        public GasExposureFailureInputDto gasExposureFailure;
    }
}
