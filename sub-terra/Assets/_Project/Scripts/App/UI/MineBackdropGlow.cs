using UnityEngine;

namespace SubTerra.App.UI
{
    /// <summary>배경 광물의 드문 반짝임 타이머. 일시정지(timeScale 0)와 모션 감소에서 멈추는 순수 로직.</summary>
    public sealed class MineBackdropGlow
    {
        private readonly System.Random random;
        private float untilNext;
        private float elapsed;
        private float duration;
        private bool active;
        private bool reduced;

        public float Clock { get; private set; }
        /// <summary>0..1 부드러운 반짝 곡선. 쉬는 동안 0.</summary>
        public float Envelope { get; private set; }
        /// <summary>반짝마다 달라지는 위치용 위상(라디안).</summary>
        public float Seed { get; private set; }

        public MineBackdropGlow(int randomSeed = 220187)
        {
            random = new System.Random(randomSeed);
            Seed = (float)(random.NextDouble() * Mathf.PI * 2f);
        }

        public void Advance(float deltaTime, float timeScale, bool reduceMotion, MineBackdropSettings settings)
        {
            reduced = reduceMotion;
            if (settings == null || deltaTime <= 0f || timeScale <= 0f) return;
            float delta = deltaTime * Mathf.Min(1f, timeScale);
            Clock += delta;
            if (reduceMotion)
            {
                active = false;
                Envelope = 0f;
                return;
            }
            if (untilNext <= 0f && !active) untilNext = Roll(settings.pulseIntervalMin, settings.pulseIntervalMax);
            if (!active)
            {
                untilNext -= delta;
                if (untilNext > 0f) return;
                active = true;
                elapsed = 0f;
                duration = Roll(settings.pulseDurationMin, settings.pulseDurationMax);
                Seed = (float)(random.NextDouble() * Mathf.PI * 2f);
                delta = -untilNext;
            }
            elapsed += delta;
            if (elapsed >= duration)
            {
                active = false;
                Envelope = 0f;
                untilNext = Roll(settings.pulseIntervalMin, settings.pulseIntervalMax);
                return;
            }
            float s = Mathf.Sin(Mathf.PI * elapsed / duration);
            Envelope = s * s;
        }

        public float LithiumBase(MineBackdropSettings settings) => reduced
            ? Mathf.Min(.08f, settings.reducedMotionLithiumGlow) : settings.lithiumGlowBase;
        public float LithiumPulse(MineBackdropSettings settings) => reduced ? 0f : settings.lithiumPulseBoost * Envelope;
        public float CopperPulse(MineBackdropSettings settings) => reduced ? 0f : settings.copperPulseStrength * Envelope;
        public float IronPulse(MineBackdropSettings settings) => reduced ? 0f : settings.ironPulseStrength * Envelope;

        public float LithiumShimmer(MineBackdropSettings settings) => reduced ? 0f : settings.lithiumShimmer;
        public float CopperShimmer(MineBackdropSettings settings) => reduced ? 0f : settings.copperShimmer;
        public float IronShimmer(MineBackdropSettings settings) => reduced ? 0f : settings.ironShimmer;

        private float Roll(float min, float max)
        {
            float lo = Mathf.Min(min, max), hi = Mathf.Max(min, max);
            return Mathf.Lerp(lo, hi, (float)random.NextDouble());
        }
    }
}
