using System;
using System.Collections.Generic;
using System.Text;
using static SubTerra.Shared.Localization.LocalizationService;
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
        [SerializeField] private string plannedRequirementEnglish;
        [Tooltip("업그레이드 창 썸네일. 비어 있으면 레벨별 단색 칸으로 대체한다.")]
        [SerializeField] private Sprite thumbnail;
        [Tooltip("썸네일과 같은 크기의 투명 PNG. 광물 자리만 불투명하게 칠한다. 비어 있으면 반짝임을 끈다.")]
        [SerializeField] private Sprite mineralMask;
        public int MinimumDepth => minimumDepth;
        public int MaximumDepth => maximumDepth;
        public bool AvailableInDemo => availableInDemo;
        public string PlannedRequirement => plannedRequirement ?? string.Empty;
        public string LocalizedPlannedRequirement => string.IsNullOrWhiteSpace(plannedRequirementEnglish)
            ? PlannedRequirement : Text(PlannedRequirement, plannedRequirementEnglish);
        public Sprite Thumbnail => thumbnail;
        public Sprite MineralMask => mineralMask;

        public DeepZoneDisplayLevel(int minimumDepth, int maximumDepth, bool availableInDemo, string plannedRequirement)
        {
            this.minimumDepth = minimumDepth;
            this.maximumDepth = maximumDepth;
            this.availableInDemo = availableInDemo;
            this.plannedRequirement = plannedRequirement;
        }

#if UNITY_EDITOR
        public void EditorSetArt(Sprite thumbnailSprite, Sprite mask)
        {
            thumbnail = thumbnailSprite;
            mineralMask = mask;
        }
#endif
    }

    /// <summary>표시 전용 단계 정의. 실제 해금·탐사 범위·저장 상태는 변경하지 않는다.</summary>
    [CreateAssetMenu(menuName = "SubTerra/Progression/Deep Zone Display Settings")]
    public sealed class DeepZoneDisplaySettings : ScriptableObject
    {
        public const string ResourcePath = "DeepZoneDisplaySettings";
        [SerializeField] private DeepZoneDisplayLevel[] levels = Array.Empty<DeepZoneDisplayLevel>();
        [SerializeField] private DeepZoneThumbnailStyle thumbnailStyle = new DeepZoneThumbnailStyle();
        public IReadOnlyList<DeepZoneDisplayLevel> Levels => levels;
        public DeepZoneThumbnailStyle ThumbnailStyle => thumbnailStyle ?? (thumbnailStyle = new DeepZoneThumbnailStyle());

        public static string Describe(ZoneAccessResult access, bool compact)
        {
            var settings = Resources.Load<DeepZoneDisplaySettings>(ResourcePath);
            return settings != null ? settings.BuildText(access, compact)
                : Text("심층 구역", "Deep Zone") + " · " + (access.IsUnlocked ? Text("해금됨", "Unlocked") : Text("잠금", "Locked"))
                    + "\n" + DeepZoneUnlockRule.Mvp.DescribeUpgradeRequirements();
        }

        /// <summary>표시상 해금된 레벨 수. 지금은 기존 해금 1개만 Lv.1로 본다(#50에서 Lv.2·3이 생기면 여기만 바꾼다).</summary>
        public int UnlockedLevelCount(ZoneAccessResult access) => access.IsUnlocked && levels.Length > 0 ? 1 : 0;

        /// <summary>업그레이드 창 썸네일 칸 위 한 줄 요약.</summary>
        public string BuildSummary(ZoneAccessResult access)
        {
            var current = UnlockedLevelCount(access);
            var text = Text("심층 구역 Lv.", "Deep Zone Lv.") + current + "/" + levels.Length;
            return current == levels.Length && current > 0 ? text + Text(" · 모든 구역 해금", " · All Zones Unlocked") : text;
        }

        /// <summary>썸네일 아래 띠 첫 줄. 예: "Lv.2 · 41~75M (예정)"</summary>
        public string LevelTitle(int index)
        {
            var level = levels[index];
            var text = "Lv." + (index + 1) + " · " + Range(level);
            return level.AvailableInDemo ? text : text + Text(" (예정)", " (Planned)");
        }

        /// <summary>썸네일 아래 띠 둘째 줄. 해금됨 / Lv.1 실제 조건 / 이후 레벨 예정 조건.</summary>
        public string LevelCondition(int index, bool unlocked)
        {
            if (unlocked) return Text("해금됨", "Unlocked");
            return index == 0 ? DeepZoneUnlockRule.Mvp.DescribeUpgradeRequirements() : levels[index].LocalizedPlannedRequirement;
        }

        public string BuildText(ZoneAccessResult access, bool compact)
        {
            var current = UnlockedLevelCount(access);
            var text = new StringBuilder(Text("심층 구역 Lv.", "Deep Zone Lv.")).Append(current).Append('/').Append(levels.Length);
            if (current > 0) text.Append(" · ").Append(Range(levels[0])).Append(Text(" · 해금됨", " · Unlocked"));
            else text.Append(Text(" · 잠금", " · Locked"));
            if (current == levels.Length && current > 0) text.Append(Text(" · 모든 구역 해금", " · All Zones Unlocked"));
            if (compact)
            {
                text.Append('\n');
                for (var i = 0; i < levels.Length; i++)
                {
                    if (i > 0) text.Append("  /  ");
                    text.Append("Lv.").Append(i + 1).Append(' ').Append(Range(levels[i]));
                    if (!levels[i].AvailableInDemo) text.Append(Text(" (예정)", " (Planned)"));
                }
                return text.Append('\n').Append(DeepZoneUnlockRule.Mvp.DescribeUpgradeRequirements()).ToString();
            }
            for (var i = 0; i < levels.Length; i++)
            {
                var level = levels[i];
                text.Append("\n\nLv.").Append(i + 1).Append(" · ").Append(Range(level));
                text.Append(i < current ? Text(" · 해금", " · Unlocked")
                    : level.AvailableInDemo ? Text(" · 잠금", " · Locked") : Text(" · 예정 (Demo 미지원)", " · Planned (Not in Demo)"));
                text.Append('\n').Append(i == 0 ? Text("해금 조건: ", "Requires: ") + DeepZoneUnlockRule.Mvp.DescribeUpgradeRequirements()
                    : Text("예정 조건: ", "Planned Requirements: ") + level.LocalizedPlannedRequirement);
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
