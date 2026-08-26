namespace SubTerra.Shared
{
    /// <summary>
    /// 시설 건설 Preview 활성 여부를 UI 입력 계층이 공유하기 위한 게이트.
    /// </summary>
    public static class BuildingPlacementActivity
    {
        private static int activeCount;

        public static bool IsActive => activeCount > 0;

        public static void Begin()
        {
            activeCount++;
        }

        public static void End()
        {
            if (activeCount > 0)
            {
                activeCount--;
            }
        }

        /// <summary>테스트·도메인 리로드 후 카운터를 초기화한다.</summary>
        public static void ResetForTests()
        {
            activeCount = 0;
        }
    }
}
