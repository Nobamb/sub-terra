namespace SubTerra.App.UI.EmergencyEscape
{
    /// <summary>긴급 탈출 터미널 창. 상태 변경 API는 노출하지 않는다.</summary>
    public interface IEmergencyEscapePanelView
    {
        void SetVisible(bool visible);
        void SetDestinations(EscapeDestinationListModel model);
        void SetCost(int gold, int energy);
        void SetResult(string message, bool isError);
        void SetBusy(bool busy);
        int SelectedDestinationIndex { get; }
    }
}
