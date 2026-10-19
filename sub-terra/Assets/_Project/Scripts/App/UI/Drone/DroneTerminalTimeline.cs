using System;
using SubTerra.App.UI.ControlTerminal;
using SubTerra.App.UI.RunFailure;
using UnityEngine;

namespace SubTerra.App.UI.Drone
{
    /// <summary>드론 분석 창 터미널 연출의 시간·색 값. 인스펙터에서 조정한다.</summary>
    [Serializable]
    public sealed class DroneTerminalFxSettings
    {
        [Min(0.05f)] public float openDuration = 0.30f;
        [Min(0.05f)] public float closeDuration = 0.25f;
        [Min(0.05f)] public float scanSweepDuration = 0.22f;
        /// <summary>열기 시작부터 헤더 타이핑이 시작되기까지.</summary>
        [Min(0f)] public float textStartDelay = 0.22f;
        [Min(0.02f)] public float headerTypeDuration = 0.14f;
        [Min(1f)] public float dialogueCharsPerSecond = 160f;
        [Min(0.02f)] public float dialogueMaxDuration = 0.28f;
        [Min(1f)] public float actionCharsPerSecond = 140f;
        [Min(0.02f)] public float actionMaxDuration = 0.12f;
        [Range(0.05f, 0.1f)] public float reasonLineInterval = 0.07f;
        /// <summary>열기 시작부터 글자가 모두 나올 때까지의 상한. 넘으면 타이핑 전체를 압축한다.</summary>
        [Min(0.3f)] public float revealBudget = 0.95f;
        [Min(0.05f)] public float priorityFlashDuration = 0.30f;
        [Min(0.1f)] public float cursorBlinkPeriod = 0.5f;
        /// <summary>글자가 다 나온 뒤 커서가 깜빡이는 횟수. 이후에는 켜진 채로 둔다.</summary>
        [Min(0)] public int cursorBlinkCount = 3;
        public Color lineColor = new Color(0.36f, 0.96f, 1f, 1f);
        public Color scanColor = new Color(0.18f, 0.84f, 0.92f, 0.20f);
        public Color priorityColor = new Color(1f, 0.58f, 0.12f, 1f);
        public Color glitchColor = new Color(1f, 0.56f, 0.14f, 0.55f);
    }

    /// <summary>창 열기·닫기 한 시점의 값.</summary>
    public readonly struct DroneTerminalWindowFrame
    {
        /// <summary>중앙 가로선이 좌우로 펼쳐진 정도. 0~1.</summary>
        public float LineWidth { get; }
        public float LineAlpha { get; }
        /// <summary>창 세로 비율. 0이면 가로선, 1이면 전체 창.</summary>
        public float Open { get; }
        /// <summary>창 안 글자의 불투명도. 닫힐 때 먼저 사라진다.</summary>
        public float ContentAlpha { get; }
        /// <summary>스캔라인 위치. 0 = 위, 1 = 아래.</summary>
        public float ScanPosition { get; }
        public float ScanAlpha { get; }

        public DroneTerminalWindowFrame(
            float lineWidth, float lineAlpha, float open, float contentAlpha, float scanPosition, float scanAlpha)
        {
            LineWidth = lineWidth;
            LineAlpha = lineAlpha;
            Open = open;
            ContentAlpha = contentAlpha;
            ScanPosition = scanPosition;
            ScanAlpha = scanAlpha;
        }
    }

    /// <summary>PRIORITY 진입 글리치 띠 한 시점의 값.</summary>
    public readonly struct DroneTerminalGlitchFrame
    {
        public bool Active { get; }
        /// <summary>띠 중심. 0 = 위, 1 = 아래.</summary>
        public float BandPosition { get; }
        public float BandAlpha { get; }
        /// <summary>띠의 가로 흔들림(-1~1). View가 픽셀로 바꾼다.</summary>
        public float OffsetX { get; }

        public DroneTerminalGlitchFrame(bool active, float bandPosition, float bandAlpha, float offsetX)
        {
            Active = active;
            BandPosition = bandPosition;
            BandAlpha = bandAlpha;
            OffsetX = offsetX;
        }
    }

