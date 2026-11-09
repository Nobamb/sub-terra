using System.Collections.Generic;
using SubTerra.App.Core.Data;

namespace SubTerra.App.UI.Inventory
{
    /// <summary>prompt-B 190: 적재 내역 정렬. 값 순서는 드롭다운 항목 순서와 같다.</summary>
    public enum InventoryManifestSort
    {
        QuantityDescending = 0,
        QuantityAscending = 1,
        WeightDescending = 2,
        WeightAscending = 3,
        RarityAscending = 4,
        RarityDescending = 5,
        NameAscending = 6,
        NameDescending = 7
    }

    /// <summary>
    /// prompt-B 190: 적재 내역 표의 검색·정렬 규칙. 수량 0 제외 → 이름 검색 → 정렬 순서로 적용한다.
    /// 표시만 정하며 수량·무게·가치를 계산하거나 바꾸지 않는다.
    /// </summary>
    public static class InventoryManifestQuery
    {
        public const InventoryManifestSort DefaultSort = InventoryManifestSort.RarityAscending;
        public const string EmptyCargoText = "적재된 자원이 없습니다";
        public const string NoSearchResultText = "검색 결과가 없습니다";
        public const string SearchPlaceholder = "자원 이름 검색";

        private static readonly string[] Labels =
        {
            "수량 많은 순",
            "수량 적은 순",
            "무게 무거운 순",
            "무게 가벼운 순",
            "희귀도순",
            "희귀도 역순",
            "가나다순",
            "가나다 역순"
        };

        // 아이템 데이터에 희귀도 필드가 없어 현재 적재 자원 순서를 여기 둔다(#117: 새 종류 추가 없음).
        private static readonly string[] MineralRarityOrder =
        {
            DataIds.Minerals.Copper,
            DataIds.Minerals.Iron,
            DataIds.Minerals.Lithium
        };

        private const int UnknownMineralRank = 50;
        private const int RareItemRank = 100;
        private const string InitialConsonants = "ㄱㄲㄴㄷㄸㄹㅁㅂㅃㅅㅆㅇㅈㅉㅊㅋㅌㅍㅎ";

        public static int SortCount => Labels.Length;

        public static string Label(InventoryManifestSort sort)
        {
            var index = (int)sort;
            return index >= 0 && index < Labels.Length ? Labels[index] : Labels[(int)DefaultSort];
        }

        public static IReadOnlyList<string> AllLabels()
        {
            return Labels;
        }

        public static InventoryManifestSort FromIndex(int index)
        {
            return index >= 0 && index < Labels.Length ? (InventoryManifestSort)index : DefaultSort;
        }

        /// <summary>낮을수록 흔하다. 구리 &lt; 철 &lt; 리튬, 희귀 품목(엔진 연료)은 광물보다 높다.</summary>
        public static int RarityRank(string itemId)
        {
            for (var i = 0; i < MineralRarityOrder.Length; i++)
            {
                if (MineralRarityOrder[i] == itemId)
                {
                    return i;
                }
            }

            return DataIds.RareItems.IsRare(itemId) ? RareItemRank : UnknownMineralRank;
        }

        public static int LoadedCount(IReadOnlyList<InventoryStackReadModel> stacks)
        {
            var count = 0;
            if (stacks == null)
            {
                return count;
            }

            for (var i = 0; i < stacks.Count; i++)
            {
                if (stacks[i].Quantity > 0)
                {
                    count++;
                }
            }

            return count;
        }

        public static bool Matches(InventoryStackReadModel stack, string query)
        {
            if (stack.Quantity <= 0)
            {
                return false;
            }

            var trimmed = query == null ? string.Empty : query.Trim();
            var name = DisplayName(stack);
            for (var start = 0; start <= name.Length - trimmed.Length; start++)
            {
                var matched = true;
                for (var i = 0; i < trimmed.Length; i++)
                {
                    if (!MatchesCharacter(name[start + i], trimmed[i], i == trimmed.Length - 1))
                    {
                        matched = false;
                        break;
                    }
                }

                if (matched) return true;
            }

            return false;
        }

        private static bool MatchesCharacter(char name, char query, bool last)
        {
            if (char.ToUpperInvariant(name) == char.ToUpperInvariant(query)) return true;
            if (name < '가' || name > '힣') return false;

            var initial = query >= '\u1100' && query <= '\u1112'
                ? query - '\u1100'
                : InitialConsonants.IndexOf(query);
            if (initial >= 0) return (name - '가') / 588 == initial;

            // 마지막 음절에 아직 받침을 조합하지 않았어도 같은 초성·중성 후보를 보여 준다.
            return last && query >= '가' && query <= '힣' && (query - '가') % 28 == 0
                && (name - '가') / 28 == (query - '가') / 28;
        }

        public static List<InventoryStackReadModel> Apply(
            IReadOnlyList<InventoryStackReadModel> stacks,
            string query,
            InventoryManifestSort sort)
        {
            var result = new List<InventoryStackReadModel>();
            if (stacks == null)
            {
                return result;
            }

            for (var i = 0; i < stacks.Count; i++)
            {
                if (Matches(stacks[i], query))
                {
                    result.Add(stacks[i]);
                }
            }

            // List.Sort는 안정 정렬이 아니므로 동률 규칙(희귀도순 → 가나다순)을 비교자에 모두 넣는다.
            result.Sort((a, b) => Compare(a, b, sort));
            return result;
        }

        public static int Compare(InventoryStackReadModel a, InventoryStackReadModel b, InventoryManifestSort sort)
        {
            int primary;
            switch (sort)
            {
                case InventoryManifestSort.QuantityDescending:
                    primary = b.Quantity.CompareTo(a.Quantity);
                    break;
                case InventoryManifestSort.QuantityAscending:
                    primary = a.Quantity.CompareTo(b.Quantity);
                    break;
                case InventoryManifestSort.WeightDescending:
                    // 개당 무게 기준(이 줄 무게 아님).
                    primary = b.UnitWeight.CompareTo(a.UnitWeight);
                    break;
                case InventoryManifestSort.WeightAscending:
                    primary = a.UnitWeight.CompareTo(b.UnitWeight);
                    break;
                case InventoryManifestSort.RarityDescending:
                    primary = RarityRank(b.MineralId).CompareTo(RarityRank(a.MineralId));
                    break;
                case InventoryManifestSort.NameAscending:
                    primary = CompareNames(a, b);
                    break;
                case InventoryManifestSort.NameDescending:
                    primary = CompareNames(b, a);
                    break;
                default:
                    primary = RarityRank(a.MineralId).CompareTo(RarityRank(b.MineralId));
                    break;
            }

            if (primary != 0)
            {
                return primary;
            }

            var rarity = RarityRank(a.MineralId).CompareTo(RarityRank(b.MineralId));
            if (rarity != 0)
            {
                return rarity;
            }

            var name = CompareNames(a, b);
            return name != 0 ? name : string.CompareOrdinal(a.MineralId, b.MineralId);
        }

        private static int CompareNames(InventoryStackReadModel a, InventoryStackReadModel b)
        {
            // 한글 완성형은 유니코드 순서가 가나다순이다.
            return string.CompareOrdinal(DisplayName(a), DisplayName(b));
        }

        private static string DisplayName(InventoryStackReadModel stack)
        {
            return string.IsNullOrEmpty(stack.DisplayName) ? stack.MineralId : stack.DisplayName;
        }
    }
}
