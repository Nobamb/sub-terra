using System;
using UnityEngine;

namespace SubTerra.App.UI.MainMenu
{
    /// <summary>B-150 부팅 연출 시간 값. 인스펙터에서 조정하며 합계는 2초 안쪽으로 묶는다(B-170에서 약 2배).</summary>
    [Serializable]
    public sealed class MainMenuBootSettings
    {
        [Tooltip("가로선이 번쩍 그어지는 시간")]
        public float lineDuration = 0.16f;
        [Tooltip("가로선이 위아래로 펼쳐지는 시간")]
        public float openDuration = 0.44f;
        public float headerStart = 0.32f;
        public float headerTyping = 0.48f;
        [Tooltip("'신호 연결됨'으로 바뀌는 시점")]
        public float headerConnectedAt = 0.92f;
        public float logoStart = 0.60f;
        public float logoGlitch = 0.36f;
        public float subtitleStart = 0.88f;
        public float subtitleTyping = 0.40f;
        public float cardStart = 1.08f;
        [Tooltip("슬롯 카드 사이 등장 간격")]
        public float cardStagger = 0.16f;
        public float cardFade = 0.36f;
        [Tooltip("카드가 아래에서 올라오는 거리(px)")]
        public float cardRise = 22f;
        public float continueStart = 1.44f;
        public float buttonFade = 0.24f;
        public float secondaryStart = 1.60f;
        public float secondaryStagger = 0.06f;
    }

    /// <summary>부팅 연출 한 시점의 값. View는 이 값만 적용한다.</summary>
    public readonly struct MainMenuBootFrame
    {
        public float LineWidth { get; }
        public float LineAlpha { get; }
        public float Open { get; }
        public float HeaderTyping { get; }
        public bool HeaderConnected { get; }
        public float LogoAlpha { get; }
        public float LogoGlitch { get; }
        public float SubtitleTyping { get; }
        public bool IsComplete { get; }

        public MainMenuBootFrame(float lineWidth, float lineAlpha, float open, float headerTyping, bool headerConnected,
            float logoAlpha, float logoGlitch, float subtitleTyping, bool isComplete)
        {
            LineWidth = lineWidth;
            LineAlpha = lineAlpha;
            Open = open;
            HeaderTyping = headerTyping;
            HeaderConnected = headerConnected;
            LogoAlpha = logoAlpha;
            LogoGlitch = logoGlitch;
            SubtitleTyping = subtitleTyping;
            IsComplete = isComplete;
        }
    }

    /// <summary>
    /// B-150 메인화면 켜짐: 가로선 → 위아래 펼침 → 부팅 문구 타이핑 → 로고 글리치 → 부제 타이핑
    /// → 카드 3장 순차 등장 → 이어하기 → 새 게임·설정·종료. 건너뛰면 즉시 끝 상태.
    /// </summary>
    public sealed class MainMenuBootTimeline
    {
        public const float MaxDuration = 2.0f;
        public const int CardCount = 3;
        public const int SecondaryButtonCount = 3;
        /// <summary>B-170: 시작 프레임과 그다음 프레임의 dt는 씬 로딩 시간이라 버린다.</summary>
        public const int DiscardedFrames = 2;
        /// <summary>한 프레임에 더하는 최대 시간. 렉이 걸리면 연출이 그만큼 느려진다.</summary>
        public const float MaxStep = 1f / 30f;

        private readonly MainMenuBootSettings settings;
        private float elapsed;
        private bool skipped;
        private int discarded;
        private bool clockStarted;

        public MainMenuBootTimeline(MainMenuBootSettings settings = null)
        {
            this.settings = settings ?? new MainMenuBootSettings();
        }

        public MainMenuBootSettings Settings => settings;
        public float Elapsed => elapsed;
        public bool WasSkipped => skipped;
        public bool IsComplete => skipped || elapsed >= TotalDuration;

        /// <summary>마지막 버튼까지 다 나타나는 시점. MaxDuration을 넘지 않는다.</summary>
        public float TotalDuration
        {
            get
            {
                var end = Mathf.Max(
                    settings.lineDuration + settings.openDuration,
                    settings.headerConnectedAt,
                    settings.logoStart + settings.logoGlitch,
                    settings.subtitleStart + settings.subtitleTyping,
                    CardStart(CardCount - 1) + settings.cardFade,
                    settings.continueStart + settings.buttonFade,
                    SecondaryStart(SecondaryButtonCount - 1) + settings.buttonFade);
                return Mathf.Min(end, MaxDuration);
            }
        }

        public void Advance(float deltaSeconds)
        {
            if (deltaSeconds > 0f)
            {
                elapsed = Mathf.Min(elapsed + deltaSeconds, TotalDuration);
            }
        }

        /// <summary>시계가 실제로 움직이기 시작했는지(처음 두 프레임 뒤).</summary>
        public bool ClockStarted => clockStarted;

        /// <summary>
        /// 프레임마다 부른다. 처음 DiscardedFrames 프레임의 dt는 버리고, 그 뒤는 MaxStep으로 잘라 더한다.
        /// 시계가 처음 움직인 프레임에만 true(켜짐 소리 시점).
        /// </summary>
        public bool Tick(float rawDeltaSeconds)
        {
            if (IsComplete) return false;
            if (discarded < DiscardedFrames)
            {
                discarded++;
                return false;
            }

            Advance(Mathf.Min(rawDeltaSeconds, MaxStep));
            if (clockStarted || elapsed <= 0f) return false;
            clockStarted = true;
            return true;
        }

