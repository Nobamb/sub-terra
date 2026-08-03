using System;
using UnityEngine;

namespace SubTerra.Gameplay.Hazards
{
    [Serializable]
    public sealed class GasExposureEffectSettings
    {
        [SerializeField, Min(0.1f)] private float tickInterval = 1f;
        [SerializeField, Min(0f)] private float fullIntensityEnergyDrainPerTick = 2f;
        [SerializeField, Range(0f, 1f)] private float minimumSpeedMultiplier = 0.55f;
        [SerializeField, Range(0f, 1f)] private float maximumVisionObscuration = 0.7f;
        [SerializeField, Min(0.1f)] private float failureExposureThreshold = 10f;
        [SerializeField, Min(0f)] private float recoveryPerTick = 1f;

        public float TickInterval => tickInterval;
        public float FullIntensityEnergyDrainPerTick => fullIntensityEnergyDrainPerTick;
        public float MinimumSpeedMultiplier => minimumSpeedMultiplier;
        public float MaximumVisionObscuration => maximumVisionObscuration;
        public float FailureExposureThreshold => failureExposureThreshold;
        public float RecoveryPerTick => recoveryPerTick;

        public GasExposureEffectSettings()
        {
        }

        public GasExposureEffectSettings(
            float interval,
            float energyDrain,
            float minimumSpeed,
            float maximumVision,
            float failureThreshold,
            float recovery)
        {
            tickInterval = Mathf.Max(0.1f, interval);
            fullIntensityEnergyDrainPerTick = Mathf.Max(0f, energyDrain);
            minimumSpeedMultiplier = Mathf.Clamp01(minimumSpeed);
            maximumVisionObscuration = Mathf.Clamp01(maximumVision);
            failureExposureThreshold = Mathf.Max(0.1f, failureThreshold);
            recoveryPerTick = Mathf.Max(0f, recovery);
        }
    }

    public readonly struct GasExposureEffectState : IEquatable<GasExposureEffectState>
    {
        public bool IsExposed { get; }
        public bool IsSheltered { get; }
        public string GasZoneId { get; }
        public float EffectiveIntensity { get; }
        public float CumulativeExposure { get; }
        public float FailureThreshold { get; }
        public float SpeedMultiplier { get; }
        public float VisionObscuration { get; }
        public GasRiskLevel Risk { get; }

        public GasExposureEffectState(
            bool isExposed,
            bool isSheltered,
            string gasZoneId,
            float effectiveIntensity,
            float cumulativeExposure,
            float failureThreshold,
            float speedMultiplier,
            float visionObscuration,
            GasRiskLevel risk)
        {
            IsExposed = isExposed;
            IsSheltered = isSheltered;
            GasZoneId = gasZoneId ?? string.Empty;
            EffectiveIntensity = effectiveIntensity;
            CumulativeExposure = cumulativeExposure;
            FailureThreshold = failureThreshold;
            SpeedMultiplier = speedMultiplier;
            VisionObscuration = visionObscuration;
            Risk = risk;
        }

        public bool Equals(GasExposureEffectState other)
        {
            return IsExposed == other.IsExposed
                && IsSheltered == other.IsSheltered
                && GasZoneId == other.GasZoneId
                && Risk == other.Risk
                && Mathf.Abs(EffectiveIntensity - other.EffectiveIntensity) < 0.001f
                && Mathf.Abs(CumulativeExposure - other.CumulativeExposure) < 0.001f
                && Mathf.Abs(SpeedMultiplier - other.SpeedMultiplier) < 0.001f
                && Mathf.Abs(VisionObscuration - other.VisionObscuration) < 0.001f;
        }

        public override bool Equals(object obj) => obj is GasExposureEffectState other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(IsExposed, IsSheltered, GasZoneId, Risk);
    }

    public readonly struct GasExposureTickResult
    {
        public GasExposureEffectState State { get; }
        public int EnergyDrain { get; }
        public bool FailureThresholdCrossed { get; }

        public GasExposureTickResult(
            GasExposureEffectState state,
            int energyDrain,
            bool failureThresholdCrossed)
        {
            State = state;
            EnergyDrain = energyDrain;
            FailureThresholdCrossed = failureThresholdCrossed;
        }
    }

    /// <summary>프레임률과 무관하게 고정 tick으로 전력과 누적 노출을 계산한다.</summary>
    public sealed class GasExposureEffectModel
    {
        private readonly GasExposureEffectSettings settings;
        private GasExposureState exposure;
        private float resistance;
        private bool sheltered;
        private float tickAccumulator;
        private float energyAccumulator;
        private float cumulativeExposure;
        private bool failureLatched;

        public GasExposureEffectState CurrentState { get; private set; }

        public GasExposureEffectModel(GasExposureEffectSettings effectSettings = null)
        {
            settings = effectSettings ?? new GasExposureEffectSettings();
            RefreshState();
        }

        public GasExposureEffectState SetExposure(
            GasExposureState nextExposure,
            float gasResistance,
            bool isSheltered)
        {
            exposure = nextExposure;
            resistance = Mathf.Clamp01(gasResistance);
            sheltered = isSheltered;
            if (!exposure.IsExposed || sheltered)
            {
                energyAccumulator = 0f;
            }

            RefreshState();
            return CurrentState;
        }

        public GasExposureTickResult Advance(float deltaTime)
        {
            tickAccumulator += Mathf.Max(0f, deltaTime);
            var drain = 0;
            var crossed = false;
            while (tickAccumulator + 0.0001f >= settings.TickInterval)
            {
                tickAccumulator -= settings.TickInterval;
                var effectiveIntensity = ResolveEffectiveIntensity();
                if (effectiveIntensity > 0f)
                {
                    cumulativeExposure += settings.TickInterval * effectiveIntensity;
                    energyAccumulator += settings.FullIntensityEnergyDrainPerTick * effectiveIntensity;
                    var wholeDrain = Mathf.FloorToInt(energyAccumulator + 0.0001f);
                    energyAccumulator -= wholeDrain;
                    drain += wholeDrain;
                }
                else
                {
                    cumulativeExposure = Mathf.Max(
                        0f,
                        cumulativeExposure - settings.RecoveryPerTick * settings.TickInterval);
                }

                if (!failureLatched
                    && cumulativeExposure >= settings.FailureExposureThreshold)
                {
                    failureLatched = true;
                    crossed = true;
                }
                else if (cumulativeExposure <= 0f)
                {
                    failureLatched = false;
                }
            }

            RefreshState();
            return new GasExposureTickResult(CurrentState, drain, crossed);
        }

        public void Reset()
        {
            exposure = default;
            resistance = 0f;
            sheltered = false;
            tickAccumulator = 0f;
            energyAccumulator = 0f;
            cumulativeExposure = 0f;
            failureLatched = false;
            RefreshState();
        }

        private float ResolveEffectiveIntensity()
        {
            return exposure.IsExposed && !sheltered
                ? GasRiskEvaluator.ClampIntensity(exposure.Intensity * (1f - resistance))
                : 0f;
        }

        private void RefreshState()
        {
            var intensity = ResolveEffectiveIntensity();
            var speed = Mathf.Lerp(1f, settings.MinimumSpeedMultiplier, intensity);
            var vision = settings.MaximumVisionObscuration * intensity;
            CurrentState = new GasExposureEffectState(
                exposure.IsExposed,
                sheltered && exposure.IsExposed,
                exposure.GasZoneId,
                intensity,
                cumulativeExposure,
                settings.FailureExposureThreshold,
                speed,
                vision,
                GasRiskEvaluator.Evaluate(intensity));
        }
    }
}
