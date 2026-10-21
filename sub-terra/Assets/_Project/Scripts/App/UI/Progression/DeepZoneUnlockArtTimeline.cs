using System;

namespace SubTerra.App.UI.Progression
{
    /// <summary>
    /// 심층 해금 창 그림 연출 시간표(비스케일 초, 본문이 보이기 시작한 순간이 0).
    /// 테두리 점등 → 위에서 아래로 훑어 내려가며 심층이 드러남(깊이 숫자 상승) → 발견 섬광 →
    /// 일정 간격의 푸른 호흡·탐지 파동·훑기 빛.
    /// </summary>
    public static class DeepZoneUnlockArtTimeline
    {
        public const float BorderInEnd = 0.25f;
        public const float RevealStart = 0.15f;
        public const float RevealEnd = 0.9f;
        public const float ScanFadeEnd = RevealEnd + 0.18f;
        public const float FlashRise = 0.08f;
        public const float FlashDecay = 1.1f;
        public const float IdleRampEnd = RevealEnd + 0.7f;

        // 탐지 파동: 발견 순간 첫 파동 뒤 일정 간격으로 테두리에서 바깥으로 퍼진다.
        public const float PingPeriod = 3.6f;
        public const float PingDuration = 1.5f;
        public const float PingSpread = 0.22f;

        // 훑기 빛: 머무는 동안 가끔 위에서 아래로 약하게 지나간다.
        public const float SweepFirst = RevealEnd + 2.4f;
        public const float SweepPeriod = 6.5f;
        public const float SweepDuration = 1.6f;

        public static float BorderIn(float t) => Smooth(Progress(t, 0f, BorderInEnd));

        /// <summary>그림이 위에서부터 드러난 비율(0~1). 처음과 끝이 부드러운 굴착 속도.</summary>
        public static float Reveal(float t) => Smooth(Progress(t, RevealStart, RevealEnd));

        /// <summary>깊이 숫자가 0에서 실제 값까지 오르는 비율. 드러나는 높이와 같이 움직인다.</summary>
        public static float Counter(float t) => Reveal(t);

        public static float ScanAlpha(float t)
        {
            if (t < RevealStart) return 0f;
            if (t <= RevealEnd) return Progress(t, RevealStart, RevealStart + 0.06f);
            return 1f - Progress(t, RevealEnd, ScanFadeEnd);
        }

        /// <summary>발견 섬광(0~1): 다 드러나는 순간 빠르게 올랐다가 천천히 가라앉는다.</summary>
        public static float Flash(float t)
        {
            var start = RevealEnd - FlashRise;
            if (t < start) return 0f;
            if (t < RevealEnd) return Progress(t, start, RevealEnd);
            var u = Progress(t, RevealEnd, RevealEnd + FlashDecay);
            var inverse = 1f - u;
            return inverse * inverse * inverse;
        }

        /// <summary>발견 후 유지 발광이 켜지는 정도(0~1).</summary>
        public static float Idle(float t) => Smooth(Progress(t, RevealEnd, IdleRampEnd));

        /// <summary>현재 파동 진행도(0~1). 파동이 없으면 -1.</summary>
        public static float Ping(float t)
        {
            if (t < RevealEnd) return -1f;
            var local = (t - RevealEnd) % PingPeriod;
            return local < PingDuration ? local / PingDuration : -1f;
        }

        /// <summary>파동 테두리 배율. 시작은 그림 테두리와 같고 바깥으로 퍼진다.</summary>
        public static float PingScale(float p) => p < 0f ? 1f : 1f + PingSpread * EaseOut(p);

        public static float PingAlpha(float p)
        {
            if (p < 0f) return 0f;
            var fade = 1f - p;
            return (float)Math.Pow(fade, 1.6d) * Progress(p, 0f, 0.08f);
        }

        /// <summary>훑기 빛 진행도(0~1, 위→아래). 없으면 -1.</summary>
        public static float Sweep(float t)
        {
            if (t < SweepFirst) return -1f;
            var local = (t - SweepFirst) % SweepPeriod;
            return local < SweepDuration ? local / SweepDuration : -1f;
        }

        public static float SweepAlpha(float p) => p < 0f ? 0f : (float)Math.Sin(p * Math.PI);

        public static float Breath(float t, float period, float phase)
        {
            if (period <= 0f) return 0f;
            var cycle = t / period + phase;
            return 0.5f - 0.5f * (float)Math.Cos(cycle * Math.PI * 2d);
        }

        /// <summary>발견 섬광(peak)에서 유지 호흡 수준으로 이어지는 발광 세기.</summary>
        public static float Level(float idleLevel, float peak, float flash, float idle)
        {
            var settled = idleLevel * idle;
            return settled + (peak - settled) * flash;
        }

        public static float Progress(float t, float start, float end)
        {
            if (end <= start) return t >= end ? 1f : 0f;
            var u = (t - start) / (end - start);
            return u < 0f ? 0f : u > 1f ? 1f : u;
        }

        private static float Smooth(float u) => u * u * (3f - 2f * u);

        private static float EaseOut(float u)
        {
            var inverse = 1f - u;
            return 1f - inverse * inverse * inverse;
        }
    }
}
