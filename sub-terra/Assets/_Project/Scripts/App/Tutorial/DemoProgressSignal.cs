namespace SubTerra.App.Tutorial
{
    /// <summary>각 퀘스트의 실제 성공 결과에서만 발행하는 진행 신호.</summary>
    public enum DemoProgressSignal
    {
        None = 0,
        BlockMined = 1,
        CopperMined = 2,
        DrillSpeedUpgraded = 3,
        SurfaceReachedByElevator = 4,
        MineReachedByElevator = 5,
        IronMined = 6,
        SupportPlacedInDanger = 7,
        LadderPlaced = 8,
        LightPlacedAtDepth = 9,
        MineralStored = 10,
        OutpostCoreInstalled = 11,
        ChargedNearOutpost = 12,
        DeepZoneUnlocked = 13,
        LithiumMined = 14,
        GasPurifiedByOutpost = 15,
        MineralSoldAtSettlement = 16,
        EmergencyEscapeSucceeded = 17,
        HealedNearOutpost = 18
    }
}
