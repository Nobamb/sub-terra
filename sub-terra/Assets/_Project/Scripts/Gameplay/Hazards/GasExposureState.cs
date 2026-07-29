using System;

namespace SubTerra.Gameplay.Hazards
{
    /// <summary>Read-only gameplay result; it deliberately does not apply power or movement penalties.</summary>
    [Serializable]
    public readonly struct GasExposureState : IEquatable<GasExposureState>
    {
        public bool IsExposed { get; }
        public GasRiskLevel Risk { get; }
        public GasType Type { get; }
        public string GasZoneId { get; }
        public float RemainingDuration { get; }

        public GasExposureState(bool isExposed, GasRiskLevel risk, GasType type, string gasZoneId, float remainingDuration)
        {
            IsExposed = isExposed;
            Risk = risk;
            Type = type;
            GasZoneId = gasZoneId ?? string.Empty;
            RemainingDuration = remainingDuration;
        }

        public bool Equals(GasExposureState other)
        {
            return IsExposed == other.IsExposed && Risk == other.Risk && Type == other.Type &&
                   GasZoneId == other.GasZoneId && Math.Abs(RemainingDuration - other.RemainingDuration) < 0.1f;
        }

        public override bool Equals(object obj) => obj is GasExposureState other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(IsExposed, Risk, Type, GasZoneId);
    }
}
