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

        public int MaximumHealth { get; }
        public int Health { get; private set; }
        public bool CanAct { get; private set; }
        public RunFailureCause LastDamageCause { get; private set; }

        public PlayerSurvivalState(int maximumHealth)
        {
            MaximumHealth = Math.Max(1, maximumHealth);
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

            Health = Math.Max(0, Health - appliedDamage);
            LastDamageCause = cause;
            invulnerableUntil = currentTime + Math.Max(0f, invulnerabilitySeconds);
            if (Health == 0)
            {
                CanAct = false;
                becameIncapacitated = true;
            }

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
}
