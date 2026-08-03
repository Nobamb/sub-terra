using System;

namespace SubTerra.Gameplay.Hazards
{
    /// <summary>GasHazardSystem이 판정한 Zone 노출 결과. 실제 효과 적용은 별도 컨트롤러가 담당한다.</summary>
    [Serializable]
    public readonly struct GasExposureState : IEquatable<GasExposureState>
    {
        public bool IsExposed { get; }
        public GasRiskLevel Risk { get; }
        public GasType Type { get; }
        public string GasZoneId { get; }
        public float RemainingDuration { get; }
        public float Intensity { get; }

        public GasExposureState(bool isExposed, GasRiskLevel risk, GasType type, string gasZoneId, float remainingDuration)
            : this(isExposed, risk, type, gasZoneId, remainingDuration, DefaultIntensity(risk))
        {
        }

        public GasExposureState(
            bool isExposed,
            GasRiskLevel risk,
            GasType type,
            string gasZoneId,
            float remainingDuration,
            float intensity)
        {
            IsExposed = isExposed;
            Risk = risk;
            Type = type;
            GasZoneId = gasZoneId ?? string.Empty;
            RemainingDuration = remainingDuration;
            Intensity = GasRiskEvaluator.ClampIntensity(intensity);
        }

        public bool Equals(GasExposureState other)
        {
            return IsExposed == other.IsExposed && Risk == other.Risk && Type == other.Type &&
                   GasZoneId == other.GasZoneId
                   && Math.Abs(RemainingDuration - other.RemainingDuration) < 0.1f
                   && Math.Abs(Intensity - other.Intensity) < 0.001f;
        }

        public override bool Equals(object obj) => obj is GasExposureState other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(IsExposed, Risk, Type, GasZoneId);

        private static float DefaultIntensity(GasRiskLevel risk)
        {
            return risk == GasRiskLevel.Critical
                ? 0.7f
                : risk == GasRiskLevel.Caution ? 0.1f : 0f;
        }
    }
}
