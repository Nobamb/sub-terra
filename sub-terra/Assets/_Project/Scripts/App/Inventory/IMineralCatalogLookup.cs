namespace SubTerra.App.Inventory
{
    /// <summary>
    /// 인벤토리 전용 광물 단위 조회 포트.
    /// Shared를 확장하지 않고 App 계층에서 카탈로그 조회 경계를 둔다.
    /// </summary>
    public interface IMineralCatalogLookup
    {
        bool TryGetMineral(string mineralId, out MineralUnitInfo info);
    }
}
