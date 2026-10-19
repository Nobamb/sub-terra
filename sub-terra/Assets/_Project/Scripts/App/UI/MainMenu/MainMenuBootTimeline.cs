using System;
using UnityEngine;

namespace SubTerra.App.UI.MainMenu
{
    /// <summary>B-150 부팅 연출 시간 값. 인스펙터에서 조정하며 합계는 1초 안쪽으로 묶는다.</summary>
    [Serializable]
    public sealed class MainMenuBootSettings
    {
        [Tooltip("가로선이 번쩍 그어지는 시간")]
        public float lineDuration = 0.08f;
        [Tooltip("가로선이 위아래로 펼쳐지는 시간")]
        public float openDuration = 0.22f;
        public float headerStart = 0.16f;
        public float headerTyping = 0.24f;
        [Tooltip("'신호 연결됨'으로 바뀌는 시점")]
        public float headerConnectedAt = 0.46f;
        public float logoStart = 0.30f;
        public float logoGlitch = 0.18f;
        public float subtitleStart = 0.44f;
        public float subtitleTyping = 0.20f;
        public float cardStart = 0.54f;
        [Tooltip("슬롯 카드 사이 등장 간격")]
        public float cardStagger = 0.08f;
        public float cardFade = 0.18f;
        [Tooltip("카드가 아래에서 올라오는 거리(px)")]
        public float cardRise = 22f;
        public float continueStart = 0.72f;
        public float buttonFade = 0.12f;
        public float secondaryStart = 0.80f;
        public float secondaryStagger = 0.03f;
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
        public const float MaxDuration = 1.0f;
        public const int CardCount = 3;
        public const int SecondaryButtonCount = 3;

        private readonly MainMenuBootSettings settings;
        private float elapsed;
        private bool skipped;

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

        public void Skip()
        {
            skipped = true;
            elapsed = TotalDuration;
        }

        public void Restart()
        {
            skipped = false;
            elapsed = 0f;
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
}
