using System;
using SubTerra.App.State;
using UnityEngine;

namespace SubTerra.App.UI.HUD
{
    /// <summary>단계(Elevated=Caution, Hazard=Critical) 하나의 연출 수치. Inspector에서 조절한다.</summary>
    [Serializable]
    public sealed class GasPoisonLevelSettings
    {
        [SerializeField, Range(0f, 1f)] private float vignetteMaxAlpha;
        [SerializeField, Min(0.1f)] private float pulsePeriod;
        [SerializeField, Range(0f, 1f)] private float vignetteMinAlphaRatio;
        [SerializeField, Range(0f, 1f)] private float filterAlpha;
        [SerializeField, Min(0.5f)] private float vignetteScale;
        [SerializeField, Range(0f, 1f)] private float streakAlphaRatio;

        public float VignetteMaxAlpha => vignetteMaxAlpha;
        public float PulsePeriod => pulsePeriod > 0.1f ? pulsePeriod : 0.1f;
        public float VignetteMinAlphaRatio => vignetteMinAlphaRatio;
        public float FilterAlpha => filterAlpha;
        public float VignetteScale => vignetteScale > 0.5f ? vignetteScale : 0.5f;
        /// <summary>방사 줄무늬 알파 = 비네팅 알파 × 이 비율. 맥박은 비네팅과 같이 간다.</summary>
        public float StreakAlphaRatio => streakAlphaRatio;

        public GasPoisonLevelSettings(
            float vignetteMaxAlpha,
            float pulsePeriod,
            float vignetteMinAlphaRatio,
            float filterAlpha,
            float vignetteScale,
            float streakAlphaRatio = 0.5f)
        {
            this.vignetteMaxAlpha = vignetteMaxAlpha;
            this.pulsePeriod = pulsePeriod;
            this.vignetteMinAlphaRatio = vignetteMinAlphaRatio;
            this.filterAlpha = filterAlpha;
            this.vignetteScale = vignetteScale;
            this.streakAlphaRatio = streakAlphaRatio;
        }
    }

    /// <summary>가스 중독 화면 연출 전체 설정.</summary>
    [Serializable]
    public sealed class GasPoisonOverlaySettings
    {
        [SerializeField] private GasPoisonLevelSettings caution = new GasPoisonLevelSettings(0.35f, 2f, 0.3f, 0.06f, 1.06f, 0.4f);
        [SerializeField] private GasPoisonLevelSettings critical = new GasPoisonLevelSettings(0.6f, 1.2f, 0.35f, 0.12f, 1f, 0.7f);
        [SerializeField, Min(0.01f)] private float enterSeconds = 0.4f;
        [SerializeField, Min(0.01f)] private float changeSeconds = 0.3f;
        [SerializeField, Min(0.01f)] private float exitSeconds = 0.5f;
        [SerializeField, Min(0.5f)] private float restScale = 1.06f;

        public GasPoisonLevelSettings Caution => caution;
        public GasPoisonLevelSettings Critical => critical;
        public float EnterSeconds => enterSeconds > 0.01f ? enterSeconds : 0.01f;
        public float ChangeSeconds => changeSeconds > 0.01f ? changeSeconds : 0.01f;
        public float ExitSeconds => exitSeconds > 0.01f ? exitSeconds : 0.01f;
        public float RestScale => restScale > 0.5f ? restScale : 0.5f;

        public GasPoisonOverlaySettings()
        {
        }

        public GasPoisonOverlaySettings(GasPoisonLevelSettings caution, GasPoisonLevelSettings critical)
        {
            this.caution = caution;
            this.critical = critical;
        }
    }

    /// <summary>타임라인이 한 프레임에 내놓는 값. View는 이 값을 Image에 적용만 한다.</summary>
    public readonly struct GasPoisonOverlayFrame
    {
        public float VignetteAlpha { get; }
        public float VignetteScale { get; }
        public float FilterAlpha { get; }
        public float StreakAlpha { get; }
        /// <summary>연출이 켜진 정도(0~1). 맥박과 무관하게 진입·이탈에 따라서만 단조롭게 변한다.</summary>
        public float Presence { get; }

        public bool IsVisible => VignetteAlpha > 0.0005f || FilterAlpha > 0.0005f || StreakAlpha > 0.0005f;

        public GasPoisonOverlayFrame(
            float vignetteAlpha,
            float vignetteScale,
            float filterAlpha,
            float presence,
            float streakAlpha = 0f)
        {
            StreakAlpha = streakAlpha;
            VignetteAlpha = vignetteAlpha;
            VignetteScale = vignetteScale;
            FilterAlpha = filterAlpha;
            Presence = presence;
        }
    }

    /// <summary>
    /// 가스 중독 화면 연출 시간표(순수 계산). 입력은 위험 단계와 경과 시간, 출력은 비네팅 알파·스케일과 필터 알파.
    /// 단계별 가중치(0~1)를 목표로 선형 이동시킨 뒤 smoothstep으로 풀어 값이 튀지 않게 한다.
    /// 맥박은 단계별 코사인 곡선이라 딱딱 끊기지 않고, 단계 가중치로 섞여 단계 전환 때도 이어진다.
    /// </summary>
    public sealed class GasPoisonOverlayTimeline
    {
        private readonly GasPoisonOverlaySettings settings;

