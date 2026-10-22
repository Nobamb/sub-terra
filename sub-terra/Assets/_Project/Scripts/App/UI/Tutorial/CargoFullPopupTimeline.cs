using System;

namespace SubTerra.App.UI.Tutorial
{
    /// <summary>
    /// 화물 공간 부족 팝업(B-152) 시간표. 인스펙터에서 조정하는 값이며, 열기 전체는 MaxOpenDuration을 넘지 않는다.
    /// </summary>
    [Serializable]
    public sealed class CargoFullPopupTiming
    {
        public const float MaxOpenDuration = 0.6f;
        public const float MaxCloseDuration = 0.3f;

        public float scanEnd = 0.1f;
        public float revealStart = 0.08f;
        public float revealEnd = 0.26f;
        public float frameFlashPeak = 0.27f;
        public float frameFlashEnd = 0.4f;
        public float contentStart = 0.22f;
        public float contentEnd = 0.34f;
        public float gaugeStart = 0.3f;
        public float gaugeDuration = 0.24f;
        public float overflowFlashRise = 0.02f;
        public float overflowFlashFall = 0.03f;
        public float closeDuration = 0.2f;

        public float OpenDuration
        {
            get
            {
                var end = Math.Max(scanEnd, Math.Max(revealEnd, Math.Max(frameFlashEnd, contentEnd)));
                return Math.Max(end, GaugeEnd + Math.Max(0f, overflowFlashRise) + Math.Max(0f, overflowFlashFall));
            }
        }

        public float GaugeEnd => gaugeStart + Math.Max(0f, gaugeDuration);

        /// <summary>음수·역순 값을 바로잡고, 열기가 0.6초를 넘으면 비율을 유지한 채 줄인 복사본.</summary>
        public CargoFullPopupTiming Sanitized()
        {
            var copy = (CargoFullPopupTiming)MemberwiseClone();
            copy.scanEnd = Math.Max(0.01f, copy.scanEnd);
            copy.revealStart = Math.Max(0f, copy.revealStart);
            copy.revealEnd = Math.Max(copy.revealStart + 0.01f, copy.revealEnd);
            copy.frameFlashPeak = Math.Max(0f, copy.frameFlashPeak);
            copy.frameFlashEnd = Math.Max(copy.frameFlashPeak + 0.01f, copy.frameFlashEnd);
            copy.contentStart = Math.Max(0f, copy.contentStart);
            copy.contentEnd = Math.Max(copy.contentStart + 0.01f, copy.contentEnd);
            copy.gaugeStart = Math.Max(0f, copy.gaugeStart);
            copy.gaugeDuration = Math.Max(0.01f, copy.gaugeDuration);
            copy.overflowFlashRise = Math.Max(0.005f, copy.overflowFlashRise);
            copy.overflowFlashFall = Math.Max(0.005f, copy.overflowFlashFall);
            copy.closeDuration = Math.Min(MaxCloseDuration, Math.Max(0.05f, copy.closeDuration));

            var total = copy.OpenDuration;
            if (total > MaxOpenDuration)
            {
                var k = MaxOpenDuration / total;
                copy.scanEnd *= k;
                copy.revealStart *= k;
                copy.revealEnd *= k;
                copy.frameFlashPeak *= k;
                copy.frameFlashEnd *= k;
                copy.contentStart *= k;
                copy.contentEnd *= k;
                copy.gaugeStart *= k;
                copy.gaugeDuration *= k;
                copy.overflowFlashRise *= k;
                copy.overflowFlashFall *= k;
            }

            return copy;
        }
    }

    /// <summary>
    /// 열기: 가운데 가로 빛 → 창이 위아래로 열림 → 테두리 한 번 켜짐 → 내용 페이드 인 → 게이지 참 → 초과 칸 한 번 섬광.
    /// 닫기: 내용이 먼저 빠지고 창이 가운데 선으로 접힌 뒤 사라진다. 끝난 뒤에는 값이 고정되고 반복 깜빡임이 없다.
    /// </summary>
    public static class CargoFullPopupTimeline
    {
        public const float RevealMinFraction = 0.012f;

