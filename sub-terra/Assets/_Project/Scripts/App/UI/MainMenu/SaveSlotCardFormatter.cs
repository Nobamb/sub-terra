using System;
using SubTerra.App.Save;
using SubTerra.App.Tutorial;

namespace SubTerra.App.UI.MainMenu
{
    /// <summary>슬롯 카드 표시 종류. 빈 슬롯은 삭제 버튼과 썸네일이 없다.</summary>
    public enum SaveSlotCardKind
    {
        Empty = 0,
        Ready = 1,
        Corrupted = 2
    }

    /// <summary>
    /// B-150 슬롯 카드 문구. 골드 / 최대 깊이 / 목표 N/M, 마지막 저장 시각을 만든다.
    /// 세이브에 값이 없으면 "-"로 표시한다.
    /// </summary>
    public static class SaveSlotCardFormatter
    {
        public const string MissingValue = "-";

        public static SaveSlotCardKind KindOf(SlotContinueEligibility eligibility)
        {
            if (SlotContinuePolicy.CanContinue(eligibility))
            {
                return SaveSlotCardKind.Ready;
            }

            return eligibility == SlotContinueEligibility.Unrecoverable
                ? SaveSlotCardKind.Corrupted
                : SaveSlotCardKind.Empty;
        }

        public static string Title(int slotId)
        {
            return "탐사 기록 " + slotId.ToString("00");
        }

        public static string MaximumDepth(SaveSlotMetadata metadata)
        {
            return metadata == null || metadata.MaximumDepth < 0
                ? MissingValue
                : metadata.MaximumDepth + "m";
        }

        public static string Objectives(SaveSlotMetadata metadata)
        {
            return metadata == null || metadata.CompletedObjectives < 0
                ? MissingValue
                : Math.Min(metadata.CompletedObjectives, DemoObjectiveIds.RequiredCount)
                    + "/" + DemoObjectiveIds.RequiredCount;
        }

        public static string SavedAt(SaveSlotMetadata metadata)
        {
            return metadata != null && metadata.SavedAtUtc > 0
                ? DateTimeOffset.FromUnixTimeSeconds(metadata.SavedAtUtc).ToLocalTime().ToString("yyyy.MM.dd  HH:mm")
                : MissingValue;
        }

        public static string BuildLabel(SaveSlotMetadata metadata, SlotContinueEligibility eligibility)
        {
            if (metadata == null)
            {
                return "Slot ?";
            }

            var title = Title(metadata.SlotId);
            switch (KindOf(eligibility))
            {
                case SaveSlotCardKind.Empty:
                    return title
                        + "\n<size=78%><color=#91A7AE>비어 있는 기록</color></size>"
                        + "\n<size=70%><color=#6F858C>새 탐사를 시작하세요.</color></size>";
                case SaveSlotCardKind.Corrupted:
                    return title
                        + "\n<size=78%><color=#E6A23C>기록 손상 · 이어하기 불가</color></size>";
            }

            return title
                + (metadata.IsRecoverableFromBackup ? "  <size=70%><color=#E6A23C>[Backup]</color></size>" : string.Empty)
                + "\n<size=78%><color=#C7DFE3>골드 <color=#FFE66B>" + metadata.Gold + "G</color>"
                + "   ·   최대 깊이 " + MaximumDepth(metadata)
                + "   ·   목표 " + Objectives(metadata) + "</color></size>"
                + "\n<size=70%><color=#91A7AE>마지막 저장  " + SavedAt(metadata) + "</color></size>";
        }
    }
}
