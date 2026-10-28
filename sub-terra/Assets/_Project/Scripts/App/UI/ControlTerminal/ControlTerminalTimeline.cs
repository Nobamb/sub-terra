using UnityEngine;

namespace SubTerra.App.UI.ControlTerminal
{
    /// <summary>최초 등장 한 시점의 표시 값. 정착 후(t ≥ Duration)에는 모두 기준값이다.</summary>
    public readonly struct ControlTerminalIntroFrame
    {
        /// <summary>true인 동안은 홀로그램 연출 층(빛·테두리·잔상)이 그려진다.</summary>
        public bool HoloActive { get; }
        /// <summary>키보드 판(키캡 제외)의 불투명도. 키캡은 각자 튀어나온다.</summary>
        public float KeyboardAlpha { get; }
        /// <summary>제목·설명·하단 요소의 점등 비율.</summary>
        public float TextAlpha { get; }
        public bool Settled { get; }

        public ControlTerminalIntroFrame(bool holoActive, float keyboardAlpha, float textAlpha, bool settled)
        {
            HoloActive = holoActive;
            KeyboardAlpha = keyboardAlpha;
            TextAlpha = textAlpha;
            Settled = settled;
        }
    }

    /// <summary>
    /// 프레임 홀로그램 연출 한 시점의 값. 수평 빛이 펼쳐져 사각 테두리가 열리고,
    /// 흰빛이 점차 청록색으로 가라앉는다. 등장과 퇴장이 같은 모양을 쓴다.
    /// </summary>
    public readonly struct ControlTerminalHoloFrame
    {
        /// <summary>열린 창의 가로·세로 크기(카드 대비 0~1).</summary>
        public float Width { get; }
        public float Height { get; }
        /// <summary>열리는 동안 창 안에 보이는 프레임 그림의 불투명도.</summary>
        public float CopyAlpha { get; }
        /// <summary>완성된 실제 프레임을 보여야 하는가(창 연출과 겹치지 않는 구간).</summary>
        public bool RealFrame { get; }
        /// <summary>수평으로 뻗는 빛줄기.</summary>
        public float LineAlpha { get; }
        /// <summary>열리는 창을 따라가는 얇은 사각 테두리.</summary>
        public float EdgeAlpha { get; }
        /// <summary>프레임이 흰색으로 빛나는 정도.</summary>
        public float Flash { get; }
        /// <summary>카드 바깥으로 번지는 청록 빛.</summary>
        public float Glow { get; }
        /// <summary>위에서 아래로 훑는 스캔 빔의 위치(0=위 1=아래)와 세기.</summary>
        public float BeamPosition { get; }
        public float BeamAlpha { get; }
        /// <summary>네 모서리의 십자 반짝임.</summary>
        public float Flare { get; }
        public bool Active { get; }

        public ControlTerminalHoloFrame(float width, float height, float copyAlpha, bool realFrame, float lineAlpha,
            float edgeAlpha, float flash, float glow, float beamPosition, float beamAlpha, float flare, bool active)
        {
            Width = width;
            Height = height;
            CopyAlpha = copyAlpha;
            RealFrame = realFrame;
            LineAlpha = lineAlpha;
            EdgeAlpha = edgeAlpha;
            Flash = flash;
            Glow = glow;
            BeamPosition = beamPosition;
            BeamAlpha = beamAlpha;
            Flare = flare;
            Active = active;
        }
    }

    /// <summary>카드 바깥에서 안쪽(등장) 또는 안쪽에서 바깥(퇴장)으로 움직이는 얇은 잔상 테두리 하나.</summary>
    public readonly struct ControlTerminalGhostFrame
    {
        /// <summary>카드 크기 대비 늘어난 비율(0이면 카드와 같은 크기).</summary>
        public float Spread { get; }
        public float Alpha { get; }

        public ControlTerminalGhostFrame(float spread, float alpha)
        {
            Spread = spread;
            Alpha = alpha;
        }
    }

