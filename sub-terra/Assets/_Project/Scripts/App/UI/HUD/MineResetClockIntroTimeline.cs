using System;

namespace SubTerra.App.UI.HUD
{
    /// <summary>
    /// 3시간 만료 알림 직전 시계 연출 시간표. 순수 계산이라 Edit Mode에서 시점별 값을 검사할 수 있다.
    /// 등장(0→1.2→1) → 시침만 시계 방향 한 바퀴 → 시계 전체가 좌우로 짧게 흔들림 → 빠른 페이드아웃.
    /// </summary>
    public static class MineResetClockIntroTimeline
    {
        public const float PopEnd = 0.2f;
        public const float SettleEnd = 0.32f;
        public const float HandStart = 0.2f;
        public const float HandEnd = 0.78f;
        public const float ShakeEnd = 1f;
        public const float FadeEnd = 1.1f;
        public const float Duration = FadeEnd;

        public const float PeakScale = 1.2f;
        public const float ShakeAmplitude = 12f;
        public const float ShakeCycles = 2.5f;
        public const float DimFloor = 0.5f;

        public struct Pose
        {
            public float Scale;
            public float Alpha;
            /// <summary>시침의 z 회전(도). 음수가 시계 방향이며 한 바퀴는 -360이다.</summary>
            public float HandAngle;
            /// <summary>시계 전체의 가로 흔들림(px).</summary>
            public float ShakeX;
            public float Dim;
        }

        public static Pose Evaluate(float t)
        {
            var pose = new Pose();
            if (t < PopEnd)
            {
                pose.Scale = PeakScale * EaseOutCubic(Progress(t, 0f, PopEnd));
            }
            else
            {
                pose.Scale = PeakScale + (1f - PeakScale) * EaseInOutSine(Progress(t, PopEnd, SettleEnd));
            }

            pose.Alpha = Progress(t, 0f, 0.08f) * (1f - Progress(t, ShakeEnd, FadeEnd));
            pose.HandAngle = -360f * EaseInOutSine(Progress(t, HandStart, HandEnd));

            var shake = Progress(t, HandEnd, ShakeEnd);
            pose.ShakeX = (float)Math.Sin(shake * Math.PI * 2d * ShakeCycles) * ShakeAmplitude * (1f - 0.35f * shake);
            pose.Dim = DimFloor * Progress(t, 0f, 0.12f);
            return pose;
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
