using System.Collections.Generic;

namespace SubTerra.App.Inventory
{
    /// <summary>
    /// 테스트·경계 검증용 최소 카탈로그. ScriptableObject 없이 단위값을 등록한다.
    /// </summary>
    public sealed class InMemoryMineralCatalog : IMineralCatalogLookup
    {
        private readonly Dictionary<string, MineralUnitInfo> byId =
            new Dictionary<string, MineralUnitInfo>();

        public void Register(string id, float unitWeight, int unitPrice, string displayName = null)
        {
            if (string.IsNullOrEmpty(id))
            {
                return;
            }

            byId[id] = new MineralUnitInfo(id, displayName ?? id, unitWeight, unitPrice);
        }

        public void Register(MineralUnitInfo info)
        {
            if (string.IsNullOrEmpty(info.Id))
            {
                return;
            }

            byId[info.Id] = info;
        }

        public bool TryGetMineral(string mineralId, out MineralUnitInfo info)
        {
            if (string.IsNullOrEmpty(mineralId))
            {
                info = default;
                return false;
            }

            return byId.TryGetValue(mineralId, out info);
        }
    }
}
