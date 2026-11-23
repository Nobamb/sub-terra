using UnityEngine;

namespace SubTerra.App.UI.MainMenu
{
    /// <summary>화면 전체 전환(글리치 또는 암전)의 한 시점 값.</summary>
    public readonly struct MinerSelectTransitionFrame
    {
        /// <summary>캡처 화면 글리치 세기(0~1). 캡처를 못 하면 노이즈 막대 알파로 쓴다.</summary>
        public float Intensity { get; }
        public float NoiseAlpha { get; }
        /// <summary>화면 조각의 좌우 어긋남(기준 해상도 px). 캡처를 못 할 때 내용 전체를 민다.</summary>
        public float SliceOffset { get; }
        /// <summary>TV 꺼지듯 세로로 접힘(0~1).</summary>
        public float Collapse { get; }
        /// <summary>접힌 뒤 남은 가로선 길이(1~0).</summary>
        public float LineWidth { get; }
        public float Flash { get; }
        public float BlackAlpha { get; }
        public int Seed { get; }
        public bool IsComplete { get; }

        public MinerSelectTransitionFrame(float intensity, float noiseAlpha, float sliceOffset, float collapse, float lineWidth,
            float flash, float blackAlpha, int seed, bool isComplete)
        {
            Intensity = intensity;
            NoiseAlpha = noiseAlpha;
            SliceOffset = sliceOffset;
            Collapse = collapse;
            LineWidth = lineWidth;
            Flash = flash;
            BlackAlpha = blackAlpha;
            Seed = seed;
            IsComplete = isComplete;
        }

        public static MinerSelectTransitionFrame Clear => new MinerSelectTransitionFrame(0f, 0f, 0f, 0f, 1f, 0f, 0f, 0, false);
    }

