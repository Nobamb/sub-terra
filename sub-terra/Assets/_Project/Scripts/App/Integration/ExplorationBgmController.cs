using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.App.Integration
{
    /// <summary>플레이어의 현재 체력과 현재 위치의 위험만 탐사 BGM에 반영한다.</summary>
    public sealed class ExplorationBgmController : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour healthSource;
        [SerializeField] private MonoBehaviour contextProvider;
        [SerializeField] private AudioSource explorationBgmSource;
        [SerializeField] private AudioSource dangerBgmSource;
        [SerializeField, Range(0f, 1f)] private float lowHealthRatio = 0.3f;

        private float nextRefreshTime;

        private void OnEnable()
        {
            nextRefreshTime = 0f;
        }

        private void Update()
        {
            if (Time.unscaledTime < nextRefreshTime) return;
            nextRefreshTime = Time.unscaledTime + 0.1f;

            var health = healthSource != null && healthSource is IPlayerHealthSource source
                ? source.GetHealth() : new PlayerHealthReadModel(1f, 1);
            var context = contextProvider != null && contextProvider is IDroneContextProvider provider
                ? provider.CreateContext() : null;
            bool danger = ShouldPlayDanger(health, context, lowHealthRatio);
            AudioSource active = danger ? dangerBgmSource : explorationBgmSource;
            AudioSource inactive = danger ? explorationBgmSource : dangerBgmSource;
            if (inactive != null && inactive.isPlaying) inactive.Pause();
            if (active != null && !active.isPlaying)
            {
                active.UnPause();
                if (!active.isPlaying) active.Play();
            }
        }

        public static bool ShouldPlayDanger(
            PlayerHealthReadModel health, DroneContextDto context, float lowHealthRatio)
        {
            // Shared Context의 0.3 이하는 현재 위치의 Danger/CollapseImminent 단계다.
            return health.Current <= health.Maximum * lowHealthRatio
                || (context != null && (context.structuralIntegrity <= 0.3f || context.gasRisk > 0f));
        }
    }
}
