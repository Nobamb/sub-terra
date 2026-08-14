using System.Collections.Generic;
using SubTerra.App.Outpost;

namespace SubTerra.App.UI.Outpost
{
    /// <summary>전진기지 패널 표시 계약. 상태 변경 API는 노출하지 않는다.</summary>
    public interface IOutpostPanelView
    {
        void SetVisible(bool visible);
        void SetPower(float supply, float consumption, bool active, string inactiveReasonId);
        void SetFacilities(IReadOnlyList<OutpostFacilityReadModel> facilities);
        void SetCargo(string playerCargo, string storageCargo);
        void SetCheckpoint(string checkpoint);
        void SetResult(string message, bool isError);
        void ShowTemporaryMessage(string message, float durationSeconds);
        void SetTutorialVisible(bool visible);
        void SetBusy(bool busy);
    }
}
