using UnityEngine;

namespace SubTerra.Gameplay.Hazards
{
    public static class GasRiskEvaluator
    {
        public static GasRiskLevel Evaluate(float intensity)
        {
            if (intensity >= 0.7f) return GasRiskLevel.Critical;
            if (intensity > 0f) return GasRiskLevel.Caution;
            return GasRiskLevel.Safe;
        }

        public static float ClampIntensity(float intensity) => Mathf.Clamp01(intensity);
    }
}
