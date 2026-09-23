namespace SubTerra.App.Drone
{
    /// <summary>드론 분석 UI가 표시하는 현재 운영 상태.</summary>
    public enum DroneOperationalState
    {
        Idle,
        Scanning,
        Priority
    }

    /// <summary>월드 스캔과 분석 우선순위를 하나의 표시 상태로 결정한다.</summary>
    public static class DroneOperationalStateResolver
    {
        public static DroneOperationalState Resolve(bool isUrgent, bool isScanPulseActive)
        {
            if (isUrgent)
            {
                return DroneOperationalState.Priority;
            }

            return isScanPulseActive
                ? DroneOperationalState.Scanning
                : DroneOperationalState.Idle;
        }
    }
}
