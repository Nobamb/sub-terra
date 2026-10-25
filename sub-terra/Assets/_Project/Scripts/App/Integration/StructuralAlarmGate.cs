using System;
using System.Collections.Generic;
using SubTerra.Gameplay.Structural;
using UnityEngine;

namespace SubTerra.App.Integration
{
    /// <summary>
    /// HUD 위험도 단계가 바뀔 때 낼 소리를 고른다. 오를 때 경보, 내릴 때 틱 한 번이 기본이고,
    /// 경계 칸을 오가며 같은 소리가 반복되지 않도록 단계별·틱별로 쿨타임을 둔다. 시간은 실제 시각이다.
    /// </summary>
    public sealed class StructuralAlarmGate
    {
        public const float RepeatCooldownSeconds = 3f;

        private readonly Func<float> clock;
        private readonly Dictionary<StructuralRiskLevel, float> lastAlarmAt = new Dictionary<StructuralRiskLevel, float>();
        private float lastTickAt;
        private bool hasTick;

        public StructuralAlarmGate(Func<float> clock = null)
        {
            this.clock = clock ?? (() => Time.realtimeSinceStartup);
        }

        public StructuralRiskFeedback.RiskSound Evaluate(StructuralRiskLevel previous, StructuralRiskLevel next)
        {
            var sound = StructuralRiskFeedback.ResolveRiskSound(previous, next);
            var now = clock();
            if (sound == StructuralRiskFeedback.RiskSound.Alarm)
            {
                // 같은 단계로 다시 오르면 그 단계의 마지막 경보 뒤 쿨타임 안에는 무음.
                if (lastAlarmAt.TryGetValue(next, out var last) && now - last < RepeatCooldownSeconds)
                {
                    return StructuralRiskFeedback.RiskSound.None;
                }

                lastAlarmAt[next] = now;
                return sound;
            }

            if (sound == StructuralRiskFeedback.RiskSound.Tick)
            {
                // 붕괴 임박에서 내려가는 건 대개 천장이 무너진 경우라 틱을 내지 않는다.
                if (previous == StructuralRiskLevel.CollapseImminent
                    || (hasTick && now - lastTickAt < RepeatCooldownSeconds))
                {
                    return StructuralRiskFeedback.RiskSound.None;
                }

                lastTickAt = now;
                hasTick = true;
                return sound;
            }

            return StructuralRiskFeedback.RiskSound.None;
        }
    }
}
