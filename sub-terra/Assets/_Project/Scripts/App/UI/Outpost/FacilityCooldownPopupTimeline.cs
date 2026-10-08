using System;

namespace SubTerra.App.UI.Outpost
{
    /// <summary>
    /// 재사용 대기 팝업의 TV 켜짐/꺼짐 자세와, 남은 시간에 따른 마지막 30초 응집 강도.
    /// 모두 순수 함수라 EditMode에서 검증한다. 켜짐은 중앙 빛 → 가로선 → 상하 펼침 → 내용 순서,
    /// 꺼짐은 같은 진행값을 거꾸로 써서 내용 소멸 → 상하 접힘 → 점 소멸 순서가 된다.
    /// </summary>
    public static class FacilityCooldownPopupTimeline
    {
        public const float OnDuration = 0.32f;
        public const float OffDuration = 0.26f;

        /// <summary>사용 가능 시점 직전 이 시간 안에 관측되던 대기였으면 완료 연출을 재생한다.</summary>
        public const double CompletionObserveSeconds = 1.5d;

        /// <summary>0초에 프레임 가장자리부터 중앙까지 빠르게 환해지는 시간. 끝나면 곧바로 TV 꺼짐이 시작된다.</summary>
        public const float CompletionFlashSeconds = 0.22f;

        /// <summary>5초 시점에 바깥 빛이 도달해 있는 세기. 이후 0초까지 1로 올라간다.</summary>
        public const float OuterGlowAtFinalWindow = 0.4f;

        /// <summary>안쪽 가장자리 빛의 최대 세기(0초 직전). 글자 가독성을 위해 1보다 낮게 둔다.</summary>
        public const float InnerGlowMax = 0.6f;

        /// <summary>응집 효과가 시작되는 남은 시간(초).</summary>
        public const double GatherStartSeconds = 30d;

        /// <summary>입자 속도가 올라가는 남은 시간(초).</summary>
        public const double FinalWindowSeconds = 5d;

        public const float FinalParticleSpeedScale = 1.5f;

        /// <summary>응집 강도가 목표값을 향해 움직일 수 있는 최대 속도(1/초).</summary>
        public const float GatherRatePerSecond = 2f;

        public struct Pose
        {
            public float SparkAlpha;
            public float BeamWidth01;
            public float BeamHeightPx;
            public float BeamAlpha;
            public float WindowWidth01;
            public float WindowHeight01;
            public float FrameAlpha;
            public float ReadoutAlpha;
        }

        public static float Advance(float progress, bool on, float dt)
        {
            var step = Math.Max(0f, dt) / (on ? OnDuration : OffDuration);
            return Clamp(progress + (on ? step : -step));
        }

        public static Pose Evaluate(float progress)
        {
            var p = Clamp(progress);
            if (p <= 0f)
            {
                return default;
            }

            if (p >= 1f)
            {
                return new Pose { WindowWidth01 = 1f, WindowHeight01 = 1f, FrameAlpha = 1f, ReadoutAlpha = 1f };
            }

            var width = Ramp(p, 0.10f, 0.40f);
            var expansion = Ramp(p, 0.35f, 0.85f);
            expansion = 1f - (1f - expansion) * (1f - expansion) * (1f - expansion);
            const float line = 2.5f / 210f;
            return new Pose
            {
                SparkAlpha = Ramp(p, 0f, 0.15f) * (1f - Ramp(p, 0.15f, 0.40f)),
                BeamWidth01 = width,
                BeamHeightPx = 2.5f,
                BeamAlpha = Ramp(p, 0.10f, 0.20f) * (1f - Ramp(p, 0.40f, 0.65f)),
                WindowWidth01 = width,
                WindowHeight01 = width * (line + (1f - line) * expansion),
                FrameAlpha = Ramp(p, 0.30f, 0.45f),
                ReadoutAlpha = Ramp(p, 0.55f, 0.90f)
            };
        }

        /// <summary>남은 시간에 따른 응집 강도 0~1. 30초 이상은 0, 0초에 1이며 부드럽게 올라간다.</summary>
        public static float Gather01(double remainingSeconds)
        {
            if (remainingSeconds > GatherStartSeconds)
            {
                return 0f;
            }

            if (remainingSeconds <= 0d)
            {
                return 1f;
            }

            var t = Clamp((float)((GatherStartSeconds - remainingSeconds) / GatherStartSeconds));
            return t * t * (3f - 2f * t);
        }

        /// <summary>안쪽 가장자리 띠 빛의 최대 세기(0초 직전). 30초부터 켜지며 넓은 안쪽 빛보다 옅게 둔다.</summary>
        public const float InnerEdgeMax = 0.55f;

        /// <summary>5초 시점에 안쪽 가장자리 띠 빛이 도달해 있는 세기(최대값 대비 비율).</summary>
        public const float InnerEdgeLevelAtFinalWindow = 0.45f;

        /// <summary>5초 시점에 안쪽 가장자리 띠가 도달해 있는 두께(최대 두께 대비 비율). 30초에는 거의 0이다.</summary>
        public const float InnerEdgeSizeAtFinalWindow = 0.55f;

