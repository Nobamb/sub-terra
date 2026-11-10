namespace SubTerra.App.UI.Building
{
    /// <summary>B-188 건설 모드 화면. 레일은 항상 보이고 큰 창은 목록·상세 중 하나만 보인다.</summary>
    public enum BuildingModePage
    {
        Rail = 0,
        List = 1,
        Detail = 2
    }

    /// <summary>
    /// B-188: 레일 `>>`·목록·상세·뒤로가기 전환 규칙만 가진 순수 상태.
    /// 설치 대상 선택·비용·판정은 Presenter가 그대로 소유한다.
    /// </summary>
    public sealed class BuildingModeNavigation
    {
        public BuildingModePage Page { get; private set; } = BuildingModePage.Rail;
        public string Query { get; private set; } = string.Empty;
        public string DetailBuildingId { get; private set; } = string.Empty;
        public BuildingCatalogSort Sort { get; private set; } = BuildingCatalogQuery.DefaultSort;

        public bool IsExpanded => Page != BuildingModePage.Rail;
        public string ExpandLabel => IsExpanded ? "<<" : ">>";

        /// <summary>`>>`/`<<`: 접혀 있으면 목록을 펼치고, 목록·상세 어디서든 다시 누르면 레일+카드로 접는다.</summary>
        public void ToggleExpanded()
        {
            Page = IsExpanded ? BuildingModePage.Rail : BuildingModePage.List;
        }

        public void ShowDetail(string buildingId)
        {
            if (string.IsNullOrEmpty(buildingId))
            {
                return;
            }

            DetailBuildingId = buildingId;
            Page = BuildingModePage.Detail;
        }

        /// <summary>상세의 `시설 목록`: 목록으로만 돌아간다. 검색어는 유지하고 건설 모드는 닫지 않는다.</summary>
        public void BackToList()
        {
            if (Page == BuildingModePage.Detail)
            {
                Page = BuildingModePage.List;
            }
        }

        public void SetQuery(string query)
        {
            Query = query ?? string.Empty;
        }

        /// <summary>B-191: 목록 정렬만 바꾼다. 검색어는 그대로 둔다.</summary>
        public void SetSort(BuildingCatalogSort sort)
        {
            Sort = BuildingCatalogQuery.FromIndex((int)sort);
        }

        /// <summary>건설 모드를 닫을 때: 다음에 열면 레일+미선택·검색어 없음·기본 순에서 시작한다.</summary>
        public void Reset()
        {
            Page = BuildingModePage.Rail;
            Query = string.Empty;
            Sort = BuildingCatalogQuery.DefaultSort;
            DetailBuildingId = string.Empty;
        }
    }
}
