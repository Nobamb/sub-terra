using System;
using System.Collections.Generic;

namespace SubTerra.App.UI.Outpost
{
    /// <summary>한 시점(초)에서 보건소·충전기 등장 연출의 각 요소가 가질 값.</summary>
    public readonly struct FacilityServiceFrame
    {
        public float IconAlpha { get; }
        /// <summary>등장·정착 크기 배율(1 = 정상). 이동 중 확대 배율과 맥동은 View가 곱한다.</summary>
        public float IconScale { get; }
        /// <summary>0 = 화면 중앙, 1 = 팝업 상단 아이콘 자리.</summary>
        public float IconMove { get; }
        /// <summary>보건소: 심전도가 솟는 순간의 맥동, 충전기: 섬광 직후 아이콘 반동. 0~1.</summary>
        public float Pulse { get; }
        public float LineProgress { get; }
        public float LineAlpha { get; }
        public float ArcAlpha { get; }
        public int ArcStep { get; }
        public float Flash { get; }
        public float FlashSpread { get; }
        public float PanelOpen { get; }
        public float PanelGlow { get; }
        public float TextAlpha { get; }
        public float GaugeStartTime { get; }

        public FacilityServiceFrame(
            float iconAlpha, float iconScale, float iconMove, float pulse,
            float lineProgress, float lineAlpha, float arcAlpha, int arcStep,
            float flash, float flashSpread, float panelOpen, float panelGlow,
            float textAlpha, float gaugeStartTime)
        {
            IconAlpha = iconAlpha;
            IconScale = iconScale;
            IconMove = iconMove;
            Pulse = pulse;
            LineProgress = lineProgress;
            LineAlpha = lineAlpha;
            ArcAlpha = arcAlpha;
            ArcStep = arcStep;
            Flash = flash;
            FlashSpread = flashSpread;
            PanelOpen = panelOpen;
            PanelGlow = panelGlow;
            TextAlpha = textAlpha;
            GaugeStartTime = gaugeStartTime;
        }
    }

    /// <summary>
    /// 아이콘 등장 → 시설별 효과 → 팝업 펼침 흐름의 시간표. 상태를 갖지 않는 순수 계산이라 EditMode에서 검증한다.
    /// 시설별 효과가 끝나기 전에 패널이 펼쳐지도록 구간을 일부러 겹쳐 둔다.
    /// </summary>
    public static class FacilityServicePopupTimeline
    {
        /// <summary>정보 텍스트가 모두 나타나는 시점. 전체 0.6~0.8초 범위를 지킨다.</summary>
        public const float ClinicInfoTime = 0.70f;
        public const float ChargerInfoTime = 0.62f;
        public const float GaugeDuration = 0.32f;
        public const float ExitDuration = 0.18f;
        /// <summary>충전기 전기 아크가 새 모양으로 바뀌는 간격(초). 정전기처럼 끊겨 움직인다.</summary>
        public const float ArcStepSeconds = 1f / 30f;

        public static bool IsServiceMode(OutpostPanelMode mode)
        {
            return mode == OutpostPanelMode.Clinic || mode == OutpostPanelMode.Charger;
        }

        public static float InfoTime(OutpostPanelMode mode)
        {
            return mode == OutpostPanelMode.Charger ? ChargerInfoTime : ClinicInfoTime;
        }

        /// <summary>효과·펼침·게이지가 모두 끝나 정지 상태가 되는 시점.</summary>
        public static float TotalDuration(OutpostPanelMode mode)
        {
            return Evaluate(mode, 0f).GaugeStartTime + GaugeDuration;
        }

        public static FacilityServiceFrame Evaluate(OutpostPanelMode mode, float t)
        {
            return mode == OutpostPanelMode.Charger ? EvaluateCharger(t) : EvaluateClinic(t);
        }

        // 하트: 작게 나타나 살짝 커졌다 정착 → 심전도가 왼쪽에서 그려지고 중앙에서 한 번 솟음 → 하트 맥동 → 빛으로 잦아들며 패널 펼침
        private static FacilityServiceFrame EvaluateClinic(float t)
        {
            var appear = Overshoot(Range(t, 0f, 0.16f), 0.35f, 1.14f);
            var settle = Range(t, 0.14f, 0.28f);
            var scale = t < 0.16f ? appear : Lerp(1.14f, 1f, EaseInOut(settle));
            var line = Range(t, 0.10f, 0.44f);
            // 심전도가 중앙(진행 0.5)에 닿는 순간 하트가 한 번 맥동한다.
            var pulse = Bell(t, 0.27f, 0.07f);
            var lineAlpha = 1f - EaseInOut(Range(t, 0.42f, 0.58f));
            var open = EaseOut(Range(t, 0.40f, 0.64f));
            var move = EaseInOut(Range(t, 0.40f, 0.64f));
            var glow = Bell(t, 0.50f, 0.09f);
            var text = Smooth(Range(t, 0.56f, 0.70f));
            return new FacilityServiceFrame(
                Range(t, 0f, 0.10f), scale, move, pulse,
                line, lineAlpha, 0f, 0, 0f, 0f, open, glow, text, 0.64f);
        }

