using System;
using UnityEngine;

namespace SubTerra.Gameplay.Hazards
{
    /// <summary>
    /// 가스 구름·조명 구멍의 시각 계산. 판정(GasZone.Contains, IsCleared)과 무관한 순수 함수만 둔다.
    /// 셰이더(GasCloud, GasVisionVeil, GasVisionOverlayUI)와 같은 곡선을 CPU에서 재현해 테스트한다.
    /// </summary>
    public static class GasVisualMath
    {
        public static float SmoothStep(float edge0, float edge1, float x)
        {
            if (edge1 <= edge0)
            {
                return x < edge0 ? 0f : 1f;
            }

            var t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
            return t * t * (3f - 2f * t);
        }

        /// <summary>
        /// 조명 구멍이 베일을 걷는 정도(0~1). 반지름 안쪽에서만 감쇠하고, 반지름 이상은 항상 0이다.
        /// inner 비율까지는 1, 경계로 갈수록 0으로 스며든다.
        /// </summary>
        public static float LightCover(float distance, float radius, float inner)
        {
            if (radius <= 0f || distance >= radius)
            {
                return 0f;
            }

            var clampedInner = Mathf.Clamp(inner, 0f, 0.99f);
            return 1f - SmoothStep(clampedInner * radius, radius, distance);
        }

        /// <summary>
        /// 구름 밀도 가중치. 중심만 진하고 바깥으로 부드럽게 사라지며, 반지름(1) 밖은 항상 0이다.
        /// 유독가스는 바닥에 고이므로 yNormalized(0 바닥, 1 천장)가 높을수록 옅다.
        /// </summary>
        public static float CloudDensity(
            float radialNormalized,
            float yNormalized,
            float core,
            float floorBias,
            float topDensity)
        {
            if (radialNormalized >= 1f)
            {
                return 0f;
            }

            var radial = 1f - SmoothStep(Mathf.Clamp(core, 0f, 0.95f), 1f, radialNormalized);
            var heavy = 1f - SmoothStep(0.25f, 1f, Mathf.Clamp01(yNormalized));
            var floorWeight = Mathf.Lerp(1f, Mathf.Lerp(Mathf.Clamp01(topDensity), 1f, heavy), Mathf.Clamp01(floorBias));
            var inside = 1f - SmoothStep(0.88f, 1f, radialNormalized);
            return Mathf.Clamp01(radial * floorWeight) * inside;
        }

        /// <summary>가스 안 햇무리 정도에 따라 베일 색이 가스 녹색 쪽으로 살짝 물든다.</summary>
        public static Color VeilDark(float opacity, float haze, float strength)
        {
            var amount = Mathf.Clamp01(haze) * Mathf.Clamp01(strength);
            var rgb = Color.Lerp(GasVisualRules.VeilDarkColor, GasVisualRules.HazeDarkColor, amount);
            return new Color(rgb.r, rgb.g, rgb.b, Mathf.Clamp01(opacity));
        }

        /// <summary>조명 구멍 중심 색. 가스 안에서는 알파가 조금 올라가고 녹색이 섞여 탁해진다.</summary>
        public static Color LightHole(float haze, float strength, float extraAlpha)
        {
            var amount = Mathf.Clamp01(haze) * Mathf.Clamp01(strength);
            var rgb = Color.Lerp(GasVisualRules.LightClearTint, GasVisualRules.GasTint, amount);
            var alpha = GasVisualRules.LightClearRedOpacity + Mathf.Max(0f, extraAlpha) * Mathf.Clamp01(haze);
            return new Color(rgb.r, rgb.g, rgb.b, Mathf.Clamp01(alpha));
        }

        /// <summary>가스 안 햇무리 정도. 안에 있으면 rise초, 나가면 fall초에 걸쳐 서서히 움직여 번쩍이지 않는다.</summary>
        public static float StepHaze(float current, float target, float deltaTime, float riseSeconds, float fallSeconds)
        {
            var clampedTarget = Mathf.Clamp01(target);
            var dt = Mathf.Max(0f, deltaTime);
            if (clampedTarget >= current)
            {
                var rate = dt / Mathf.Max(0.0001f, riseSeconds);
                return Mathf.Min(clampedTarget, current + rate);
            }

            var fall = dt / Mathf.Max(0.0001f, fallSeconds);
            return Mathf.Max(clampedTarget, current - fall);
        }

