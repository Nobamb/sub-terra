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

        public string GasZoneId => gasZoneId;
        public GasType GasType => gasType;
        public float Intensity => intensity;
        public float Radius => radius;
        public float RemainingDuration => remainingDuration;
        public bool IsActive { get; private set; }
        public float SpawnProgress { get; private set; }
        public bool IsSpawnComplete => SpawnProgress >= 1f - 0.0001f;

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
            if (!IsActive)
            {
                return;
            }

            var step = Mathf.Max(0f, deltaTime);
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

            remainingDuration = 0f;
            IsActive = false;
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
