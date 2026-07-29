using System.Collections.Generic;
using SubTerra.App.Progression;

namespace SubTerra.App.UI.Progression
{
    /// <summary>업그레이드 패널 표시 계약. State를 직접 변경하는 API를 두지 않는다.</summary>
    public interface IProgressionPanelView
    {
        void SetUpgradeList(IReadOnlyList<UpgradeSnapshot> upgrades);
        void SetSelectedUpgrade(UpgradeSnapshot upgrade);
        void SetPurchaseResult(string message, string detail);
        void SetDeepZoneAccess(ZoneAccessResult access);
        void SetBusy(bool busy);
        void SetVisible(bool visible);
    }
}
