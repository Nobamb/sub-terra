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

    /// <summary>이번 구조 재평가에서 위험을 실제로 키운 주원인.</summary>
    public enum StructuralRiskCause
    {
        None = 0,
        Unsupported = 1,
        MiningImpact = 2,
        SupportRemoved = 3
    }

    /// <summary>UI와 월드 표시가 재계산 없이 사용하는 천장 구조 상태.</summary>
    public readonly struct StructuralRiskStatus
    {
        public UnityEngine.Vector3Int Cell { get; }
        public float Score { get; }
        public StructuralRiskLevel Level { get; }
        public StructuralRiskCause Cause { get; }
        public bool IsTelegraphing { get; }

        public StructuralRiskStatus(
            UnityEngine.Vector3Int cell,
            float score,
            StructuralRiskLevel level,
            StructuralRiskCause cause,
            bool isTelegraphing)
        {
            Cell = cell;
            Score = score;
            Level = level;
            Cause = cause;
            IsTelegraphing = isTelegraphing;
        }

        public static StructuralRiskStatus Stable(UnityEngine.Vector3Int cell)
        {
            return new StructuralRiskStatus(
                cell,
                0f,
                StructuralRiskLevel.Stable,
                StructuralRiskCause.None,
                false);
        }
    }
}
