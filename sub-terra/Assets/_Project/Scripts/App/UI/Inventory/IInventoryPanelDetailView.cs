namespace SubTerra.App.UI.Inventory
{
    /// <summary>
    /// prompt-B 112: 적재 게이지·미정산 가치 카드용 선택 표시 계약.
    /// 스냅샷 수치를 그대로 전달받아 표시만 하며 State를 쓰지 않는다.
    /// </summary>
    public interface IInventoryPanelDetailView
    {
        void SetCargoLoad(float currentWeight, float maxCapacity);
        void SetUnsettledAmount(float value);
    }
}
