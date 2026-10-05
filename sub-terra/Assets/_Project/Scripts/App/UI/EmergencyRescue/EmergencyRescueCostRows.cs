using System.Collections.Generic;
using System.Globalization;
using SubTerra.App.Run;

namespace SubTerra.App.UI.EmergencyRescue
{
    /// <summary>비용 표 한 줄. 문자열만 담아 View가 열 위치에 그대로 놓는다.</summary>
    public readonly struct EmergencyRescueCostRow
    {
        public string ResourceId { get; }
        public string Name { get; }
        public bool IsGold { get; }
        public string Deduct { get; }
        public string Before { get; }
        public string After { get; }

        public EmergencyRescueCostRow(
            string resourceId, string name, bool isGold, string deduct, string before, string after)
        {
            ResourceId = resourceId;
            Name = name;
            IsGold = isGold;
            Deduct = deduct;
            Before = before;
            After = after;
        }
    }

    /// <summary>
    /// EmergencyRescueCost(기존 계산 결과)를 표 행으로 바꾼다. 비용·잔량은 다시 계산하지 않고 그대로 옮긴다.
    /// 순수 C#이라 EditMode에서 검증한다.
    /// </summary>
    public static class EmergencyRescueCostRows
    {
        public const string GoldResourceId = "gold";
        public const string GoldName = "골드";
        public const string GoldUnit = "G";
        public const string ItemUnit = "개";
        public const string FreeMessage = "보유 골드와 미정산 화물이 없어 무료로 구출됩니다.";

        public static List<EmergencyRescueCostRow> Build(EmergencyRescueCost cost)
        {
            var rows = new List<EmergencyRescueCostRow>();
            if (cost == null)
            {
                return rows;
            }

            if (cost.GoldBefore > 0 || cost.GoldCharged > 0)
            {
                rows.Add(new EmergencyRescueCostRow(
                    GoldResourceId,
                    GoldName,
                    true,
                    FormatDeduct(cost.GoldCharged, GoldUnit),
                    FormatAmount(cost.GoldBefore, GoldUnit),
                    FormatAmount(cost.GoldAfter, GoldUnit)));
            }

            for (var i = 0; i < cost.Minerals.Count; i++)
            {
                EmergencyRescueMineralCost mineral = cost.Minerals[i];
                rows.Add(new EmergencyRescueCostRow(
                    mineral.MineralId,
                    string.IsNullOrWhiteSpace(mineral.DisplayName) ? mineral.MineralId : mineral.DisplayName,
                    false,
                    FormatDeduct(mineral.Charged, ItemUnit),
                    FormatAmount(mineral.Before, ItemUnit),
                    FormatAmount(mineral.After, ItemUnit)));
            }

            return rows;
        }

        /// <summary>천 단위 구분과 단위를 붙인다. 예: 1,240G / 12개.</summary>
        public static string FormatAmount(int value, string unit)
        {
            return value.ToString("N0", CultureInfo.InvariantCulture) + unit;
        }

        public static string FormatDeduct(int value, string unit)
        {
            return "-" + FormatAmount(value, unit);
        }

        public static string BuildFooter(EmergencyRescueCost cost)
        {
            if (cost == null)
            {
                return "비용 정보를 불러올 수 없습니다.";
            }

            return cost.IsFree
                ? FreeMessage
                : "※ 미정산 광물은 종류별 " + EmergencyRescueService.MineralLossPercent + "%가 차감됩니다.";
        }

        /// <summary>표시한 비용과 지금 계산한 비용이 같은지. 다르면 결제 전에 다시 보여 준다.</summary>
        public static bool AreSame(EmergencyRescueCost a, EmergencyRescueCost b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            if (a == null || b == null
                || a.GoldBefore != b.GoldBefore
                || a.GoldCharged != b.GoldCharged
                || a.Minerals.Count != b.Minerals.Count)
            {
                return false;
            }

            for (var i = 0; i < a.Minerals.Count; i++)
            {
                EmergencyRescueMineralCost left = a.Minerals[i];
                EmergencyRescueMineralCost right = b.Minerals[i];
                if (left.MineralId != right.MineralId
                    || left.Before != right.Before
                    || left.Charged != right.Charged)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
