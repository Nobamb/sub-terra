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
        /// <summary>화물 부족 팝업(B-152) 표시값: 받을 보상, 현재/최대 적재량, 부족한 공간.</summary>
        void SetCapacityChoice(CargoShortfallModel shortfall, QuestReward reward);
        void SetDumpPanelVisible(bool visible);
        void SetDumpSummary(string summary);
        /// <summary>화물 버리기 창(B-153) 표시값: 지금 부족한 공간과 받을 보상. SetDumpRows보다 먼저 불린다.</summary>
        void SetDumpChoice(CargoShortfallModel shortfall, QuestReward reward);
        void SetDumpRows(System.Collections.Generic.IReadOnlyList<SubTerra.App.Tutorial.QuestDumpRow> rows);
        void SetClaimVisible(bool visible);
        void SetClaimText(string title, string questTitle, string rewardText, string hint);
    }
}