    /// <summary>
    /// 헤더·대사·추천·근거 줄의 타이핑 시간표. 시각은 모두 reveal 시계 기준이다.
    /// 상한을 넘으면 비율을 유지한 채 압축해 글자가 예산 안에 모두 나오게 한다.
    /// </summary>
    public readonly struct DroneTerminalTypingPlan
    {
        public bool IncludesHeader { get; }
        public float HeaderStart { get; }
        public float HeaderEnd { get; }
        public float DialogueStart { get; }
        public float DialogueEnd { get; }
        public float ActionStart { get; }
        public float ActionEnd { get; }
        public float ReasonStart { get; }
        public float ReasonInterval { get; }
        public int ReasonCount { get; }
        public float End { get; }

        private DroneTerminalTypingPlan(
            bool includesHeader,
            float headerStart, float headerEnd,
            float dialogueStart, float dialogueEnd,
            float actionStart, float actionEnd,
            float reasonStart, float reasonInterval, int reasonCount)
        {
            IncludesHeader = includesHeader;
            HeaderStart = headerStart;
            HeaderEnd = headerEnd;
            DialogueStart = dialogueStart;
            DialogueEnd = dialogueEnd;
            ActionStart = actionStart;
            ActionEnd = actionEnd;
            ReasonStart = reasonStart;
            ReasonInterval = reasonInterval;
            ReasonCount = reasonCount;
            End = reasonCount > 0
                ? reasonStart + reasonInterval * (reasonCount - 1)
                : actionEnd;
        }

        /// <summary>창을 열 때: 헤더부터 타이핑한다. 시계 0은 열기 시작.</summary>
        public static DroneTerminalTypingPlan ForOpen(
            DroneTerminalFxSettings settings, int dialogueLength, int actionLength, int reasonCount)
        {
            return Build(settings, true, settings.textStartDelay, settings.revealBudget,
                dialogueLength, actionLength, reasonCount);
        }

        /// <summary>추천 행동이 바뀌었을 때: 헤더는 두고 대사부터 다시 타이핑한다. 시계 0은 변경 시점.</summary>
        public static DroneTerminalTypingPlan ForRetype(
            DroneTerminalFxSettings settings, int dialogueLength, int actionLength, int reasonCount)
        {
            var budget = Math.Max(0.1f, settings.revealBudget - settings.textStartDelay);
            return Build(settings, false, 0f, budget, dialogueLength, actionLength, reasonCount);
        }

        private static DroneTerminalTypingPlan Build(
            DroneTerminalFxSettings settings,
            bool includeHeader,
            float start,
            float budget,
            int dialogueLength,
            int actionLength,
            int reasonCount)
        {
            var header = includeHeader ? settings.headerTypeDuration : 0f;
            var dialogue = TypeDuration(dialogueLength, settings.dialogueCharsPerSecond, settings.dialogueMaxDuration);
            var action = TypeDuration(actionLength, settings.actionCharsPerSecond, settings.actionMaxDuration);
            var count = Math.Max(0, reasonCount);
            var interval = settings.reasonLineInterval;
            var span = header + dialogue + action + interval * Math.Max(0, count - 1);
            if (start + span > budget && span > 0f)
            {
                var scale = Math.Max(0f, budget - start) / span;
                header *= scale;
                dialogue *= scale;
                action *= scale;
                interval *= scale;
            }

            var headerEnd = start + header;
            var dialogueEnd = headerEnd + dialogue;
            var actionEnd = dialogueEnd + action;
            return new DroneTerminalTypingPlan(
                includeHeader,
                start, headerEnd,
                headerEnd, dialogueEnd,
                dialogueEnd, actionEnd,
                actionEnd, interval, count);
        }

        private static float TypeDuration(int length, float charsPerSecond, float max)
        {
            if (length <= 0)
            {
                return 0f;
            }

            return Math.Min(max, length / Math.Max(1f, charsPerSecond));
        }

        public bool IsComplete(float clock)
        {
            return clock >= End;
        }

        public int VisibleHeaderCharacters(float clock, int length)
        {
            return VisibleCharacters(clock, HeaderStart, HeaderEnd, length);
        }

        public int VisibleDialogueCharacters(float clock, int length)
        {
            return VisibleCharacters(clock, DialogueStart, DialogueEnd, length);
        }

        public int VisibleActionCharacters(float clock, int length)
        {
            return VisibleCharacters(clock, ActionStart, ActionEnd, length);
        }

        /// <summary>지금 보이는 근거 줄 수. 줄은 간격마다 한 줄씩 통째로 나타난다.</summary>
        public int VisibleReasonLines(float clock)
        {
            if (ReasonCount <= 0 || clock < ReasonStart)
            {
                return 0;
            }

            if (ReasonInterval <= 0f)
            {
                return ReasonCount;
            }

            var shown = 1 + (int)Math.Floor((clock - ReasonStart) / ReasonInterval + 0.0001f);
            return Math.Min(ReasonCount, shown);
        }

        private static int VisibleCharacters(float clock, float start, float end, int length)
        {
            if (length <= 0)
            {
                return 0;
            }

            if (clock >= end)
            {
                return length;
            }

            if (clock < start)
            {
                return 0;
            }

            var k = ControlTerminalTimeline.Range(clock, start, end);
            return Math.Min(length, (int)Math.Ceiling(k * length));
        }
    }

