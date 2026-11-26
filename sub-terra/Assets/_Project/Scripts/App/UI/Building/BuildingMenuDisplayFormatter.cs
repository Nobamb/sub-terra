using System.Collections.Generic;
using System.Text;
using SubTerra.App.Core.Data;
using SubTerra.Shared;
using SubTerra.Shared.Localization;

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

        private static string NoCost => LocalizationService.Text("비용 없음", "No Cost");

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
                    return LocalizationService.Text("설치 가능", "Ready to Place");
                case BuildingAvailabilityDisplayKind.NeedResources:
                    return LocalizationService.Text("자원 부족", "Not Enough Resources");
                case BuildingAvailabilityDisplayKind.CheckPlacement:
                    return LocalizationService.Text("설치 위치 확인", "Check Placement");
                default:
                    return LocalizationService.Text("시설 미선택", "No Facility Selected");
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
                // B-188: 설치 가능 옆에는 조작 안내를 붙이지 않는다(안내는 게임 가이드에만 둔다).
                case BuildingAvailabilityDisplayKind.Ready:
                    return string.Empty;
                case BuildingAvailabilityDisplayKind.NeedResources:
                    return LocalizationService.Text("자원이 부족합니다.", "You need more resources.");
                case BuildingAvailabilityDisplayKind.CheckPlacement:
                    return LocalizationService.Text("설치 가능한 위치로 커서를 옮기세요.", "Move the cursor to a valid tile.");
                default:
                    return LocalizationService.Text("시설을 고르면 설치 상태를 확인합니다.", "Choose a facility to check placement.");
            }
        }

        public static string AvailabilityText(BuildingAvailabilityDisplayKind kind, string message)
        {
            var detail = Detail(kind, message);
            return string.IsNullOrEmpty(detail)
                ? "<b>" + Title(kind) + "</b>"
                : "<b>" + Title(kind) + "</b>\n<size=90%>" + detail + "</size>";
        }

        /// <summary>B-188: 전진기지 코어 영역 전용 시설에만 짧은 운영 조건 라벨을 준다. 전력 소비량은 쓰지 않는다.</summary>
        public static string OperatingCondition(string buildingId)
        {
            return RequiresOutpostArea(buildingId)
                ? LocalizationService.Text(ItemDisplayNames.OutpostName + " 범위 내", "Within Outpost Range")
                : string.Empty;
        }

        public static bool RequiresOutpostArea(string buildingId)
        {
            return buildingId == DataIds.Buildings.ChargerBasic
                || buildingId == DataIds.Buildings.ClinicBasic
                || buildingId == DataIds.Buildings.SettlementBasic;
        }

        public const int ShortPurposeMaxLength = 40;

        /// <summary>
        /// B-188: 카드·목록용 짧은 용도(한두 줄). 설명의 첫 문장만 쓰고, 그래도 길면 단어 경계에서 줄여 "…"를 붙인다.
        /// 전체 설명은 `>>` 상세에서 보여 준다.
        /// </summary>
        public static string ShortPurpose(string description)
        {
            if (string.IsNullOrEmpty(description))
            {
                return string.Empty;
            }

            var text = description.Trim();
            for (var i = 0; i < text.Length; i++)
            {
                if (text[i] == '.' && (i == text.Length - 1 || char.IsWhiteSpace(text[i + 1])))
                {
                    text = text.Substring(0, i + 1);
                    break;
                }
            }

            if (text.Length <= ShortPurposeMaxLength)
            {
                return text;
            }

            var cut = text.LastIndexOf(' ', ShortPurposeMaxLength - 1);
            if (cut <= 0)
            {
                cut = ShortPurposeMaxLength - 1;
            }

            return text.Substring(0, cut).TrimEnd() + "…";
        }

        /// <summary>
        /// B-188: 시설명·짧은 용도에 검색어가 들어 있으면 true. 공백·대소문자는 무시하고 빈 검색어는 전체를 남긴다.
        /// </summary>
        public static bool MatchesSearch(string displayName, string purpose, string query)
        {
            var needle = NormalizeSearch(query);
            if (needle.Length == 0)
            {
                return true;
            }

            // B-191: 시설명은 초성·조합 중 글자까지 맞추고, 짧은 용도는 일반 글자 포함만 본다.
            return KoreanSearch.Matches(displayName, needle)
                || NormalizeSearch(purpose).Contains(needle);
        }

        private static string NormalizeSearch(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(value.Length);
            for (var i = 0; i < value.Length; i++)
            {
                if (!char.IsWhiteSpace(value[i]))
                {
                    builder.Append(char.ToLowerInvariant(value[i]));
                }
            }

            return builder.ToString();
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
                return NoCost;
            }

            var builder = new StringBuilder();
            for (var i = 0; i < costs.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append("  ");
                }

                var cost = costs[i];
                var label = ItemDisplayNames.LocalizedMineral(cost.ItemId) + " " + cost.Required;
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

            var english = LocalizationService.Current == GameLanguage.English;
            var shortages = new List<string>();
            for (var i = 0; i < costs.Count; i++)
            {
                var cost = costs[i];
                if (cost.IsEnough)
                {
                    continue;
                }

                var name = ItemDisplayNames.LocalizedMineral(cost.ItemId);
                var missing = cost.Required - cost.Owned;
                shortages.Add(english ? $"{missing} more {name}" : $"{name} {missing}개");
            }

            if (shortages.Count == 0) return string.Empty;
            if (!english) return string.Join(", ", shortages) + "가 더 필요합니다.";
            if (shortages.Count == 1) return "Need " + shortages[0] + ".";

            var last = shortages[shortages.Count - 1];
            shortages.RemoveAt(shortages.Count - 1);
            return "Need " + string.Join(", ", shortages) + " and " + last + ".";
        }

        public static string EntryStateLabel(IReadOnlyList<BuildingCostReadModel> costs)
        {
            return HasAllCosts(costs) ? ReadyGlyph : LocalizationService.Text("부족", "Short");
        }

        /// <summary>B-188: 비용 행 가운데 칸. `보유 / 필요` 순서이며 부족할 때만 보유 수량을 경고색으로 쓴다.</summary>
        public static string CostAmount(BuildingCostReadModel cost)
        {
            var owned = cost.IsEnough
                ? "<b>" + cost.Owned + "</b>"
                : "<color=" + MissingColorHex + "><b>" + cost.Owned + "</b></color>";
            return owned + " / " + cost.Required;
        }

        /// <summary>B-188: 비용 행 오른쪽 칸. `충분` 또는 `2 부족`.</summary>
        public static string CostState(BuildingCostReadModel cost)
        {
            return cost.IsEnough
                ? LocalizationService.Text("충분", "Enough")
                : (cost.Required - cost.Owned) + LocalizationService.Text(" 부족", " short");
        }

        /// <summary>B-188: 전체 목록 한 칸의 비용 비교. `철 3 / 5  2 부족`을 자원마다 한 줄씩 쓴다.</summary>
        public static string CostLines(IReadOnlyList<BuildingCostReadModel> costs)
        {
            if (costs == null || costs.Count == 0)
            {
                return NoCost;
            }

            var builder = new StringBuilder();
            for (var i = 0; i < costs.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append('\n');
                }

                var cost = costs[i];
                builder.Append(ItemDisplayNames.LocalizedMineral(cost.ItemId)).Append(' ')
                    .Append(CostAmount(cost)).Append("  <color=")
                    .Append(cost.IsEnough ? EnoughColorHex : MissingColorHex).Append('>')
                    .Append(CostState(cost)).Append("</color>");
            }

            return builder.ToString();
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
