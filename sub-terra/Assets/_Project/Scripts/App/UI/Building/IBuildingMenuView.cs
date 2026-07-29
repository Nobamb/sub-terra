using System.Collections.Generic;

namespace SubTerra.App.UI.Building
{
    /// <summary>건설 메뉴 Presenter가 갱신하는 B 소유 View 경계.</summary>
    public interface IBuildingMenuView
    {
        void SetBuildingList(IReadOnlyList<BuildingMenuItemReadModel> items);
        void SetSelection(BuildingMenuItemReadModel item);
        void ClearSelection();
        void SetAvailability(BuildingAvailabilityReadModel availability);
        void SetStatusMessage(string message);
        void SetVisible(bool visible);
    }
}
