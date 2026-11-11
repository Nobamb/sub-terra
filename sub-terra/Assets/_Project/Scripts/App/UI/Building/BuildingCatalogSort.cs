using System.Collections.Generic;

namespace SubTerra.App.UI.Building
{
    /// <summary>B-191: `>>` 전체 목록 정렬. 값 순서는 드롭다운 항목 순서와 같다.</summary>
    public enum BuildingCatalogSort
    {
        Default = 0,
        DefaultReverse = 1,
        NameAscending = 2,
        NameDescending = 3
    }

    /// <summary>
    /// B-191: 큰 목록의 정렬 규칙(순수 함수). 검색으로 걸러진 결과만 정렬하고 레일 순서는 건드리지 않는다.
    /// </summary>
    public static class BuildingCatalogQuery
    {
        public const BuildingCatalogSort DefaultSort = BuildingCatalogSort.Default;

        private static readonly string[] Labels =
        {
            "기본 순",
            "기본 역순",
            "가나다순",
            "가나다 역순"
        };

        public static int SortCount => Labels.Length;

        public static IReadOnlyList<string> AllLabels()
        {
            return Labels;
        }

        public static string Label(BuildingCatalogSort sort)
        {
            var index = (int)sort;
            return index >= 0 && index < Labels.Length ? Labels[index] : Labels[(int)DefaultSort];
        }

        public static BuildingCatalogSort FromIndex(int index)
        {
            return index >= 0 && index < Labels.Length ? (BuildingCatalogSort)index : DefaultSort;
        }

        /// <summary>
        /// 기본 순서(레일·목록 순서)로 넘긴 표시 이름들의 보여 줄 순서를 인덱스로 돌려준다.
        /// </summary>
        public static List<int> Order(IReadOnlyList<string> displayNames, BuildingCatalogSort sort)
        {
            var order = new List<int>();
            if (displayNames == null)
            {
                return order;
            }

            for (var i = 0; i < displayNames.Count; i++)
            {
                order.Add(i);
            }

            switch (sort)
            {
                case BuildingCatalogSort.DefaultReverse:
                    order.Reverse();
                    break;
                case BuildingCatalogSort.NameAscending:
                    // List.Sort는 안정 정렬이 아니므로 같은 이름은 기본 순서로 가른다.
                    order.Sort((a, b) => CompareNames(displayNames, a, b, 1));
                    break;
                case BuildingCatalogSort.NameDescending:
                    order.Sort((a, b) => CompareNames(displayNames, a, b, -1));
                    break;
            }

            return order;
        }

        private static int CompareNames(IReadOnlyList<string> names, int a, int b, int direction)
        {
            // 한글 완성형은 유니코드 순서가 가나다순이다.
            var result = string.CompareOrdinal(names[a] ?? string.Empty, names[b] ?? string.Empty);
            return result != 0 ? result * direction : a.CompareTo(b);
        }
    }
}
