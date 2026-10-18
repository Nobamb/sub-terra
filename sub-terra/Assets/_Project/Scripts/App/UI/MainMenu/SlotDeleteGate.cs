namespace SubTerra.App.UI.MainMenu
{
    /// <summary>
    /// B-150 슬롯 삭제 확인 게이트. 확인 전에는 대상 슬롯만 기억하고,
    /// 파일 삭제는 Confirm이 돌려준 슬롯으로 호출자가 수행한다.
    /// </summary>
    public sealed class SlotDeleteGate
    {
        private int pendingSlotId;

        public bool IsAwaitingConfirm => pendingSlotId > 0;
        public int PendingSlotId => pendingSlotId;

        /// <summary>기록이 있는 슬롯(정상·백업·손상)만 삭제 확인을 연다.</summary>
        public static bool CanDelete(SlotContinueEligibility eligibility)
        {
            return SlotContinuePolicy.RequiresOverwriteConfirm(eligibility);
        }

        public bool Request(int slotId, SlotContinueEligibility eligibility)
        {
            if (slotId < 1 || !CanDelete(eligibility))
            {
                pendingSlotId = 0;
                return false;
            }

            pendingSlotId = slotId;
            return true;
        }

        /// <summary>확인. 삭제할 슬롯 번호를 돌려주며, 대기 중이 아니면 0.</summary>
        public int Confirm()
        {
            var slot = pendingSlotId;
            pendingSlotId = 0;
            return slot;
        }

        public void Cancel()
        {
            pendingSlotId = 0;
        }
    }
}
