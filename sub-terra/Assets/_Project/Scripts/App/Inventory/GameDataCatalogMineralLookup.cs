using SubTerra.App.Core.Data;

namespace SubTerra.App.Inventory
{
    /// <summary>
    /// 프로덕션 GameDataCatalog → 인벤토리 조회 포트 어댑터.
    /// Bootstrap의 IDataCatalogPort(Validate 전용)와 달리 TryGet을 노출한다.
    /// </summary>
    public sealed class GameDataCatalogMineralLookup : IMineralCatalogLookup
    {
        private readonly GameDataCatalog catalog;

        public GameDataCatalogMineralLookup(GameDataCatalog catalog)
        {
            this.catalog = catalog;
        }

        public bool TryGetMineral(string mineralId, out MineralUnitInfo info)
        {
            info = default;
            if (catalog == null || string.IsNullOrEmpty(mineralId))
            {
                return false;
            }

            if (!catalog.TryGetMineral(mineralId, out var data) || data == null)
            {
                return false;
            }

            info = new MineralUnitInfo(data.Id, data.DisplayName, data.UnitWeight, data.UnitPrice);
            return true;
        }
    }
}
