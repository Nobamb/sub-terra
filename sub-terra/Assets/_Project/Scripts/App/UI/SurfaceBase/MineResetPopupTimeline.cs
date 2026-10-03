using System;

namespace SubTerra.App.UI.SurfaceBase
{
    /// <summary>
    /// '새 광산 구역' 팝업 연출 시간표. 순수 계산이라 Edit Mode에서 시점별 값을 검사할 수 있다.
    /// 열기: 중앙 가로 빛 → 좌우로 뻗음 → 위아래로 펼침 → 테두리 섬광 → 본문 페이드, 후반부에 육각형이 겹쳐 등장.
    /// 닫기: 발광이 먼저 잦아들고 창 전체가 부드럽게 사라진다.
    /// </summary>
    public static class MineResetPopupTimeline
    {
        public const float ScanStart = 0f;
        public const float ScanEnd = 0.12f;
        public const float RevealStart = 0.1f;
        public const float RevealEnd = 0.42f;
        public const float FrameFlashStart = 0.36f;
        public const float FrameFlashPeak = 0.42f;
        public const float FrameFlashEnd = 0.64f;
        public const float ContentStart = 0.28f;
        public const float ContentEnd = 0.46f;
        public const float CoreStart = 0.3f;
        public const float HexStart = 0.34f;
        public const float HexEnd = 0.6f;
        public const float HexFlashEnd = 0.95f;
        public const float BackdropEnd = 0.2f;

        /// <summary>창이 정위치에 정착하는 시점(초). 이후는 섬광이 잦아드는 꼬리뿐이다.</summary>
        public const float SettleTime = HexEnd;
        public const float OpenDuration = HexFlashEnd;

        public const float GlowFadeDuration = 0.12f;
        public const float CloseFadeStart = 0.04f;
        public const float CloseDuration = 0.26f;

        public const float RevealMinHeight = 6f;
        public const float HexStartScale = 0.12f;

        public struct OpenPose
        {
            public float Backdrop;
            public float ScanWidth;
            public float ScanAlpha;
            public float Reveal;
            public float EdgeAlpha;
            public float FrameFlash;
            public float Content;
            public bool Interactable;
            public float CoreScale;
            public float CoreAlpha;
            public float HexScale;
            public float HexAlpha;
            public float RingAlpha;
            /// <summary>완성 순간 1에서 0으로 감쇠. 유지 발광 위에 더해진다.</summary>
            public float HexFlash;
            /// <summary>유지 발광(호흡)이 섞이는 비율. 육각형 완성 전에는 0.</summary>
            public float Idle;
        }

        public struct ClosePose
        {
            public float Glow;
            public float Alpha;
            public bool Finished;
        }

        public static OpenPose EvaluateOpen(float t)
        {
            var pose = new OpenPose();
            pose.Backdrop = EaseOutCubic(Progress(t, 0f, BackdropEnd));
            pose.ScanWidth = EaseOutCubic(Progress(t, ScanStart, ScanEnd));
            // 가로 빛은 바로 켜지고, 위아래로 펼쳐지기 시작하면 두 갈래 가장자리 빛으로 넘어간다.
            pose.ScanAlpha = Progress(t, 0f, 0.04f) * (1f - Progress(t, RevealStart, RevealStart + 0.12f));
            pose.Reveal = EaseOutCubic(Progress(t, RevealStart, RevealEnd));
            pose.EdgeAlpha = Progress(t, RevealStart, RevealStart + 0.04f) * (1f - Progress(t, RevealEnd - 0.08f, RevealEnd + 0.04f));
            pose.FrameFlash = t < FrameFlashPeak
                ? Progress(t, FrameFlashStart, FrameFlashPeak)
                : 1f - EaseOutCubic(Progress(t, FrameFlashPeak, FrameFlashEnd));
            pose.Content = EaseOutCubic(Progress(t, ContentStart, ContentEnd));
            pose.Interactable = t >= ContentStart;

            var core = Progress(t, CoreStart, CoreStart + 0.1f);
            pose.CoreScale = 0.25f + 0.75f * EaseOutCubic(core);
            pose.CoreAlpha = core * (1f - Progress(t, HexEnd - 0.1f, HexEnd + 0.04f));
            var hex = EaseOutCubic(Progress(t, HexStart, HexEnd));
            pose.HexScale = HexStartScale + (1f - HexStartScale) * hex;
            pose.HexAlpha = EaseOutCubic(Progress(t, HexStart, HexStart + 0.16f));
            pose.RingAlpha = EaseOutCubic(Progress(t, HexEnd - 0.14f, HexEnd + 0.06f));
            pose.HexFlash = t < HexEnd
                ? Progress(t, HexEnd - 0.08f, HexEnd)
                : 1f - EaseOutCubic(Progress(t, HexEnd, HexFlashEnd));
            pose.Idle = Progress(t, HexEnd - 0.06f, HexEnd);
            return pose;
        }

        public static ClosePose EvaluateClose(float t)
        {
            return new ClosePose
            {
                Glow = 1f - EaseOutCubic(Progress(t, 0f, GlowFadeDuration)),
                Alpha = 1f - EaseInOutSine(Progress(t, CloseFadeStart, CloseDuration)),
                Finished = t >= CloseDuration
            };
        }

        /// <summary>0~1 사이를 천천히 오가는 호흡. period초 주기, phase는 0~1 위상.</summary>
        public static float Breath(float time, float period, float phase)
        {
            if (period <= 0f) return 0f;
            var cycle = time / period + phase;
            return 0.5f - 0.5f * (float)Math.Cos(cycle * Math.PI * 2d);
        }

        /// <summary>화면이 좁아도 창이 넘치지 않게 하는 균일 배율(1 이하).</summary>
        public static float FitScale(float availableWidth, float availableHeight, float width, float height, float margin)
        {
            if (width <= 0f || height <= 0f) return 1f;
            var sx = (availableWidth - margin * 2f) / width;
            var sy = (availableHeight - margin * 2f) / height;
            var scale = Math.Min(sx, sy);
            if (float.IsNaN(scale) || scale <= 0f) return 1f;
            return Math.Min(1f, scale);
        }

        public static float Progress(float t, float start, float end)
        {
            if (end <= start) return t >= end ? 1f : 0f;
            var p = (t - start) / (end - start);
            return p < 0f ? 0f : (p > 1f ? 1f : p);
        }

        public static float EaseOutCubic(float p)
        {
            var inv = 1f - p;
            return 1f - inv * inv * inv;
        }

        public static float EaseInOutSine(float p)
        {
            return 0.5f - 0.5f * (float)Math.Cos(p * Math.PI);
        }
    }
}
