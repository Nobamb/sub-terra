using System.Collections.Generic;
using SubTerra.App.Core.Data;
using SubTerra.Shared;

namespace SubTerra.App.Economy
{
    /// <summary>
    /// App 로컬 ItemCostEntry(ScriptableObject 직렬화) ↔ Shared ItemCostDto 매핑.
    /// 카탈로그 정의는 Entry, 런타임 지갑 계약은 Dto를 사용한다.
    /// </summary>
    public static class ItemCostMapping
    {
        public static ItemCostDto ToDto(ItemCostEntry entry)
        {
            return new ItemCostDto(entry.ItemId, entry.Quantity);
        }

        public static List<ItemCostDto> ToDtoList(IReadOnlyList<ItemCostEntry> entries)
        {
            var list = new List<ItemCostDto>();
            if (entries == null)
            {
                return list;
            }

            for (var i = 0; i < entries.Count; i++)
            {
                list.Add(ToDto(entries[i]));
            }

            return list;
        }
    }
}
