using SubTerra.App.Tutorial;

namespace SubTerra.App.UI.Tutorial
{
    /// <summary>데모 목표·안내 UI. State를 직접 변경하지 않는다.</summary>
    public interface IDemoObjectiveView
    {
        void SetObjective(DemoObjectiveReadModel model);
        void SetGuidanceVisible(bool visible);
        void SetGuidanceText(string title, string body);
        void SetInputLocked(bool locked);
        void SetHazardYield(bool yieldToHazard);
        void SetDemoCompleteVisible(bool visible, string summary);
        void SetDetailsVisible(bool visible);
        void SetDetailsText(string title, string body, string nextAction);
    }
}
