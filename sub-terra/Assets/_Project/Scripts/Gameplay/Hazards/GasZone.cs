using UnityEngine;

namespace SubTerra.Gameplay.Hazards
{
    /// <summary>
    /// 원형·시간제 가스 구역. 노출 효과는 구독자가 담당한다.
    /// 생성 연출 1초가 끝나기 전에는 접근 판정을 열지 않는다.
    /// </summary>
    public sealed class GasZone : MonoBehaviour
    {
        [SerializeField] private string gasZoneId;
        [SerializeField] private GasType gasType = GasType.Toxic;
        [SerializeField, Range(0f, 1f)] private float intensity = 0.8f;
        [SerializeField, Min(0.1f)] private float radius = GasVisualRules.GasRadiusBlocks;
        [SerializeField, Min(0.1f)] private float remainingDuration = 12f;

        private float spawnElapsed;
        private float fadeOutElapsed;

        public string GasZoneId => gasZoneId;
        public GasType GasType => gasType;
        public float Intensity => intensity;
        public float Radius => radius;
        public float RemainingDuration => remainingDuration;
        public bool IsActive { get; private set; }
        public float SpawnProgress { get; private set; }
        public bool IsSpawnComplete => SpawnProgress >= 1f - 0.0001f;

        /// <summary>지속시간이 끝난 뒤 그림만 남아 흐려지는 중이다. 이때 게임플레이는 이미 끝난 상태(IsActive=false)다.</summary>
        public bool IsFadingOut { get; private set; }

        /// <summary>시각용 진행도. 등장 중에는 0→1, 유지 중에는 1, 페이드아웃 중에는 1→0이다.</summary>
        public float VisualProgress
        {
            get
            {
                if (IsActive)
                {
                    return SpawnProgress;
                }

                if (!IsFadingOut)
                {
                    return 0f;
                }

                var duration = Mathf.Max(0.0001f, GasVisualRules.SpawnDurationSeconds);
                return 1f - Mathf.Clamp01(fadeOutElapsed / duration);
            }
        }

        public void Activate(string id, GasType type, float zoneIntensity, float zoneRadius, float duration)
        {
            Activate(id, type, zoneIntensity, zoneRadius, duration, true);
        }

        public void Activate(
            string id,
            GasType type,
            float zoneIntensity,
            float zoneRadius,
            float duration,
            bool playSpawnAnimation)
        {
            gasZoneId = id;
            gasType = type;
            intensity = GasRiskEvaluator.ClampIntensity(zoneIntensity);
            radius = Mathf.Max(0.1f, zoneRadius);
            remainingDuration = Mathf.Max(0.1f, duration);
            if (playSpawnAnimation)
            {
                spawnElapsed = 0f;
                SpawnProgress = 0f;
            }
            else
            {
                spawnElapsed = GasVisualRules.SpawnDurationSeconds;
                SpawnProgress = 1f;
            }

            IsActive = true;
            IsFadingOut = false;
            fadeOutElapsed = 0f;
            SyncTriggerRadius();
            gameObject.SetActive(true);
        }

        public bool Contains(Vector2 worldPosition)
        {
            // 생성 연출이 끝나기 전에는 접근 탁화·피해를 열지 않는다.
            return IsActive
                && IsSpawnComplete
                && Vector2.Distance(transform.position, worldPosition) <= radius;
        }

        public void Tick(float deltaTime)
        {
            var step = Mathf.Max(0f, deltaTime);
            if (!IsActive)
            {
                TickFadeOut(step);
                return;
            }

            if (!IsSpawnComplete)
            {
                spawnElapsed += step;
                var duration = Mathf.Max(0.0001f, GasVisualRules.SpawnDurationSeconds);
                SpawnProgress = Mathf.Clamp01(spawnElapsed / duration);
                return;
            }

            remainingDuration -= step;
            if (remainingDuration > 0f)
            {
                return;
            }

            // 지속시간이 끝나면 피해·탁화·경고·저장은 바로 끝내고(IsActive=false), 그림만 등장과 같은 길이로 흐려진다.
            remainingDuration = 0f;
            IsActive = false;
            IsFadingOut = true;
            fadeOutElapsed = 0f;
        }

        private void TickFadeOut(float step)
        {
            if (!IsFadingOut)
            {
                return;
            }

            fadeOutElapsed += step;
            if (fadeOutElapsed < Mathf.Max(0.0001f, GasVisualRules.SpawnDurationSeconds))
            {
                return;
            }

            IsFadingOut = false;
            gameObject.SetActive(false);
        }

        private void SyncTriggerRadius()
        {
            var trigger = GetComponent<CircleCollider2D>();
            if (trigger == null)
            {
                return;
            }

            trigger.isTrigger = true;
            trigger.radius = radius;
        }
    }
}
