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
        public const float CloseDuration = 0.2f;

        /// <summary>회전 속도(도/초). 처음엔 느리게 시작해 SpinUpEnd에서 최대에 도달한다.</summary>
        public const float SpinSpeedStart = 120f;
        public const float SpinSpeedMax = 1260f;
        public const float PopStartScale = 0.15f;
        /// <summary>창이 처음에 뒤로 눕는 각도(도). 정면에 가까워지며 0이 된다.</summary>
        public const float PopStartTilt = 55f;
        public const float WormholeFadeInEnd = 0.1f;

        public struct Pose
        {
            public float WormholeAlpha;
            public float WormholeScale;
            /// <summary>웜홀 z 회전(도). 시간이 갈수록 빨라지는 누적 각도.</summary>
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
            // 창이 튀어나오는 동안 웜홀은 뒤로 물러나며 옅어지지만, 창 뒤에서 계속 돈다.
            pose.WormholeAlpha = Progress(t, 0f, WormholeFadeInEnd)
                * (1f - 0.4f * Progress(t, PopStart, PopEnd));

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
            var p = Progress(t, 0f, CloseDuration);
            var pull = EaseInCubic(p);
            var pose = new Pose();
            pose.WindowScale = 1f + (PopStartScale - 1f) * pull;
            pose.WindowTilt = -PopStartTilt * 0.5f * pull;
            pose.WindowAlpha = 1f - Progress(p, 0.4f, 1f);
            // 창이 빨려 들어가는 동안 웜홀이 다시 또렷해졌다가 마지막에 사라진다.
            pose.WormholeAlpha = Math.Min(1f, 0.45f + 0.55f * Progress(p, 0f, 0.5f)) * (1f - Progress(p, 0.7f, 1f));
            pose.WormholeScale = 1f - 0.4f * Progress(p, 0.7f, 1f);
            pose.WormholeAngle = startAngle + SpinSpeedMax * t;
            pose.Interactable = false;
            pose.Finished = t >= CloseDuration;
            return pose;
        }

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
