using SubTerra.App.Drone;

namespace SubTerra.App.UI.Drone
{
    /// <summary>드론 분석 상태를 표시하는 UI의 최소 계약.</summary>
    public interface IDroneOperationalStateView
    {
        void SetOperationalState(DroneOperationalState state);
    }
}
