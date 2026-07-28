using SubTerra.App.Drone;

namespace SubTerra.App.UI.Drone
{
    public interface IDroneReasonView
    {
        void SetAnalysis(DroneAnalysisResult analysis);
        void SetVisible(bool visible);
    }
}
