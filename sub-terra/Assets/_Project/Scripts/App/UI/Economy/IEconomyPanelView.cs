namespace SubTerra.App.UI.Economy
{
    /// <summary>
    /// 판매·제작 패널 표시 계약.
    /// State/Inventory를 직접 쓰지 않고 결과 메시지·버튼 활성만 설정한다.
    /// </summary>
    public interface IEconomyPanelView
    {
        void SetStatusMessage(string message);
        void SetStatusDetail(string detail);
        void SetBusy(bool busy);
        void SetVisible(bool visible);
    }
}
