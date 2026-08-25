using UnityEngine;

namespace SubTerra.Gameplay.Player
{
    /// <summary>적재율을 점프력과 낙하 충격 배율로 변환한다.</summary>
    public static class CargoLoadEffectPolicy
    {
        public const float FullLoadJumpMultiplier = 0.75f;
        public const float FullLoadFallImpactMultiplier = 1.5f;

        public static float EvaluateLoadRatio(float currentWeight, float maximumWeight)
        {
            if (maximumWeight <= 0f)
            {
                return currentWeight > 0f ? 1f : 0f;
            }

            return Mathf.Clamp01(Mathf.Max(0f, currentWeight) / maximumWeight);
        }

        public static float EvaluateJumpMultiplier(float currentWeight, float maximumWeight)
        {
            return Mathf.Lerp(
                1f,
                FullLoadJumpMultiplier,
                EvaluateLoadRatio(currentWeight, maximumWeight));
        }

        public static float EvaluateFallImpactMultiplier(float currentWeight, float maximumWeight)
        {
            return Mathf.Lerp(
                1f,
                FullLoadFallImpactMultiplier,
                EvaluateLoadRatio(currentWeight, maximumWeight));
        }
    }

    /// <summary>PRD의 0~50/50~80/80~100% 화물 구간을 이동 배율로 변환한다.</summary>
    public static class CargoSpeedPolicy
    {
        public const float LightLoadMultiplier = 1f;
        public const float MediumLoadMultiplier = 0.85f;
        public const float HeavyLoadMultiplier = 0.65f;

        public static float Evaluate(float currentWeight, float maximumWeight)
        {
            if (maximumWeight <= 0f)
            {
                return currentWeight > 0f ? HeavyLoadMultiplier : LightLoadMultiplier;
            }

            var ratio = CargoLoadEffectPolicy.EvaluateLoadRatio(currentWeight, maximumWeight);
            if (ratio >= 0.8f)
            {
                return HeavyLoadMultiplier;
            }

            return ratio >= 0.5f ? MediumLoadMultiplier : LightLoadMultiplier;
        }
    }
}
