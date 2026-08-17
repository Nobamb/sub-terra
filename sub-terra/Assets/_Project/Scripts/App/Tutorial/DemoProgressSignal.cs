namespace SubTerra.App.Tutorial
{
    /// <summary>
    /// 목표 완료에 사용하는 진행 신호.
    /// 채굴·구조·가스·설치 판정 수식이 아니라 기존 이벤트·Service 성공 결과에서만 만든다.
    /// </summary>
    public enum DemoProgressSignal
    {
        None = 0,
        ExplorationStarted = 1,
        CopperAndIronCollected = 2,
        PathGuidanceAcknowledged = 3,
        LithiumCollected = 4,
        StructuralHazardObserved = 5,
        SupportPlaced = 6,
        GasHazardResolved = 7,
        OutpostInstalled = 8,
        ReturnRecommendationPresented = 9,
        SettlementSucceeded = 10,
        BatteryUpgradeSucceeded = 11,
        DeepZoneUnlocked = 12,
        DemoCompleted = 13
    }
}
