using UnityEngine;

namespace SubTerra.Gameplay.Power
{
    /// <summary>
    /// B-204: 전진기지 코어·조명 범위가 중심에서 바깥으로 퍼지는 "시각" 연출의 공통 수치.
    /// 판정 반지름(전력 10칸, 조명 5칸)은 건드리지 않고 표시 반지름·불투명도에만 쓴다.
    /// 가스 등장(GasVisualRules.SpawnDurationSeconds 1초)보다 빠르게 끝난다.
    /// </summary>
    public static class RangeSpreadMath
    {
        /// <summary>전진기지 코어 범위 원이 최종 반지름까지 퍼지는 시간(초).</summary>
        public const float CoreSpreadSeconds = 0.35f;

        /// <summary>조명 범위(후광·암전 구멍·범위 스프라이트)가 최종 반지름까지 퍼지는 시간(초).</summary>
        public const float LightSpreadSeconds = 0.30f;

        /// <summary>표시 반지름이 0이 되어 스프라이트 스케일이 무너지지 않게 하는 최소 비율.</summary>
        public const float MinRadiusFraction = 0.02f;

        /// <summary>선형 진행도(0~1)를 시간으로 만든다. 길이가 0 이하면 바로 끝난 것으로 본다.</summary>
        public static float Advance(float progress, float deltaTime, float durationSeconds)
        {
            if (durationSeconds <= 0.0001f)
            {
                return 1f;
            }

            return Mathf.Clamp01(progress + Mathf.Max(0f, deltaTime) / durationSeconds);
        }

        /// <summary>반지름 비율. 처음에 빠르게 커지고 끝에서 부드럽게 멈춘다(ease-out).</summary>
        public static float RadiusFraction(float progress)
        {
            var p = Mathf.Clamp01(progress);
            if (p >= 1f)
            {
                return 1f;
            }

            var inverse = 1f - p;
            return Mathf.Max(MinRadiusFraction, 1f - inverse * inverse * inverse);
        }

        /// <summary>불투명도 비율. 퍼지는 동안 점점 짙어진다(smoothstep).</summary>
        public static float OpacityFraction(float progress)
        {
            var p = Mathf.Clamp01(progress);
            return p * p * (3f - 2f * p);
        }
    }
}
