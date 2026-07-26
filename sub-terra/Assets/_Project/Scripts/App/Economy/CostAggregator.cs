using System.Collections.Generic;
using SubTerra.Shared;

namespace SubTerra.App.Economy
{
    /// <summary>
    /// 비용 목록 정규화·합산 순수 로직.
    /// 동일 ID는 합산하고, 빈 ID·비양수·정수 오버플로를 거부한다.
    /// I/O·MonoBehaviour와 분리해 Edit Mode에서 단독 검증 가능하게 둔다.
    /// </summary>
    public static class CostAggregator
    {
        /// <summary>
        /// 비용을 ID 기준으로 합산한 정규화 목록을 만든다.
        /// 실패 시 normalized는 비어 있고 diagnostic에 원인을 담는다.
        /// </summary>
        public static bool TryNormalize(
            IReadOnlyList<ItemCostDto> costs,
            out List<ItemCostDto> normalized,
            out string diagnostic)
        {
            normalized = new List<ItemCostDto>();
            diagnostic = string.Empty;

            if (costs == null)
            {
                diagnostic = "Costs list is null.";
                return false;
            }

            // 빈 목록은 “비용 없음”으로 허용한다(무료 설치 등). 합산 맵만 비어 반환.
            if (costs.Count == 0)
            {
                return true;
            }

            // 삽입 순서를 유지하면서 합산하기 위해 Dictionary + 키 목록을 함께 쓴다.
            var totals = new Dictionary<string, int>();
            var order = new List<string>();

            for (var i = 0; i < costs.Count; i++)
            {
                var entry = costs[i];
                var id = entry.ItemId;
                if (string.IsNullOrEmpty(id))
                {
                    diagnostic = "Empty item id in costs.";
                    normalized = new List<ItemCostDto>();
                    return false;
                }

                // 음수·0 비용은 카탈로그/정의 오류로 간주하고 거부한다.
                if (entry.Quantity <= 0)
                {
                    diagnostic = "Non-positive quantity for id=" + id;
                    normalized = new List<ItemCostDto>();
                    return false;
                }

                if (totals.TryGetValue(id, out var existing))
                {
                    // 동일 ID 중복 비용: 합산 시 int 오버플로를 사전 차단한다.
                    if (existing > int.MaxValue - entry.Quantity)
                    {
                        diagnostic = "Cost quantity overflow for id=" + id;
                        normalized = new List<ItemCostDto>();
                        return false;
                    }

                    totals[id] = existing + entry.Quantity;
                }
                else
                {
                    totals[id] = entry.Quantity;
                    order.Add(id);
                }
            }

            for (var i = 0; i < order.Count; i++)
            {
                var id = order[i];
                normalized.Add(new ItemCostDto(id, totals[id]));
            }

            return true;
        }
    }
}
