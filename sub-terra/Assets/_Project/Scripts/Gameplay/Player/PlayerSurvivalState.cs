using System;
using SubTerra.Shared;

namespace SubTerra.Gameplay.Player
{
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
            int damage,
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

            var appliedDamage = forceIncapacitate ? Health : Math.Max(0, damage);
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

        public void RestoreFull()
        {
            Health = MaximumHealth;
            CanAct = true;
            LastDamageCause = RunFailureCause.Unknown;
            invulnerableUntil = 0f;
        }
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

        public static int ScaleDamage(int damage, float impactMultiplier)
        {
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
