using System;

namespace SubTerra.App.UI.FacilityNameTag
{
    /// <summary>이름표 한 시점의 표시 값. 글자는 크기를 바꾸지 않고 마스크로만 드러낸다.</summary>
    public readonly struct FacilityNameTagFrame
    {
        /// <summary>프레임 가로 비율. 0은 작은 네모, 1은 전체 폭.</summary>
        public float Spread { get; }
        /// <summary>프레임·배경 불투명도.</summary>
        public float FrameAlpha { get; }
        /// <summary>모서리 빛 세기. 등장 직후 가장 강하고 이후 은은한 수준으로 가라앉는다.</summary>
        public float Flare { get; }
        /// <summary>글자를 드러낸 비율(왼쪽부터). 0~1.</summary>
        public float Reveal { get; }
        /// <summary>스캔선 위치(0~1)와 세기.</summary>
        public float ScanPosition { get; }
        public float ScanAlpha { get; }
        public float TextAlpha { get; }

        public FacilityNameTagFrame(
            float spread, float frameAlpha, float flare, float reveal, float scanPosition, float scanAlpha, float textAlpha)
        {
            Spread = spread;
            FrameAlpha = frameAlpha;
            Flare = flare;
            Reveal = reveal;
            ScanPosition = scanPosition;
            ScanAlpha = scanAlpha;
            TextAlpha = textAlpha;
        }
    }

    /// <summary>
    /// 시설 이름표 등장·퇴장 시간표. 상태가 없는 순수 계산이라 EditMode에서 검증한다.
    /// level(0~1) 하나로 진행을 표현해, 등장 도중 사라지거나 그 반대여도 현재 모습에서 이어진다.
    /// </summary>
    public static class FacilityNameTagTimeline
    {
        public const float ShowDuration = 0.26f;
        public const float HideDuration = 0.17f;
        public const float PulsePeriod = 2.4f;
        public const float SteadyFlare = 0.5f;

        // level 구간: 모서리 빛 → 프레임 펼침 → 스캔선과 글자 드러남
        private const float CornerEnd = 0.22f;
        private const float SpreadStart = 0.18f;
        private const float SpreadEnd = 0.62f;
        private const float ScanStart = 0.55f;

        /// <summary>wanted 방향으로 level을 dt만큼 진행한다.</summary>
        public static float Advance(float level, bool wanted, float deltaSeconds)
        {
            var duration = wanted ? ShowDuration : HideDuration;
            var next = level + (wanted ? deltaSeconds : -deltaSeconds) / duration;
            return next < 0f ? 0f : next > 1f ? 1f : next;
        }

        public static FacilityNameTagFrame Evaluate(float level, bool showing)
        {
            var spread = EaseOut(Range(level, SpreadStart, SpreadEnd));
            var frameAlpha = Range(level, 0f, 0.15f);
            var corner = Range(level, 0f, CornerEnd);
            var settle = Range(level, CornerEnd, SpreadEnd + 0.1f);
            var flare = Math.Max(SteadyFlare, corner * (1f - settle));
            var scan = Range(level, ScanStart, 1f);

            if (showing)
            {
                // 스캔선은 양 끝에서 부드럽게 나타나고 사라진다.
                var scanAlpha = scan <= 0f || scan >= 1f ? 0f : Math.Min(1f, Math.Min(scan * 8f, (1f - scan) * 8f));
                return new FacilityNameTagFrame(spread, frameAlpha, flare, scan, scan, scanAlpha, scan > 0f ? 1f : 0f);
            }

            // 퇴장: 글자가 먼저 흐려지고 프레임이 중앙으로 접힌다.
            return new FacilityNameTagFrame(
                spread, frameAlpha, Math.Min(flare, SteadyFlare), 1f, 0f, 0f, Smooth(Range(level, 0.3f, 0.75f)));
        }

        /// <summary>표시 중 테두리 발광의 느린 호흡. 0.85~1. 점멸하지 않는다.</summary>
        public static float Pulse(float time)
        {
            return 0.925f + 0.075f * (float)Math.Sin(time * 2.0 * Math.PI / PulsePeriod);
        }

        private static float Range(float t, float from, float to)
        {
            return to <= from ? 1f : Clamp01((t - from) / (to - from));
        }

        private static float Clamp01(float v)
        {
            return v < 0f ? 0f : v > 1f ? 1f : v;
        }

        private static float Smooth(float k)
        {
            return k * k * (3f - 2f * k);
        }

        private static float EaseOut(float k)
        {
            var inv = 1f - k;
            return 1f - inv * inv * inv;
        }
    }
}
