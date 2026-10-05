using System;

namespace SubTerra.App.UI.EmergencyRescue
{
    /// <summary>글리치 한 시점의 값. 강도·시드·색 계열과 레이어별 투명도.</summary>
    public readonly struct EmergencyRescueGlitchFrame
    {
        /// <summary>글리치 세기 0~1.</summary>
        public float Intensity { get; }
        /// <summary>30fps 단위로 끊겨 바뀌는 시드. 같은 시드는 같은 조각 배치를 만든다.</summary>
        public int Seed { get; }
        /// <summary>0 = 청록, 1 = 붉은색 계열.</summary>
        public int Tint { get; }
        public float VisualAlpha { get; }
        public float ContentAlpha { get; }
        public float BackdropAlpha { get; }

        public EmergencyRescueGlitchFrame(
            float intensity, int seed, int tint, float visualAlpha, float contentAlpha, float backdropAlpha)
        {
            Intensity = intensity;
            Seed = seed;
            Tint = tint;
            VisualAlpha = visualAlpha;
            ContentAlpha = contentAlpha;
            BackdropAlpha = backdropAlpha;
        }
    }

    /// <summary>머리 위 홀로그램 등장 시점의 값.</summary>
    public readonly struct EmergencyRescueChipShowFrame
    {
        public float FrameAlpha { get; }
        /// <summary>홀로그램 세로 펼침 0~1.</summary>
        public float Unfold { get; }
        /// <summary>모서리 빛 세기. 등장 직후 가장 강하고 은은한 수준으로 가라앉는다.</summary>
        public float Corner { get; }
        public float ContentAlpha { get; }

        public EmergencyRescueChipShowFrame(float frameAlpha, float unfold, float corner, float contentAlpha)
        {
            FrameAlpha = frameAlpha;
            Unfold = unfold;
            Corner = corner;
            ContentAlpha = contentAlpha;
        }
    }

    /// <summary>픽토그램 호버 한 시점의 값. 레벨(0~1) 하나로 결정되므로 연타해도 누적되지 않는다.</summary>
    public readonly struct EmergencyRescueHoverFrame
    {
        public float Glow { get; }
        /// <summary>사람 상승 0~1 (Ease In-Out).</summary>
        public float Human { get; }
        /// <summary>화살표 상승. 끝에서 한 번 살짝 넘쳤다 돌아오므로 1을 약간 넘는다.</summary>
        public float Arrow { get; }

        public EmergencyRescueHoverFrame(float glow, float human, float arrow)
        {
            Glow = glow;
            Human = human;
            Arrow = arrow;
        }
    }

    /// <summary>
    /// 전력 고갈 구출 UI의 시간표. 상태를 갖지 않는 순수 계산이라 EditMode에서 검증한다.
    /// 팝업 글리치(등장·표시 중·종료)와 머리 위 홀로그램(등장·호버·키캡 안내·눌림)을 다룬다.
    /// </summary>
    public static class EmergencyRescueTimeline
    {
        public const float IntroDuration = 0.5f;
        public const float OutroDuration = 0.3f;
        /// <summary>글리치 조각이 새 배치로 바뀌는 간격(30fps). 부드럽게 움직이지 않고 끊겨 보인다.</summary>
        public const float StepSeconds = 1f / 30f;

        public const float ChipShowDuration = 0.32f;
        public const float ChipDismissDuration = 0.16f;
        public const float HoverRiseSeconds = 0.5f;
        public const float HoverFallSeconds = 0.35f;
        /// <summary>키캡 반복 안내 한 번의 길이.</summary>
        public const float KeycapPulseDuration = 0.34f;
        public const float KeycapFirstDelay = 1.8f;
        public const float KeycapMinInterval = 3f;
        public const float KeycapMaxInterval = 5f;
        /// <summary>실제 입력 때의 더 분명한 눌림.</summary>
        public const float PressFeedbackDuration = 0.16f;

        private const float IdleSlotSeconds = 0.4f;
        private const float IdleBurstChance = 0.24f;

        public static float Hash01(int seed)
        {
            unchecked
            {
                var h = (uint)seed * 2654435761u;
                h ^= h >> 15;
                h *= 2246822519u;
                h ^= h >> 13;
                return (h & 0xFFFFFF) / (float)0x1000000;
            }
        }

        /// <summary>팝업 등장. 프레임이 끊겨 나타나고 청록·붉은 글리치가 3번 지난 뒤 선명하게 정착한다.</summary>
        public static EmergencyRescueGlitchFrame Intro(float t)
        {
            t = t < 0f ? 0f : t;
            var seed = (int)(t / StepSeconds);
            float visual;
            if (t < 0.04f) visual = 0f;
            else if (t < 0.08f) visual = 0.9f;
            else if (t < 0.11f) visual = 0.1f;
            else if (t < 0.15f) visual = 1f;
            else if (t < 0.19f) visual = 0.35f;
            else visual = 1f;

            var content = Smooth(Range(t, 0.20f, 0.42f));
            if (t >= 0.30f && t < 0.33f)
            {
                content *= 0.5f;
            }

            var intensity = Bell(t, 0.06f, 0.025f)
                + 0.85f * Bell(t, 0.19f, 0.03f)
                + 0.6f * Bell(t, 0.32f, 0.03f);
            intensity *= 1f - Range(t, 0.38f, 0.44f);
            var tint = t < 0.13f ? 0 : t < 0.26f ? 1 : 0;
            return new EmergencyRescueGlitchFrame(
                Clamp01(intensity), seed, tint, visual, content, Range(t, 0f, 0.3f));
        }

