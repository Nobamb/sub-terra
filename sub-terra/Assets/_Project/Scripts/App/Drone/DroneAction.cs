namespace SubTerra.App.Drone
{
    /// <summary>드론이 제안할 수 있는 MVP 행동 후보. 선언 순서는 동점 우선순위와 무관하다.</summary>
    public enum DroneAction
    {
        ReturnToBase = 0,
        InstallSupport = 1,
        LeaveGasZone = 2,
        MineNearbyMineral = 3,
        BuildOutpost = 4,
        ContinueDescending = 5,
        Recharge = 6
    }
}