    /// <summary>키캡 하나가 튀어나오는 한 시점 값. 정착하면 scale 1·lift 0·alpha 1이다.</summary>
    public readonly struct ControlTerminalKeyPop
    {
        public float Scale { get; }
        /// <summary>위로 솟은 거리(디자인 px).</summary>
        public float Lift { get; }
        public float Alpha { get; }

        public ControlTerminalKeyPop(float scale, float lift, float alpha)
        {
            Scale = scale;
            Lift = lift;
            Alpha = alpha;
        }
    }

    /// <summary>퇴장 한 시점의 표시 값. 끝나면 모두 0이다.</summary>
    public readonly struct ControlTerminalOutroFrame
    {
        public float ContentAlpha { get; }
        public bool HoloActive { get; }

        public ControlTerminalOutroFrame(float contentAlpha, bool holoActive)
        {
            ContentAlpha = contentAlpha;
            HoloActive = holoActive;
        }
    }

    /// <summary>시연 화면이 켜지는/꺼지는 한 시점의 모양(level 0=꺼짐 1=켜짐).</summary>
    public readonly struct ControlTerminalScreenFrame
    {
        public float WidthFraction { get; }
        public float HeightFraction { get; }
        public float LightAlpha { get; }
        public float ScanPosition { get; }
        public float ScanAlpha { get; }
        public float ContentAlpha { get; }

        public ControlTerminalScreenFrame(float width, float height, float light, float scan, float scanAlpha, float content)
        {
            WidthFraction = width;
            HeightFraction = height;
            LightAlpha = light;
            ScanPosition = scan;
            ScanAlpha = scanAlpha;
            ContentAlpha = content;
        }
    }

    /// <summary>
    /// 조작 단말의 시간표. 모두 상태 없는 순수 계산이라 EditMode에서 검증한다.
    /// 등장은 '수평 빛 → 사각 테두리 전개 → 잔상 테두리 수렴 → 흰빛 소멸 → 키캡 돌출' 순서다.
    /// </summary>
    public static class ControlTerminalTimeline
    {
        public const float IntroDuration = 1.04f;
        public const float OutroDuration = 0.36f;
        public const float PageSwapDuration = 0.22f;
        public const int GhostCount = 3;
        /// <summary>수평 빛이 카드 폭까지 뻗는 시각.</summary>
        public const float LineEnd = 0.13f;
        /// <summary>창이 모두 열려 실제 프레임이 이어받는 시각. 키캡은 조금 뒤에 시작한다.</summary>
        public const float OpenEnd = 0.36f;
        /// <summary>흰빛·잔상 같은 연출 층이 모두 사라지는 시각.</summary>
        public const float HoloEnd = 0.82f;
        /// <summary>키캡이 솟았다 돌아오는 시간.</summary>
        public const float KeyPopDuration = 0.32f;
        /// <summary>키캡이 가장 크게 튀어나온 배율.</summary>
        public const float KeyPopPeak = 1.55f;
        /// <summary>줄(0~3)·마우스(4) 별 키캡 시차.</summary>
        public const float KeyStagger = 0.04f;
        public const int RowCount = 5;
        /// <summary>키캡 시작 시각(프레임이 다 열린 직후).</summary>
        public const float KeyPopStart = 0.42f;

        private const float KeyColumnSpan = 0.06f;
        private const float SpaceExtraDelay = 0.04f;
        private const float KeyLift = 16f;
        private const float OutroFlashEnd = 0.10f;
        private const float OutroCopyStart = 0.06f;

        public static float Range(float t, float start, float end)
        {
            if (end <= start) return t >= end ? 1f : 0f;
            var x = (t - start) / (end - start);
            return x < 0f ? 0f : x > 1f ? 1f : x;
        }

        /// <summary>키캡 줄(0~3)·마우스(4)가 튀어나오기 시작하는 시각.</summary>
        public static float KeyRowStart(int row) => KeyPopStart + row * KeyStagger;

