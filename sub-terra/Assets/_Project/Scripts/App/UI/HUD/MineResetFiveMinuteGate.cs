using System;

namespace SubTerra.App.UI.HUD
{
    /// <summary>
    /// 남은 시간 5분 알림을 낼지 정한다(B-163). 직전 관측이 300초보다 크고 이번 값이 61~300초일 때(GetBand가 Critical로 바뀌는 순간)만 낸다.
    /// 첫 관측(불러오기·씬 진입), 시간 증가(초기화), 10초 넘는 큰 건너뛰기는 무음이다.
    /// 300초 아래로 내려온 뒤에는 다시 300초를 넘기 전까지 조건이 성립하지 않아 한 주기에 한 번만 난다.
    /// </summary>
    public sealed class MineResetFiveMinuteGate
    {
        public const int AlertSeconds = 300;
        public const int FinalSeconds = 60;
        public const int MaxJump = 10;

        private int previous = -1;

        /// <summary>시계 표시(MineResetService.FormatClock)와 같은 방식으로 정수 초로 내린다.</summary>
        public static int ToSeconds(double remainingSeconds)
        {
            return (int)Math.Floor(Math.Max(0d, remainingSeconds) + 0.0000001d);
        }

        /// <summary>이번 관측 값을 기록하고, 알림을 낼 순간이면 true.</summary>
        public bool Observe(int seconds)
        {
            var last = previous;
            previous = seconds;
            if (last < 0 || last <= AlertSeconds)
            {
                return false;
            }

            return seconds <= AlertSeconds
                && seconds > FinalSeconds
                && last - seconds <= MaxJump;
        }

        /// <summary>다음 관측을 첫 관측으로 본다.</summary>
        public void Reset()
        {
            previous = -1;
        }
    }
}
