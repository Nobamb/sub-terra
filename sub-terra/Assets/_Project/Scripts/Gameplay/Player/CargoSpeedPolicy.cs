using UnityEngine;

namespace SubTerra.Gameplay.Player
{
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

            var ratio = Mathf.Clamp01(Mathf.Max(0f, currentWeight) / maximumWeight);
            if (ratio >= 0.8f)
            {
                return HeavyLoadMultiplier;
            }

            return ratio >= 0.5f ? MediumLoadMultiplier : LightLoadMultiplier;
        }
    }
}
