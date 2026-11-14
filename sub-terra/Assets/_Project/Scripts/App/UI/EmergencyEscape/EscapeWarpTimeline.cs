using System;
using SubTerra.App.UI.RunFailure;
using SubTerra.App.UI.Sell;
using UnityEngine;

namespace SubTerra.App.UI.EmergencyEscape
{
    /// <summary>긴급 탈출 워프 연출 시간. 모두 검사기에서 조절하며 unscaled 시간으로 진행한다.</summary>
    [Serializable]
    public sealed class EscapeWarpSettings
    {
        [Header("비상 탈출(전력 고갈 구출): 암전 + 간단 글리치")]
        [Tooltip("화면이 검게 변하는 시간(초).")]
        [Min(0.05f)] public float blackoutDuration = 0.25f;
        [Tooltip("간단한 글리치가 도는 시간(초). 암전과 겹쳐서 시작한다.")]
        [Min(0.05f)] public float glitchDuration = 0.25f;
        [Tooltip("완전히 검은 채로 머무는 시간(초). 이 사이에 플레이어를 목적지로 옮긴다.")]
        [Min(0f)] public float blackHoldDuration = 0.15f;
        [Tooltip("화면이 다시 켜지는 시간(초). 켜질 때 이미 목적지에 도착해 있다.")]
        [Min(0.05f)] public float screenOnDuration = 0.30f;
        [Tooltip("체력 0 구조의 적색/녹색과 구분되는 약한 청록 글리치 색.")]
        public Color glitchColor = new Color(0.30f, 0.86f, 0.92f, 1f);
        public Color secondaryColor = new Color(0.55f, 0.95f, 1.00f, 1f);

        [Header("긴급 탈출 포탈: 검은 화면 + 웜홀")]
        [Tooltip("화면이 어두워지며 캐릭터가 웜홀에 빨려 들어가는 시간(초).")]
        [Min(0.05f)] public float suckInDuration = 0.55f;
        [Tooltip("완전히 검은 화면에서 웜홀이 커졌다가 한 점으로 모이는 시간(초). 끝에 플레이어를 옮긴다.")]
        [Min(0.05f)] public float voidDuration = 0.45f;
        [Tooltip("검은 화면이 걷히며 목적지 웜홀에서 캐릭터가 나오는 시간(초).")]
        [Min(0.05f)] public float emergeDuration = 0.65f;
        [Tooltip("웜홀 기본 지름(월드 단위). 캐릭터보다 조금 크게 둔다.")]
        [Min(0.5f)] public float wormholeWorldDiameter = 2.4f;
    }

    public enum EscapeWarpPhase
    {
        NotStarted = 0,
        Blackout = 1,
        ScreenOn = 2,
        Completed = 4
    }

    public readonly struct EscapeWarpFrame
    {
        public EscapeWarpPhase Phase { get; }
        public float ElapsedSeconds { get; }
        public float BlackoutAlpha { get; }
        public float GlitchIntensity { get; }
        public int GlitchSeed { get; }
        /// <summary>이 프레임에서 플레이어를 옮겨야 한다. 한 번만 참이다. 화면은 완전히 검다.</summary>
        public bool ShouldTeleport { get; }
        public bool IsComplete { get; }

        public EscapeWarpFrame(
            EscapeWarpPhase phase,
            float elapsedSeconds,
            float blackoutAlpha,
            float glitchIntensity,
            int glitchSeed,
            bool shouldTeleport,
            bool isComplete)
        {
            Phase = phase;
            ElapsedSeconds = elapsedSeconds;
            BlackoutAlpha = blackoutAlpha;
            GlitchIntensity = glitchIntensity;
            GlitchSeed = glitchSeed;
            ShouldTeleport = shouldTeleport;
            IsComplete = isComplete;
        }
    }

    /// <summary>
    /// 비상 탈출(전력 고갈 구출): 암전 + 간단 글리치 → 완전히 검은 화면에서 목적지로 이동 → 화면 켜짐.
    /// 카메라는 움직이지 않는다. 켜질 때 플레이어는 이미 목적지에 있다. 사망 전환(0.4+0.6+0.4)보다 짧고 약하다.
    /// 모션 감소: 글리치 없이 페이드 아웃 → 검은 화면에서 이동 → 페이드 인.
    /// </summary>
    public sealed class EscapeWarpTimeline
    {
        public const float MaxGlitchIntensity = 0.55f;
        /// <summary>화면이 켜지는 첫 순간에 남는 잔류 글리치 비율(최대 강도 대비).</summary>
        public const float ArrivalGlitchRatio = 0.45f;

        private readonly float blackoutDuration;
        private readonly float glitchDuration;
        private readonly float holdDuration;
        private readonly float screenOnDuration;
        private float elapsedSeconds;
        private int teleportCount;

