using UnityEngine;

namespace SubTerra.App.UI.Intro
{
    /// <summary>한 시점의 게임 화면 켜짐 값. View는 이 값만 적용한다.</summary>
    public readonly struct NewGameTurnOnFrame
    {
        /// <summary>중앙 가로선 길이(0~1, 화면 너비 비율).</summary>
        public float LineWidth { get; }
        public float LineAlpha { get; }
        /// <summary>검은 셔터가 열린 정도(0 닫힘 ~ 1 완전히 열림). 위아래로 갈라진다.</summary>
        public float Open { get; }
        /// <summary>켜지는 순간의 밝은 번쩍임 알파.</summary>
        public float FlashAlpha { get; }
        /// <summary>영상 끝과 같은 가로 노이즈 막대의 알파(걷히는 중).</summary>
        public float NoiseAlpha { get; }
        public int NoiseSeed { get; }
        public bool IsComplete { get; }

        public NewGameTurnOnFrame(float lineWidth, float lineAlpha, float open, float flashAlpha,
            float noiseAlpha, int noiseSeed, bool isComplete)
        {
            LineWidth = lineWidth;
            LineAlpha = lineAlpha;
            Open = open;
            FlashAlpha = flashAlpha;
            NoiseAlpha = noiseAlpha;
            NoiseSeed = noiseSeed;
            IsComplete = isComplete;
        }
    }

    /// <summary>
    /// 인트로 영상 끝(노이즈 → 검은 화면 → 가로선으로 꺼짐)을 되감듯,
    /// 가로선이 번쩍 그어지고 → 위아래로 펼쳐지며 게임 화면이 드러나고 → 노이즈가 걷히는 켜짐 연출.
    /// 시간은 호출자가 비스케일 dt로 넣는다. 순수 C#이라 EditMode에서 검사한다.
    /// </summary>
    public sealed class NewGameTurnOnTimeline
    {
        public const float DefaultLineDuration = 0.16f;
        public const float DefaultOpenDuration = 0.55f;
        public const float DefaultNoiseTail = 0.30f;
        public const float MaxStep = 1f / 30f;
        public const float NoiseStepSeconds = 1f / 24f;

        private readonly float lineDuration;
        private readonly float openDuration;
        private readonly float noiseTail;
        private float elapsed;
        private bool skipped;

        public NewGameTurnOnTimeline(
            float lineDuration = DefaultLineDuration,
            float openDuration = DefaultOpenDuration,
            float noiseTail = DefaultNoiseTail)
        {
            this.lineDuration = Mathf.Max(0.01f, lineDuration);
            this.openDuration = Mathf.Max(0.01f, openDuration);
            this.noiseTail = Mathf.Max(0f, noiseTail);
        }

        public float Elapsed => elapsed;
        public float TotalDuration => lineDuration + openDuration + noiseTail;
        public bool IsComplete => skipped || elapsed >= TotalDuration;

        public NewGameTurnOnFrame Advance(float deltaSeconds)
        {
            if (deltaSeconds > 0f && !skipped)
            {
                elapsed += Mathf.Min(deltaSeconds, MaxStep);
            }

            return Evaluate();
        }

        public void Skip() => skipped = true;

        public NewGameTurnOnFrame Evaluate()
        {
            if (skipped || elapsed >= TotalDuration)
            {
                return new NewGameTurnOnFrame(1f, 0f, 1f, 0f, 0f, 0, true);
            }

            var t = elapsed;
            var lineT = Mathf.Clamp01(t / lineDuration);
            var lineWidth = EaseOut(lineT);

            var openT = Mathf.Clamp01((t - lineDuration) / openDuration);
            var open = EaseOut(openT);
            // 셔터가 벌어지는 동안 선은 얇게 남다가 서서히 사라진다.
            var lineAlpha = t < lineDuration ? 1f : 1f - Mathf.Clamp01(openT * 1.6f);

            var flash = t < lineDuration ? 0f : 0.2f * (1f - openT) * (1f - openT);

            var noiseT = Mathf.Clamp01((t - lineDuration) / (openDuration + noiseTail));
            var noiseAlpha = t < lineDuration ? 0f : 0.7f * (1f - noiseT);
            var seed = (int)(t / NoiseStepSeconds);

            return new NewGameTurnOnFrame(lineWidth, lineAlpha, open, flash, noiseAlpha, seed, false);
        }

        private static float EaseOut(float x)
        {
            var inv = 1f - x;
            return 1f - inv * inv * inv;
        }
    }
}
