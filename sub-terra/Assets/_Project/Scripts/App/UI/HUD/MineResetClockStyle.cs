using System;
using UnityEngine;

namespace SubTerra.App.UI.HUD
{
    public enum MineResetClockBand { Normal, Warning, Critical, Final }

    public sealed class MineResetClockStyle
    {
        public const float TransitionDuration = 1.5f;
        public const float PulsePeriod = 1.75f;
        public static readonly Color NormalColor = new Color(0.45f, 0.96f, 1f, 1f);
        public static readonly Color WarningColor = new Color(1f, 0.78f, 0.2f, 1f);
        public static readonly Color CriticalColor = new Color(1f, 0.24f, 0.2f, 1f);

        private int previousSeconds;
        private bool observed;
        private Color from;
        private Color target;
        private float transitionTime;
        private float pulseTime;

        public MineResetClockBand Band { get; private set; }
        public Color DisplayColor { get; private set; } = NormalColor;
        public bool IsTransitioning { get; private set; }
        public bool IsPulsing => observed && Band == MineResetClockBand.Final;
        public float PulseFactor { get; private set; } = 1f;

        public static MineResetClockBand GetBand(int displaySeconds)
        {
            if (displaySeconds > 1800) return MineResetClockBand.Normal;
            if (displaySeconds > 300) return MineResetClockBand.Warning;
            return displaySeconds > 60 ? MineResetClockBand.Critical : MineResetClockBand.Final;
        }

        public static Color GetColor(MineResetClockBand band)
        {
            return band == MineResetClockBand.Normal ? NormalColor
                : band == MineResetClockBand.Warning ? WarningColor : CriticalColor;
        }

        public bool Observe(int displaySeconds)
        {
            if (observed && previousSeconds == displaySeconds) return false;
            var nextBand = GetBand(displaySeconds);
            var decrement = previousSeconds - displaySeconds;
            var snap = !observed || decrement < 0 || decrement > 3;
            if (snap)
            {
                DisplayColor = GetColor(nextBand);
                IsTransitioning = false;
                transitionTime = 0f;
                pulseTime = 0f;
                PulseFactor = 1f;
            }
            else if (nextBand != Band)
            {
                from = DisplayColor;
                target = GetColor(nextBand);
                transitionTime = 0f;
                IsTransitioning = true;
                pulseTime = 0f;
                PulseFactor = 1f;
            }
            Band = nextBand;
            previousSeconds = displaySeconds;
            observed = true;
            return true;
        }

        public void Advance(float dt)
        {
            if (!observed || dt <= 0f) return;
            if (IsTransitioning)
            {
                transitionTime = Math.Min(TransitionDuration, transitionTime + dt);
                var p = transitionTime / TransitionDuration;
                p = p * p * (3f - 2f * p);
                DisplayColor = new Color(from.r + (target.r - from.r) * p,
                    from.g + (target.g - from.g) * p, from.b + (target.b - from.b) * p, 1f);
                IsTransitioning = transitionTime < TransitionDuration;
            }
            if (IsPulsing)
            {
                pulseTime = (pulseTime + dt) % PulsePeriod;
                PulseFactor = 0.725f + 0.275f * (float)Math.Cos(pulseTime * Math.PI * 2d / PulsePeriod);
            }
        }

        public void Reset()
        {
            observed = false;
            previousSeconds = 0;
            IsTransitioning = false;
            transitionTime = pulseTime = 0f;
            PulseFactor = 1f;
            Band = MineResetClockBand.Normal;
            DisplayColor = NormalColor;
        }
    }
}
