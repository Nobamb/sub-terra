using System;

namespace SubTerra.App.State
{
    /// <summary>
    /// 광산 초기화 주기. 플레이 경과 시간, 유료 초기화 누적 횟수, 시계 표시 여부.
    /// Unity Object 참조 없이 세이브 DTO로만 왕복한다.
    /// </summary>
    [Serializable]
    public sealed class MineResetCycleState
    {
        public double ElapsedSeconds { get; private set; }
        public int PaidResetCount { get; private set; }
        public bool ClockVisible { get; private set; }

        public MineResetCycleState()
            : this(0d, 0, true)
        {
        }

        public MineResetCycleState(double elapsedSeconds, int paidResetCount, bool clockVisible)
        {
            ElapsedSeconds = SanitizeElapsed(elapsedSeconds);
            PaidResetCount = paidResetCount < 0 ? 0 : paidResetCount;
            ClockVisible = clockVisible;
        }

        internal void AddElapsed(double deltaSeconds)
        {
            if (deltaSeconds <= 0d
                || double.IsNaN(deltaSeconds)
                || double.IsInfinity(deltaSeconds))
            {
                return;
            }

            ElapsedSeconds += deltaSeconds;
        }

#if UNITY_EDITOR || SUBTERRA_BUILD_DEVELOPMENT
        /// <summary>일시정지·양수 델타 규칙과 별개로 경과 시간을 직접 쓴다.</summary>
        internal bool SetElapsedSeconds(double elapsedSeconds)
        {
            if (double.IsNaN(elapsedSeconds) || double.IsInfinity(elapsedSeconds))
            {
                return false;
            }

            ElapsedSeconds = elapsedSeconds < 0d ? 0d : elapsedSeconds;
            return true;
        }
#endif

        internal void ApplyPaidReset()
        {
            ElapsedSeconds = 0d;
            if (PaidResetCount < int.MaxValue)
            {
                PaidResetCount++;
            }
        }

        internal void ApplyTimedReset()
        {
            ElapsedSeconds = 0d;
            PaidResetCount = 0;
        }

        internal void SetClockVisible(bool visible)
        {
            ClockVisible = visible;
        }

        private static double SanitizeElapsed(double elapsedSeconds)
        {
            if (elapsedSeconds < 0d
                || double.IsNaN(elapsedSeconds)
                || double.IsInfinity(elapsedSeconds))
            {
                return 0d;
            }

            return elapsedSeconds;
        }
    }
}
