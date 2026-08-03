using System;

namespace SubTerra.Shared
{
    public enum GasExposureFailureSeverity
    {
        Damage = 1,
        RescueRequired = 2
    }

    /// <summary>H 단계가 L 단계의 피해·구조 실패 처리기로 전달하는 확정 입력.</summary>
    [Serializable]
    public sealed class GasExposureFailureInputDto
    {
        public string gasZoneId;
        public float effectiveIntensity;
        public float cumulativeExposureSeconds;
        public GasExposureFailureSeverity severity;
    }
}
