using System.Collections.Generic;
using System.Text;
using SubTerra.App.Core.Data;
using SubTerra.Shared;

namespace SubTerra.App.UI.Building
{
    /// <summary>건설창 설치 상태 배너 종류. 판정은 Presenter 읽기 모델을 그대로 해석만 한다.</summary>
    public enum BuildingAvailabilityDisplayKind
    {
        Idle = 0,
        Ready = 1,
        NeedResources = 2,
        CheckPlacement = 3
    }

    /// <summary>
    /// prompt-B 110: 건설창 표시 문구를 만드는 순수 로직.
    /// 색상만으로 상태를 구분하지 않도록 제목·기호·안내 문구를 함께 만든다.
    /// </summary>
    public static class BuildingMenuDisplayFormatter
    {
        public const string ReadyGlyph = "✓";
        public const string WarningGlyph = "!";
        public const string BlockedGlyph = "×";
        public const string IdleGlyph = "·";

        public const string MissingColorHex = "#FF7A6B";
        public const string EnoughColorHex = "#8FF5C8";

        public static BuildingAvailabilityDisplayKind Classify(BuildingAvailabilityReadModel availability)
        {
            if (availability.CanPlace)
            {
                return BuildingAvailabilityDisplayKind.Ready;
            }

            if (availability.PlacementState == BuildingPlacementState.None)
            {
                return BuildingAvailabilityDisplayKind.Idle;
            }

            return availability.CanAfford
                ? BuildingAvailabilityDisplayKind.CheckPlacement
                : BuildingAvailabilityDisplayKind.NeedResources;
        }

        public static string Glyph(BuildingAvailabilityDisplayKind kind)
        {
            switch (kind)
            {
                case BuildingAvailabilityDisplayKind.Ready:
                    return ReadyGlyph;
                case BuildingAvailabilityDisplayKind.NeedResources:
                    return BlockedGlyph;
                case BuildingAvailabilityDisplayKind.CheckPlacement:
                    return WarningGlyph;
                default:
                    return IdleGlyph;
            }
        }

        public static string Title(BuildingAvailabilityDisplayKind kind)
        {
            switch (kind)
            {
                case BuildingAvailabilityDisplayKind.Ready:
                    return "설치 가능";
                case BuildingAvailabilityDisplayKind.NeedResources:
                    return "자원 부족";
                case BuildingAvailabilityDisplayKind.CheckPlacement:
                    return "설치 위치 확인";
                default:
                    return "시설 미선택";
            }
        }

        /// <summary>Presenter 메시지를 우선하고, 비어 있을 때만 다음 행동 안내를 채운다.</summary>
        public static string Detail(BuildingAvailabilityDisplayKind kind, string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                return message;
            }

            switch (kind)
            {
                case BuildingAvailabilityDisplayKind.Ready:
                    return "좌클릭으로 설치하거나 C로 가까운 위치에 설치하세요.";
                case BuildingAvailabilityDisplayKind.NeedResources:
                    return "자원이 부족합니다.";
                case BuildingAvailabilityDisplayKind.CheckPlacement:
                    return "커서를 설치 가능한 위치로 옮기세요.";
                default:
                    return "목록에서 시설을 고르면 설치 조건을 확인합니다.";
            }
        }

        public static string AvailabilityText(BuildingAvailabilityDisplayKind kind, string message)
        {
            return "<b>" + Title(kind) + "</b>\n<size=90%>" + Detail(kind, message) + "</size>";
        }

        public static string PowerLabel(int powerDraw)
        {
            return powerDraw > 0 ? "전력 소비 " + powerDraw : "전력 불필요";
        }

        public static bool HasAllCosts(IReadOnlyList<BuildingCostReadModel> costs)
        {
            if (costs == null)
            {
                return true;
            }

            for (var i = 0; i < costs.Count; i++)
            {
                if (!costs[i].IsEnough)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>목록 행의 짧은 비용 요약. 부족한 광물만 경고색으로 표시한다.</summary>
        public static string CostSummary(IReadOnlyList<BuildingCostReadModel> costs)
        {
            if (costs == null || costs.Count == 0)
            {
                return "비용 없음";
            }

            var builder = new StringBuilder();
            for (var i = 0; i < costs.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append("  ");
                }

                var cost = costs[i];
                var label = ItemDisplayNames.Mineral(cost.ItemId) + " " + cost.Required;
                if (cost.IsEnough)
                {
                    builder.Append(label);
                }
                else
                {
                    builder.Append("<color=").Append(MissingColorHex).Append('>')
                        .Append(label).Append("</color>");
                }
            }

            return builder.ToString();
        }

        /// <summary>부족한 광물과 수량만 모아 "철 5개가 더 필요합니다." 형태로 만든다. 부족분이 없으면 빈 문자열.</summary>
        public static string ShortageDetail(IReadOnlyList<BuildingCostReadModel> costs)
        {
            if (costs == null)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            for (var i = 0; i < costs.Count; i++)
            {
                var cost = costs[i];
                if (cost.IsEnough)
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(ItemDisplayNames.Mineral(cost.ItemId)).Append(' ')
                    .Append(cost.Required - cost.Owned).Append('개');
            }

            return builder.Length > 0 ? builder.Append("가 더 필요합니다.").ToString() : string.Empty;
        }

        public static string EntryStateLabel(IReadOnlyList<BuildingCostReadModel> costs)
        {
            return HasAllCosts(costs) ? ReadyGlyph : "부족";
        }

        public static string CostAmount(BuildingCostReadModel cost)
        {
            var color = cost.IsEnough ? EnoughColorHex : MissingColorHex;
            return "<size=85%>보유</size> <color=" + color + "><b>" + cost.Owned + "</b></color>"
                + " <size=85%>/ 필요</size> <b>" + cost.Required + "</b>";
        }

        public static string CostState(BuildingCostReadModel cost)
        {
            return cost.IsEnough ? ReadyGlyph + " 충분" : "× " + (cost.Required - cost.Owned) + " 부족";
        }

        public static float CostFill(BuildingCostReadModel cost)
        {
            if (cost.Required <= 0)
            {
                return 1f;
            }

            var ratio = (float)cost.Owned / cost.Required;
            return ratio < 0f ? 0f : ratio > 1f ? 1f : ratio;
        }
    }
}
