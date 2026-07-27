using System;
using SubTerra.App.Core.Data;
using SubTerra.Shared;

namespace SubTerra.App.Progression
{
    /// <summary>
    /// 현재 레벨의 단계 값을 최종 보너스로 읽는다.
    /// 레벨 0 또는 손상된 범위는 기본값을 반환해 잘못된 세이브가 효과를 만들지 않게 한다.
    /// </summary>
    public sealed class UpgradeEffectProvider : IUpgradeEffectProvider
    {
        private readonly UpgradeState state;
        private readonly IUpgradeCatalog catalog;

        public UpgradeEffectProvider(UpgradeState state, IUpgradeCatalog catalog)
        {
            this.state = state;
            this.catalog = catalog;
        }

        public float GetDrillSpeedMultiplier()
        {
            return 1f + GetCurrentEffect(DataIds.Upgrades.DrillSpeed);
        }

        public float GetEnergyEfficiencyMultiplier()
        {
            return 1f + GetCurrentEffect(DataIds.Upgrades.DrillEfficiency);
        }

        public int GetMaximumEnergy(int baseMaximum)
        {
            var safeBase = baseMaximum < 0 ? 0 : baseMaximum;
            var value = (double)safeBase + GetCurrentEffect(DataIds.Upgrades.MaximumEnergy);
            return value >= int.MaxValue
                ? int.MaxValue
                : (int)Math.Round(value, MidpointRounding.AwayFromZero);
        }

        public float GetMaximumCargoWeight(float baseMaximum)
        {
            return AddNonNegative(baseMaximum, GetCurrentEffect(DataIds.Upgrades.MaximumCargo));
        }

        public float GetDroneScanRadius(float baseRadius)
        {
            return AddNonNegative(baseRadius, GetCurrentEffect(DataIds.Upgrades.DroneScan));
        }

        public float GetDroneRescuePreservation(float basePreservation)
        {
            var result = AddNonNegative(basePreservation, GetCurrentEffect(DataIds.Upgrades.DroneRescue));
            return result > 1f ? 1f : result;
        }

        public float GetGasResistance()
        {
            var result = GetCurrentEffect(DataIds.Upgrades.GasResistance);
            return result > 1f ? 1f : result;
        }

        public float GetCurrentEffect(string upgradeId)
        {
            if (state == null || catalog == null || string.IsNullOrEmpty(upgradeId))
            {
                return 0f;
            }

            var currentLevel = state.GetLevel(upgradeId);
            if (currentLevel <= 0
                || !catalog.TryGetUpgrade(upgradeId, out var data)
                || data == null
                || data.Levels == null
                || currentLevel > data.MaxLevel
                || currentLevel > data.Levels.Count)
            {
                return 0f;
            }

            var level = data.Levels[currentLevel - 1];
            if (level == null
                || level.Level != currentLevel
                || level.EffectValue <= 0f
                || float.IsNaN(level.EffectValue)
                || float.IsInfinity(level.EffectValue))
            {
                return 0f;
            }

            return level.EffectValue;
        }

        private static float AddNonNegative(float baseValue, float bonus)
        {
            var safeBase = baseValue < 0f ? 0f : baseValue;
            var result = safeBase + bonus;
            return float.IsInfinity(result) ? float.MaxValue : result;
        }
    }
}
