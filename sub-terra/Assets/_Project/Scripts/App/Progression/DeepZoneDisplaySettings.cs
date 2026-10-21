using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SubTerra.App.Progression
{
    [Serializable]
    public sealed class DeepZoneDisplayLevel
    {
        [SerializeField] private int minimumDepth;
        [SerializeField] private int maximumDepth;
        [SerializeField] private bool availableInDemo;
        [SerializeField] private string plannedRequirement;
        public int MinimumDepth => minimumDepth;
        public int MaximumDepth => maximumDepth;
        public bool AvailableInDemo => availableInDemo;
        public string PlannedRequirement => plannedRequirement ?? string.Empty;

        public DeepZoneDisplayLevel(int minimumDepth, int maximumDepth, bool availableInDemo, string plannedRequirement)
        {
            this.minimumDepth = minimumDepth;
            this.maximumDepth = maximumDepth;
            this.availableInDemo = availableInDemo;
            this.plannedRequirement = plannedRequirement;
        }
    }

    /// <summary>표시 전용 단계 정의. 실제 해금·탐사 범위·저장 상태는 변경하지 않는다.</summary>
    [CreateAssetMenu(menuName = "SubTerra/Progression/Deep Zone Display Settings")]
    public sealed class DeepZoneDisplaySettings : ScriptableObject
    {
        public const string ResourcePath = "DeepZoneDisplaySettings";
        [SerializeField] private DeepZoneDisplayLevel[] levels = Array.Empty<DeepZoneDisplayLevel>();
        public IReadOnlyList<DeepZoneDisplayLevel> Levels => levels;

        public static string Describe(ZoneAccessResult access, bool compact)
        {
            var settings = Resources.Load<DeepZoneDisplaySettings>(ResourcePath);
            return settings != null ? settings.BuildText(access, compact)
                : "심층 구역 · " + (access.IsUnlocked ? "해금됨" : "잠금") + "\n" + DeepZoneUnlockRule.Mvp.DescribeUpgradeRequirements();
        }

        public string BuildText(ZoneAccessResult access, bool compact)
        {
            var current = access.IsUnlocked && levels.Length > 0 ? 1 : 0;
            var text = new StringBuilder("심층 구역 Lv.").Append(current).Append('/').Append(levels.Length);
            if (current > 0) text.Append(" · ").Append(Range(levels[0])).Append(" · 해금됨");
            else text.Append(" · 잠금");
            if (current == levels.Length && current > 0) text.Append(" · 모든 구역 해금");
            if (compact)
            {
                text.Append('\n');
                for (var i = 0; i < levels.Length; i++)
                {
                    if (i > 0) text.Append("  /  ");
                    text.Append("Lv.").Append(i + 1).Append(' ').Append(Range(levels[i]));
                    if (!levels[i].AvailableInDemo) text.Append(" (예정)");
                }
                return text.Append('\n').Append(DeepZoneUnlockRule.Mvp.DescribeUpgradeRequirements()).ToString();
            }
            for (var i = 0; i < levels.Length; i++)
            {
                var level = levels[i];
                text.Append("\n\nLv.").Append(i + 1).Append(" · ").Append(Range(level));
                text.Append(i < current ? " · 해금" : level.AvailableInDemo ? " · 잠금" : " · 예정 (Demo 미지원)");
                text.Append('\n').Append(i == 0 ? "해금 조건: " + DeepZoneUnlockRule.Mvp.DescribeUpgradeRequirements()
                    : "예정 조건: " + level.PlannedRequirement);
            }
            if (!access.IsUnlocked && !string.IsNullOrEmpty(access.Reason)) text.Append("\n\n").Append(access.Reason);
            return text.ToString();
        }

        private static string Range(DeepZoneDisplayLevel level) => level.MinimumDepth + "~" + level.MaximumDepth + "M";

#if UNITY_EDITOR
        public void EditorSet(DeepZoneDisplayLevel[] values) => levels = values ?? Array.Empty<DeepZoneDisplayLevel>();
#endif
    }
}
