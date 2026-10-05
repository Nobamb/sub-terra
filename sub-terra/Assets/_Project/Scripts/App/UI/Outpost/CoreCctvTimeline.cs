using System;

namespace SubTerra.App.UI.Outpost
{
    /// <summary>등장 연출 한 시점의 창 프레임 값. 내부 글자·아이콘은 이 값으로 크기를 바꾸지 않는다.</summary>
    public readonly struct CoreCctvWindowFrame
    {
        /// <summary>중앙 가로선이 좌우로 펼쳐진 정도. 0~1.</summary>
        public float LineWidth { get; }
        /// <summary>세로 확장 정도. 0이면 가로선, 1이면 전체 창.</summary>
        public float Open { get; }
        /// <summary>시작의 짧은 청록 섬광. 0~1.</summary>
        public float Flash { get; }
        /// <summary>가장자리 노이즈. 0~1.</summary>
        public float EdgeNoise { get; }
        /// <summary>프레임이 거의 펼쳐진 뒤 검은 CCTV 화면이 드러나는 정도.</summary>
        public float ScreenAlpha { get; }
        /// <summary>제목·닫기 버튼이 나타나는 정도.</summary>
        public float TitleAlpha { get; }

        public CoreCctvWindowFrame(
            float lineWidth, float open, float flash, float edgeNoise, float screenAlpha, float titleAlpha)
        {
            LineWidth = lineWidth;
            Open = open;
            Flash = flash;
            EdgeNoise = edgeNoise;
            ScreenAlpha = screenAlpha;
            TitleAlpha = titleAlpha;
        }
    }

    /// <summary>영상이 켜지는 순간의 값.</summary>
    public readonly struct CoreCctvPowerFrame
    {
        public float Alpha { get; }
        public float Brightness { get; }
        public float BarAlpha { get; }
        public float NoiseBoost { get; }
        public float Rec { get; }

        public CoreCctvPowerFrame(float alpha, float brightness, float barAlpha, float noiseBoost, float rec)
        {
            Alpha = alpha;
            Brightness = brightness;
            BarAlpha = barAlpha;
            NoiseBoost = noiseBoost;
            Rec = rec;
        }
    }

    /// <summary>종료 연출 한 시점의 값. 1에서 0으로 줄어든다.</summary>
    public readonly struct CoreCctvExitFrame
    {
        /// <summary>CCTV 영상과 내부 요소의 밝기.</summary>
        public float Content { get; }
        /// <summary>세로 높이 비율. 0이면 중앙 가로선.</summary>
        public float Open { get; }
        /// <summary>가로선의 남은 길이 비율.</summary>
        public float LineWidth { get; }
        /// <summary>선이 사라지기 직전의 짧은 빛.</summary>
        public float Glint { get; }
        /// <summary>창 전체 불투명도.</summary>
        public float Alpha { get; }

        public CoreCctvExitFrame(float content, float open, float lineWidth, float glint, float alpha)
        {
            Content = content;
            Open = open;
            LineWidth = lineWidth;
            Glint = glint;
            Alpha = alpha;
        }
    }

    /// <summary>
    /// 전진기지 코어 CCTV 팝업의 시간표. 상태를 갖지 않는 순수 계산이라 EditMode에서 검증한다.
    /// 오래된 TV가 켜지고 코어 단말이 연결되는 흐름: 선 → 프레임 펼침 → 터미널 → 연결 표시 → 목록·영상.
    /// </summary>
    public static class CoreCctvTimeline
    {
        public const float LineEnd = 0.10f;
        public const float OpenEnd = 0.34f;
        public const float TerminalStart = 0.34f;
        public const float TerminalLineInterval = 0.12f;
        public const float TerminalLineTyping = 0.10f;
        /// <summary>연결된 시설이 있는지 확정하는 시점. 터미널이 끝나고 다음 단계를 정한다.</summary>
        public const float DecideTime = 0.80f;
        public const float ConnectedDuration = 0.22f;
        public const float RevealStartConnected = 1.02f;
        public const float RevealStartEmpty = 0.92f;
        public const float EmptyTerminalFade = 0.10f;
        public const float ItemStagger = 0.05f;
        public const float ItemStaggerSpan = 0.20f;
        public const float ItemFade = 0.14f;
        public const float VideoOnDelay = 0.08f;
        public const float VideoOnDuration = 0.24f;
        public const float VideoOffDuration = 0.12f;
        public const float MoveDuration = 0.28f;
        public const float ExitDuration = 0.26f;
        public const int TerminalLineCount = 4;

