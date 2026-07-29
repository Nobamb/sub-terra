namespace SubTerra.App.UI.Hazards
{
    public enum HazardSeverity
    {
        Safe = 0,
        Caution = 1,
        Critical = 2
    }

    /// <summary>A의 확정 위험 단계와 실제 수치를 UI에 전달하는 읽기 모델.</summary>
    public readonly struct HazardStatusReadModel
    {
        public HazardSeverity Severity { get; }
        public string Label { get; }
        public string ValueText { get; }

        public HazardStatusReadModel(HazardSeverity severity, string label, string valueText)
        {
            Severity = severity;
            Label = label ?? string.Empty;
            ValueText = valueText ?? string.Empty;
        }
    }

    /// <summary>A가 계산한 전력망 결과. UI는 공급·소비·연결 여부를 재계산하지 않는다.</summary>
    public readonly struct PowerStatusReadModel
    {
        public bool IsConnected { get; }
        public float Supply { get; }
        public float Demand { get; }
        public int ActiveFacilityCount { get; }
        public string Reason { get; }

        public PowerStatusReadModel(
            bool isConnected,
            float supply,
            float demand,
            int activeFacilityCount,
            string reason)
        {
            IsConnected = isConnected;
            Supply = supply < 0f ? 0f : supply;
            Demand = demand < 0f ? 0f : demand;
            ActiveFacilityCount = activeFacilityCount < 0 ? 0 : activeFacilityCount;
            Reason = reason ?? string.Empty;
        }
    }
}
