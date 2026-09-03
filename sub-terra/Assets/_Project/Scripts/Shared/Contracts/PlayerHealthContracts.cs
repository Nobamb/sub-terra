using System;

namespace SubTerra.Shared
{
    /// <summary>Gameplay 생존 상태를 HUD에 노출하는 읽기 전용 값.</summary>
    public readonly struct PlayerHealthReadModel
    {
        public float Current { get; }
        public int Maximum { get; }

        public PlayerHealthReadModel(float current, int maximum)
        {
            Maximum = Math.Max(1, maximum);
            Current = Math.Max(0f, Math.Min(current, Maximum));
        }
    }

    /// <summary>App HUD가 Gameplay 구현을 직접 참조하지 않고 체력을 구독하는 경계.</summary>
    public interface IPlayerHealthSource
    {
        event Action<PlayerHealthReadModel> HealthChanged;
        PlayerHealthReadModel GetHealth();
    }

    /// <summary>App 시설이 Gameplay 구현을 참조하지 않고 플레이어 체력을 회복하는 명령 경계.</summary>
    public interface IPlayerHealthCommand
    {
        /// <returns>체력이 실제로 증가했으면 true, 이미 최대 체력이면 false.</returns>
        bool RestoreFull();
    }

    /// <summary>붕괴 낙석이 Player 구현을 직접 알지 않고 접촉 피해를 전달하는 경계.</summary>
    public interface ICollapseDamageReceiver
    {
        bool IsCollapseContact(float fromX, float fromY, float toX, float toY);
        bool ApplyCollapseImpact();
    }

    /// <summary>체력 관련 진행도 효과만 Gameplay 생존 상태에 제공하는 경계.</summary>
    public interface IPlayerHealthUpgradeProvider
    {
        int GetMaximumHealth(int baseMaximum);
        float GetHealthRegenerationPerSecond();
    }
}
