using System.Collections.Generic;
using SubTerra.App.Inventory;
using SubTerra.App.Outpost;

namespace SubTerra.App.UI.Outpost
{
    public enum OutpostPanelMode
    {
        None = 0,
        Core = 1,
        Charger = 2,
        Settlement = 3,
        Storage = 4,
        Clinic = 5
    }

    /// <summary>전진기지 패널 표시 계약. 상태 변경 API는 노출하지 않는다.</summary>
    public interface IOutpostPanelView
    {
        void SetVisible(bool visible);
        void SetMode(OutpostPanelMode mode);
        void SetPower(float supply, float consumption, bool active, string inactiveReasonId);
        void SetFacilities(IReadOnlyList<OutpostFacilityReadModel> facilities);
        void SetCargo(string playerCargo, string storageCargo);
        void SetSettlementCargo(string cargo);
        void SetCheckpoint(string checkpoint);
        void SetSelectedMineral(string summary);
        void SetMineralOptions(IReadOnlyList<OutpostMineralOption> options, string selectedMineralId);
        void ClearMineralSearch();
        void SetResult(string message, bool isError);
        void ShowTemporaryMessage(string message, float durationSeconds);
        void SetTutorialVisible(bool visible);
        void SetBusy(bool busy);

        /// <summary>
        /// 충전기·보건소 팝업 게이지의 실제 사용 전후 값. 표시 전용이며 처리 결과는 이미 반영된 뒤다.
        /// 기존 View 구현이 깨지지 않도록 기본 구현은 아무 일도 하지 않는다.
        /// </summary>
        void SetServiceVital(OutpostOperationKind kind, float before, float after, float maximum)
        {
        }

        /// <summary>보관함 팝업(B-138) 목록용 화물·보관 스냅샷. 표시 전용.</summary>
        void SetStorageCargo(InventorySnapshot playerCargo, InventorySnapshot storage)
        {
        }

        /// <summary>보관함 팝업의 선택 자원·보유·보관·요청 수량. 빈 ID면 미선택이다.</summary>
        void SetStorageSelection(string mineralId, string displayName, int owned, int stored, int quantity)
        {
        }
    }
}