        /// <summary>
        /// 프레임 바깥 빛·입자 세기 0~1. 30초 이상은 0, 5초에 <see cref="OuterGlowAtFinalWindow"/>,
        /// 0초에 1이며 5초 지점에서 멈칫하지 않고 이어지는 곡선이다.
        /// </summary>
        public static float OuterGlow01(double remainingSeconds)
        {
            return TwoStageCurve(remainingSeconds, OuterGlowAtFinalWindow);
        }

        /// <summary>
        /// 프레임 안쪽 가장 바깥 띠의 빛 세기 0~<see cref="InnerEdgeMax"/>. 30초부터 서서히 켜진다.
        /// </summary>
        public static float InnerEdgeGlow01(double remainingSeconds)
        {
            return InnerEdgeMax * TwoStageCurve(remainingSeconds, InnerEdgeLevelAtFinalWindow);
        }

        /// <summary>안쪽 가장자리 띠의 두께 0~1. 30초에는 얇고 5초에는 <see cref="InnerEdgeSizeAtFinalWindow"/>, 0초에 1이다.</summary>
        public static float InnerEdgeSize01(double remainingSeconds)
        {
            return TwoStageCurve(remainingSeconds, InnerEdgeSizeAtFinalWindow);
        }

        /// <summary>
        /// 30초(0) → 5초(atFinalWindow) → 0초(1)를 지나는 단조 증가 곡선.
        /// 5초 지점에서 기울기를 이어 붙여(Hermite) 밝아지다 멈췄다가 다시 오르는 끊김을 없앤다.
        /// 30초 직후에는 완만하지만 0이 아닌 기울기로 시작해 빛이 늦게 튀어나오지 않게 한다.
        /// </summary>
        private static float TwoStageCurve(double remainingSeconds, float atFinalWindow)
        {
            if (remainingSeconds >= GatherStartSeconds)
            {
                return 0f;
            }

            if (remainingSeconds <= 0d)
            {
                return 1f;
            }

            var span = (float)(GatherStartSeconds - FinalWindowSeconds);
            var tail = (float)FinalWindowSeconds;
            var earlySlope = atFinalWindow / span;
            var lateSlope = (1f - atFinalWindow) / tail;
            var startSlope = 0.5f * earlySlope;
            var joinSlope = 2f * earlySlope * lateSlope / (earlySlope + lateSlope);

            if (remainingSeconds > FinalWindowSeconds)
            {
                var t = Clamp((float)((GatherStartSeconds - remainingSeconds) / span));
                return Hermite(0f, atFinalWindow, startSlope * span, joinSlope * span, t);
            }

            var u = Clamp((float)((FinalWindowSeconds - remainingSeconds) / tail));
            return Hermite(atFinalWindow, 1f, joinSlope * tail, lateSlope * tail, u);
        }

        private static float Hermite(float p0, float p1, float m0, float m1, float t)
        {
            var t2 = t * t;
            var t3 = t2 * t;
            return (2f * t3 - 3f * t2 + 1f) * p0
                + (t3 - 2f * t2 + t) * m0
                + (-2f * t3 + 3f * t2) * p1
                + (t3 - t2) * m1;
        }

        /// <summary>프레임 안쪽 가장자리 옅은 빛 세기 0~<see cref="InnerGlowMax"/>. 마지막 5초에만 켜진다.</summary>
        public static float InnerGlow01(double remainingSeconds)
        {
            if (remainingSeconds > FinalWindowSeconds)
            {
                return 0f;
            }

            if (remainingSeconds <= 0d)
            {
                return InnerGlowMax;
            }

            var t = Clamp((float)((FinalWindowSeconds - remainingSeconds) / FinalWindowSeconds));
            return InnerGlowMax * Smooth(t);
        }

        /// <summary>완료 섬광 진행 u(0~1)에서 가장자리부터 차오르는 빛의 세기.</summary>
        public static float FlashEdge01(float u) => Smooth(Ramp(u, 0f, 0.55f));

        /// <summary>완료 섬광 진행 u(0~1)에서 중앙까지 번지는 빛의 세기. 가장자리보다 늦게 시작한다.</summary>
        public static float FlashCore01(float u) => Smooth(Ramp(u, 0.3f, 1f));

        public static bool IsFinalWindow(double remainingSeconds)
        {
            return remainingSeconds > 0d && remainingSeconds <= FinalWindowSeconds;
        }

        public static float ParticleSpeed(double remainingSeconds)
        {
            return IsFinalWindow(remainingSeconds) ? FinalParticleSpeedScale : 1f;
        }

        /// <summary>청록에서 푸른빛으로 섞는 비율. 최대 35%.</summary>
        public static float BlueTint01(double remainingSeconds)
        {
            return Gather01(remainingSeconds) * 0.35f;
        }

        private static float Smooth(float t) => t * t * (3f - 2f * t);

        private static float Ramp(float p, float start, float end) => Clamp((p - start) / (end - start));

        private static float Clamp(float value) => Math.Max(0f, Math.Min(1f, value));
    }
}