    /// <summary>드론 분석 창 터미널 연출의 순수 계산. 상태가 없어 EditMode에서 검증한다.</summary>
    public static class DroneTerminalTimeline
    {
        private const float GlitchStepSeconds = 1f / 30f;

        /// <summary>가로선 → 위아래 펼침 → 스캔라인 한 번. t는 열기 시작 기준(펼친 뒤에도 계속 증가).</summary>
        public static DroneTerminalWindowFrame Open(float t, DroneTerminalFxSettings settings)
        {
            var d = settings.openDuration;
            var lineEnd = d * 0.30f;
            var lineWidth = ControlTerminalTimeline.EaseOut(ControlTerminalTimeline.Range(t, 0f, lineEnd));
            var open = ControlTerminalTimeline.EaseOut(ControlTerminalTimeline.Range(t, lineEnd, d));
            var flash = Bell(t, d * 0.15f, d * 0.12f);
            var lineAlpha = (1f - ControlTerminalTimeline.Range(t, d * 0.75f, d)) * (0.7f + 0.3f * flash);
            var scan = ControlTerminalTimeline.Range(t, d, d + settings.scanSweepDuration);
            var scanAlpha = t < d || scan >= 1f ? 0f : (float)Math.Sin(scan * Math.PI);
            return new DroneTerminalWindowFrame(lineWidth, lineAlpha, open, 1f, scan, scanAlpha);
        }

        /// <summary>열기의 역순: 글자 → 세로 접힘 → 가로선 소멸.</summary>
        public static DroneTerminalWindowFrame Close(float t, DroneTerminalFxSettings settings)
        {
            var c = settings.closeDuration;
            var content = 1f - ControlTerminalTimeline.Smooth(ControlTerminalTimeline.Range(t, 0f, c * 0.36f));
            var open = 1f - EaseIn(ControlTerminalTimeline.Range(t, c * 0.20f, c * 0.70f));
            var lineWidth = 1f - EaseIn(ControlTerminalTimeline.Range(t, c * 0.70f, c));
            var lineAlpha = ControlTerminalTimeline.Range(t, c * 0.40f, c * 0.65f)
                * (1f - ControlTerminalTimeline.Range(t, c * 0.85f, c));
            return new DroneTerminalWindowFrame(lineWidth, lineAlpha, open, content, 1f, 0f);
        }

