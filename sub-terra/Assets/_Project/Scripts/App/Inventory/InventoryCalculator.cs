using System;
using System.Collections.Generic;

namespace SubTerra.App.Inventory
{
    /// <summary>
    /// 총중량·미정산 가치 단일 계산 경로.
    /// UI/Save DTO는 이 결과를 재계산하지 않고 읽기만 한다.
    /// </summary>
    public static class InventoryCalculator
    {
        /// <summary>수량×단위중량 합. 카탈로그에 없는 ID 스택은 무시(비정상 상태 방어).</summary>
        public static float ComputeTotalWeight(
            IReadOnlyDictionary<string, int> quantities,
            IMineralCatalogLookup catalog)
        {
            if (quantities == null || catalog == null)
            {
                return 0f;
            }

            var total = 0f;
            foreach (var pair in quantities)
            {
                if (pair.Value <= 0)
                {
                    continue;
                }

                if (!catalog.TryGetMineral(pair.Key, out var info))
                {
                    continue;
                }

                total += pair.Value * info.UnitWeight;
            }

            return total < 0f ? 0f : total;
        }

        /// <summary>수량×단위가격 합. 정수 오버플로 시 long으로 누적 후 float로 안전하게 내린다.</summary>
        public static float ComputeUnsettledValue(
            IReadOnlyDictionary<string, int> quantities,
            IMineralCatalogLookup catalog)
        {
            if (quantities == null || catalog == null)
            {
                return 0f;
            }

            long total = 0;
            foreach (var pair in quantities)
            {
                if (pair.Value <= 0)
                {
                    continue;
                }

                if (!catalog.TryGetMineral(pair.Key, out var info))
                {
                    continue;
                }

                // unitPrice >= 0, quantity > 0 가정. long으로 누적해 int 곱 오버플로를 피한다.
                total += (long)pair.Value * info.UnitPrice;
            }

            if (total <= 0)
            {
                return 0f;
            }

            // float 정밀도 상한 근처는 그대로 두고, long→float 캐스트로 충분하다.
            return total > int.MaxValue ? int.MaxValue : total;
        }

        /// <summary>
        /// 잔여 적재량에 들어갈 수 있는 완전 단위 수.
        /// 부동소수점 오차로 한 단위를 놓치지 않도록 소량 허용 오차를 둔다.
        /// </summary>
        public static int MaxFittingUnits(float remainingCapacity, float unitWeight)
        {
            if (remainingCapacity <= 0f || unitWeight <= 0f)
            {
                return 0;
            }

            const float Epsilon = 0.0001f;
            var raw = (remainingCapacity + Epsilon) / unitWeight;
            if (raw >= int.MaxValue)
            {
                return int.MaxValue;
            }

            var units = (int)Math.Floor(raw);
            return units < 0 ? 0 : units;
        }
    }
}
