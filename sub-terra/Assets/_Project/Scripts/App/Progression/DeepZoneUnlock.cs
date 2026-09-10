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

        /// <summary>앞선 목표 완료 + 필수 진행인 드릴 2레벨. 선택 성능 업그레이드는 강제하지 않는다.</summary>
        public static DeepZoneUnlockRule Mvp { get; } = new DeepZoneUnlockRule(
            13,
            new[]
            {
                new UpgradeLevelRequirement(DataIds.Upgrades.DrillSpeed, 2)
            });
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
