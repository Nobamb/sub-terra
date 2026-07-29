namespace SubTerra.App.AI
{
    /// <summary>클라우드 표현을 요청할 수 있는 명시적 게임 이벤트만 열어 둔다.</summary>
    public enum CloudDialogueEvent
    {
        Unknown = 0,
        NewDepthZone = 1,
        GasDetected = 2,
        CollapseImminent = 3,
        ValuableMineralDetected = 4,
        PowerShortage = 5,
        OutpostInstalled = 6,
        ManualAnalysis = 7
    }
}
