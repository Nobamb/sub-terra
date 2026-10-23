using System;
using UnityEngine;

namespace SubTerra.App.UI.HUD
{
    /// <summary>
    /// 막판(1~60초) 째깍을 낼지 정한다(B-161). 표시 초가 1~3초 줄었고 마지막 째깍 뒤 실제 시간이 MinInterval 이상 흘렀을 때만 낸다.
    /// 프레임이 튀어 2~3초가 한 번에 줄어도 놓치지 않고, 같은 순간에 두 번 줄어도 한 번만 낸다.
    /// 첫 관측·증가·0초·시간 건너뛰기(4초 이상 감소)는 무음이다. 시계는 주입할 수 있고 기본은 비스케일 실제 시각이다.
    /// </summary>
    public sealed class MineResetClockTickGate
    {
        public const int FinalSeconds = 60;
        public const int MaxDecrement = 3;
        public const float MinInterval = 0.9f;

        private float lastTickAt;
        private bool ticked;

        public MineResetClockTickGate(Func<float> clock = null)
        {
            Clock = clock ?? (() => Time.realtimeSinceStartup);
        }

        public Func<float> Clock { get; set; }

        /// <summary>표시 초 변화(previous → next)가 막판 째깍 대상인지. 간격은 보지 않는다.</summary>
        public static bool IsFinalDecrement(int previous, int next)
        {
            var decrement = previous - next;
            return previous >= 0 && decrement >= 1 && decrement <= MaxDecrement
                && next >= 1 && next <= FinalSeconds;
        }

        public bool TryTick(int previous, int next, bool poweredOn)
        {
            return TryTick(previous, next, poweredOn, Clock());
        }

        /// <summary>낼 차례면 이 시각을 기록하고 true.</summary>
        public bool TryTick(int previous, int next, bool poweredOn, float now)
        {
            if (!poweredOn || !IsFinalDecrement(previous, next))
            {
                return false;
            }

            if (ticked && now - lastTickAt < MinInterval)
            {
                return false;
            }

            ticked = true;
            lastTickAt = now;
            return true;
        }

        public void Reset()
        {
            ticked = false;
        }
    }
}
