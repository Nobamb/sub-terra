using System.Collections.Generic;
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

        public bool IsMaximumLevel => CurrentLevel >= MaximumLevel;

        public UpgradeSnapshot(
            string upgradeId,
            string displayName,
            int currentLevel,
            int maximumLevel,
            float currentEffectValue,
            float nextEffectValue,
            IReadOnlyList<ItemCostDto> nextCosts,
            bool canAffordNextLevel)
        {
            UpgradeId = upgradeId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            CurrentLevel = currentLevel;
            MaximumLevel = maximumLevel;
            CurrentEffectValue = currentEffectValue;
            NextEffectValue = nextEffectValue;
            NextCosts = nextCosts ?? System.Array.Empty<ItemCostDto>();
            CanAffordNextLevel = canAffordNextLevel;
        }
    }
}