        /// <summary>이번 진행(previous → now)에서 시작 시각을 넘은 줄 중 가장 늦은 줄. 없으면 -1.</summary>
        public static int CrossedKeyRow(float previous, float now)
        {
            int row = -1;
            for (int i = 0; i < RowCount; i++)
            {
                var start = KeyRowStart(i);
                if (previous < start && start <= now) row = i;
            }

            return row;
        }

        public static float Smooth(float x) => x * x * (3f - 2f * x);
        public static float EaseOut(float x) => 1f - (1f - x) * (1f - x) * (1f - x);

        public static ControlTerminalIntroFrame Intro(float t)
        {
            if (t >= IntroDuration) return new ControlTerminalIntroFrame(false, 1f, 1f, true);
            if (t < 0f) t = 0f;
            return new ControlTerminalIntroFrame(
                t < HoloEnd,
                Smooth(Range(t, 0.28f, 0.42f)),
                Smooth(Range(t, 0.30f, 0.62f)),
                false);
        }

        /// <summary>
        /// 등장 홀로그램. 0~0.13초 수평 빛이 뻗고, 0.10~0.36초 사각 테두리가 위아래로 열리며 프레임이 드러난다.
        /// 흰빛은 열림이 끝나는 순간 가장 강하고 0.82초까지 서서히 청록색으로 가라앉는다.
        /// </summary>
        public static ControlTerminalHoloFrame IntroHolo(float t)
        {
            if (t >= HoloEnd) return new ControlTerminalHoloFrame(1f, 1f, 0f, true, 0f, 0f, 0f, 0f, 1f, 0f, 0f, false);
            if (t < 0f) t = 0f;
            var flick = Flicker(t);
            var open = t >= OpenEnd;
            var width = EaseOut(Range(t, 0f, LineEnd));
            var height = t < 0.10f ? 0f : EaseOut(Range(t, 0.10f, OpenEnd));
            var line = (1f - Smooth(Range(t, 0.10f, 0.28f))) * flick;
            var edge = Smooth(Range(t, 0.06f, 0.16f)) * (1f - Smooth(Range(t, OpenEnd, 0.64f))) * flick;
            var flash = Smooth(Range(t, 0.05f, OpenEnd)) * (1f - Smooth(Range(t, OpenEnd, HoloEnd)));
            if (!open) flash *= flick;
            var glow = Smooth(Range(t, 0f, 0.26f)) * (1f - Smooth(Range(t, 0.40f, HoloEnd)));
            var beam = Range(t, 0.26f, 0.62f);
            var beamAlpha = beam <= 0f || beam >= 1f ? 0f : Mathf.Min(1f, Mathf.Min(beam * 7f, (1f - beam) * 4f));
            var flare = Mathf.Max(0f, Mathf.Sin(Range(t, 0.30f, 0.66f) * Mathf.PI));
            return new ControlTerminalHoloFrame(width, height, open ? 0f : 1f, open, line, edge, flash, glow, beam, beamAlpha, flare, true);
        }

        /// <summary>등장 때 카드 바깥 잔상 테두리 i가 안쪽으로 모여드는 모습. 바깥쪽 것일수록 늦게 시작한다.</summary>
        public static ControlTerminalGhostFrame IntroGhost(int index, float t)
        {
            var start = 0.12f + 0.05f * index;
            var u = Range(t, start, start + 0.32f);
            if (u <= 0f || u >= 1f) return new ControlTerminalGhostFrame(0f, 0f);
            var spread = (0.10f + 0.06f * index) * (1f - EaseOut(u));
            var alpha = Mathf.Max(0f, Mathf.Sin(u * Mathf.PI)) * (0.9f - 0.15f * index) * (index == 1 ? Flicker(t * 1.7f) : 1f);
            return new ControlTerminalGhostFrame(spread, alpha);
        }

