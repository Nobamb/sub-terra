using System;
using SubTerra.App.UI.EmergencyEscape;
using SubTerra.App.UI.RunFailure;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.App.UI.DamageFeedback
{
    /// <summary>B-207: 일반 피해 순간의 짧은 붉은 글리치·틴트 값. 검사기에서 조절한다.</summary>
    [Serializable]
    public sealed class DamageHitGlitchSettings
    {
        public const float DefaultDuration = 0.28f;
        public const float DefaultTintMaxAlpha = 0.22f;
        public const float DefaultGlitchStrength = 0.28f;

        [Tooltip("붉은 글리치·틴트가 나타났다 사라지는 전체 시간(초). 연속 피격에도 이 길이를 넘지 않는다.")]
        [Range(0.2f, 0.4f)] public float hitGlitchDuration = DefaultDuration;
        [Tooltip("화면 전체 붉은 틴트의 최대 알파. 모션 감소에서도 남는다.")]
        [Range(0f, 0.5f)] public float hitTintMaxAlpha = DefaultTintMaxAlpha;
        [Tooltip("글리치 왜곡(스캔라인·조각) 최대 세기. 워프 글리치(0.55)보다 약하게 둔다.")]
        [Range(0f, 0.55f)] public float hitGlitchStrength = DefaultGlitchStrength;
        public Color tintColor = new Color(0.86f, 0.05f, 0.07f, 1f);
        public Color glitchColor = new Color(1f, 0.20f, 0.22f, 1f);
        public Color secondaryColor = new Color(0.55f, 0.02f, 0.06f, 1f);
    }

    public readonly struct DamageHitGlitchFrame
    {
        public static readonly DamageHitGlitchFrame Inactive = new DamageHitGlitchFrame(0f, 0f, 0, false);

        public float TintAlpha { get; }
        public float GlitchIntensity { get; }
        public int GlitchSeed { get; }
        public bool IsActive { get; }

        public DamageHitGlitchFrame(float tintAlpha, float glitchIntensity, int glitchSeed, bool isActive)
        {
            TintAlpha = tintAlpha;
            GlitchIntensity = glitchIntensity;
            GlitchSeed = glitchSeed;
            IsActive = isActive;
        }
    }

    /// <summary>
    /// B-207: 낙하·붕괴 피해에만 켜지는 짧은 붉은 글리치 시간표. unscaled 시간 기준.
    /// 다시 맞으면 남은 시간을 더하지 않고 처음부터 다시 시작한다. 모션 감소면 왜곡은 0, 틴트만 남는다.
    /// </summary>
    public sealed class DamageHitGlitchTimeline
    {
        /// <summary>조각 위치가 바뀌는 간격(초). 지터처럼 보이게 한다.</summary>
        public const float GlitchStepSeconds = 0.04f;
        /// <summary>시작 직후 틴트를 최대로 잡아 두는 비율.</summary>
        private const float TintHoldRatio = 0.12f;
        private const float EndToleranceSeconds = 0.0005f;

        private readonly DamageHitGlitchSettings settings;
        private float startedAt;
        private bool running;
        private int startCount;

        public DamageHitGlitchTimeline(DamageHitGlitchSettings settings = null)
        {
            this.settings = settings ?? new DamageHitGlitchSettings();
        }

        public DamageHitGlitchSettings Settings => settings;
        public float Duration => Mathf.Clamp(settings.hitGlitchDuration, 0.05f, 0.4f);
        public float TintMaxAlpha => Mathf.Clamp01(settings.hitTintMaxAlpha);
        public float GlitchStrength => Mathf.Clamp(settings.hitGlitchStrength, 0f, EscapeWarpTimeline.MaxGlitchIntensity);
        public bool IsRunning => running;
        public float StartedAt => startedAt;

        /// <summary>일반 피해(낙하·구조물 붕괴)만 켠다. 가스·산소 0 틱·전력 고갈은 제외.</summary>
        public static bool ShouldTrigger(RunFailureCause cause)
        {
            return cause == RunFailureCause.Fall || cause == RunFailureCause.StructuralCollapse;
        }

        /// <summary>켜지는 원인이면 지금 시각부터 다시 시작하고 true.</summary>
        public bool TryStart(RunFailureCause cause, float now)
        {
            if (!ShouldTrigger(cause))
            {
                return false;
            }

            startedAt = now;
            running = true;
            startCount++;
            return true;
        }

        public void Stop()
        {
            running = false;
        }

        public DamageHitGlitchFrame Evaluate(float now, bool reduceMotion)
        {
            if (!running)
            {
                return DamageHitGlitchFrame.Inactive;
            }

            var elapsed = Mathf.Max(0f, now - startedAt);
            var duration = Duration;
            // 큰 시각 값의 float 오차로 Duration 지점이 끝나지 않는 일을 막는다.
            if (elapsed >= duration - EndToleranceSeconds)
            {
                running = false;
                return DamageHitGlitchFrame.Inactive;
            }

            var t = elapsed / duration;
            var fade = 1f - Smooth(Mathf.Clamp01((t - TintHoldRatio) / (1f - TintHoldRatio)));
            var tint = TintMaxAlpha * fade;
            var step = Mathf.FloorToInt(elapsed / GlitchStepSeconds);
            var seed = 7919 + (startCount * 131) + step;
            var glitch = 0f;
            if (!reduceMotion)
            {
                // 끝으로 갈수록 빠르게 약해지고, 조각 단계마다 살짝 깜빡인다(0이 되지는 않는다).
                var flicker = 0.65f + (0.35f * RunFailureRevealTimeline.Hash01(seed, 3));
                glitch = GlitchStrength * Mathf.Pow(1f - t, 1.5f) * flicker;
            }

            return new DamageHitGlitchFrame(tint, glitch, seed, true);
        }

        private static float Smooth(float x)
        {
            return x * x * (3f - (2f * x));
        }
    }
}
