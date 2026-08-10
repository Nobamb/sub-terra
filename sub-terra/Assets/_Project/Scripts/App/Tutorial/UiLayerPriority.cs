namespace SubTerra.App.Tutorial
{
    /// <summary>
    /// 튜토리얼 안내와 긴급(구조·가스) UI의 표시 우선순위.
    /// 위험 경고 sort order가 항상 일반 안내보다 커야 한다.
    /// </summary>
    public static class UiLayerPriority
    {
        public const int TutorialGuidance = 100;
        public const int SettingsModal = 800;
        /// <summary>장비 업그레이드·인벤토리 등 플레이어 호출 모달. 일반 HUD/튜토리얼 위.</summary>
        public const int ModalPanel = 1_000;
        public const int HazardWarning = 500;
        public const int CriticalHazard = 600;

        /// <summary>긴급 경고가 일반 튜토리얼 안내보다 위에 와야 하는지.</summary>
        public static bool HazardBeatsTutorial(int hazardSortOrder, int tutorialSortOrder)
        {
            return hazardSortOrder > tutorialSortOrder;
        }

        /// <summary>위험 중 튜토리얼 패널이 입력을 가로채지 않도록 해야 하는지.</summary>
        public static bool ShouldYieldTutorialInput(bool hazardActive)
        {
            return hazardActive;
        }

        public static int ResolveHazardSortOrder(bool isCritical)
        {
            return isCritical ? CriticalHazard : HazardWarning;
        }
    }
}
