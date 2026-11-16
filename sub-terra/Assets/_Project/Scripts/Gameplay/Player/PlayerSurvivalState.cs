using System;
using SubTerra.Shared;

namespace SubTerra.Gameplay.Player
{
    public enum OxygenRiskLevel
    {
        Safe,
        Caution,
        Critical
    }

    public sealed class PlayerOxygenState
    {
        private const float MaximumRecoveryDepthMeters = 10f;
        private double timeWithoutOxygen;
        public float Maximum { get; }
        public double Remaining { get; private set; }

        public PlayerOxygenState(float maximum)
        {
            Maximum = Math.Max(1f, maximum);
            Refill();
        }

        public void Refill()
        {
            Remaining = Maximum;
            timeWithoutOxygen = 0d;
        }

#if UNITY_EDITOR || SUBTERRA_BUILD_DEVELOPMENT
        internal void SetDebugRemainingPercent(int percent)
        {
            Remaining = Maximum * (Math.Clamp(percent, 0, 100) / 100d);
            timeWithoutOxygen = 0d;
        }
#endif

        public OxygenRiskLevel GetRisk(float cautionRatio, float criticalRatio)
        {
            double ratio = Remaining / Maximum;
            if (ratio <= criticalRatio) return OxygenRiskLevel.Critical;
            return ratio <= cautionRatio ? OxygenRiskLevel.Caution : OxygenRiskLevel.Safe;
        }

        public static float DrainMultiplier(bool climbing, bool moving, float loadRatio, float movementMultiplier,
            float ladderMultiplier, float fullCargoMultiplier, float ladderCargoCap)
        {
            float cargo = 1f + (fullCargoMultiplier - 1f) * Math.Clamp(loadRatio, 0f, 1f);
            if (climbing) return ladderMultiplier * Math.Min(cargo, ladderCargoCap);
            return (moving ? movementMultiplier : 1f) * cargo;
        }

        public int Advance(float deltaTime, float depth, float baseRate, float perMeter, float damageInterval,
            float shallowRecoveryPerSecond, float drainMultiplier = 1f)
        {
            if (deltaTime <= 0f) return 0;

            if (depth <= MaximumRecoveryDepthMeters)
            {
                Remaining = Math.Min(Maximum, Remaining + shallowRecoveryPerSecond * (double)deltaTime);
                timeWithoutOxygen = 0d;
                return 0;
            }

            double depletedTime = deltaTime;
            if (Remaining > 0d)
            {
                // 깊이별 소모량에는 기존과 같이 반올림한 깊이를 사용한다.
                double drainRate = (baseRate + perMeter * Math.Round(depth)) * drainMultiplier;
                if (drainRate <= 0d) return 0;
                depletedTime -= Remaining / drainRate;
                Remaining = Math.Max(0d, Remaining - drainRate * deltaTime);
                if (Remaining > 0d) return 0;
            }

            timeWithoutOxygen += Math.Max(0d, depletedTime);
            double interval = Math.Max(0.1f, damageInterval);
            int ticks = (int)(timeWithoutOxygen / interval);
            timeWithoutOxygen -= ticks * interval;
            return ticks;
        }
    }

    /// <summary>
    /// Unity 생명주기와 분리된 Player 생존 상태. 피해 시간은 호출자가 전달해 테스트를 결정론적으로 만든다.
    /// </summary>
    public sealed class PlayerSurvivalState
    {
        private float invulnerableUntil;
        private float regenerationPerSecond;

        public int MaximumHealth { get; private set; }
        public float Health { get; private set; }
        public bool CanAct { get; private set; }
        public RunFailureCause LastDamageCause { get; private set; }

        public PlayerSurvivalState(int maximumHealth, float healthRegenerationPerSecond = 0f)
        {
            MaximumHealth = Math.Max(1, maximumHealth);
            regenerationPerSecond = Math.Max(0f, healthRegenerationPerSecond);
            RestoreFull();
        }

