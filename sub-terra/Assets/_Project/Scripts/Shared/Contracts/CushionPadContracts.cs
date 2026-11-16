using System;

namespace SubTerra.Shared
{
    /// <summary>
    /// 플레이어가 서서 윗면 높이를 한 단계씩 바꿀 수 있는 착지면(완충판).
    /// Player는 Gameplay.Structural을 참조하지 않으므로 이 계약으로만 판을 다룬다.
    /// </summary>
    public interface IAdjustableLandingPlatform : IStableLandingPlatform
    {
        /// <summary>지면에서 윗면까지의 현재 높이(월드 단위).</summary>
        float CurrentHeight { get; }

        /// <summary>direction &gt; 0 한 단계 상승, &lt; 0 한 단계 하강. 높이가 실제로 바뀌면 true와 변화량.</summary>
        bool TryStepHeight(int direction, out float heightDelta);
    }

    /// <summary>
    /// 완충판 높이·착지 피해 비율 규칙. 모든 수치는 프리팹/설정 검사기에서 덮어쓰며,
    /// 이 값은 검사기 필드의 기본값과 단위 테스트 기준으로만 쓴다.
    /// </summary>
    public static class CushionPadRules
    {
        /// <summary>기본(=최소) 윗면 높이. 만적 점프(약 1.16)보다 여유 있게 낮다.</summary>
        public const float DefaultMinHeight = 0.8f;

        /// <summary>최대 윗면 높이. 예전 버팀목 착지면 높이와 같다.</summary>
        public const float DefaultMaxHeight = 1.8f;

        /// <summary>위/아래 키 한 번에 바뀌는 높이.</summary>
        public const float DefaultStepHeight = 0.25f;

        /// <summary>같은 낙하·같은 짐의 일반 착지 피해 대비 완충판 착지 피해 비율.</summary>
        public const float DefaultLandingDamageRatio = 0.1f;

        /// <summary>저장값이 없거나(0 이하) 깨졌으면 최소 높이, 아니면 범위로 자른다.</summary>
        public static float NormalizeHeight(float saved, float min, float max)
        {
            if (max < min) max = min;
            if (float.IsNaN(saved) || float.IsInfinity(saved) || saved <= 0f) return min;
            return Math.Min(max, Math.Max(min, saved));
        }

        /// <summary>한 단계 위/아래 높이. 최소~최대로 자르고, 부동소수 오차를 줄이려 소수 넷째 자리에서 반올림한다.</summary>
        public static float StepHeight(float current, int direction, float min, float max, float step)
        {
            current = NormalizeHeight(current, min, max);
            if (direction == 0 || step <= 0f) return current;
            float next = current + (direction > 0 ? step : -step);
            next = (float)Math.Round(next, 4);
            return Math.Min(max, Math.Max(min, next));
        }

        /// <summary>최소에서 최대까지 닿는 단계 수(최소 포함 높이 개수 - 1).</summary>
        public static int StepCount(float min, float max, float step)
        {
            if (step <= 0f || max <= min) return 0;
            return (int)Math.Ceiling((max - min) / step - 0.0001f);
        }
    }
}