        /// <summary>
        /// 퇴장 홀로그램. 프레임이 흰빛으로 달아오르고, 창이 위아래로 닫혀 수평 빛이 되었다가 양쪽에서 사라진다.
        /// </summary>
        public static ControlTerminalHoloFrame OutroHolo(float t)
        {
            if (t >= OutroDuration) return new ControlTerminalHoloFrame(0f, 0f, 0f, false, 0f, 0f, 0f, 0f, 0f, 0f, 0f, false);
            if (t < 0f) t = 0f;
            var copy = t >= OutroCopyStart;
            var height = 1f - Smooth(Range(t, 0.10f, 0.26f));
            var width = 1f - Smooth(Range(t, 0.24f, 0.35f));
            var flash = Smooth(Range(t, 0f, OutroFlashEnd)) * (1f - Smooth(Range(t, 0.26f, OutroDuration)));
            var line = Smooth(Range(t, 0.18f, 0.26f)) * (1f - Smooth(Range(t, 0.30f, OutroDuration)));
            var edge = Smooth(Range(t, 0f, 0.08f)) * (1f - Smooth(Range(t, 0.14f, 0.28f)));
            var glow = Smooth(Range(t, 0f, 0.10f)) * (1f - Smooth(Range(t, 0.10f, 0.28f)));
            var flare = Mathf.Max(0f, Mathf.Sin(Range(t, 0.02f, 0.20f) * Mathf.PI));
            return new ControlTerminalHoloFrame(width, height, copy ? 1f : 0f, !copy, line, edge, flash, glow, 0f, 0f, flare, true);
        }

        /// <summary>퇴장 때 잔상 테두리 i가 바깥으로 퍼지며 사라지는 모습.</summary>
        public static ControlTerminalGhostFrame OutroGhost(int index, float t)
        {
            var start = 0.04f + 0.03f * index;
            var u = Range(t, start, OutroDuration - 0.02f);
            if (u <= 0f || u >= 1f) return new ControlTerminalGhostFrame(0f, 0f);
            var spread = (0.05f + 0.04f * index) * EaseOut(u);
            var alpha = (1f - Smooth(u)) * Smooth(Range(u, 0f, 0.15f)) * (0.75f - 0.12f * index);
            return new ControlTerminalGhostFrame(spread, alpha);
        }

        /// <summary>
        /// 키캡이 없던 자리에서 크게 튀어나왔다가 제자리로 돌아오는 값.
        /// 시작 전에는 보이지 않고(scale 0), 끝나면 정확히 기준값이다. column01은 판 왼쪽 0~오른쪽 1.
        /// </summary>
        public static ControlTerminalKeyPop IntroKeyPop(float t, int row, float column01, bool isSpace)
        {
            var start = KeyPopStart + row * KeyStagger + Mathf.Clamp01(column01) * KeyColumnSpan + (isSpace ? SpaceExtraDelay : 0f);
            if (t < start) return new ControlTerminalKeyPop(0f, 0f, 0f);
            var u = Range(t, start, start + KeyPopDuration);
            if (u >= 1f) return new ControlTerminalKeyPop(1f, 0f, 1f);
            float scale;
            if (u < 0.45f) scale = KeyPopPeak * EaseOut(u / 0.45f);
            else if (u < 0.75f) scale = Mathf.Lerp(KeyPopPeak, 0.92f, Smooth((u - 0.45f) / 0.30f));
            else scale = Mathf.Lerp(0.92f, 1f, Smooth((u - 0.75f) / 0.25f));
            var lift = Mathf.Max(0f, scale - 1f) * KeyLift;
            return new ControlTerminalKeyPop(scale, lift, Smooth(Range(u, 0f, 0.15f)));
        }

        public static ControlTerminalOutroFrame Outro(float t)
        {
            if (t >= OutroDuration) return new ControlTerminalOutroFrame(0f, false);
            if (t < 0f) t = 0f;
            return new ControlTerminalOutroFrame(1f - Smooth(Range(t, 0f, 0.10f)), true);
        }

