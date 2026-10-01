namespace SubTerra.App.Economy
{
    /// <summary>
    /// 지갑이 보유량을 읽기 전용으로 알려 주는 App 내부 경계.
    /// 업그레이드 UI가 "골드 N 부족"처럼 실제 부족량을 안내하는 데만 쓴다(Shared 계약은 바꾸지 않는다).
    /// </summary>
    public interface IResourceBalanceProvider
    {
        int GetOwnedQuantity(string itemId);
    }
}