        /// <summary>PRIORITY 진입 시 헤더 주황 깜빡임 세기. 두 번 켜졌다가 사라진다.</summary>
        public static float PriorityFlash(float t, float duration)
        {
            if (t < 0f || t >= duration || duration <= 0f)
            {
                return 0f;
            }

            var k = t / duration;
            if (k < 0.22f)
            {
                return 1f;
            }

            if (k < 0.36f)
            {
                return 0f;
            }

            if (k < 0.58f)
            {
                return 1f;
            }

            return 1f - ControlTerminalTimeline.Smooth(ControlTerminalTimeline.Range(k, 0.58f, 1f));
        }

        /// <summary>PRIORITY 진입 글리치 띠. 위에서 아래로 한 번 지나가며 프레임 단위로 좌우로 튄다.</summary>
        public static DroneTerminalGlitchFrame Glitch(float t, float duration)
        {
            if (t < 0f || t >= duration || duration <= 0f)
            {
                return new DroneTerminalGlitchFrame(false, 0f, 0f, 0f);
            }

            var k = t / duration;
            var step = (int)Math.Floor(t / GlitchStepSeconds);
            var alpha = Math.Min(ControlTerminalTimeline.Range(k, 0f, 0.08f), 1f - ControlTerminalTimeline.Range(k, 0.70f, 1f));
            var offset = (RunFailureRevealTimeline.Hash01(step + 149, 3) - 0.5f) * 2f;
            return new DroneTerminalGlitchFrame(true, k, alpha, offset);
        }

        /// <summary>
        /// 헤더 끝 커서. 헤더가 다 찍힌 뒤 나타나 깜빡이고, 정해진 횟수 뒤에는 켜진 채로 둔다.
        /// t는 reveal 시계, headerEnd는 헤더가 다 찍힌 시각, revealEnd는 글자가 모두 나온 시각.
        /// </summary>
        public static bool CursorVisible(float t, float headerEnd, float revealEnd, DroneTerminalFxSettings settings)
        {
            if (t < headerEnd)
            {
                return false;
            }

            var period = Math.Max(0.1f, settings.cursorBlinkPeriod);
            if (t >= revealEnd + period * settings.cursorBlinkCount)
            {
                return true;
            }

            var phase = (t - headerEnd) % period;
            return phase < period * 0.5f;
        }

        /// <summary>같은 추천에서 근거 줄이 늘었을 때 새 줄만 순서대로 보인다.</summary>
        public static int AppendedReasonLines(int fromCount, int toCount, float t, float interval)
        {
            if (toCount <= fromCount)
            {
                return toCount;
            }

            if (t < 0f)
            {
                return fromCount;
            }

            if (interval <= 0f)
            {
                return toCount;
            }

            var shown = fromCount + 1 + (int)Math.Floor(t / interval + 0.0001f);
            return Math.Min(toCount, shown);
        }

        private static float EaseIn(float k)
        {
            return k * k * k;
        }

        private static float Bell(float t, float center, float width)
        {
            var d = (t - center) / width;
            return (float)Math.Exp(-d * d);
        }
    }

    public enum DroneTerminalPhase
    {
        Hidden,
        Opening,
        Shown,
        Closing
    }

    /// <summary>
    /// 창 열기·닫기 상태. 열기 중 닫으면(또는 그 반대) 진행 비율을 이어받아 자연스럽게 되돌린다.
    /// </summary>
    public sealed class DroneTerminalWindowState
    {
        private const int ReverseSamples = 64;

        private readonly DroneTerminalFxSettings settings;

        public DroneTerminalPhase Phase { get; private set; } = DroneTerminalPhase.Hidden;
        /// <summary>현재 단계의 경과 시간. Shown에서는 열기 시작 기준으로 계속 늘어난다.</summary>
        public float Clock { get; private set; }

