namespace SubTerra.App.UI.MainMenu
{
    public enum NewGameRequestStatus
    {
        ReadyToStart = 0,
        AwaitingOverwriteConfirm = 1,
        Cancelled = 2,
        InvalidSlot = 3,
        Failed = 4
    }

    /// <summary>
    /// 새 게임 덮어쓰기 확인 게이트. 파일·State 변경은 하지 않으며,
    /// 취소 시 부작용 없이 대기 상태만 해제한다(소유권: App/UI).
    /// </summary>
    public sealed class NewGameOverwriteGate
    {
        private int pendingSlotId;
        private bool awaitingConfirm;

        public bool IsAwaitingConfirm => awaitingConfirm;
        public int PendingSlotId => pendingSlotId;

        public NewGameRequestStatus Request(
            int slotId,
            SlotContinueEligibility eligibility)
        {
            if (eligibility == SlotContinueEligibility.InvalidSlot
                || slotId < 1)
            {
                awaitingConfirm = false;
                pendingSlotId = 0;
                return NewGameRequestStatus.InvalidSlot;
            }

            if (SlotContinuePolicy.RequiresOverwriteConfirm(eligibility))
            {
                pendingSlotId = slotId;
                awaitingConfirm = true;
                return NewGameRequestStatus.AwaitingOverwriteConfirm;
            }

            pendingSlotId = slotId;
            awaitingConfirm = false;
            return NewGameRequestStatus.ReadyToStart;
        }

        /// <summary>덮어쓰기 확인. ReadyToStart만 반환하며 실제 Start는 호출자가 수행한다.</summary>
        public NewGameRequestStatus ConfirmOverwrite()
        {
            if (!awaitingConfirm || pendingSlotId < 1)
            {
                return NewGameRequestStatus.Failed;
            }

            awaitingConfirm = false;
            return NewGameRequestStatus.ReadyToStart;
        }

        /// <summary>확인 취소. 파일/런타임 State를 건드리지 않는다.</summary>
        public NewGameRequestStatus CancelOverwrite()
        {
            awaitingConfirm = false;
            pendingSlotId = 0;
            return NewGameRequestStatus.Cancelled;
        }

        public void Clear()
        {
            awaitingConfirm = false;
            pendingSlotId = 0;
        }
    }
}
