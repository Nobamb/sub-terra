using UnityEngine;

namespace SubTerra.Gameplay.Structural
{
    /// <summary>Pure, deterministic score calculation shared by runtime and tests.</summary>
    public static class StructuralRiskEvaluator
    {
        public static float CalculateScore(float miningImpact, int unsupportedTiles, int supportStrength)
        {
            return Mathf.Max(0, miningImpact + unsupportedTiles * 20 - supportStrength);
        }

        public static StructuralRiskLevel Evaluate(float miningImpact, int unsupportedTiles, int supportStrength)
        {
            float score = CalculateScore(miningImpact, unsupportedTiles, supportStrength);
            if (score >= 60) return StructuralRiskLevel.Critical;
            if (score >= 30) return StructuralRiskLevel.Caution;
            return StructuralRiskLevel.Stable;
        }
    }
}
