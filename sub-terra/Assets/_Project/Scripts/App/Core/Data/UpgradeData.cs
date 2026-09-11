using System;
using System.Collections.Generic;
using UnityEngine;

namespace SubTerra.App.Core.Data
{
    /// <summary>
    /// 채굴 타일 1칸당 광물 보너스. 비용 항목과 섞이지 않도록 별도 타입이다.
    /// </summary>
    [Serializable]
    public sealed class MineralBonusEntry
    {
        [SerializeField] private string mineralId;
        [SerializeField] private int quantity;

        public string MineralId => mineralId;
        public int Quantity => quantity;

        public MineralBonusEntry() { }

        public MineralBonusEntry(string mineralId, int quantity)
        {
            this.mineralId = mineralId ?? string.Empty;
            this.quantity = quantity;
        }
    }

    [Serializable]
    public sealed class UpgradeLevelDefinition
    {
        [SerializeField] private int level = 1;
        [SerializeField] private List<ItemCostEntry> costs = new List<ItemCostEntry>();
        [SerializeField] private float effectValue;
        [SerializeField] private List<MineralBonusEntry> miningYieldBonuses
            = new List<MineralBonusEntry>();

        public int Level => level;
        public IReadOnlyList<ItemCostEntry> Costs => costs;
        public float EffectValue => effectValue;
        public IReadOnlyList<MineralBonusEntry> MiningYieldBonuses => miningYieldBonuses;

        public UpgradeLevelDefinition() { }

        public UpgradeLevelDefinition(int level, float effectValue, List<ItemCostEntry> costs)
            : this(level, effectValue, costs, null)
        {
        }

        public UpgradeLevelDefinition(
            int level,
            float effectValue,
            List<ItemCostEntry> costs,
            List<MineralBonusEntry> miningYieldBonuses)
        {
            this.level = level;
            this.effectValue = effectValue;
            this.costs = costs ?? new List<ItemCostEntry>();
            this.miningYieldBonuses = miningYieldBonuses ?? new List<MineralBonusEntry>();
        }

        public int GetMiningYieldBonus(string mineralId)
        {
            if (string.IsNullOrEmpty(mineralId) || miningYieldBonuses == null)
            {
                return 0;
            }

            for (var i = 0; i < miningYieldBonuses.Count; i++)
            {
                var entry = miningYieldBonuses[i];
                if (entry == null || entry.MineralId != mineralId)
                {
                    continue;
                }

                return entry.Quantity < 1 ? 0 : entry.Quantity;
            }

            return 0;
        }
    }

    /// <summary>
    /// 업그레이드 정적 정의. 현재 보유 레벨은 넣지 않는다(플레이어 상태).
    /// </summary>
    [CreateAssetMenu(fileName = "UpgradeData", menuName = "SubTerra/Data/Upgrade", order = 40)]
    public sealed class UpgradeData : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private int maxLevel = 1;
        [SerializeField] private List<UpgradeLevelDefinition> levels = new List<UpgradeLevelDefinition>();

        public string Id => id;
        public string DisplayName => displayName;
        public int MaxLevel => maxLevel;
        public IReadOnlyList<UpgradeLevelDefinition> Levels => levels;

#if UNITY_EDITOR
        public void EditorSet(
            string permanentId,
            string name,
            int max,
            List<UpgradeLevelDefinition> levelDefs)
        {
            id = permanentId;
            displayName = name;
            maxLevel = max;
            levels = levelDefs ?? new List<UpgradeLevelDefinition>();
        }
#endif
    }
}
