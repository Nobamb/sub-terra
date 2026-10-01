using System.Collections.Generic;
using SubTerra.App.Core.Data;
using SubTerra.Shared;

namespace SubTerra.App.Progression
{
    /// <summary>UI가 읽는 업그레이드 한 항목. 변경 가능한 State/에셋 참조를 노출하지 않는다.</summary>
    public readonly struct UpgradeSnapshot
    {
        public string UpgradeId { get; }
        public string DisplayName { get; }
        public int CurrentLevel { get; }
        public int MaximumLevel { get; }
        public float CurrentEffectValue { get; }
        public float NextEffectValue { get; }
        public IReadOnlyList<ItemCostDto> NextCosts { get; }
        public bool CanAffordNextLevel { get; }
        public IReadOnlyList<MineralBonusEntry> CurrentMiningYieldBonuses { get; }
        public IReadOnlyList<MineralBonusEntry> NextMiningYieldBonuses { get; }
        /// <summary>트리 해금 여부. 기존 호출부 호환을 위해 기본값은 해금 상태다.</summary>
        public bool IsUnlocked { get; }
        /// <summary>미해금일 때만 채워지는 조건 문구(예: "드릴 속도 Lv.2 필요").</summary>
        public string LockedReason { get; }
        public string TreeParentId { get; }
        public string UnlockRequirementUpgradeId { get; }
        public int UnlockRequiredLevel { get; }
        /// <summary>다음 레벨 비용 중 보유량이 모자란 항목과 부족 수량. 부족이 없으면 비어 있다.</summary>
        public IReadOnlyList<ItemCostDto> NextCostShortages { get; }

        public bool IsMaximumLevel => CurrentLevel >= MaximumLevel;

        public UpgradeSnapshot(
            string upgradeId,
            string displayName,
            int currentLevel,
            int maximumLevel,
            float currentEffectValue,
            float nextEffectValue,
            IReadOnlyList<ItemCostDto> nextCosts,
            bool canAffordNextLevel,
            IReadOnlyList<MineralBonusEntry> currentMiningYieldBonuses = null,
            IReadOnlyList<MineralBonusEntry> nextMiningYieldBonuses = null,
            bool isUnlocked = true,
            string lockedReason = null,
            string treeParentId = null,
            IReadOnlyList<ItemCostDto> nextCostShortages = null,
            string unlockRequirementUpgradeId = null,
            int unlockRequiredLevel = 0)
        {
            UpgradeId = upgradeId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            CurrentLevel = currentLevel;
            MaximumLevel = maximumLevel;
            CurrentEffectValue = currentEffectValue;
            NextEffectValue = nextEffectValue;
            NextCosts = nextCosts ?? System.Array.Empty<ItemCostDto>();
            CanAffordNextLevel = canAffordNextLevel;
            CurrentMiningYieldBonuses = currentMiningYieldBonuses
                ?? System.Array.Empty<MineralBonusEntry>();
            NextMiningYieldBonuses = nextMiningYieldBonuses
                ?? System.Array.Empty<MineralBonusEntry>();
            IsUnlocked = isUnlocked;
            LockedReason = lockedReason ?? string.Empty;
            TreeParentId = treeParentId ?? string.Empty;
            NextCostShortages = nextCostShortages ?? System.Array.Empty<ItemCostDto>();
            UnlockRequirementUpgradeId = unlockRequirementUpgradeId ?? string.Empty;
            UnlockRequiredLevel = unlockRequiredLevel < 0 ? 0 : unlockRequiredLevel;
        }
    }
}