        public bool TryApplyDamage(
            RunFailureCause cause,
            float damage,
            float currentTime,
            float invulnerabilitySeconds,
            bool forceIncapacitate,
            out bool becameIncapacitated)
        {
            becameIncapacitated = false;
            if (!CanAct || cause == RunFailureCause.Unknown)
            {
                return false;
            }

            if (!forceIncapacitate && currentTime < invulnerableUntil)
            {
                return false;
            }

            var appliedDamage = forceIncapacitate ? Health : Math.Max(0f, damage);
            if (appliedDamage <= 0)
            {
                return false;
            }

            Health = Math.Max(0f, Health - appliedDamage);
            LastDamageCause = cause;
            invulnerableUntil = currentTime + Math.Max(0f, invulnerabilitySeconds);
            if (Health <= 0f)
            {
                CanAct = false;
                becameIncapacitated = true;
            }

            return true;
        }

        public bool AdvanceRegeneration(float deltaTime)
        {
            if (!CanAct
                || regenerationPerSecond <= 0f
                || deltaTime <= 0f
                || Health >= MaximumHealth)
            {
                return false;
            }

            var previous = Health;
            Health = Math.Min(MaximumHealth, Health + regenerationPerSecond * deltaTime);
            return Math.Abs(Health - previous) > 0.0001f;
        }

        public bool IsInvulnerable(float currentTime)
        {
            return CanAct && currentTime < invulnerableUntil;
        }

        public bool ApplyUpgradeEffects(int maximumHealth, float healthRegenerationPerSecond)
        {
            var nextMaximum = Math.Max(1, maximumHealth);
            var nextRegeneration = Math.Max(0f, healthRegenerationPerSecond);
            if (MaximumHealth == nextMaximum
                && Math.Abs(regenerationPerSecond - nextRegeneration) < 0.0001f)
            {
                return false;
            }

            var maximumIncrease = nextMaximum - MaximumHealth;
            MaximumHealth = nextMaximum;
            regenerationPerSecond = nextRegeneration;
            Health = maximumIncrease > 0
                ? Math.Min(MaximumHealth, Health + maximumIncrease)
                : Math.Min(Health, MaximumHealth);
            return true;
        }

        public bool RestoreFull()
        {
            var changed = Health < MaximumHealth || !CanAct;
            Health = MaximumHealth;
            CanAct = true;
            LastDamageCause = RunFailureCause.Unknown;
            invulnerableUntil = 0f;
            return changed;
        }

#if UNITY_EDITOR || SUBTERRA_BUILD_DEVELOPMENT
        /// <summary>피해·실패 경로를 타지 않고 체력과 행동 가능 여부만 맞춘다.</summary>
        internal void SetHealthAbsolute(int health)
        {
            var clamped = health < 0 ? 0 : health;
            if (clamped > MaximumHealth)
            {
                clamped = MaximumHealth;
            }

            Health = clamped;
            CanAct = clamped > 0;
        }
#endif
    }

    public static class PlayerFallDamageRules
    {
        public static int CalculateDamage(
            float fallDistance,
            bool usedLadder,
            float minimumDamageHeight = 10f,
            int damageAtThreshold = 10,
            float damagePerAdditionalMeter = 1f)
        {
            var threshold = Math.Max(0f, minimumDamageHeight);
            var baseDamage = Math.Max(0, damageAtThreshold);
            var perMeter = Math.Max(0f, damagePerAdditionalMeter);
            if (usedLadder || float.IsNaN(fallDistance) || fallDistance < threshold)
            {
                return 0;
            }

            if (float.IsPositiveInfinity(fallDistance))
            {
                return int.MaxValue;
            }

            var additional = Math.Floor((fallDistance - threshold) * perMeter);
            return additional >= int.MaxValue - baseDamage
                ? int.MaxValue
                : baseDamage + (int)additional;
        }

        public static int ScaleDamage(int damage, float impactMultiplier, bool stableLanding = false)
        {
            if (stableLanding) return Math.Max(0, damage);
            if (damage <= 0 || impactMultiplier <= 0f || float.IsNaN(impactMultiplier))
            {
                return 0;
            }

            if (float.IsPositiveInfinity(impactMultiplier))
            {
                return int.MaxValue;
            }

            var scaled = Math.Round(
                damage * (double)impactMultiplier,
                MidpointRounding.AwayFromZero);
            return scaled >= int.MaxValue ? int.MaxValue : (int)scaled;
        }
    }
}