        /// <summary>팝업 종료. 등장보다 강한 글리치가 짧게 이어지다 끊겨 사라진다.</summary>
        public static EmergencyRescueGlitchFrame Outro(float t)
        {
            t = t < 0f ? 0f : t;
            var seed = (int)(t / StepSeconds) + 101;
            float visual;
            if (t >= OutroDuration) visual = 0f;
            else if (t < 0.09f) visual = 1f;
            else if (t < 0.11f) visual = 0.6f;
            else if (t < 0.15f) visual = 1f;
            else if (t < 0.20f) visual = 0.8f;
            else if (t < 0.23f) visual = 0.2f;
            else if (t < 0.26f) visual = 0.7f;
            else if (t < 0.28f) visual = 0.1f;
            else visual = 0.45f;

            var intensity = t >= OutroDuration ? 0f : 0.7f + 0.3f * Range(t, 0f, 0.2f);
            var tint = ((seed / 2) % 3) == 1 ? 1 : 0;
            return new EmergencyRescueGlitchFrame(
                Clamp01(intensity),
                seed,
                tint,
                visual,
                1f - Smooth(Range(t, 0.03f, 0.2f)),
                1f - Range(t, 0.05f, 0.3f));
        }

        /// <summary>
        /// 표시 중 간헐 글리치. 0.4초 칸마다 확률로 짧게 한 번 일어나 창 가장자리 조각에만 쓰인다.
        /// 화면 전체나 본문·버튼에는 영향을 주지 않는 약한 값(최대 0.8)이다.
        /// </summary>
        public static float Idle(float time, out int seed)
        {
            seed = 0;
            if (time < 0f)
            {
                return 0f;
            }

            var slot = (int)(time / IdleSlotSeconds);
            var local = time - slot * IdleSlotSeconds;
            var roll = Hash01(slot * 31 + 7);
            if (roll > IdleBurstChance)
            {
                return 0f;
            }

            var start = Hash01(slot * 17 + 3) * 0.18f;
            var length = 0.08f + Hash01(slot * 13 + 5) * 0.08f;
            if (local < start || local > start + length)
            {
                return 0f;
            }

            seed = slot * 7 + (int)(local / StepSeconds);
            return 0.35f + Hash01(slot * 11 + 1) * 0.45f;
        }

        public static EmergencyRescueChipShowFrame ChipShow(float level)
        {
            var corner = Range(level, 0f, 0.3f);
            var settle = Range(level, 0.3f, 0.75f);
            return new EmergencyRescueChipShowFrame(
                Range(level, 0f, 0.2f),
                EaseOut(Range(level, 0.2f, 0.7f)),
                Math.Max(0.35f, corner * (1f - settle)),
                Smooth(Range(level, 0.55f, 0.95f)));
        }

        public static float AdvanceHover(float level, bool hovered, float deltaSeconds)
        {
            var next = level + (hovered ? deltaSeconds / HoverRiseSeconds : -deltaSeconds / HoverFallSeconds);
            return Clamp01(next);
        }

        /// <summary>청록빛이 먼저 퍼지고, 사람이 부드럽게 오른 뒤, 화살표가 늦게 올라 끝에서 한 번 튄다.</summary>
        public static EmergencyRescueHoverFrame Hover(float level)
        {
            var glow = EaseOut(Range(level, 0f, 0.45f));
            var human = Smooth(Range(level, 0.15f, 0.75f));
            var k = Range(level, 0.32f, 0.95f);
            const float split = 0.7f;
            const float peak = 1.18f;
            var arrow = k < split
                ? EaseOut(k / split) * peak
                : Lerp(peak, 1f, Smooth((k - split) / (1f - split)));
            return new EmergencyRescueHoverFrame(glow, human, arrow);
        }

        /// <summary>키캡 반복 안내까지 남은 시간. cycle마다 3~5초 사이에서 정해진다.</summary>
        public static float KeycapInterval(int cycle)
        {
            return KeycapMinInterval + Hash01(cycle * 19 + 11) * (KeycapMaxInterval - KeycapMinInterval);
        }

        /// <summary>눌림 깊이 0~1. 빠르게 눌렸다 약간 천천히 돌아온다.</summary>
        public static float KeycapPulse(float elapsed, float duration)
        {
            if (elapsed <= 0f || elapsed >= duration)
            {
                return 0f;
            }

            var k = elapsed / duration;
            const float down = 0.35f;
            return k < down ? EaseOut(k / down) : 1f - Smooth((k - down) / (1f - down));
        }

        private static float Range(float t, float from, float to)
        {
            return to <= from ? 1f : Clamp01((t - from) / (to - from));
        }

        private static float Clamp01(float v)
        {
            return v < 0f ? 0f : v > 1f ? 1f : v;
        }

        private static float Lerp(float a, float b, float k)
        {
            return a + (b - a) * k;
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

        private static float Bell(float t, float center, float width)
        {
            var d = (t - center) / width;
            return (float)Math.Exp(-d * d);
        }
    }
}