        public bool ReduceMotion { get; }
        public float BlackoutDuration => blackoutDuration;
        public float GlitchDuration => glitchDuration;
        public float HoldDuration => holdDuration;
        public float ScreenOnDuration => screenOnDuration;
        /// <summary>어두워지는 구간 길이(글리치가 더 길면 글리치가 끝날 때까지) + 검은 채로 머무는 시간.</summary>
        public float BlackoutPhaseDuration { get; }
        public float TeleportTime { get; }
        public float TotalDuration { get; }
        public float ElapsedSeconds => elapsedSeconds;
        public int TeleportCount => teleportCount;
        public bool IsComplete => elapsedSeconds >= TotalDuration - 0.0001f;
        public EscapeWarpPhase CurrentPhase { get; private set; } = EscapeWarpPhase.NotStarted;

        public EscapeWarpTimeline(EscapeWarpSettings settings = null, bool reduceMotion = false)
        {
            var active = settings ?? new EscapeWarpSettings();
            ReduceMotion = reduceMotion;
            blackoutDuration = Mathf.Max(0.05f, active.blackoutDuration);
            glitchDuration = reduceMotion ? 0f : Mathf.Max(0.05f, active.glitchDuration);
            holdDuration = Mathf.Max(0f, active.blackHoldDuration);
            screenOnDuration = Mathf.Max(0.05f, active.screenOnDuration);
            BlackoutPhaseDuration = Mathf.Max(blackoutDuration, glitchDuration) + holdDuration;
            // 화면이 완전히 꺼진 상태에서 옮기고, 이어서 켠다. 카메라 이동은 없다.
            TeleportTime = BlackoutPhaseDuration;
            TotalDuration = BlackoutPhaseDuration + screenOnDuration;
        }

        public EscapeWarpFrame Advance(float deltaTime)
        {
            if (deltaTime > 0f)
            {
                elapsedSeconds = Mathf.Min(TotalDuration, elapsedSeconds + deltaTime);
            }

            var crossed = teleportCount == 0 && elapsedSeconds >= TeleportTime - 0.0001f;
            if (crossed)
            {
                teleportCount++;
            }

            var frame = Evaluate(elapsedSeconds, crossed);
            if (crossed)
            {
                // 프레임 간격만큼 이미 켜지기 시작했더라도, 옮기는 프레임은 완전히 검게 보고한다.
                // 켜짐은 다음 프레임부터라 이동이 화면에 비치지 않는다.
                frame = new EscapeWarpFrame(
                    frame.Phase, frame.ElapsedSeconds, 1f, frame.GlitchIntensity, frame.GlitchSeed, true, frame.IsComplete);
            }

            CurrentPhase = frame.Phase;
            return frame;
        }

        public EscapeWarpFrame Evaluate(float timeSeconds)
        {
            return Evaluate(Mathf.Clamp(timeSeconds, 0f, TotalDuration), false);
        }

        private EscapeWarpFrame Evaluate(float t, bool shouldTeleport)
        {
            var darkEnd = BlackoutPhaseDuration;
            if (t < darkEnd - 0.0001f)
            {
                var blackout = ResourceSellTimeline.EaseOutCubic(Mathf.Clamp01(t / blackoutDuration));
                var glitch = 0f;
                var seed = 0;
                if (!ReduceMotion && t < glitchDuration)
                {
                    seed = Mathf.FloorToInt(t / RunFailureRevealTimeline.StepSeconds) + 1301;
                    var phaseT = Mathf.Clamp01(t / glitchDuration);
                    // 시작에서 가장 세고 끝으로 갈수록 가라앉는 한 번의 짧은 요동.
                    var wave = 0.7f + (0.3f * Mathf.Abs(Mathf.Sin((phaseT * Mathf.PI * 3f) + 0.4f)));
                    var step = RunFailureRevealTimeline.Hash01(seed, 13) > 0.45f ? 0.12f : 0f;
                    glitch = Mathf.Clamp01(wave + step) * (1f - (phaseT * 0.45f)) * MaxGlitchIntensity;
                }

                return new EscapeWarpFrame(
                    EscapeWarpPhase.Blackout, t, blackout, glitch, seed, shouldTeleport, false);
            }

            var onEnd = darkEnd + screenOnDuration;
            if (t < onEnd - 0.0001f)
            {
                var onT = Mathf.Clamp01((t - darkEnd) / screenOnDuration);
                var glitch = 0f;
                var seed = 0;
                if (!ReduceMotion)
                {
                    // 켜지는 순간 도착 신호처럼 글리치가 잠깐 남았다가 가라앉는다.
                    var tail = 1f - Mathf.Clamp01(onT / 0.4f);
                    if (tail > 0f)
                    {
                        seed = Mathf.FloorToInt((t - darkEnd) / RunFailureRevealTimeline.StepSeconds) + 2203;
                        glitch = tail * ArrivalGlitchRatio * MaxGlitchIntensity;
                    }
                }

                return new EscapeWarpFrame(
                    EscapeWarpPhase.ScreenOn, t, 1f - ResourceSellTimeline.EaseOutCubic(onT),
                    glitch, seed, shouldTeleport, false);
            }

            return new EscapeWarpFrame(
                EscapeWarpPhase.Completed, TotalDuration, 0f, 0f, 0, shouldTeleport, true);
        }
    }
}
