using System;

namespace SubTerra.App.UI.Progression
{
    /// <summary>
    /// B-155: 심층 구역 썸네일 중 어디까지 컬러로 그렸는지와, 해금 연출을 기다리는 레벨을 런타임 메모리로만 추적한다.
    /// 처음 그릴 때와 DidUnlockNow가 아닌 증가는 연출 없이 바로 반영하고, 해금 직후 증가만 연출 대기로 둔다.
    /// 저장하지 않으므로 다시 켜거나 세이브를 불러오면 연출 없이 컬러로 보인다.
    /// </summary>
    public sealed class DeepZoneRevealTracker
    {
        private int revealed = -1;
        private int target;

        public bool HasDrawn => revealed >= 0;
        /// <summary>컬러(해금)로 그린 레벨 수. 연출 대기 레벨은 포함하지 않는다.</summary>
        public int Revealed => Math.Max(0, revealed);
        /// <summary>실제 표시상 해금 레벨 수(연출 대기 포함).</summary>
        public int Target => target;
        public bool HasPending => revealed >= 0 && revealed < target;

        public bool IsRevealed(int index) => index < Revealed;
        public bool IsPending(int index) => HasPending && index >= revealed && index < target;

        public void Apply(int unlockedCount, bool didUnlockNow)
        {
            unlockedCount = Math.Max(0, unlockedCount);
            if (revealed < 0)
            {
                revealed = target = unlockedCount;
                return;
            }

            if (unlockedCount < target)
            {
                target = unlockedCount;
                revealed = Math.Min(revealed, unlockedCount);
                return;
            }

            if (unlockedCount == target)
            {
                return;
            }

            // 해금 직후가 아니면 바로 컬러. 이미 대기 중인 연출이 있으면 뒤 레벨도 함께 연출한다.
            if (!didUnlockNow && !HasPending)
            {
                revealed = unlockedCount;
            }

            target = unlockedCount;
        }

        /// <summary>대기 중인 연출 범위 [from, to)를 꺼내고 대기를 지운다. 한 번 꺼낸 범위는 다시 나오지 않는다.</summary>
        public bool TryTakePending(out int from, out int to)
        {
            from = Revealed;
            to = target;
            if (!HasPending)
            {
                return false;
            }

            revealed = target;
            return true;
        }
    }
}