        /// <summary>연결된 시설이 있을 때 등장이 모두 끝나는 시점. 목표 1~1.4초.</summary>
        public static float IntroDuration(int facilityCount)
        {
            return facilityCount > 0
                ? RevealStartConnected + VideoOnDelay + VideoOnDuration
                : RevealStartEmpty + ItemFade;
        }

        public static float RevealStart(bool connected)
        {
            return connected ? RevealStartConnected : RevealStartEmpty;
        }

        public static float ItemDelay(int index, int count)
        {
            if (count <= 1)
            {
                return 0f;
            }

            return Math.Min(ItemStagger, ItemStaggerSpan / (count - 1)) * index;
        }

        public static float ItemAlpha(float revealClock, int index, int count)
        {
            return Smooth(Range(revealClock - ItemDelay(index, count), 0f, ItemFade));
        }

        public static CoreCctvWindowFrame Window(float t)
        {
            var lineWidth = EaseOut(Range(t, 0f, LineEnd));
            var open = EaseOut(Range(t, LineEnd, OpenEnd));
            var flash = Bell(t, 0.05f, 0.05f);
            var edge = Math.Min(Range(t, 0f, 0.04f), 1f - Range(t, 0.20f, 0.42f)) * 0.6f;
            var screen = Smooth(Range(t, 0.24f, OpenEnd));
            var title = Smooth(Range(t, 0.30f, 0.42f));
            return new CoreCctvWindowFrame(lineWidth, open, flash, edge, screen, title);
        }

        /// <summary>줄 lineIndex가 보이는 글자 수 비율(0~1). 터미널처럼 한 글자씩 찍힌다.</summary>
        public static float TerminalTyping(int lineIndex, float t)
        {
            var start = TerminalStart + TerminalLineInterval * lineIndex;
            return Range(t, start, start + TerminalLineTyping);
        }

        public static float TerminalLineStart(int lineIndex)
        {
            return TerminalStart + TerminalLineInterval * lineIndex;
        }

        /// <summary>
        /// 터미널 글자 전체의 불투명도. 연결된 시설이 있으면 결정 시점에 바로 사라지고,
        /// 없으면 잠시 남았다가 천천히 꺼진다.
        /// </summary>
        public static float TerminalAlpha(float t, bool connected)
        {
            if (t < DecideTime)
            {
                return 1f;
            }

            return connected ? 0f : 1f - Smooth(Range(t, DecideTime, DecideTime + EmptyTerminalFade));
        }

        /// <summary>‘연결되었습니다’가 짧게 깜빡이는 정도. 등장 연출에서만 쓰인다.</summary>
        public static float ConnectedAlpha(float t)
        {
            var local = t - DecideTime;
            if (local < 0f || local >= ConnectedDuration)
            {
                return 0f;
            }

            if (local < 0.06f)
            {
                return 1f;
            }

            if (local < 0.09f)
            {
                return 0f;
            }

            if (local < 0.15f)
            {
                return 1f;
            }

            if (local < 0.17f)
            {
                return 0.15f;
            }

            return 1f - Smooth(Range(local, 0.17f, ConnectedDuration));
        }

        /// <summary>영상이 켜진 뒤 경과 시간 t의 값. 검은 화면 → 수평 노이즈 → 밝기 상승.</summary>
        public static CoreCctvPowerFrame Power(float t)
        {
            var alpha = Smooth(Range(t, 0f, 0.10f));
            var brightness = 0.15f + 0.85f * EaseOut(Range(t, 0f, VideoOnDuration));
            var bars = (1f - Range(t, 0.04f, VideoOnDuration - 0.02f)) * Range(t, 0f, 0.02f);
            var noise = 1f - Range(t, 0.06f, VideoOnDuration);
            var rec = Smooth(Range(t, 0.04f, 0.14f));
            return new CoreCctvPowerFrame(alpha, brightness, bars, noise, rec);
        }

