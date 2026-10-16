using System;
using System.Collections.Generic;
using SubTerra.App.UI.Sell;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.App.UI.RunFailure
{
    [Serializable]
    public sealed class RunFailureVisualSettings
    {
        [Min(0.05f)] public float windowGlitchDuration = 0.30f;
        [Min(0.02f)] public float itemStaggerSeconds = 0.12f;
        [Range(0.30f, 0.50f)] public float itemCountUpDuration = 0.40f;
        [Min(0.05f)] public float darkenDuration = 0.40f;
        [Range(0.50f, 0.70f)] public float heavyGlitchDuration = 0.60f;
        [Min(0.05f)] public float tvTurnOnDuration = 0.40f;

        public Color gasGlitchColor = new Color(0.22f, 0.92f, 0.38f, 1f);
        public Color gasSecondaryColor = new Color(0.10f, 0.55f, 0.18f, 1f);
        public Color defaultGlitchColor = new Color(1.00f, 0.28f, 0.24f, 1f);
        public Color defaultSecondaryColor = new Color(0.24f, 0.86f, 1.00f, 1f);
    }

    public readonly struct RunFailureItemFrame
    {
        public int Index { get; }
        public string MineralId { get; }
        public int TargetQuantity { get; }
        public int DisplayQuantity { get; }
        public float StartTime { get; }
        public float CountUpDuration { get; }
        public float Progress { get; }
        public float Alpha { get; }
        public bool IsVisible { get; }
        public bool IsCompleted { get; }

        public RunFailureItemFrame(
            int index,
            string mineralId,
            int targetQuantity,
            int displayQuantity,
            float startTime,
            float countUpDuration,
            float progress,
            float alpha,
            bool isVisible,
            bool isCompleted)
        {
            Index = index;
            MineralId = mineralId ?? string.Empty;
            TargetQuantity = targetQuantity;
            DisplayQuantity = displayQuantity;
            StartTime = startTime;
            CountUpDuration = countUpDuration;
            Progress = progress;
            Alpha = alpha;
            IsVisible = isVisible;
            IsCompleted = isCompleted;
        }
    }

    public readonly struct RunFailureRevealFrame
    {
        public RunFailureCause Cause { get; }
        public string CauseTitle { get; }
        public string CauseSubtitle { get; }
        public Color GlitchColor { get; }
        public Color SecondarySplitColor { get; }
        public Color GlitchTint { get; }
        public float ElapsedSeconds { get; }
        public bool IsWindowGlitching { get; }
        public float WindowGlitchIntensity { get; }
        public float WindowFlickerAlpha { get; }
        public Vector2 WindowJitterOffset { get; }
        public Vector2 RgbSplitOffset { get; }
        public int GlitchSeed { get; }
        public bool HasLostCargo { get; }
        public string EmptyCargoText { get; }
        public string PreservationText { get; }
        public IReadOnlyList<RunFailureItemFrame> Items { get; }
        public bool IsComplete { get; }
        public bool IsSkipped { get; }

        public RunFailureRevealFrame(
            RunFailureCause cause,
            string causeTitle,
            Color glitchColor,
            Color secondarySplitColor,
            float elapsedSeconds,
            bool isWindowGlitching,
            float windowGlitchIntensity,
            float windowFlickerAlpha,
            Vector2 windowJitterOffset,
            Vector2 rgbSplitOffset,
            bool hasLostCargo,
            string emptyCargoText,
            string preservationText,
            IReadOnlyList<RunFailureItemFrame> items,
            bool isComplete,
            bool isSkipped,
            string causeSubtitle = null,
            int glitchSeed = 0,
            Color glitchTint = default)
        {
            Cause = cause;
            CauseTitle = causeTitle ?? string.Empty;
            CauseSubtitle = causeSubtitle ?? RunFailureRevealTimeline.ResolveCauseSubtitle(cause);
            GlitchColor = glitchColor;
            SecondarySplitColor = secondarySplitColor;
            GlitchTint = glitchTint.a > 0f ? glitchTint : glitchColor;
            ElapsedSeconds = elapsedSeconds;
            IsWindowGlitching = isWindowGlitching;
            WindowGlitchIntensity = windowGlitchIntensity;
            WindowFlickerAlpha = windowFlickerAlpha;
            WindowJitterOffset = windowJitterOffset;
            RgbSplitOffset = rgbSplitOffset;
            GlitchSeed = glitchSeed;
            HasLostCargo = hasLostCargo;
            EmptyCargoText = emptyCargoText ?? string.Empty;
            PreservationText = preservationText ?? string.Empty;
            Items = items ?? Array.Empty<RunFailureItemFrame>();
            IsComplete = isComplete;
            IsSkipped = isSkipped;
        }
    }

    /// <summary>
    /// B-148: 탐사 실패 창 등장 글리치, 잃은 자원 순차 등장/카운트업, 스킵 및 확인 대기 상태를 계산한다.
    /// </summary>
    public sealed class RunFailureRevealTimeline
    {
        public const string EmptyCargoMessage = "잃은 자원 없음";
        public const float StepSeconds = 1f / 30f;

        private const float IdleSlotSeconds = 0.4f;
        private const float IdleBurstChance = 0.34f;

        private readonly RunFailureCause cause;
        private readonly List<CargoLossEntryDto> lostItems;
        private readonly float preservationRatio;
        private readonly float windowGlitchDuration;
        private readonly float itemStaggerSeconds;
        private readonly float itemCountUpDuration;
        private readonly Color glitchColor;
        private readonly Color secondarySplitColor;

        private float elapsedSeconds;
        private bool isSkipped;
        private bool isConfirmed;

        public RunFailureCause Cause => cause;
        public string CauseTitle => ResolveCauseTitle(cause);
        public string CauseSubtitle => ResolveCauseSubtitle(cause);
        public Color GlitchColor => glitchColor;
        public Color SecondarySplitColor => secondarySplitColor;
        public bool HasLostCargo => lostItems.Count > 0;
        public string EmptyCargoText => HasLostCargo ? string.Empty : EmptyCargoMessage;
        public string PreservationText => FormatPreservationLine(preservationRatio);
        public int ItemCount => lostItems.Count;
        public float WindowGlitchDuration => windowGlitchDuration;
        public float ItemStaggerSeconds => itemStaggerSeconds;
        public float ItemCountUpDuration => itemCountUpDuration;
        public float TotalDuration { get; }
        public float ElapsedSeconds => elapsedSeconds;
        public bool IsSkipped => isSkipped;
        public bool IsComplete => isSkipped || elapsedSeconds >= TotalDuration - 0.0001f;
        public bool IsConfirmed => isConfirmed;

        public RunFailureRevealTimeline(
            PlayerRescueResultDto rescue,
            RunFailureVisualSettings settings = null)
            : this(
                rescue != null ? rescue.cause : RunFailureCause.Unknown,
                rescue != null ? rescue.lostCargo : null,
                rescue != null ? rescue.preservationRatio : 0.6f,
                settings)
        {
        }

        public RunFailureRevealTimeline(
            RunFailureCause cause,
            IReadOnlyList<CargoLossEntryDto> lostCargo,
            float preservationRatio,
            RunFailureVisualSettings settings = null)
        {
            this.cause = cause;
            this.preservationRatio = Mathf.Clamp01(preservationRatio);

            var activeSettings = settings ?? new RunFailureVisualSettings();
            windowGlitchDuration = Mathf.Max(0.05f, activeSettings.windowGlitchDuration);
            itemStaggerSeconds = Mathf.Max(0.02f, activeSettings.itemStaggerSeconds);
            itemCountUpDuration = Mathf.Clamp(activeSettings.itemCountUpDuration, 0.30f, 0.50f);
            glitchColor = ResolveGlitchColor(cause, activeSettings);
            secondarySplitColor = ResolveSecondarySplitColor(cause, activeSettings);

            lostItems = new List<CargoLossEntryDto>();
            if (lostCargo != null)
            {
                for (var i = 0; i < lostCargo.Count; i++)
                {
                    var entry = lostCargo[i];
                    if (entry == null || string.IsNullOrEmpty(entry.mineralId) || entry.quantity <= 0)
                    {
                        continue;
                    }

                    lostItems.Add(new CargoLossEntryDto
                    {
                        mineralId = entry.mineralId,
                        quantity = entry.quantity
                    });
                }
            }

            if (lostItems.Count == 0)
            {
                TotalDuration = windowGlitchDuration;
            }
            else
            {
                var lastStart = GetItemStartTime(lostItems.Count - 1);
                TotalDuration = lastStart + itemCountUpDuration;
            }
        }

        public float GetItemStartTime(int index)
        {
            var clampedIndex = Mathf.Max(0, index);
            return windowGlitchDuration + (clampedIndex * itemStaggerSeconds);
        }

        public RunFailureRevealFrame Advance(float deltaTime)
        {
            if (!isSkipped && deltaTime > 0f)
            {
                elapsedSeconds = Mathf.Min(TotalDuration, elapsedSeconds + deltaTime);
            }

            return Evaluate(elapsedSeconds);
        }

        public RunFailureRevealFrame Skip()
        {
            isSkipped = true;
            elapsedSeconds = TotalDuration;
            return Evaluate(TotalDuration);
        }

        /// <summary>
        /// 연출이 진행 중이면 즉시 최종 상태로 스킵하고 false를 반환한다.
        /// 이미 최종 상태에 도달해 있으면 확인 완료로 표시하고 true를 반환한다.
        /// </summary>
        public bool TryConfirmOrSkip()
        {
            if (!IsComplete)
            {
                Skip();
                return false;
            }

            isConfirmed = true;
            return true;
        }

        public RunFailureRevealFrame Evaluate(float timeSeconds)
        {
            var effectiveTime = isSkipped
                ? TotalDuration
                : Mathf.Clamp(timeSeconds, 0f, TotalDuration);
            var isComplete = isSkipped || effectiveTime >= TotalDuration - 0.0001f;

            var isWindowGlitching = !isSkipped && effectiveTime < windowGlitchDuration - 0.0001f;
            var windowIntensity = 0f;
            var windowFlickerAlpha = 1f;
            var jitter = Vector2.zero;
            var rgbOffset = Vector2.zero;
            var glitchSeed = 0;
            var glitchTint = glitchColor;

            if (isWindowGlitching)
            {
                var normalized = Mathf.Clamp01(effectiveTime / windowGlitchDuration);
                glitchSeed = Mathf.FloorToInt(effectiveTime / StepSeconds) + 17;

                // B-135 EmergencyRescueTimeline 스타일의 다중 버스트 + 감쇠 곡선
                var burst = (Bell(normalized, 0.04f, 0.16f) * 1.00f)
                    + (Bell(normalized, 0.36f, 0.14f) * 0.88f)
                    + (Bell(normalized, 0.72f, 0.12f) * 0.68f);
                var baseDecay = 1f - (normalized * 0.65f);
                var envelope = Mathf.Clamp01(Mathf.Max(baseDecay * 0.85f, burst));
                windowIntensity = envelope;

                var randX = (Hash01(glitchSeed, 11) * 2f) - 1f;
                var randY = (Hash01(glitchSeed, 23) * 2f) - 1f;
                var splitSign = Hash01(glitchSeed, 31) >= 0.5f ? 1f : -1f;
                var splitMag = Mathf.Lerp(5.5f, 15.5f, Hash01(glitchSeed, 37)) * envelope;

                jitter = new Vector2(randX * 12f * envelope, randY * 4.5f * envelope);
                rgbOffset = new Vector2(splitSign * splitMag, randY * 2.5f * envelope);

                // 30fps 계단식 스타카토 플리커
                var flickerRoll = Hash01(glitchSeed, 43);
                // 창 전체가 사라졌다 켜지는 느낌이 아니라 밝기가 살짝 흔들리는 정도로 제한한다.
                if (normalized < 0.15f)
                {
                    windowFlickerAlpha = flickerRoll > 0.45f ? 0.92f : 0.62f;
                }
                else if (flickerRoll > 0.64f)
                {
                    windowFlickerAlpha = Mathf.Lerp(0.74f, 0.96f, normalized);
                }
                else
                {
                    windowFlickerAlpha = 1f;
                }

                var tintRoll = Hash01(glitchSeed, 53);
                if (tintRoll > 0.78f)
                {
                    glitchTint = Color.Lerp(glitchColor, Color.white, 0.38f);
                }
                else if (tintRoll < 0.24f)
                {
                    glitchTint = Color.Lerp(glitchColor, secondarySplitColor, 0.55f);
                }
            }

            var itemFrames = new RunFailureItemFrame[lostItems.Count];
            for (var i = 0; i < lostItems.Count; i++)
            {
                var item = lostItems[i];
                var startTime = GetItemStartTime(i);
                if (isComplete)
                {
                    itemFrames[i] = new RunFailureItemFrame(
                        i,
                        item.mineralId,
                        item.quantity,
                        item.quantity,
                        startTime,
                        itemCountUpDuration,
                        1f,
                        1f,
                        true,
                        true);
                    continue;
                }

                var itemElapsed = effectiveTime - startTime;
                if (itemElapsed < 0f)
                {
                    itemFrames[i] = new RunFailureItemFrame(
                        i,
                        item.mineralId,
                        item.quantity,
                        0,
                        startTime,
                        itemCountUpDuration,
                        0f,
                        0f,
                        false,
                        false);
                    continue;
                }

                var progress = Mathf.Clamp01(itemElapsed / itemCountUpDuration);
                var displayQuantity = EvaluateCountUpQuantity(item.quantity, itemElapsed, itemCountUpDuration);
                var alpha = Mathf.Clamp01(itemElapsed / Mathf.Min(0.10f, itemCountUpDuration));
                var itemCompleted = progress >= 0.9999f;
                itemFrames[i] = new RunFailureItemFrame(
                    i,
                    item.mineralId,
                    item.quantity,
                    displayQuantity,
                    startTime,
                    itemCountUpDuration,
                    progress,
                    alpha,
                    true,
                    itemCompleted);
            }

            return new RunFailureRevealFrame(
                cause,
                CauseTitle,
                glitchColor,
                secondarySplitColor,
                effectiveTime,
                isWindowGlitching,
                windowIntensity,
                windowFlickerAlpha,
                jitter,
                rgbOffset,
                HasLostCargo,
                EmptyCargoText,
                PreservationText,
                itemFrames,
                isComplete,
                isSkipped,
                CauseSubtitle,
                glitchSeed,
                glitchTint);
        }

        public static float EvaluateIdle(float timeSeconds, out int seed)
        {
            var t = Mathf.Max(0f, timeSeconds);
            seed = Mathf.FloorToInt(t / StepSeconds) + 911;

            // 0.4초 칸마다 확률로 짧게 한 번, 창 가장자리 조각에만 쓰이는 간헐 글리치 (전력 고갈 팝업과 같은 방식)
            var slot = Mathf.FloorToInt(t / IdleSlotSeconds);
            var local = t - (slot * IdleSlotSeconds);
            if (Hash01(slot, 7) > IdleBurstChance)
            {
                return 0f;
            }

            var start = Hash01(slot, 3) * 0.18f;
            var length = 0.08f + (Hash01(slot, 5) * 0.08f);
            if (local < start || local > start + length)
            {
                return 0f;
            }

            seed = (slot * 7) + Mathf.FloorToInt(local / StepSeconds) + 911;
            return 0.40f + (Hash01(slot, 11) * 0.45f);
        }

        public static float Hash01(int seed, int salt)
        {
            unchecked
            {
                var n = (seed * 374761393) + (salt * 668265263);
                n = (n ^ (n >> 13)) * 1274126177;
                n ^= n >> 16;
                return (n & 0x7fffffff) / (float)0x7fffffff;
            }
        }

        public static int EvaluateCountUpQuantity(int targetQuantity, float itemElapsedSeconds, float duration = 0.40f)
        {
            if (targetQuantity <= 0 || itemElapsedSeconds <= 0f)
            {
                return 0;
            }

            var safeDuration = Mathf.Max(0.0001f, duration);
            if (itemElapsedSeconds >= safeDuration)
            {
                return targetQuantity;
            }

            var t = Mathf.Clamp01(itemElapsedSeconds / safeDuration);
            var eased = ResourceSellTimeline.EaseOutCubic(t);
            return Mathf.Clamp(Mathf.RoundToInt(targetQuantity * eased), 0, targetQuantity);
        }

        public static bool IsGasCause(RunFailureCause cause)
        {
            return cause == RunFailureCause.GasExposure;
        }

        public static Color ResolveGlitchColor(RunFailureCause cause, RunFailureVisualSettings settings = null)
        {
            var active = settings ?? new RunFailureVisualSettings();
            return IsGasCause(cause) ? active.gasGlitchColor : active.defaultGlitchColor;
        }

        public static Color ResolveSecondarySplitColor(RunFailureCause cause, RunFailureVisualSettings settings = null)
        {
            var active = settings ?? new RunFailureVisualSettings();
            return IsGasCause(cause) ? active.gasSecondaryColor : active.defaultSecondaryColor;
        }

        public static string ResolveCauseTitle(RunFailureCause cause)
        {
            return cause == RunFailureCause.GasExposure
                ? "가스 중독"
                : "체력 소진";
        }

        public static string ResolveCauseSubtitle(RunFailureCause cause)
        {
            switch (cause)
            {
                case RunFailureCause.GasExposure:
                    return "유독 가스 농도 임계치 초과로 긴급 구조 시퀀스가 가동되었습니다.";
                case RunFailureCause.StructuralCollapse:
                    return "낙석 및 구조 붕괴 충격으로 기체가 파손되어 긴급 이송되었습니다.";
                case RunFailureCause.Fall:
                    return "추락 충격으로 생명 유지 장치가 손상되어 긴급 이송되었습니다.";
                default:
                    return "생명 유지 한계 도달로 긴급 구조 드론이 출동했습니다.";
            }
        }

        public static string FormatPreservationLine(float preservationRatio)
        {
            var percent = Mathf.RoundToInt(Mathf.Clamp01(preservationRatio) * 100f);
            return "드론 구조: 화물 " + percent + "% 보존";
        }

        private static float Bell(float value, float center, float radius)
        {
            var d = Mathf.Abs(value - center) / Mathf.Max(0.0001f, radius);
            if (d >= 1f)
            {
                return 0f;
            }

            var x = 1f - (d * d);
            return x * x;
        }
    }
}