        // 번개: 빠르게 나타남 → 아이콘 가장자리에서 아크가 튐 + 섬광 한 번 → 아크가 불규칙하게 움직이다 잦아듦 → 패널 펼침
        private static FacilityServiceFrame EvaluateCharger(float t)
        {
            var appear = Overshoot(Range(t, 0f, 0.12f), 0.35f, 1.18f);
            var settle = Range(t, 0.12f, 0.20f);
            var scale = t < 0.12f ? appear : Lerp(1.18f, 1f, EaseInOut(settle));
            var arcIn = Range(t, 0.06f, 0.10f);
            var arcOut = 1f - Range(t, 0.26f, 0.36f);
            var arc = Math.Min(arcIn, arcOut);
            var flashRise = Range(t, 0.10f, 0.15f);
            var flashFall = 1f - EaseOut(Range(t, 0.15f, 0.30f));
            var flash = Math.Min(flashRise, flashFall);
            var spread = EaseOut(Range(t, 0.10f, 0.30f));
            var pulse = flash * 0.9f;
            var open = EaseOut(Range(t, 0.30f, 0.52f));
            var move = EaseInOut(Range(t, 0.30f, 0.52f));
            var glow = Bell(t, 0.40f, 0.07f);
            var text = Smooth(Range(t, 0.46f, 0.60f));
            return new FacilityServiceFrame(
                Range(t, 0f, 0.08f), scale, move, pulse,
                0f, 0f, arc, (int)(t / ArcStepSeconds), flash, spread, open, glow, text, 0.52f);
        }

        /// <summary>닫을 때 창 전체가 빠르게 사라지는 정도. 1에서 0으로.</summary>
        public static float ExitAlpha(float elapsed)
        {
            return 1f - Smooth(Range(elapsed, 0f, ExitDuration));
        }

        /// <summary>
        /// 심전도 한 번분의 꼭짓점. x는 -1~1(왼쪽→오른쪽), y는 -1~1(진폭 비율).
        /// 평평하다가 중앙 부근에서 한 번 솟았다 내려온 뒤 다시 평평해진다.
        /// </summary>
        private static readonly float[] EcgKeyX = { -1f, -0.20f, -0.12f, -0.05f, 0.02f, 0.10f, 0.17f, 1f };
        private static readonly float[] EcgKeyY = { 0f, 0f, 0.12f, 1f, -0.42f, 0.10f, 0f, 0f };

        /// <summary>선이 왼쪽에서 오른쪽으로 progress(0~1)만큼 그려진 꼭짓점을 만든다. 통째로 이동하지 않는다.</summary>
        public static void BuildEcg(float halfWidth, float amplitude, float progress, List<UnityEngine.Vector2> points)
        {
            points.Clear();
            var p = Clamp01(progress);
            if (p <= 0f)
            {
                return;
            }

            var limit = -1f + 2f * p;
            for (var i = 0; i < EcgKeyX.Length; i++)
            {
                if (EcgKeyX[i] <= limit)
                {
                    points.Add(new UnityEngine.Vector2(EcgKeyX[i] * halfWidth, EcgKeyY[i] * amplitude));
                    continue;
                }

                if (i > 0)
                {
                    var span = EcgKeyX[i] - EcgKeyX[i - 1];
                    var k = span > 0f ? (limit - EcgKeyX[i - 1]) / span : 0f;
                    points.Add(new UnityEngine.Vector2(
                        limit * halfWidth,
                        Lerp(EcgKeyY[i - 1], EcgKeyY[i], k) * amplitude));
                }

                break;
            }
        }

        /// <summary>시드마다 같은 0~1 값을 돌려주는 가벼운 해시. 전기 아크가 재생마다 같은 모양으로 튄다.</summary>
        public static float Hash01(int seed)
        {
            unchecked
            {
                var h = (uint)seed * 2654435761u;
                h ^= h >> 15;
                h *= 2246822519u;
                h ^= h >> 13;
                return (h & 0xFFFFFF) / (float)0x1000000;
            }
        }

        private static float Range(float t, float from, float to)
        {
            return to <= from ? 1f : Clamp01((t - from) / (to - from));
        }

        private static float Clamp01(float v)
        {
            return v < 0f ? 0f : v > 1f ? 1f : v;
        }

        private static float Lerp(float a, float b, float k)
        {
            return a + (b - a) * k;
        }

        private static float Smooth(float k)
        {
            return k * k * (3f - 2f * k);
        }

        private static float EaseInOut(float k)
        {
            return Smooth(k);
        }

        private static float EaseOut(float k)
        {
            var inv = 1f - k;
            return 1f - inv * inv * inv;
        }

        private static float Overshoot(float k, float from, float peak)
        {
            return Lerp(from, peak, EaseOut(k));
        }

        private static float Bell(float t, float center, float width)
        {
            var d = (t - center) / width;
            return (float)Math.Exp(-d * d);
        }
    }
}
