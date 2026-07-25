using System;
using System.Collections.Generic;
using UnityEngine;

namespace SubTerra.App.Core.Data
{
    [Serializable]
    public sealed class UpgradeLevelDefinition
    {
        [SerializeField] private int level = 1;
        [SerializeField] private List<ItemCostEntry> costs = new List<ItemCostEntry>();
        [SerializeField] private float effectValue;

        public int Level => level;
        public IReadOnlyList<ItemCostEntry> Costs => costs;
        public float EffectValue => effectValue;

        public UpgradeLevelDefinition() { }

        public UpgradeLevelDefinition(int level, float effectValue, List<ItemCostEntry> costs)
        {
            this.level = level;
            this.effectValue = effectValue;
            this.costs = costs ?? new List<ItemCostEntry>();
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