        /// <summary>꺼질 때 경과 시간 t의 영상 밝기. 1에서 0으로 짧게 어두워진다.</summary>
        public static float VideoOff(float t)
        {
            return 1f - Smooth(Range(t, 0f, VideoOffDuration));
        }

        /// <summary>카메라 이동의 진행도(0~1). 출발과 도착에 짧은 가감속.</summary>
        public static float MoveEase(float t)
        {
            return Smooth(Range(t, 0f, MoveDuration));
        }

        /// <summary>이동 중 속도 비율(0~1). 출발·도착에서 0, 중간에서 최대. 잔상 세기에 쓴다.</summary>
        public static float MoveSpeed(float t)
        {
            var k = Range(t, 0f, MoveDuration);
            return 4f * k * (1f - k);
        }

        public static CoreCctvExitFrame Exit(float t)
        {
            var content = 1f - Smooth(Range(t, 0f, 0.10f));
            var open = 1f - EaseIn(Range(t, 0.06f, 0.18f));
            var line = 1f - EaseIn(Range(t, 0.18f, ExitDuration));
            var glint = Math.Min(Range(t, 0.14f, 0.18f), 1f - Range(t, 0.18f, ExitDuration));
            var alpha = 1f - Smooth(Range(t, 0.22f, ExitDuration));
            return new CoreCctvExitFrame(content, open, line, glint, alpha);
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

        private static float EaseIn(float k)
        {
            return k * k * k;
        }

        private static float Bell(float t, float center, float width)
        {
            var d = (t - center) / width;
            return (float)Math.Exp(-d * d);
        }
    }

    /// <summary>목록 항목 배치와 선택 항목이 보이도록 스크롤 위치를 구하는 순수 계산.</summary>
    public static class CoreCctvListLayout
    {
        public const float ItemHeight = 76f;
        public const float ItemSpacing = 10f;
        public const float Padding = 8f;

        public static float ContentHeight(int count)
        {
            if (count <= 0)
            {
                return 0f;
            }

            return Padding * 2f + count * ItemHeight + (count - 1) * ItemSpacing;
        }

        /// <summary>항목 상단의 콘텐츠 위쪽 기준 거리.</summary>
        public static float ItemTop(int index)
        {
            return Padding + index * (ItemHeight + ItemSpacing);
        }

        /// <summary>
        /// 콘텐츠가 뷰포트를 넘을 때만 스크롤이 필요하다.
        /// scroll은 콘텐츠를 위로 밀어 올린 거리(0 = 맨 위).
        /// </summary>
        public static bool NeedsScroll(int count, float viewportHeight)
        {
            return ContentHeight(count) > viewportHeight + 0.5f;
        }

        public static float MaxScroll(int count, float viewportHeight)
        {
            return Math.Max(0f, ContentHeight(count) - viewportHeight);
        }

        /// <summary>선택 항목이 뷰포트 안에 완전히 보이도록 가장 가까운 스크롤 위치를 돌려준다.</summary>
        public static float ScrollToReveal(int index, int count, float viewportHeight, float currentScroll)
        {
            var max = MaxScroll(count, viewportHeight);
            if (max <= 0f)
            {
                return 0f;
            }

            var top = ItemTop(index);
            var bottom = top + ItemHeight;
            var scroll = Math.Max(0f, Math.Min(max, currentScroll));
            // 첫·마지막 항목은 여백까지 보이도록 끝에 붙인다.
            if (top - Padding < scroll)
            {
                scroll = index == 0 ? 0f : top - Padding;
            }
            else if (bottom + Padding > scroll + viewportHeight)
            {
                scroll = index == count - 1 ? max : bottom + Padding - viewportHeight;
            }

            return Math.Max(0f, Math.Min(max, scroll));
        }
    }
}