        /// <summary>홀로그램이 불안정하게 깜빡이는 정도(0.5~1). 열리는 동안에만 흔들리고 같은 t에서는 항상 같은 값이다.</summary>
        public static float Flicker(float t)
        {
            if (t >= OpenEnd) return 1f;
            var step = (int)(t * 52f);
            return (step * 37 + 11) % 5 == 0 ? 0.5f : (step * 13) % 7 == 0 ? 0.75f : 1f;
        }

        /// <summary>페이지 교체 때 키·설명 글자의 불투명도. 가운데에서 가장 어둡고 끝에서 정확히 1.</summary>
        public static float PageSwapAlpha(float t)
        {
            if (t <= 0f || t >= PageSwapDuration) return 1f;
            var u = t / PageSwapDuration;
            var dip = u < 0.4f ? Smooth(u / 0.4f) : 1f - Smooth((u - 0.4f) / 0.6f);
            return 1f - 0.8f * dip;
        }

        /// <summary>수평 빛 → 위아래로 펼침 → 짧은 스캔 → 영상. 꺼질 때는 같은 곡선을 거꾸로 쓴다.</summary>
        public static ControlTerminalScreenFrame Screen(float level)
        {
            if (level <= 0f) return new ControlTerminalScreenFrame(0f, 0f, 0f, 0f, 0f, 0f);
            if (level > 1f) level = 1f;
            var width = EaseOut(Range(level, 0f, 0.30f));
            var height = EaseOut(Range(level, 0.30f, 0.80f));
            var light = level >= 1f ? 0f : 1f - Smooth(Range(level, 0.55f, 0.95f));
            var scan = Range(level, 0.70f, 1f);
            var scanAlpha = scan <= 0f || scan >= 1f ? 0f : Mathf.Min(1f, Mathf.Min(scan * 6f, (1f - scan) * 6f));
            var content = Smooth(Range(level, 0.55f, 0.85f));
            return new ControlTerminalScreenFrame(width, height, light, scan, scanAlpha, content);
        }
    }

    /// <summary>
    /// 키 눌림 모션. 호버/자동 시연이 SetDown으로 방향만 정하고 Tick이 값을 진행한다.
    /// 눌림 0.08초, 복귀 0.12초에 아주 약한 탄성이 한 번 있고 끝나면 정확히 0이다.
    /// </summary>
    public sealed class ControlTerminalKeyMotion
    {
        public const float PressSeconds = 0.08f;
        public const float ReleaseSeconds = 0.12f;
        private const float BackStrength = 2.2f;

        private bool down;
        private bool releasing;
        private float level;
        private float releaseFrom;
        private float releaseTime;

        /// <summary>1이면 완전히 눌림, 0이면 기준 위치. 복귀 중 살짝 음수(기준보다 위)가 될 수 있다.</summary>
        public float Level => level;
        public bool IsDown => down;
        public bool IsSettled => !down && !releasing && level == 0f;

        public void SetDown(bool value)
        {
            if (value == down) return;
            down = value;
            if (down)
            {
                releasing = false;
            }
            else
            {
                releasing = true;
                releaseFrom = level;
                releaseTime = 0f;
                if (Mathf.Approximately(releaseFrom, 0f))
                {
                    releasing = false;
                    level = 0f;
                }
            }
        }

        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds <= 0f) return;
            if (down)
            {
                level = Mathf.Min(1f, level + deltaSeconds / PressSeconds);
                return;
            }

            if (!releasing) return;
            releaseTime += deltaSeconds;
            var u = releaseTime / ReleaseSeconds;
            if (u >= 1f)
            {
                releasing = false;
                level = 0f;
                return;
            }

            level = releaseFrom * (1f - EaseOutBack(u));
        }

        public void ResetImmediate()
        {
            down = false;
            releasing = false;
            level = 0f;
        }

        private static float EaseOutBack(float x)
        {
            var c3 = BackStrength + 1f;
            var p = x - 1f;
            return 1f + c3 * p * p * p + BackStrength * p * p;
        }
    }
}
