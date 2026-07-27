using UnityEngine;

namespace SubTerra.Gameplay.Hazards
{
    /// <summary>A circular, timed gas area. Exposure effects are owned by subscribers, not this component.</summary>
    public sealed class GasZone : MonoBehaviour
    {
        [SerializeField] private string gasZoneId;
        [SerializeField] private GasType gasType = GasType.Toxic;
        [SerializeField, Range(0f, 1f)] private float intensity = 0.8f;
        [SerializeField, Min(0.1f)] private float radius = 2f;
        [SerializeField, Min(0.1f)] private float remainingDuration = 12f;

        public string GasZoneId => gasZoneId;
        public GasType GasType => gasType;
        public float Intensity => intensity;
        public float RemainingDuration => remainingDuration;
        public bool IsActive { get; private set; }

        public void Activate(string id, GasType type, float zoneIntensity, float zoneRadius, float duration)
        {
            gasZoneId = id;
            gasType = type;
            intensity = GasRiskEvaluator.ClampIntensity(zoneIntensity);
            radius = Mathf.Max(0.1f, zoneRadius);
            remainingDuration = Mathf.Max(0.1f, duration);
            IsActive = true;
            gameObject.SetActive(true);
        }

        public bool Contains(Vector2 worldPosition)
        {
            return IsActive && Vector2.Distance(transform.position, worldPosition) <= radius;
        }

        public void Tick(float deltaTime)
        {
            if (!IsActive) return;
            remainingDuration -= Mathf.Max(0f, deltaTime);
            if (remainingDuration > 0f) return;
            remainingDuration = 0f;
            IsActive = false;
            gameObject.SetActive(false);
        }
    }
}
