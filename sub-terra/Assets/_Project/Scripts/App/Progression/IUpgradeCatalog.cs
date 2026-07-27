using System.Collections.Generic;
using SubTerra.App.Core.Data;

namespace SubTerra.App.Progression
{
    /// <summary>진행도 서비스가 사용하는 App 내부 업그레이드 카탈로그 읽기 경계.</summary>
    public interface IUpgradeCatalog
    {
        IReadOnlyList<UpgradeData> Upgrades { get; }
        bool TryGetUpgrade(string upgradeId, out UpgradeData data);
    }

    /// <summary>단일 GameDataCatalog의 업그레이드 목록을 진행도 서비스에 연결한다.</summary>
    public sealed class GameDataUpgradeCatalog : IUpgradeCatalog
    {
        private readonly GameDataCatalog catalog;

        public GameDataUpgradeCatalog(GameDataCatalog catalog)
        {
            this.catalog = catalog;
        }

        public IReadOnlyList<UpgradeData> Upgrades =>
            catalog != null ? catalog.Upgrades : System.Array.Empty<UpgradeData>();

        public bool TryGetUpgrade(string upgradeId, out UpgradeData data)
        {
            data = null;
            return catalog != null && catalog.TryGetUpgrade(upgradeId, out data);
        }
    }
}