        public struct Pose
        {
            public float Backdrop;
            public float ScanWidth;
            public float ScanAlpha;
            /// <summary>창 높이 비율(RevealMinFraction~1).</summary>
            public float Reveal;
            public float FrameFlash;
            public float Content;
            public float Gauge;
            public float OverflowFlash;
            public float Alpha;
            public bool Interactable;
            public bool Finished;
        }

        public static Pose EvaluateOpen(float t, CargoFullPopupTiming timing)
        {
            var s = timing ?? new CargoFullPopupTiming();
            var pose = new Pose();
            var duration = s.OpenDuration;
            if (t >= duration)
            {
                t = duration;
            }

            pose.Backdrop = EaseOutCubic(Progress(t, 0f, s.revealEnd));
            pose.ScanWidth = EaseOutCubic(Progress(t, 0f, s.scanEnd));
            pose.ScanAlpha = Progress(t, 0f, s.scanEnd * 0.3f)
                * (1f - Progress(t, s.revealStart, s.revealStart + (s.revealEnd - s.revealStart) * 0.6f));
            pose.Reveal = RevealMinFraction + (1f - RevealMinFraction) * EaseOutCubic(Progress(t, s.revealStart, s.revealEnd));
            pose.FrameFlash = Pulse(t, s.revealEnd - 0.04f, s.frameFlashPeak, s.frameFlashEnd);
            pose.Content = EaseOutCubic(Progress(t, s.contentStart, s.contentEnd));
            pose.Gauge = EaseOutCubic(Progress(t, s.gaugeStart, s.GaugeEnd));
            var flashPeak = s.GaugeEnd + s.overflowFlashRise;
            pose.OverflowFlash = Pulse(t, s.GaugeEnd, flashPeak, flashPeak + s.overflowFlashFall);
            pose.Alpha = 1f;
            pose.Finished = t >= duration;
            // 열기 연출 중에는 버튼을 잠근다.
            pose.Interactable = pose.Finished;
            return pose;
        }

        public static Pose EvaluateClose(float t, CargoFullPopupTiming timing)
        {
            var s = timing ?? new CargoFullPopupTiming();
            var d = s.closeDuration;
            var pose = new Pose();
            pose.Content = 1f - EaseOutCubic(Progress(t, 0f, d * 0.4f));
            pose.Reveal = RevealMinFraction + (1f - RevealMinFraction) * (1f - EaseOutCubic(Progress(t, d * 0.15f, d * 0.75f)));
            pose.ScanWidth = 1f - EaseOutCubic(Progress(t, d * 0.7f, d));
            pose.ScanAlpha = Progress(t, d * 0.5f, d * 0.7f) * (1f - Progress(t, d * 0.8f, d));
            pose.Backdrop = 1f - Progress(t, 0f, d);
            pose.Alpha = 1f - Progress(t, d * 0.75f, d);
            pose.Gauge = 1f;
            pose.Finished = t >= d;
            pose.Interactable = false;
            return pose;
        }

        /// <summary>start에서 0, peak에서 1, end에서 0. 그 밖은 0이라 한 번만 켜진다.</summary>
        public static float Pulse(float t, float start, float peak, float end)
        {
            if (t <= start || t >= end)
            {
                return 0f;
            }

            return t < peak
                ? Progress(t, start, peak)
                : 1f - EaseOutCubic(Progress(t, peak, end));
        }

        public static float Progress(float t, float start, float end)
        {
            if (end <= start)
            {
                return t >= end ? 1f : 0f;
            }

            var p = (t - start) / (end - start);
            return p < 0f ? 0f : (p > 1f ? 1f : p);
        }

        public static float EaseOutCubic(float p)
        {
            var inv = 1f - p;
            return 1f - inv * inv * inv;
        }
    }
}
