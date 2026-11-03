using System;
using SubTerra.App.Audio;

namespace SubTerra.App.Integration
{
    /// <summary>
    /// B-181: 구조 순간이동 소리. 워프 한 번 → 실제 시간 RescueArriveDelay 뒤 도착 "딩" 한 번.
    /// 기다리는 동안 씬이 바뀌거나(Cancel) 탐사가 끝나면(runActive false) 도착음은 버린다.
    /// 소리 id·간격(★)은 EmergencyRescueRuntimeController 상수에 둔다.
    /// </summary>
    public sealed class EmergencyRescueArrivalSfx
    {
        private readonly Func<float> clock;
        private float pendingAt = -1f;

        public EmergencyRescueArrivalSfx(Func<float> realClock)
        {
            clock = realClock ?? throw new ArgumentNullException(nameof(realClock));
        }

        public bool IsPending => pendingAt >= 0f;

        public void BeginWarp()
        {
            SfxPlayer.Play(EmergencyRescueRuntimeController.RescueWarpSfx);
            pendingAt = clock() + EmergencyRescueRuntimeController.RescueArriveDelay;
        }

        public void Cancel()
        {
            pendingAt = -1f;
        }

        /// <returns>이번에 도착음을 냈으면 true.</returns>
        public bool Tick(bool runActive)
        {
            if (pendingAt < 0f)
            {
                return false;
            }

            if (!runActive)
            {
                pendingAt = -1f;
                return false;
            }

            if (clock() < pendingAt)
            {
                return false;
            }

            pendingAt = -1f;
            SfxPlayer.Play(EmergencyRescueRuntimeController.RescueArriveSfx);
            return true;
        }
    }
}