        public bool IsVisible => Phase != DroneTerminalPhase.Hidden;
        public bool IsTransitioning => Phase == DroneTerminalPhase.Opening || Phase == DroneTerminalPhase.Closing;

        public DroneTerminalWindowState(DroneTerminalFxSettings fxSettings)
        {
            settings = fxSettings ?? new DroneTerminalFxSettings();
        }

        public void Open()
        {
            switch (Phase)
            {
                case DroneTerminalPhase.Hidden:
                    Phase = DroneTerminalPhase.Opening;
                    Clock = 0f;
                    break;
                case DroneTerminalPhase.Closing:
                    var current = DroneTerminalTimeline.Close(Clock, settings);
                    Phase = DroneTerminalPhase.Opening;
                    Clock = FindOpenClock(current);
                    break;
            }
        }

        public void Close()
        {
            switch (Phase)
            {
                case DroneTerminalPhase.Shown:
                    Phase = DroneTerminalPhase.Closing;
                    Clock = 0f;
                    break;
                case DroneTerminalPhase.Opening:
                    var current = DroneTerminalTimeline.Open(Clock, settings);
                    Phase = DroneTerminalPhase.Closing;
                    Clock = FindCloseClock(current);
                    break;
            }
        }

        // 되돌릴 때 창 높이(접혀 있으면 가로선 길이)가 튀지 않는 시점을 찾는다.
        private float FindCloseClock(DroneTerminalWindowFrame current)
        {
            var duration = settings.closeDuration;
            for (var i = 0; i <= ReverseSamples; i++)
            {
                var t = duration * i / ReverseSamples;
                var frame = DroneTerminalTimeline.Close(t, settings);
                if (current.Open > 0.001f ? frame.Open <= current.Open : frame.Open <= 0.001f && frame.LineWidth <= current.LineWidth)
                {
                    return t;
                }
            }

            return duration;
        }

        private float FindOpenClock(DroneTerminalWindowFrame current)
        {
            var duration = settings.openDuration;
            for (var i = 0; i <= ReverseSamples; i++)
            {
                var t = duration * i / ReverseSamples;
                var frame = DroneTerminalTimeline.Open(t, settings);
                if (current.Open > 0.001f ? frame.Open >= current.Open : frame.LineWidth >= current.LineWidth)
                {
                    return t;
                }
            }

            return duration;
        }

        /// <summary>열기 연출을 건너뛰어 펼쳐진 상태(스캔라인 없음)로 둔다.</summary>
        public void SkipToShown()
        {
            if (Phase == DroneTerminalPhase.Opening || Phase == DroneTerminalPhase.Shown)
            {
                Phase = DroneTerminalPhase.Shown;
                Clock = Math.Max(Clock, settings.openDuration + settings.scanSweepDuration);
            }
        }

        public void HideImmediate()
        {
            Phase = DroneTerminalPhase.Hidden;
            Clock = 0f;
        }

        /// <summary>시간을 진행한다. 닫기가 이번에 끝났으면 true.</summary>
        public bool Tick(float deltaTime)
        {
            if (Phase == DroneTerminalPhase.Hidden)
            {
                return false;
            }

            Clock += Math.Max(0f, deltaTime);
            if (Phase == DroneTerminalPhase.Opening && Clock >= settings.openDuration)
            {
                Phase = DroneTerminalPhase.Shown;
            }
            else if (Phase == DroneTerminalPhase.Closing && Clock >= settings.closeDuration)
            {
                HideImmediate();
                return true;
            }

            return false;
        }

        public DroneTerminalWindowFrame Frame()
        {
            switch (Phase)
            {
                case DroneTerminalPhase.Closing:
                    return DroneTerminalTimeline.Close(Clock, settings);
                case DroneTerminalPhase.Hidden:
                    return new DroneTerminalWindowFrame(0f, 0f, 0f, 0f, 1f, 0f);
                default:
                    return DroneTerminalTimeline.Open(Clock, settings);
            }
        }
    }
}