    /// <summary>TV처럼 켜지고 꺼지는 화면의 한 시점 값.</summary>
    public readonly struct MinerSelectTvFrame
    {
        /// <summary>세로로 펼쳐진 정도(0 가로선 ~ 1 전체).</summary>
        public float Open { get; }
        public float LineWidth { get; }
        public float LineAlpha { get; }
        public float Flash { get; }
        public float NoiseAlpha { get; }
        /// <summary>모션 감소 때의 전체 알파(아니면 1).</summary>
        public float Alpha { get; }
        public bool IsComplete { get; }

        public MinerSelectTvFrame(float open, float lineWidth, float lineAlpha, float flash, float noiseAlpha, float alpha, bool isComplete)
        {
            Open = open;
            LineWidth = lineWidth;
            LineAlpha = lineAlpha;
            Flash = flash;
            NoiseAlpha = noiseAlpha;
            Alpha = alpha;
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
    /// 모션 감소는 화면 전체 큰 움직임(TV 켜짐·꺼짐, 닫힘 글리치)만 짧은 페이드로 바꾸고,
    /// 카드 안 스캔·색 입힘·파티클·안내 글자 글리치는 그대로 둔다.
    /// </summary>
    public static class MinerSelectMotion
    {
        // ★ 초안 상수(PM 조절용).
        /// <summary>고를 때 위→아래 스캔 줄이 얼굴·윤곽을 드러내는 시간.</summary>
        public const float SilhouetteRevealSeconds = 0.6f;
        /// <summary>바꿔 고를 때 이전 카드가 아래→위로 다시 실루엣이 되는 시간.</summary>
        public const float SilhouetteHideSeconds = 0.3f;
        /// <summary>확정 때 위→아래 스캔 줄이 원래 색을 입히는 시간.</summary>
        public const float ColorFillSeconds = 0.7f;
        /// <summary>확정 뒤 닫힘 글리치(캡처 화면 찢김 → TV 꺼짐).</summary>
        public const float GlitchSeconds = 0.6f;
        public const float HintGlitchOutSeconds = 0.25f;
        public const float ReducedFadeSeconds = 0.2f;
        /// <summary>화면이 TV 켜지듯 나타나는 시간(가로선 → 세로로 펼침 → 노이즈 걷힘).</summary>
        public const float AppearLineSeconds = 0.12f;
        public const float AppearOpenSeconds = 0.36f;
        public const float AppearNoiseTailSeconds = 0.2f;
        /// <summary>돌아가기 때 TV 꺼지듯 접히는 시간.</summary>
        public const float CloseSeconds = 0.3f;
        /// <summary>마우스를 올렸을 때 카드가 커지는 데 걸리는 시간.</summary>
        public const float HoverSeconds = 0.3f;
        public const float HoverScale = 1.03f;
        /// <summary>안 고른 카드의 밝기.</summary>
        public const float DimmedBrightness = 0.45f;
        public const float GlitchStepSeconds = 1f / 30f;
        public const float MaxSliceOffset = 48f;

        // 닫힘 글리치 구간(비율).
        private const float ImpactEnd = 0.1f;
        private const float CollapseStart = 0.62f;
        private const float CollapseEnd = 0.86f;
        private const float BlackStart = 0.62f;

        public static float AppearSeconds(bool reduceMotion) =>
            reduceMotion ? ReducedFadeSeconds : AppearLineSeconds + AppearOpenSeconds + AppearNoiseTailSeconds;

        public static float CloseDuration(bool reduceMotion) => reduceMotion ? ReducedFadeSeconds : CloseSeconds;

        public static float TransitionSeconds(bool reduceMotion) => reduceMotion ? ReducedFadeSeconds : GlitchSeconds;

        /// <summary>TV 켜짐: 가운데 가로선이 번쩍 그어지고 → 위아래로 펼쳐지며 → 노이즈가 걷힌다.</summary>
        public static MinerSelectTvFrame EvaluateAppear(float elapsed, bool reduceMotion)
        {
            var duration = AppearSeconds(reduceMotion);
            if (elapsed >= duration) return new MinerSelectTvFrame(1f, 1f, 0f, 0f, 0f, 1f, true);
            var t = Mathf.Max(0f, elapsed);
            if (reduceMotion) return new MinerSelectTvFrame(1f, 1f, 0f, 0f, 0f, t / duration, false);

            var lineT = Mathf.Clamp01(t / AppearLineSeconds);
            var openT = Mathf.Clamp01((t - AppearLineSeconds) / AppearOpenSeconds);
            var tailT = Mathf.Clamp01((t - AppearLineSeconds - AppearOpenSeconds) / AppearNoiseTailSeconds);
            var open = EaseOut(openT);
            var lineAlpha = t < AppearLineSeconds ? 1f : 1f - Mathf.Clamp01(openT * 1.4f);
            var flash = t < AppearLineSeconds ? 0f : 0.06f * (1f - openT) * (1f - openT);
            var noise = t < AppearLineSeconds ? 0f : 0.35f * (1f - Mathf.Max(openT * 0.5f, tailT));
            return new MinerSelectTvFrame(open, EaseOut(lineT), lineAlpha, flash, noise, 1f, false);
        }

        /// <summary>TV 꺼짐(돌아가기): 위아래가 가운데로 접혀 가로선이 되고, 선이 양옆에서 줄어든다.</summary>
        public static MinerSelectTvFrame EvaluateClose(float elapsed, bool reduceMotion)
        {
            var duration = CloseDuration(reduceMotion);
            if (elapsed >= duration) return new MinerSelectTvFrame(0f, 0f, 0f, 0f, 0f, 0f, true);
            var t = Mathf.Max(0f, elapsed);
            if (reduceMotion) return new MinerSelectTvFrame(1f, 1f, 0f, 0f, 0f, 1f - t / duration, false);

            var squashEnd = duration * 0.65f;
            var squashT = Mathf.Clamp01(t / squashEnd);
            var shrinkT = Mathf.Clamp01((t - squashEnd) / (duration - squashEnd));
            var open = 1f - EaseIn(squashT);
            var flash = 0.25f * squashT * squashT * (1f - shrinkT);
            return new MinerSelectTvFrame(open, 1f - EaseIn(shrinkT), Mathf.Clamp01(squashT * 1.5f), flash,
                0.5f * squashT * (1f - shrinkT), 1f, false);
        }

        /// <summary>
        /// 확정 뒤 닫힘. 충격(번쩍) → 찢김·RGB 분리·블록 밀림 → TV 꺼지듯 접힘 → 가로선이 줄며 검정.
        /// 끝 프레임은 반드시 완전한 검정이다(다음 프레임에 인트로 오버레이가 이어 덮는다).
        /// </summary>
        public static MinerSelectTransitionFrame EvaluateTransition(float elapsed, bool reduceMotion)
        {
            var duration = TransitionSeconds(reduceMotion);
            if (elapsed >= duration)
            {
                return new MinerSelectTransitionFrame(0f, 0f, 0f, 1f, 0f, 0f, 1f, 0, true);
            }

            var e = Mathf.Max(0f, elapsed);
            var t = Mathf.Clamp01(e / duration);
            if (reduceMotion)
            {
                return new MinerSelectTransitionFrame(0f, 0f, 0f, 0f, 1f, 0f, t, 0, false);
            }

            var seed = (int)(e / GlitchStepSeconds);
            var r = Hash(seed);
            float intensity;
            float flash = 0f;
            if (t < ImpactEnd)
            {
                var k = t / ImpactEnd;
                intensity = 0.45f + 0.45f * k;
                flash = 0.1f * (1f - k);
            }
            else if (t < CollapseStart)
            {
                // 들쭉날쭉한 세기. 가끔 크게 튄다.
                intensity = 0.6f + 0.4f * r;
            }
            else
            {
                var k = Mathf.Clamp01((t - CollapseStart) / (1f - CollapseStart));
                intensity = Mathf.Lerp(0.7f, 0.15f, k);
            }

            var collapse = EaseIn(Mathf.Clamp01((t - CollapseStart) / (CollapseEnd - CollapseStart)));
            var lineWidth = 1f - EaseIn(Mathf.Clamp01((t - CollapseEnd) / (1f - CollapseEnd)));
            var blackT = Mathf.Clamp01((t - BlackStart) / (1f - BlackStart));
            var black = blackT * blackT * (3f - 2f * blackT);
            var noise = intensity * (1f - collapse * 0.7f);
            var slice = (r * 2f - 1f) * MaxSliceOffset * intensity * (1f - collapse);
            return new MinerSelectTransitionFrame(intensity, noise, slice, collapse, lineWidth, flash, black, seed, false);
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

        public static float EaseOut(float x)
        {
            var inv = 1f - Mathf.Clamp01(x);
            return 1f - inv * inv * inv;
        }

        public static float EaseIn(float x)
        {
            x = Mathf.Clamp01(x);
            return x * x * x;
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
