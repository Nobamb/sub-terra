namespace SubTerra.Gameplay.Structural
{
    /// <summary>Gameplay 구조 위험 단계. App/HUD는 확정된 단계를 표시만 한다.</summary>
    public enum StructuralRiskLevel
    {
        Stable = 0,
        Caution = 1,
        Danger = 2,
        CollapseImminent = 3
    }
}
