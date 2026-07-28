namespace SubTerra.Gameplay.Drone
{
    public interface IDroneContextProvider
    {
        DroneContextDto CurrentContext { get; }
        DroneContextDto CaptureContext();
    }
}
