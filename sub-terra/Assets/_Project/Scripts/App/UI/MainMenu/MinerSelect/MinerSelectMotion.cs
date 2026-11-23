using UnityEngine;

namespace SubTerra.App.UI.MainMenu
{
    /// <summary>화면 전체 전환(글리치 또는 암전)의 한 시점 값.</summary>
    public readonly struct MinerSelectTransitionFrame
    {
        public float NoiseAlpha { get; }
        /// <summary>화면 조각의 좌우 어긋남(기준 해상도 px).</summary>
        public float SliceOffset { get; }
        public float BlackAlpha { get; }
        public int Seed { get; }
        public bool IsComplete { get; }

        public MinerSelectTransitionFrame(float noiseAlpha, float sliceOffset, float blackAlpha, int seed, bool isComplete)
        {
            NoiseAlpha = noiseAlpha;
            SliceOffset = sliceOffset;
            BlackAlpha = blackAlpha;
            Seed = seed;
            IsComplete = isComplete;
        }
    }

    /// <summary>안내 글자가 사라질 때의 한 시점 값.</summary>
    public readonly struct MinerSelectHintFrame
    {
        public float Alpha { get; }
        public float OffsetX { get; }
        public bool IsComplete { get; }

        public MinerSelectHintFrame(float alpha, float offsetX, bool isComplete)
        {
            Alpha = alpha;
            OffsetX = offsetX;
            IsComplete = isComplete;
        }
    }

    /// <summary>
    /// B-209 광부 선택 연출 시간표. 순수 계산이라 EditMode에서 검사한다.
    /// 모션 감소는 화면 전체 글리치만 끄고(짧은 암전으로 대체), 카드 안 색 변화·안내 글자 글리치는 그대로 둔다.
    /// </summary>
    public static class MinerSelectMotion
    {
        // ★ 초안 상수(PM 조절용).
        public const float SilhouetteSharpenSeconds = 0.25f;
        public const float ColorFillSeconds = 0.5f;
        public const float GlitchSeconds = 0.5f;
        public const float HintGlitchOutSeconds = 0.25f;
        public const float ReducedFadeSeconds = 0.2f;
        public const float HoverSeconds = 0.12f;
        /// <summary>안 고른 카드의 밝기.</summary>
        public const float DimmedBrightness = 0.5f;
        /// <summary>글리치 뒤쪽에서 검게 닫히기 시작하는 비율.</summary>
        public const float GlitchBlackStart = 0.45f;
        public const float GlitchStepSeconds = 1f / 30f;
        public const float MaxSliceOffset = 48f;

        public static float TransitionSeconds(bool reduceMotion) => reduceMotion ? ReducedFadeSeconds : GlitchSeconds;

        public static MinerSelectTransitionFrame EvaluateTransition(float elapsed, bool reduceMotion)
        {
            var duration = TransitionSeconds(reduceMotion);
            if (elapsed >= duration)
            {
                // 끝 프레임은 반드시 완전한 검정이다(다음 프레임에 인트로 오버레이가 이어 덮는다).
                return new MinerSelectTransitionFrame(0f, 0f, 1f, 0, true);
            }

            var t = Mathf.Clamp01(Mathf.Max(0f, elapsed) / duration);
            if (reduceMotion)
            {
                return new MinerSelectTransitionFrame(0f, 0f, t, 0, false);
            }

            var seed = (int)(Mathf.Max(0f, elapsed) / GlitchStepSeconds);
            // 결정적 의사난수: 같은 시각이면 같은 모양.
            var r = Hash(seed);
            var strength = t < GlitchBlackStart ? 0.55f + 0.45f * (t / GlitchBlackStart) : 1f;
            var noise = strength * (0.6f + 0.4f * r);
            var slice = (r * 2f - 1f) * MaxSliceOffset * strength;
            var blackT = Mathf.Clamp01((t - GlitchBlackStart) / (1f - GlitchBlackStart));
            // 닫히는 동안 살짝 깜빡이지만 전체로는 단조롭게 어두워진다.
            var black = Mathf.Clamp01(blackT * blackT * (3f - 2f * blackT));
            return new MinerSelectTransitionFrame(noise * (1f - black * 0.6f), slice, black, seed, false);
        }

        public static MinerSelectHintFrame EvaluateHint(float elapsed)
        {
            if (elapsed >= HintGlitchOutSeconds)
            {
                return new MinerSelectHintFrame(0f, 0f, true);
            }

            var t = Mathf.Clamp01(Mathf.Max(0f, elapsed) / HintGlitchOutSeconds);
            var seed = (int)(Mathf.Max(0f, elapsed) / GlitchStepSeconds);
            var r = Hash(seed + 7);
            // 지직거림: 켜졌다 꺼졌다 하며 전체로는 사라진다.
            var flicker = r > 0.35f ? 1f : 0.25f;
            var alpha = (1f - t) * flicker;
            var offset = (Hash(seed + 31) * 2f - 1f) * 18f * (1f - t * 0.5f);
            return new MinerSelectHintFrame(alpha, offset, false);
        }

        /// <summary>0~1 사이 값을 duration 동안 target 쪽으로 옮긴다.</summary>
        public static float Approach(float current, float target, float deltaSeconds, float duration)
        {
            if (duration <= 0f) return target;
            return Mathf.MoveTowards(current, target, Mathf.Max(0f, deltaSeconds) / duration);
        }

        public static float Hash(int seed)
        {
            unchecked
            {
                var x = (uint)(seed * 747796405 + 2891336453);
                x = ((x >> (int)((x >> 28) + 4)) ^ x) * 277803737u;
                x = (x >> 22) ^ x;
                return (x & 0xFFFFFF) / (float)0x1000000;
            }
        }
    }
}
