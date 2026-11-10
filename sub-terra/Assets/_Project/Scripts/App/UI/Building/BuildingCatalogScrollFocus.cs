namespace SubTerra.App.UI.Building
{
    /// <summary>
    /// B-191-3: 시설 목록에서 고른 행을 보이는 영역 가운데로 옮기는 스크롤 위치 계산.
    /// 가운데에 둘 수 없는 끝 쪽 행은 스크롤 끝(맨 위·맨 아래)에 붙인다.
    /// </summary>
    public static class BuildingCatalogScrollFocus
    {
        /// <param name="rowCenterFromTop">목록 맨 위에서 행 중심까지의 거리.</param>
        /// <param name="contentHeight">목록 전체 높이.</param>
        /// <param name="viewportHeight">보이는 영역 높이.</param>
        /// <returns>ScrollRect.verticalNormalizedPosition 값(1=맨 위, 0=맨 아래).</returns>
        public static float CenteredNormalizedPosition(float rowCenterFromTop, float contentHeight, float viewportHeight)
        {
            var range = contentHeight - viewportHeight;
            if (range <= 0f)
            {
                return 1f;
            }

            var offset = rowCenterFromTop - viewportHeight * 0.5f;
            if (offset < 0f) offset = 0f;
            if (offset > range) offset = range;
            return 1f - offset / range;
        }
    }
}
