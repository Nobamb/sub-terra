using System.Collections.Generic;
using SubTerra.Shared;

namespace SubTerra.App.UI.EmergencyEscape
{
    /// <summary>긴급 탈출 목적지 선택 창. 상태 변경 API는 노출하지 않는다.</summary>
    public interface IEmergencyEscapePanelView
    {
        void SetVisible(bool visible);
        void SetDestinations(IReadOnlyList<EmergencyEscapeDestinationOption> options, int selectedIndex);
        void SetCost(int gold, int energy);
        void SetResult(string message, bool isError);
        void SetBusy(bool busy);
        int SelectedDestinationIndex { get; }
    }
}
