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
        // prompt-B 118: 트리 해금 조건. 비어 있으면(기존 에셋 기본값) 처음부터 해금 상태다.
        [SerializeField] private string unlockRequirementUpgradeId;
        [SerializeField, Min(0)] private int unlockRequiredLevel;
        [SerializeField] private string treeParentId;

        public string Id => id;
        public string DisplayName => displayName;
        public int MaxLevel => maxLevel;
        public IReadOnlyList<UpgradeLevelDefinition> Levels => levels;
        /// <summary>해금에 필요한 업그레이드 ID. 비어 있으면 조건 없음.</summary>
        public string UnlockRequirementUpgradeId => unlockRequirementUpgradeId ?? string.Empty;
        /// <summary>해금에 필요한 위 업그레이드의 최소 레벨.</summary>
        public int UnlockRequiredLevel => unlockRequiredLevel < 0 ? 0 : unlockRequiredLevel;
        /// <summary>트리 UI에서 연결선이 이어지는 상위 노드 ID. 비어 있으면 루트.</summary>
        public string TreeParentId => treeParentId ?? string.Empty;

#if UNITY_EDITOR
        public void EditorSetUnlock(string requirementUpgradeId, int requiredLevel, string parentId)
        {
            unlockRequirementUpgradeId = requirementUpgradeId ?? string.Empty;
            unlockRequiredLevel = requiredLevel < 0 ? 0 : requiredLevel;
            treeParentId = parentId ?? string.Empty;
        }

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
