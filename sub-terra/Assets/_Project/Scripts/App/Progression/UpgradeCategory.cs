namespace SubTerra.App.Progression
{
    /// <summary>업그레이드 탭 분류. UI가 카탈로그 전체를 한 목록에 몰아넣지 않도록 한다.</summary>
    public enum UpgradeCategory
    {
        Drill = 0,
        Capacity = 1,
        Drone = 2,
        Hazard = 3
    }

    public static class UpgradeCategoryRules
    {
        public static readonly string[] TabLabels =
        {
            "드릴",
            "전력·화물",
            "드론",
            "가스"
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

            return UpgradeCategory.Drill;
        }

        public static bool Matches(string upgradeId, UpgradeCategory category)
        {
            return Resolve(upgradeId) == category;
        }
    }
}
