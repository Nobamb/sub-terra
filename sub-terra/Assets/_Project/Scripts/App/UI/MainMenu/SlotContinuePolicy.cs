using SubTerra.App.Save;

namespace SubTerra.App.UI.MainMenu
{
    /// <summary>이어하기 버튼·문구에 쓰는 슬롯 판정. LoadService 결과와 1:1로 맞춘다.</summary>
    public enum SlotContinueEligibility
    {
        Empty = 0,
        Ready = 1,
        RecoverableFromBackup = 2,
        Unrecoverable = 3,
        InvalidSlot = 4
    }

    /// <summary>
    /// Phase K LoadStatus → Main Menu 이어하기/덮어쓰기 정책.
    /// 손상 슬롯은 무조건 새 게임으로 덮지 않고 오류 표시만 한다.
    /// </summary>
    public static class SlotContinuePolicy
    {
        public static SlotContinueEligibility FromLoadStatus(LoadStatus status)
        {
            switch (status)
            {
                case LoadStatus.Success:
                    return SlotContinueEligibility.Ready;
                case LoadStatus.RecoveredFromBackup:
                    return SlotContinueEligibility.RecoverableFromBackup;
                case LoadStatus.NotFound:
                    return SlotContinueEligibility.Empty;
                case LoadStatus.InvalidSlot:
                    return SlotContinueEligibility.InvalidSlot;
                default:
                    return SlotContinueEligibility.Unrecoverable;
            }
        }

        public static SlotContinueEligibility FromMetadata(SaveSlotMetadata metadata)
        {
            if (metadata == null)
            {
                return SlotContinueEligibility.InvalidSlot;
            }

            return FromLoadStatus(metadata.LoadStatus);
        }

        public static bool CanContinue(SlotContinueEligibility eligibility)
        {
            return eligibility == SlotContinueEligibility.Ready
                || eligibility == SlotContinueEligibility.RecoverableFromBackup;
        }

        /// <summary>세이브 파일이 있거나 손상본이 있으면 새 게임 시 명시적 덮어쓰기 확인이 필요하다.</summary>
        public static bool RequiresOverwriteConfirm(SlotContinueEligibility eligibility)
        {
            return eligibility != SlotContinueEligibility.Empty
                && eligibility != SlotContinueEligibility.InvalidSlot;
        }

        public static string Describe(SlotContinueEligibility eligibility)
        {
            switch (eligibility)
            {
                case SlotContinueEligibility.Empty:
                    return "세이브 없음";
                case SlotContinueEligibility.Ready:
                    return "이어하기 가능";
                case SlotContinueEligibility.RecoverableFromBackup:
                    return "백업으로 복구 가능";
                case SlotContinueEligibility.Unrecoverable:
                    return "세이브 손상 — 복구 불가";
                default:
                    return "잘못된 슬롯";
            }
        }
    }
}
