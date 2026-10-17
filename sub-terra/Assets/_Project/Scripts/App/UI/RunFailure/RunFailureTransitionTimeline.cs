using SubTerra.App.UI.Sell;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.App.UI.RunFailure
{
    public enum RunFailureTransitionPhase
    {
        NotStarted = 0,
        Darken = 1,
        HeavyGlitch = 2,
        TvTurnOn = 3,
        Completed = 4
    }

    public readonly struct RunFailureTransitionFrame
    {
        public RunFailureTransitionPhase Phase { get; }
        public float ElapsedSeconds { get; }
        public float PhaseNormalizedTime { get; }
        public float WindowAlpha { get; }
        public float WindowScaleY { get; }
        public float WindowGlitchIntensity { get; }
        public Vector2 WindowJitterOffset { get; }
        public float BlackoutAlpha { get; }
        public float HeavyGlitchIntensity { get; }
        public int GlitchSeed { get; }
        public Color GlitchColor { get; }
        public Color SecondarySplitColor { get; }
        public bool ShouldMovePlayer { get; }
        public float TvLineWidthNormalized { get; }
        public float TvLineAlpha { get; }
        public float TvApertureOpenNormalized { get; }
        public bool IsComplete { get; }

        public RunFailureTransitionFrame(
            RunFailureTransitionPhase phase,
            float elapsedSeconds,
            float phaseNormalizedTime,
            float windowAlpha,
            float windowScaleY,
            float blackoutAlpha,
            float heavyGlitchIntensity,
            Color glitchColor,
            Color secondarySplitColor,
            bool shouldMovePlayer,
            float tvLineWidthNormalized,
            float tvLineAlpha,
            float tvApertureOpenNormalized,
            bool isComplete,
            float windowGlitchIntensity = 0f,
            int glitchSeed = 0,
            Vector2 windowJitterOffset = default)
        {
            Phase = phase;
            ElapsedSeconds = elapsedSeconds;
            PhaseNormalizedTime = phaseNormalizedTime;
            WindowAlpha = windowAlpha;
            WindowScaleY = windowScaleY;
            WindowGlitchIntensity = windowGlitchIntensity;
            WindowJitterOffset = windowJitterOffset;
            BlackoutAlpha = blackoutAlpha;
            HeavyGlitchIntensity = heavyGlitchIntensity;
            GlitchSeed = glitchSeed;
            GlitchColor = glitchColor;
            SecondarySplitColor = secondarySplitColor;
            ShouldMovePlayer = shouldMovePlayer;
            TvLineWidthNormalized = tvLineWidthNormalized;
            TvLineAlpha = tvLineAlpha;
            TvApertureOpenNormalized = tvApertureOpenNormalized;
            IsComplete = isComplete;
        }
    }

    /// <summary>
    /// B-148: 확인 입력 이후 화면 암전(0.4s) → 강한 전체 글리치 및 플레이어 이동(0.6s) → TV 켜짐(0.4s) 전환을 계산한다.
    /// </summary>
    public sealed class RunFailureTransitionTimeline
    {
        private readonly float darkenDuration;
        private readonly float heavyGlitchDuration;
        private readonly float tvTurnOnDuration;
        private readonly Color glitchColor;
        private readonly Color secondarySplitColor;

        private bool isConfirmed;
        private float elapsedSeconds;
        private bool hasMovedPlayer;
        private int playerMoveCount;

        public float DarkenDuration => darkenDuration;
        public float HeavyGlitchDuration => heavyGlitchDuration;
        public float TvTurnOnDuration => tvTurnOnDuration;
        public float PreSurfaceLoadDuration => darkenDuration + heavyGlitchDuration;
        public float TotalDuration { get; }
        public Color GlitchColor => glitchColor;
        public Color SecondarySplitColor => secondarySplitColor;
        public bool IsConfirmed => isConfirmed;
        public float ElapsedSeconds => elapsedSeconds;
        public bool HasMovedPlayer => hasMovedPlayer;
        public int PlayerMoveCount => playerMoveCount;
        public RunFailureTransitionPhase CurrentPhase { get; private set; } = RunFailureTransitionPhase.NotStarted;
        public bool IsComplete => isConfirmed && elapsedSeconds >= TotalDuration - 0.0001f;

        public RunFailureTransitionTimeline(
            RunFailureCause cause = RunFailureCause.Unknown,
            RunFailureVisualSettings settings = null,
            bool startConfirmed = false)
        {
            var active = settings ?? new RunFailureVisualSettings();
            darkenDuration = Mathf.Max(0.05f, active.darkenDuration);
            heavyGlitchDuration = Mathf.Clamp(active.heavyGlitchDuration, 0.50f, 0.70f);
            tvTurnOnDuration = Mathf.Max(0.05f, active.tvTurnOnDuration);
            TotalDuration = darkenDuration + heavyGlitchDuration + tvTurnOnDuration;
            if (TotalDuration > 2.0f)
            {
                var scale = 2.0f / TotalDuration;
                darkenDuration *= scale;
                heavyGlitchDuration *= scale;
                tvTurnOnDuration *= scale;
                TotalDuration = 2.0f;
            }

            glitchColor = RunFailureRevealTimeline.ResolveGlitchColor(cause, active);
            secondarySplitColor = RunFailureRevealTimeline.ResolveSecondarySplitColor(cause, active);
            if (startConfirmed)
            {
                BeginConfirm();
            }
        }

        public void BeginConfirm()
        {
            if (isConfirmed)
            {
                return;
            }

            isConfirmed = true;
            CurrentPhase = RunFailureTransitionPhase.Darken;
        }

        public RunFailureTransitionFrame Advance(float deltaTime)
        {
            if (!isConfirmed)
            {
                CurrentPhase = RunFailureTransitionPhase.NotStarted;
                return new RunFailureTransitionFrame(
                    RunFailureTransitionPhase.NotStarted,
                    0f,
                    0f,
                    1f,
                    1f,
                    0f,
                    0f,
                    glitchColor,
                    secondarySplitColor,
                    false,
                    0f,
                    0f,
                    0f,
                    false);
            }

            if (deltaTime > 0f)
            {
                elapsedSeconds = Mathf.Min(TotalDuration, elapsedSeconds + deltaTime);
            }

            var triggerPlayerMove = false;
            if (!hasMovedPlayer && elapsedSeconds >= darkenDuration - 0.0001f)
            {
                hasMovedPlayer = true;
                playerMoveCount++;
                triggerPlayerMove = true;
            }

            var frame = EvaluateInternal(elapsedSeconds, triggerPlayerMove);
            CurrentPhase = frame.Phase;
            return frame;
        }

        public RunFailureTransitionFrame Evaluate(float timeSeconds)
        {
            if (!isConfirmed)
            {
                return new RunFailureTransitionFrame(
                    RunFailureTransitionPhase.NotStarted,
                    0f,
                    0f,
                    1f,
                    1f,
                    0f,
                    0f,
                    glitchColor,
                    secondarySplitColor,
                    false,
                    0f,
                    0f,
                    0f,
                    false);
            }

            var clamped = Mathf.Clamp(timeSeconds, 0f, TotalDuration);
            return EvaluateInternal(clamped, false);
        }

        private RunFailureTransitionFrame EvaluateInternal(float timeSeconds, bool shouldMovePlayer)
        {
            var heavyGlitchEnd = darkenDuration + heavyGlitchDuration;

            if (timeSeconds < darkenDuration - 0.0001f)
            {
                var phaseT = Mathf.Clamp01(timeSeconds / darkenDuration);
                var easedBlackout = ResourceSellTimeline.EaseOutCubic(phaseT);
                var windowCloseT = Mathf.Clamp01(phaseT / 0.72f);
                var seed = Mathf.FloorToInt(timeSeconds / RunFailureRevealTimeline.StepSeconds) + 331;
                var outroGlitch = Mathf.Clamp01((1f - windowCloseT) * 1.15f);
                var flicker = RunFailureRevealTimeline.Hash01(seed, 19) > 0.58f ? 0.58f : 1f;
                var windowAlpha = Mathf.Clamp01((1f - (windowCloseT * windowCloseT)) * flicker);
                var windowScaleY = windowCloseT < 0.35f
                    ? Mathf.Lerp(1f, 0.94f, windowCloseT / 0.35f)
                    : Mathf.Lerp(0.94f, 0.04f, (windowCloseT - 0.35f) / 0.65f);
                var jitterX = ((RunFailureRevealTimeline.Hash01(seed, 29) * 2f) - 1f) * 18f * outroGlitch;
                var jitterY = ((RunFailureRevealTimeline.Hash01(seed, 37) * 2f) - 1f) * 5f * outroGlitch;
                return new RunFailureTransitionFrame(
                    RunFailureTransitionPhase.Darken,
                    timeSeconds,
                    phaseT,
                    windowAlpha,
                    windowScaleY,
                    easedBlackout,
                    0f,
                    glitchColor,
                    secondarySplitColor,
                    shouldMovePlayer,
                    0f,
                    0f,
                    0f,
                    false,
                    outroGlitch,
                    seed,
                    new Vector2(jitterX, jitterY));
            }

            if (timeSeconds < heavyGlitchEnd - 0.0001f)
            {
                var glitchElapsed = Mathf.Max(0f, timeSeconds - darkenDuration);
                var phaseT = Mathf.Clamp01(glitchElapsed / heavyGlitchDuration);
                var seed = Mathf.FloorToInt(glitchElapsed / RunFailureRevealTimeline.StepSeconds) + 601;
                var wave = 0.74f + (0.26f * Mathf.Abs(Mathf.Sin((phaseT * Mathf.PI * 5f) + 0.4f)));
                var stepBoost = RunFailureRevealTimeline.Hash01(seed, 13) > 0.45f ? 0.14f : 0f;
                return new RunFailureTransitionFrame(
                    RunFailureTransitionPhase.HeavyGlitch,
                    timeSeconds,
                    phaseT,
                    0f,
                    0f,
                    1f,
                    Mathf.Clamp01(wave + stepBoost),
                    glitchColor,
                    secondarySplitColor,
                    shouldMovePlayer,
                    0f,
                    0f,
                    0f,
                    false,
                    0f,
                    seed,
                    Vector2.zero);
            }

            if (timeSeconds < TotalDuration - 0.0001f)
            {
                var tvElapsed = Mathf.Max(0f, timeSeconds - heavyGlitchEnd);
                var phaseT = Mathf.Clamp01(tvElapsed / tvTurnOnDuration);

                float lineWidth;
                float lineAlpha;
                float apertureOpen;
                if (phaseT < 0.28f)
                {
                    var beamT = ResourceSellTimeline.EaseOutCubic(phaseT / 0.28f);
                    lineWidth = Mathf.Lerp(0.18f, 1.0f, beamT);
                    lineAlpha = 1f;
                    apertureOpen = Mathf.Lerp(0.005f, 0.025f, beamT);
                }
                else
                {
                    var splitT = ResourceSellTimeline.EaseOutCubic((phaseT - 0.28f) / 0.72f);
                    lineWidth = 1f;
                    lineAlpha = Mathf.Clamp01(1f - (splitT * 1.15f));
                    apertureOpen = Mathf.Lerp(0.025f, 1.0f, splitT);
                }

                return new RunFailureTransitionFrame(
                    RunFailureTransitionPhase.TvTurnOn,
                    timeSeconds,
                    phaseT,
                    0f,
                    0f,
                    1f,
                    0f,
                    glitchColor,
                    secondarySplitColor,
                    shouldMovePlayer,
                    lineWidth,
                    lineAlpha,
                    apertureOpen,
                    false);
            }

            return new RunFailureTransitionFrame(
                RunFailureTransitionPhase.Completed,
                TotalDuration,
                1f,
                0f,
                0f,
                0f,
                0f,
                glitchColor,
                secondarySplitColor,
                shouldMovePlayer,
                1f,
                0f,
                1f,
                true);
        }
    }
}
