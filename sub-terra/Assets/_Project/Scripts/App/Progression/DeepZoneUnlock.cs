using System.Collections.Generic;
using SubTerra.App.Core.Data;

namespace SubTerra.App.Progression
{
    public readonly struct UpgradeLevelRequirement
    {
        public string UpgradeId { get; }
        public int RequiredLevel { get; }

        public UpgradeLevelRequirement(string upgradeId, int requiredLevel)
        {
            UpgradeId = upgradeId ?? string.Empty;
            RequiredLevel = requiredLevel;
        }
    }

    /// <summary>심층 구역의 명시적 진행 조건. UI와 접근 게이트가 같은 규칙을 공유한다.</summary>
    public sealed class DeepZoneUnlockRule
    {
        public int RequiredCompletedObjectives { get; }
        public IReadOnlyList<UpgradeLevelRequirement> UpgradeRequirements { get; }

        public DeepZoneUnlockRule(
            int requiredCompletedObjectives,
            IReadOnlyList<UpgradeLevelRequirement> upgradeRequirements)
        {
            RequiredCompletedObjectives = requiredCompletedObjectives < 0 ? 0 : requiredCompletedObjectives;
            UpgradeRequirements = upgradeRequirements ?? System.Array.Empty<UpgradeLevelRequirement>();
        }

        /// <summary>심층 탐사에 필요한 채굴·탐지·가스 대응 장비 조건.</summary>
        public static DeepZoneUnlockRule Mvp { get; } = new DeepZoneUnlockRule(
            0,
            new[]
            {
                new UpgradeLevelRequirement(DataIds.Upgrades.DrillSpeed, 2),
                new UpgradeLevelRequirement(DataIds.Upgrades.DroneScan, 2),
                new UpgradeLevelRequirement(DataIds.Upgrades.GasResistance, 1)
            });

        public string DescribeUpgradeRequirements()
        {
            var parts = new List<string>();
            foreach (var requirement in UpgradeRequirements)
            {
                parts.Add(ItemDisplayNames.Upgrade(requirement.UpgradeId) + " Lv." + requirement.RequiredLevel);
            }

            return string.Join(" · ", parts);
        }
    }

    public readonly struct ZoneAccessResult
    {
        public bool IsUnlocked { get; }
        public bool DidUnlockNow { get; }
        public string Reason { get; }

        public ZoneAccessResult(bool isUnlocked, bool didUnlockNow, string reason)
        {
            IsUnlocked = isUnlocked;
            DidUnlockNow = didUnlockNow;
            Reason = reason ?? string.Empty;
        }
    }
}