        public void Skip()
        {
            skipped = true;
            elapsed = TotalDuration;
        }

        public void Restart()
        {
            skipped = false;
            elapsed = 0f;
            discarded = 0;
            clockStarted = false;
        }

        /// <summary>B-170 종료 연출: 시간표를 임의 시점으로 옮겨 거꾸로 샘플한다.</summary>
        public void SetElapsed(float seconds)
        {
            skipped = false;
            elapsed = Mathf.Clamp(seconds, 0f, TotalDuration);
        }

        /// <summary>가로선 → 위아래 펼침까지(화면 부분)의 길이.</summary>
        public float ScreenDuration => settings.lineDuration + settings.openDuration;

        /// <summary>부팅 문구·부제에서 지금까지 타이핑된 글자 수(신호 연결됨 교체는 세지 않는다).</summary>
        public int TypedCharacters(int headerLength, int subtitleLength)
        {
            if (IsComplete) return headerLength + subtitleLength;
            var frame = Sample();
            return Mathf.CeilToInt(headerLength * frame.HeaderTyping)
                + Mathf.CeilToInt(subtitleLength * frame.SubtitleTyping);
        }

        private float Clock => IsComplete ? float.MaxValue : elapsed;

        public float CardStart(int index) => settings.cardStart + settings.cardStagger * index;
        public float SecondaryStart(int index) => settings.secondaryStart + settings.secondaryStagger * index;

        public MainMenuBootFrame Sample()
        {
            var t = Clock;
            var lineEnd = settings.lineDuration;
            var openEnd = lineEnd + settings.openDuration;
            var lineWidth = EaseOut(Range(t, 0f, lineEnd));
            var open = EaseOut(Range(t, lineEnd, openEnd));
            // 선은 펼쳐지는 동안 밝게 남았다가 끝나면 사라진다.
            var lineAlpha = IsComplete ? 0f : 1f - Smooth(Range(t, openEnd - settings.openDuration * 0.3f, openEnd));
            var header = Range(t, settings.headerStart, settings.headerStart + settings.headerTyping);
            var connected = t >= settings.headerConnectedAt;
            var logoT = Range(t, settings.logoStart, settings.logoStart + settings.logoGlitch);
            var logoAlpha = logoT <= 0f ? 0f : Mathf.Lerp(0.35f, 1f, Smooth(logoT));
            var glitch = logoT <= 0f || logoT >= 1f ? 0f : 1f - logoT;
            var subtitle = Range(t, settings.subtitleStart, settings.subtitleStart + settings.subtitleTyping);
            return new MainMenuBootFrame(lineWidth, lineAlpha, open, header, connected, logoAlpha, glitch, subtitle, IsComplete);
        }

        /// <summary>카드 index(0=위)의 알파(0~1). 위에서부터 cardStagger 간격으로 나타난다.</summary>
        public float CardAlpha(int index) => Smooth(Range(Clock, CardStart(index), CardStart(index) + settings.cardFade));

        /// <summary>카드가 원래 자리보다 아래에 있는 거리(px). 0이면 제자리.</summary>
        public float CardOffset(int index) => settings.cardRise * (1f - EaseOut(Range(Clock, CardStart(index), CardStart(index) + settings.cardFade)));

        public float ContinueAlpha() => Smooth(Range(Clock, settings.continueStart, settings.continueStart + settings.buttonFade));

        public float SecondaryAlpha(int index) => Smooth(Range(Clock, SecondaryStart(index), SecondaryStart(index) + settings.buttonFade));

        public static float Range(float t, float start, float end)
        {
            if (end <= start) return t >= start ? 1f : 0f;
            return Mathf.Clamp01((t - start) / (end - start));
        }

        public static float Smooth(float x) => x * x * (3f - 2f * x);
        public static float EaseOut(float x) => 1f - (1f - x) * (1f - x);
    }

    /// <summary>B-170 타이핑 틱: 보이는 글자 수가 늘어난 프레임에, 예정 시각 기준 0.06초에 한 번만.</summary>
    public sealed class MainMenuTypeTickGate
    {
        public const float MinInterval = 0.06f;

        private int lastCount;
        private float lastAt = float.NegativeInfinity;

        public void Reset()
        {
            lastCount = 0;
            lastAt = float.NegativeInfinity;
        }

        public bool Step(int typedCharacters, float time)
        {
            var grew = typedCharacters > lastCount;
            if (typedCharacters > lastCount) lastCount = typedCharacters;
            if (!grew || time - lastAt < MinInterval - 0.0001f) return false;
            lastAt = time;
            return true;
        }
    }

    /// <summary>
    /// B-170 종료 연출: 부팅 시간표를 거꾸로 샘플한다(t = 전체 × (1 − 진행)).
    /// 메인 화면은 전체를 2.5배로, 지상·광산 덮개는 화면 부분(펼침 → 가로선)만 쓴다.
    /// </summary>
    public static class QuitOutroTimeline
    {
        public const float MenuSpeed = 2.5f;
        public const float MenuMaxDuration = 0.8f;
        public const float OverlayDuration = 0.45f;
        /// <summary>애니메이션 연출 감소: 바로 검게 한 뒤 이 시간에 종료.</summary>
        public const float ReducedMotionDelay = 0.35f;

        public static float MenuDuration(MainMenuBootTimeline boot) =>
            Mathf.Min(boot.TotalDuration / MenuSpeed, MenuMaxDuration);

        public static float BootTimeAt(float total, float progress) => total * (1f - Mathf.Clamp01(progress));
    }
}