        /// <summary>
        /// 떠다니는 입자 한 개의 상태. 시간만의 함수라서 별도 상태 없이 풀링된 렌더러에 그대로 적용한다.
        /// local은 구역 중심 기준 -1~1 단위 원 안(0.9 이내)이고 바닥 쪽으로 치우친다.
        /// </summary>
        public static void EvaluateMote(
            int index,
            float seed,
            float time,
            float floorBias,
            out Vector2 local,
            out float alpha,
            out float scale)
        {
            var h1 = Hash01(index * 12.9898f + seed * 78.233f);
            var h2 = Hash01(index * 39.346f + seed * 11.135f);
            var h3 = Hash01(index * 7.123f + seed * 3.719f);
            var cycleSeconds = 6f + h3 * 5f;
            var phase = time / cycleSeconds + h1;
            var frac = phase - Mathf.Floor(phase);

            var angle = (h1 + Mathf.Floor(phase) * 0.37f) * Mathf.PI * 2f;
            var distance = Mathf.Sqrt(h2) * 0.72f;
            var x = Mathf.Cos(angle) * distance;
            var y = Mathf.Sin(angle) * distance;
            x += Mathf.Sin(time * (0.35f + h2 * 0.3f) + h1 * 6.28f) * 0.08f;
            y += Mathf.Sin(time * (0.28f + h1 * 0.3f) + h2 * 6.28f) * 0.06f + frac * 0.10f;
            y = Mathf.Lerp(y, y * 0.55f - 0.30f, Mathf.Clamp01(floorBias));

            var length = Mathf.Sqrt(x * x + y * y);
            if (length > 0.9f)
            {
                var k = 0.9f / length;
                x *= k;
                y *= k;
            }

            local = new Vector2(x, y);
            var envelope = Mathf.Sin(frac * Mathf.PI);
            alpha = envelope * envelope;
            scale = 0.7f + 0.6f * h3;
        }

        private static float Hash01(float x)
        {
            var s = Mathf.Sin(x) * 43758.5453f;
            return s - Mathf.Floor(s);
        }
    }

    /// <summary>가스 구름 연출 수치. 인스펙터에서 조절한다. 판정 반지름과는 무관하다.</summary>
    [Serializable]
    public sealed class GasCloudTuning
    {
        [Range(0f, 0.9f)] public float core = 0.10f;
        [Range(0f, 1f)] public float noiseAmount = 0.80f;
        [Range(0f, 0.5f)] public float edgeWarp = 0.26f;
        [Range(0f, 1f)] public float floorBias = 0.65f;
        [Range(0f, 1f)] public float topDensity = 0.30f;
        [Min(0.001f)] public float noiseScaleA = 0.35f;
        [Min(0.001f)] public float noiseScaleB = 0.22f;
        public Vector2 scrollA = new(0.020f, 0.006f);
        public Vector2 scrollB = new(-0.012f, 0.004f);
        [Range(0f, 0.70f)] public float maxOpacity = GasVisualRules.GasVisualOpacity;
        [Range(0, 16)] public int motesPerZone = 8;
        [Range(0f, 0.4f)] public float moteMaxAlpha = 0.22f;
        [Min(0.05f)] public float moteWorldSize = 0.55f;

        public const int GlobalMoteCap = 48;

        public float OpacityCapped => Mathf.Min(maxOpacity, GasVisualRules.GasVisualOpacity);
        public int MotesCapped => Mathf.Clamp(motesPerZone, 0, GlobalMoteCap);
    }

    /// <summary>가스 안 햇무리(탁함) 수치. 판정이나 암전 해제에는 쓰이지 않는다.</summary>
    [Serializable]
    public sealed class GasInteriorHazeSettings
    {
        [Min(0.1f)] public float riseSeconds = 1.2f;
        [Min(0.05f)] public float fallSeconds = 0.4f;
    }

    /// <summary>
    /// 플레이어가 생성 연출이 끝난 가스 안에 있는 정도(0~1). GasExposureEffectController가 매 프레임 갱신하고
    /// 베일·암전 오버레이가 읽기만 한다.
    /// </summary>
    public static class GasInteriorHaze
    {
        public static float Amount { get; private set; }

        public static void Set(float value)
        {
            Amount = Mathf.Clamp01(value);
        }

        public static void Reset()
        {
            Amount = 0f;
        }
    }
}