        private float cautionWeight;
        private float criticalWeight;
        private float cautionPhase;
        private float criticalPhase;
        private float rate;
        private GasRiskLevel target = GasRiskLevel.Safe;

        public GasPoisonOverlayTimeline(GasPoisonOverlaySettings settings = null)
        {
            this.settings = settings ?? new GasPoisonOverlaySettings();
            rate = 1f / this.settings.EnterSeconds;
        }

        public GasRiskLevel Target => target;

        /// <summary>B-168: 지금 가중치가 더 큰 단계(둘 다 0이면 Safe). 읽기 전용이며 값 계산에는 쓰지 않는다.</summary>
        public GasRiskLevel DominantLevel
        {
            get
            {
                if (cautionWeight <= 0f && criticalWeight <= 0f)
                {
                    return GasRiskLevel.Safe;
                }

                return criticalWeight >= cautionWeight ? GasRiskLevel.Hazard : GasRiskLevel.Elevated;
            }
        }

        /// <summary>B-168: 우세 단계의 맥박 위상(0~1). 0.5에서 비네팅이 가장 밝다.</summary>
        public float DominantPhase => DominantLevel == GasRiskLevel.Hazard ? criticalPhase : cautionPhase;

        /// <summary>목표 단계를 바꾼다. 진입·단계 변경·이탈에 맞춰 보간 속도를 고른다.</summary>
        public void SetLevel(GasRiskLevel level)
        {
            if (level == target)
            {
                return;
            }

            if (level == GasRiskLevel.Safe)
            {
                rate = 1f / settings.ExitSeconds;
            }
            else if (target == GasRiskLevel.Safe)
            {
                rate = 1f / settings.EnterSeconds;
            }
            else
            {
                rate = 1f / settings.ChangeSeconds;
            }

            target = level;
        }

        /// <summary>즉시 모두 끈다(사망·런 종료·씬 전환). 맥박 위상도 처음으로 돌린다.</summary>
        public void Reset()
        {
            target = GasRiskLevel.Safe;
            cautionWeight = 0f;
            criticalWeight = 0f;
            cautionPhase = 0f;
            criticalPhase = 0f;
            rate = 1f / settings.EnterSeconds;
        }

        public GasPoisonOverlayFrame Advance(float deltaTime)
        {
            var dt = deltaTime > 0f ? deltaTime : 0f;
            var step = rate * dt;
            cautionWeight = MoveToward(cautionWeight, target == GasRiskLevel.Elevated ? 1f : 0f, step);
            criticalWeight = MoveToward(criticalWeight, target == GasRiskLevel.Hazard ? 1f : 0f, step);

            cautionPhase = Wrap(cautionPhase + dt / settings.Caution.PulsePeriod);
            criticalPhase = Wrap(criticalPhase + dt / settings.Critical.PulsePeriod);
            return Evaluate();
        }

        /// <summary>시간을 흘리지 않고 현재 값만 본다.</summary>
        public GasPoisonOverlayFrame Evaluate()
        {
            var c = Smooth(cautionWeight);
            var k = Smooth(criticalWeight);
            var caution = settings.Caution;
            var critical = settings.Critical;

            var cautionPulse = PulseAlpha(caution, cautionPhase);
            var criticalPulse = PulseAlpha(critical, criticalPhase);
            var alpha = c * cautionPulse + k * criticalPulse;
            var streak = c * cautionPulse * caution.StreakAlphaRatio + k * criticalPulse * critical.StreakAlphaRatio;
            var filter = c * caution.FilterAlpha + k * critical.FilterAlpha;
            var rest = settings.RestScale;
            var scale = rest + c * (caution.VignetteScale - rest) + k * (critical.VignetteScale - rest);
            return new GasPoisonOverlayFrame(
                Mathf.Clamp01(alpha),
                scale,
                Mathf.Clamp01(filter),
                Mathf.Clamp01(c + k),
                Mathf.Clamp01(streak));
        }

        private static float PulseAlpha(GasPoisonLevelSettings level, float phase)
        {
            var wave = 0.5f - 0.5f * Mathf.Cos(phase * Mathf.PI * 2f);
            var min = level.VignetteMaxAlpha * level.VignetteMinAlphaRatio;
            return Mathf.Lerp(min, level.VignetteMaxAlpha, wave);
        }

        private static float MoveToward(float value, float goal, float step)
        {
            if (value < goal)
            {
                return Mathf.Min(goal, value + step);
            }

            return Mathf.Max(goal, value - step);
        }

        private static float Smooth(float x)
        {
            x = Mathf.Clamp01(x);
            return x * x * (3f - 2f * x);
        }

        private static float Wrap(float phase)
        {
            return phase - Mathf.Floor(phase);
        }
    }
}
