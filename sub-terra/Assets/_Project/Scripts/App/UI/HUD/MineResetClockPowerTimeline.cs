using System;

namespace SubTerra.App.UI.HUD
{
    public static class MineResetClockPowerTimeline
    {
        public const float OnDuration = 0.40f;
        public const float OffDuration = 0.28f;

        public struct Pose
        {
            public float SparkAlpha, BeamWidth01, BeamHeightPx, BeamAlpha;
            public float WindowWidth01, WindowHeight01, FrameAlpha, ReadoutAlpha, EdgeGlow, SettleFlash;
        }

        public static float Advance(float progress, bool on, float dt)
        {
            var step = Math.Max(0f, dt) / (on ? OnDuration : OffDuration);
            return Clamp(progress + (on ? step : -step));
        }

        public static Pose Evaluate(float progress)
        {
            var p = Clamp(progress);
            if (p <= 0f) return default;
            if (p >= 1f) return new Pose { WindowWidth01 = 1f, WindowHeight01 = 1f, FrameAlpha = 1f, ReadoutAlpha = 1f };
            var width = Ramp(p, 0.10f, 0.40f);
            var expansion = Ramp(p, 0.35f, 0.85f);
            expansion = 1f - (1f - expansion) * (1f - expansion) * (1f - expansion);
            return new Pose
            {
                SparkAlpha = Ramp(p, 0f, 0.15f) * (1f - Ramp(p, 0.15f, 0.40f)),
                BeamWidth01 = width,
                BeamHeightPx = 2.5f,
                BeamAlpha = Ramp(p, 0.10f, 0.20f) * (1f - Ramp(p, 0.40f, 0.65f)),
                WindowWidth01 = width,
                WindowHeight01 = width * (2.5f / 78f + (1f - 2.5f / 78f) * expansion),
                FrameAlpha = Ramp(p, 0.30f, 0.45f),
                ReadoutAlpha = Ramp(p, 0.55f, 0.90f),
                EdgeGlow = Ramp(p, 0.35f, 0.45f) * (1f - Ramp(p, 0.65f, 0.85f)),
                SettleFlash = Ramp(p, 0.80f, 0.87f) * (1f - Ramp(p, 0.87f, 1f))
            };
        }

        private static float Ramp(float p, float start, float end) => Clamp((p - start) / (end - start));
        private static float Clamp(float value) => Math.Max(0f, Math.Min(1f, value));
    }
}
