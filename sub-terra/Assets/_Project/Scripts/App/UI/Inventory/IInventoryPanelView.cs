namespace SubTerra.App.UI.Inventory
{
    /// <summary>
    /// 인벤토리 패널 표시 계약. State를 쓰지 않고 표시 문자열·목록만 설정한다.
    /// </summary>
    public interface IInventoryPanelView
    {
        void SetCargoSummary(string cargoText);
        void SetUnsettledValue(string valueText);
        void SetStacksText(string stacksText);
        void SetVisible(bool visible);
    }
}
