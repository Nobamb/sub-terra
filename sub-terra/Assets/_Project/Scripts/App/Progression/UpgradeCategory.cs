namespace SubTerra.App.Progression
{
    /// <summary>업그레이드 탭 분류. UI가 카탈로그 전체를 한 목록에 몰아넣지 않도록 한다.</summary>
    public enum UpgradeCategory
    {
        Drill = 0,
        Capacity = 1,
        Drone = 2,
        Hazard = 3,
        /// <summary>prompt-B 33-3: 심층 구역 잠금·조건은 전용 탭에서만 표시한다.</summary>
        DeepZone = 4
    }

    public static class UpgradeCategoryRules
    {
        public static readonly string[] TabLabels =
        {
            "드릴",
            "전력·체력·화물",
            "드론",
            "가스",
            "심층 구역"
        };

        public static UpgradeCategory Resolve(string upgradeId)
        {
            if (string.IsNullOrEmpty(upgradeId))
            {
                return UpgradeCategory.Drill;
            }

            if (upgradeId.StartsWith("upgrade.drill."))
            {
                return UpgradeCategory.Drill;
            }

            if (upgradeId.StartsWith("upgrade.energy.")
                || upgradeId.StartsWith("upgrade.health.")
                || upgradeId.StartsWith("upgrade.cargo."))
            {
                return UpgradeCategory.Capacity;
            }

            if (upgradeId.StartsWith("upgrade.drone."))
            {
                return UpgradeCategory.Drone;
            }

            if (upgradeId.StartsWith("upgrade.gas."))
            {
                return UpgradeCategory.Hazard;
            }

            // 심층 구역은 카탈로그 업그레이드 ID가 아니라 탭 전용 상태다.
            return UpgradeCategory.Drill;
        }

        public static bool Matches(string upgradeId, UpgradeCategory category)
        {
            // 심층 탭은 개별 업그레이드 목록을 쓰지 않는다.
            if (category == UpgradeCategory.DeepZone)
            {
                return false;
            }

            return Resolve(upgradeId) == category;
        }

        public static bool IsDeepZoneTab(UpgradeCategory category)
        {
            return category == UpgradeCategory.DeepZone;
        }
    }
}
