using System;

namespace SubTerra.App.UI.EmergencyEscape
{
    /// <summary>
    /// 긴급 탈출 포탈 창의 열기·닫기 연출 시간표. 순수 계산이라 Edit Mode에서 시점별 값을 검사할 수 있다.
    /// 열기: 웜홀 등장·가속 회전 → 웜홀 중심에서 창이 앞으로 튀어나옴(작고 기울어진 상태 → 정면) → 접속 줄 1~2줄.
    /// 닫기: 창이 웜홀로 빨려 들어가고 웜홀이 사라진다.
    /// </summary>
    public static class EscapePortalOpenTimeline
    {
        public const float SpinUpEnd = 0.6f;
        public const float PopStart = 0.6f;
        public const float PopEnd = 0.85f;
        public const float ConnectStart = 0.85f;
        public const float ConnectLineSeconds = 0.15f;
        public const int ConnectLineCount = 2;
        public const float ConnectEnd = ConnectStart + ConnectLineSeconds * ConnectLineCount;
        /// <summary>전체 열기 시간. 1.2초를 넘기지 않는다.</summary>
        public const float OpenDuration = ConnectEnd;
        /// <summary>창이 완전히 나온 뒤 블랙홀이 사라지는 데 걸리는 시간.</summary>
        public const float WormholeFadeOutSeconds = 0.25f;
        public const float WormholeGoneAt = PopEnd + WormholeFadeOutSeconds;
        // 닫기: 블랙홀이 먼저 나타나고 → 창이 빨려 들어가고 → 블랙홀이 사라진다.
        public const float CloseWormholeInEnd = 0.2f;
        public const float ClosePullStart = 0.12f;
        public const float ClosePullEnd = 0.42f;
        public const float CloseWormholeOutStart = 0.38f;
        public const float CloseDuration = 0.5f;

        /// <summary>
        /// 소용돌이 기준 회전 속도(도/초). 처음엔 느리게 시작해 SpinUpEnd에서 최대에 도달한다.
        /// 선풍기처럼 보이지 않게 느리고 묵직하며, 레이어마다 배율을 달리해 안쪽이 더 빨리 돈다.
        /// </summary>
        public const float SpinSpeedStart = 50f;
        public const float SpinSpeedMax = 210f;
        public const float PopStartScale = 0.15f;
        /// <summary>창이 처음에 뒤로 눕는 각도(도). 정면에 가까워지며 0이 된다.</summary>
        public const float PopStartTilt = 55f;
        public const float WormholeFadeInEnd = 0.1f;

        public struct Pose
        {
            public float WormholeAlpha;
            public float WormholeScale;
            /// <summary>웜홀 소용돌이 기준 z 회전(도). 시간이 갈수록 빨라지는 누적 각도.</summary>
            public float WormholeAngle;
            public float WindowScale;
            /// <summary>창의 x축 기울기(도). 0이면 정면.</summary>
            public float WindowTilt;
            public float WindowAlpha;
            /// <summary>접속 줄별 글자 표시 비율(0~1).</summary>
            public float Line1;
            public float Line2;
            public bool Interactable;
            public bool Finished;
        }

        public static Pose EvaluateOpen(float t)
        {
            t = Math.Max(0f, t);
            var pose = new Pose();
            pose.WormholeAngle = SpinAngle(t);
            pose.WormholeScale = 0.55f + 0.45f * EaseOutCubic(Progress(t, 0f, SpinUpEnd));
            // 창이 튀어나온 뒤에는 웜홀이 사라진다. 창이 열려 있는 동안에는 보이지 않는다.
            pose.WormholeAlpha = Progress(t, 0f, WormholeFadeInEnd)
                * (1f - Progress(t, PopEnd, WormholeGoneAt));

            var pop = EaseOutCubic(Progress(t, PopStart, PopEnd));
            pose.WindowScale = PopStartScale + (1f - PopStartScale) * pop;
            pose.WindowTilt = PopStartTilt * (1f - pop);
            pose.WindowAlpha = Progress(t, PopStart, PopStart + 0.08f);

            pose.Line1 = Progress(t, ConnectStart, ConnectStart + ConnectLineSeconds);
            pose.Line2 = Progress(t, ConnectStart + ConnectLineSeconds, ConnectEnd);
            pose.Interactable = t >= PopEnd;
            pose.Finished = t >= OpenDuration;
            return pose;
        }

        /// <summary>연출 중 아무 키나 눌렀을 때 바로 도달하는 끝 상태.</summary>
        public static Pose OpenEndPose => EvaluateOpen(OpenDuration);

        public static Pose EvaluateClose(float t, float startAngle)
        {
            t = Math.Max(0f, t);
            var appear = EaseOutCubic(Progress(t, 0f, CloseWormholeInEnd));
            var windowP = Progress(t, ClosePullStart, ClosePullEnd);
            var pull = EaseInCubic(windowP);
            var fadeOut = Progress(t, CloseWormholeOutStart, CloseDuration);
            var pose = new Pose();
            pose.WindowScale = 1f + (PopStartScale - 1f) * pull;
            pose.WindowTilt = -PopStartTilt * 0.5f * pull;
            pose.WindowAlpha = 1f - Progress(windowP, 0.4f, 1f);
            // 블랙홀이 먼저 나타나 창을 빨아들이고, 창이 사라진 뒤 함께 사라진다.
            pose.WormholeAlpha = appear * (1f - fadeOut);
            pose.WormholeScale = (0.55f + 0.45f * appear) * (1f - 0.4f * fadeOut);
            pose.WormholeAngle = startAngle + SpinSpeedMax * t;
            pose.Interactable = false;
            pose.Finished = t >= CloseDuration;
            return pose;
        }

        /// <summary>열기 중 입자를 새로 만드는 구간. 창이 나온 뒤에는 만들지 않아 남은 입자만 흩어진다.</summary>
        public static bool OpenEmitsParticles(float t) => t < PopEnd;

        /// <summary>닫기 중 입자를 새로 만드는 구간. 블랙홀이 사라지기 시작하면 멈춘다.</summary>
        public static bool CloseEmitsParticles(float t) => t < CloseWormholeOutStart;

        /// <summary>시각 t까지 누적한 웜홀 회전각(도). 0.5초 동안 가속한 뒤 최대 속도로 계속 돈다.</summary>
        public static float SpinAngle(float t)
        {
            t = Math.Max(0f, t);
            if (t <= SpinUpEnd)
            {
                return SpinSpeedStart * t + (SpinSpeedMax - SpinSpeedStart) * t * t / (2f * SpinUpEnd);
            }

            var spinUp = SpinSpeedStart * SpinUpEnd + (SpinSpeedMax - SpinSpeedStart) * SpinUpEnd * 0.5f;
            return spinUp + SpinSpeedMax * (t - SpinUpEnd);
        }

        /// <summary>시각 t의 순간 회전 속도(도/초).</summary>
        public static float SpinSpeed(float t)
        {
            t = Math.Max(0f, t);
            return t >= SpinUpEnd
                ? SpinSpeedMax
                : SpinSpeedStart + (SpinSpeedMax - SpinSpeedStart) * t / SpinUpEnd;
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

        public static float EaseInCubic(float p)
        {
            return p * p * p;
        }
    }
}
