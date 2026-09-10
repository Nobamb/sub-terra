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
        void SetDetailsReward(string rewardText);
        void SetDetailsStatus(string statusText);
        void SetDetailsIndex(string indexText);
        void SetDetailsNavInteractable(bool previousEnabled, bool nextEnabled);
        void SetCapacityChoiceVisible(bool visible);
        void SetCapacityChoiceText(string title, string body);
        void SetDumpPanelVisible(bool visible);
        void SetDumpSummary(string summary);
        void SetDumpRows(System.Collections.Generic.IReadOnlyList<SubTerra.App.Tutorial.QuestDumpRow> rows);
        void SetClaimVisible(bool visible);
        void SetClaimText(string title, string questTitle, string rewardText, string hint);
    }
}
