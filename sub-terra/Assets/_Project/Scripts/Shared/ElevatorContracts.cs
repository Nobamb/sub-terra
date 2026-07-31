namespace SubTerra.Shared
{
    public enum ElevatorTravelState
    {
        Idle,
        Calling,
        Moving,
        Arrived,
        Blocked
    }

    public enum ElevatorDestination
    {
        SurfaceBase,
        Mine
    }

    /// <summary>Gameplay 정거장이 App의 전력·저장·Scene 전환을 직접 알지 않게 하는 경계.</summary>
    public interface IElevatorTravelPort
    {
        ElevatorTravelState State { get; }
        bool TryTravel(ElevatorDestination destination, out string reason);
    }
}
