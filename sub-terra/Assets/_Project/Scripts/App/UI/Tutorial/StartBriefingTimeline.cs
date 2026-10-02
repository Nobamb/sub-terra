using System;

namespace SubTerra.App.UI.Tutorial
{
    public enum BriefingPhase
    {
        Hidden,
        Waiting,
        Intro,
        Shown,
        Closing
    }

    /// <summary>
    /// 시작 브리핑 상태 전이. 닫기는 Shown에서 한 번만 시작되고 완료도 한 번만 인정한다.
    /// </summary>
    public sealed class BriefingLifecycle
    {
        public BriefingPhase Phase { get; private set; }

        public bool BlocksBackground =>
            Phase == BriefingPhase.Intro
            || Phase == BriefingPhase.Shown
            || Phase == BriefingPhase.Closing;

        public void Begin() => Phase = BriefingPhase.Waiting;
        public void Reset() => Phase = BriefingPhase.Hidden;

        public bool TryStartIntro()
        {
            if (Phase != BriefingPhase.Waiting)
            {
                return false;
            }

            Phase = BriefingPhase.Intro;
            return true;
        }

        public bool TryFinishIntro()
        {
            if (Phase != BriefingPhase.Intro)
            {
                return false;
            }

            Phase = BriefingPhase.Shown;
            return true;
        }

        public bool TryBeginClose()
        {
            if (Phase != BriefingPhase.Shown)
            {
                return false;
            }

            Phase = BriefingPhase.Closing;
            return true;
        }

        public bool TryCompleteClose()
        {
            if (Phase != BriefingPhase.Closing)
            {
                return false;
            }

            Phase = BriefingPhase.Hidden;
            return true;
        }
    }

    public enum BriefingGlitchKind
    {
        FrameShift,
        EdgeBurst,
        GlowFlicker
    }

    /// <summary>
    /// 지속 글리치의 발생 간격과 종류를 정한다. 강도가 높을수록 자주 일어나고,
    /// 간격에 무작위 편차와 연속 발생(클러스터)을 섞어 일정한 점멸처럼 보이지 않게 한다.
    /// </summary>
    public sealed class BriefingGlitchPlanner
    {
        private readonly Func<float> next01;

        public BriefingGlitchPlanner(Func<float> random01)
        {
            next01 = random01 ?? throw new ArgumentNullException(nameof(random01));
        }

        public float Range(float min, float max) => min + (max - min) * Clamp01(next01());

        public float NextDelay(float intensity)
        {
            if (intensity <= 0.001f)
            {
                return float.PositiveInfinity;
            }

            var baseSeconds = Lerp(2.4f, 0.28f, Clamp01(intensity));
            var jitter = Range(0.35f, 1.9f);
            if (next01() < 0.18f)
            {
                jitter *= 0.2f;
            }

            return Math.Max(0.06f, baseSeconds * jitter);
        }

        public BriefingGlitchKind PickKind()
        {
            var roll = next01();
            if (roll < 0.40f)
            {
                return BriefingGlitchKind.EdgeBurst;
            }

            return roll < 0.72f
                ? BriefingGlitchKind.FrameShift
                : BriefingGlitchKind.GlowFlicker;
        }

        private static float Clamp01(float value) => value < 0f ? 0f : value > 1f ? 1f : value;
        private static float Lerp(float a, float b, float t) => a + (b - a) * t;
    }

    /// <summary>
    /// 등장·지속·닫기 연출의 시간표와 강도 곡선. 모든 값은 비스케일 초 기준이다.
    /// </summary>
    public static class StartBriefingTimeline
    {
        public const float SustainIntensity = 0.22f;

        // 화면 글리치: 짧은 버스트 3회.
        public static readonly float[] BurstStarts = { 0.00f, 0.17f, 0.34f };
        public const float BurstLength = 0.10f;

        public const float GatherStart = 0.46f;
        public const float GatherEnd = 0.63f;
        public const float SpreadEnd = 0.80f;
        public const float FrameOpenStart = 0.80f;
        public const float FrameOpenEnd = 1.08f;
        public const float ContentFadeEnd = 1.28f;
        public const float IntroDuration = ContentFadeEnd;

        public const float CloseDuration = 0.40f;
        public const float CloseContentFadeEnd = 0.24f;
        public const float CloseFadeOutStart = 0.28f;

        public static int BurstCount => BurstStarts.Length;

        /// <summary>t 시점에 진행 중인 화면 글리치 번호. 없으면 -1.</summary>
        public static int ActiveBurst(float t)
        {
            for (var i = 0; i < BurstStarts.Length; i++)
            {
                if (t >= BurstStarts[i] && t < BurstStarts[i] + BurstLength)
                {
                    return i;
                }
            }

            return -1;
        }

        public static float IntroIntensity(float t)
        {
            if (t < FrameOpenStart)
            {
                return 1f;
            }

            if (t < FrameOpenEnd)
            {
                return Lerp(1f, 0.85f, Progress(t, FrameOpenStart, FrameOpenEnd));
            }

            return Lerp(0.85f, SustainIntensity, Smooth(Progress(t, FrameOpenEnd, IntroDuration)));
        }

        public static float CloseIntensity(float t)
        {
            var remaining = 1f - Progress(t, 0f, CloseDuration);
            return SustainIntensity * remaining * remaining;
        }

        /// <summary>어두운 배경의 목표 알파 비율(0~1). 화면 글리치가 끝난 뒤 올라온다.</summary>
        public static float BackdropAmount(float t) => Smooth(Progress(t, 0.30f, FrameOpenStart));

        public static float Gather(float t) => Progress(t, GatherStart, GatherEnd);
        public static float Spread(float t) => Smooth(Progress(t, GatherEnd, SpreadEnd));
        public static float SignalAlpha(float t)
        {
            if (t < GatherStart)
            {
                return 0f;
            }

            return t < FrameOpenStart ? 1f : 1f - Progress(t, FrameOpenStart, FrameOpenEnd - 0.06f);
        }

        /// <summary>프레임이 위아래로 열리는 정도(0~1, ease-out).</summary>
        public static float FrameOpen(float t)
        {
            var u = Progress(t, FrameOpenStart, FrameOpenEnd);
            var inverse = 1f - u;
            return 1f - inverse * inverse * inverse;
        }

        public static float ContentAlphaIntro(float t) => Progress(t, FrameOpenEnd, ContentFadeEnd);
        public static float ContentAlphaClose(float t) => 1f - Progress(t, 0f, CloseContentFadeEnd);

        /// <summary>닫을 때 프레임 청록빛(1→0).</summary>
        public static float CloseLight(float t) => 1f - Smooth(Progress(t, 0.04f, CloseDuration * 0.9f));

        public static float CloseOverallAlpha(float t) => 1f - Progress(t, CloseFadeOutStart, CloseDuration);

        public static float Progress(float t, float start, float end)
        {
            if (end <= start)
            {
                return t >= end ? 1f : 0f;
            }

            var u = (t - start) / (end - start);
            return u < 0f ? 0f : u > 1f ? 1f : u;
        }

        private static float Smooth(float u) => u * u * (3f - 2f * u);
        private static float Lerp(float a, float b, float t) => a + (b - a) * t;
    }
}
