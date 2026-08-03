using UnityEngine;

namespace SubTerra.Gameplay.Structural
{
    /// <summary>Runtime과 테스트가 공유하는 순수 구조 점수 계산.</summary>
    public static class StructuralRiskEvaluator
    {
        public static float CalculateScore(
            float miningImpact,
            int unsupportedTiles,
            int supportStrength,
            StructuralRiskSettings settings)
        {
            return Mathf.Max(
                0f,
                miningImpact + unsupportedTiles * settings.UnsupportedTileWeight - supportStrength);
        }

        public static StructuralRiskLevel Evaluate(
            float miningImpact,
            int unsupportedTiles,
            int supportStrength,
            StructuralRiskSettings settings)
        {
            float score = CalculateScore(miningImpact, unsupportedTiles, supportStrength, settings);
            if (score >= settings.CollapseImminentThreshold)
                return StructuralRiskLevel.CollapseImminent;
            if (score >= settings.DangerThreshold)
                return StructuralRiskLevel.Danger;
            if (score >= settings.CautionThreshold)
                return StructuralRiskLevel.Caution;
            return StructuralRiskLevel.Stable;
        }
    }
}
