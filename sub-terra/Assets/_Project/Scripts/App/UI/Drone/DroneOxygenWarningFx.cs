using SubTerra.App.Core.Data;
using SubTerra.App.UI.RunFailure;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.Drone
{
    /// <summary>가장자리 글리치 조각 한 개의 한 시점 모양(패널 기준 정규화 값).</summary>
    public readonly struct DroneOxygenEdgeSliver
    {
        public readonly bool RightSide;
        public readonly float CenterY;
        public readonly float Height;
        public readonly float Width;
        public readonly float OffsetX;
        public readonly float Alpha;

        public DroneOxygenEdgeSliver(bool rightSide, float centerY, float height, float width, float offsetX, float alpha)
        {
            RightSide = rightSide;
            CenterY = centerY;
            Height = height;
            Width = width;
            OffsetX = offsetX;
            Alpha = alpha;
        }
    }

    /// <summary>
    /// B-213 산소 부족 경고 연출. 말풍선·터미널 패널의 가장자리에만 빨간 글리치 조각을 흔들고,
    /// 터미널에는 둘레 빨간 후광을 깜빡인다. 화면 전체·카메라는 건드리지 않는다.
    /// 시간 → 모양 계산은 정적 함수라 EditMode에서 그대로 검사한다.
    /// </summary>
    public sealed class DroneOxygenWarningFx
    {
        public const string TemplateId = DataIds.Dialogue.DroneOxygenWarning;
        public const string DefaultText = "산소가 부족합니다. 위로 올라가거나 긴급 탈출(구출 요청)을 쓰세요.";
        public const int SliverCount = 6;
        public const float JitterStep = 0.07f;
        public const float HaloBlinkPeriod = 0.55f;
        public const float StrongSeconds = 1.2f;
        public const float CalmStrength = 0.55f;
        public const float MaxSliverOffset = 9f;
        public const float HaloPadding = 16f;

        public static readonly Color AlertRed = new Color(1f, 0.2f, 0.18f, 1f);
        public static readonly Color AlertText = new Color(1f, 0.66f, 0.62f, 1f);
        public static readonly Color AlertHeader = new Color(0.24f, 0.03f, 0.04f, 0.99f);

        // 원본 후광 스프라이트 9-slice 테두리 픽셀(RunFailureGlitchArt.HaloBorder).
        private const float HaloSpriteBorder = 24f;

        private readonly RectTransform panel;
        private readonly Image[] slivers = new Image[SliverCount];
        private readonly Image halo;

        public DroneOxygenWarningFx(RectTransform panel, bool withHalo)
        {
            this.panel = panel;
            if (panel == null)
            {
                return;
            }

            if (withHalo)
            {
                halo = CreateImage("OxygenWarningHalo", RunFailureGlitchArt.GetHaloSprite());
                halo.type = Image.Type.Sliced;
                halo.fillCenter = false;
                // 테두리가 HaloPadding 폭으로 바깥에 번지게 한다.
                halo.pixelsPerUnitMultiplier = HaloSpriteBorder / HaloPadding;
                var rect = halo.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = new Vector2(-HaloPadding, -HaloPadding);
                rect.offsetMax = new Vector2(HaloPadding, HaloPadding);
            }

            for (var i = 0; i < SliverCount; i++)
            {
                slivers[i] = CreateImage("OxygenWarningEdgeGlitch" + i, RunFailureGlitchArt.GetGlitchBlockSprite());
            }

            Hide();
        }

        public bool HasHalo => halo != null;

        /// <summary>경고 시작 후 elapsed(실제 초) 시점의 가장자리 글리치·후광을 적용한다.</summary>
        public void Apply(float elapsed)
        {
            if (panel == null)
            {
                return;
            }

            var size = panel.rect.size;
            if (halo != null)
            {
                halo.transform.SetAsLastSibling();
                var color = AlertRed;
                color.a = HaloAlpha(elapsed);
                halo.color = color;
                SetActive(halo, true);
            }

            for (var i = 0; i < SliverCount; i++)
            {
                var image = slivers[i];
                if (image == null)
                {
                    continue;
                }

                var frame = Sliver(elapsed, i);
                var visible = frame.Alpha > 0.001f && size.y > 0f;
                SetActive(image, visible);
                if (!visible)
                {
                    continue;
                }

                image.transform.SetAsLastSibling();
                var rect = image.rectTransform;
                var side = frame.RightSide ? 1f : 0f;
                rect.anchorMin = new Vector2(side, frame.CenterY);
                rect.anchorMax = new Vector2(side, frame.CenterY);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(frame.Width, Mathf.Max(2f, frame.Height * size.y));
                rect.anchoredPosition = new Vector2(frame.OffsetX, 0f);
                var color = i % 3 == 2 ? AlertText : AlertRed;
                color.a = frame.Alpha;
                image.color = color;
            }
        }

        public void Hide()
        {
            SetActive(halo, false);
            for (var i = 0; i < SliverCount; i++)
            {
                SetActive(slivers[i], false);
            }
        }

        /// <summary>후광 깜빡임 알파. 0.25~1 사이를 HaloBlinkPeriod 주기로 오간다.</summary>
        public static float HaloAlpha(float elapsed)
        {
            var phase = Mathf.Repeat(Mathf.Max(0f, elapsed), HaloBlinkPeriod) / HaloBlinkPeriod;
            var pulse = 0.5f + 0.5f * Mathf.Cos(phase * Mathf.PI * 2f);
            return Mathf.Lerp(0.25f, 1f, pulse * pulse);
        }

        /// <summary>
        /// index번 조각의 모양. 짝수는 왼쪽, 홀수는 오른쪽 가장자리. JitterStep마다 위치·폭이 바뀌고
        /// 처음 StrongSeconds 동안 강하게, 그 뒤에는 약하게 흔들린다. 같은 시간이면 항상 같은 결과다.
        /// </summary>
        public static DroneOxygenEdgeSliver Sliver(float elapsed, int index)
        {
            var step = Mathf.FloorToInt(Mathf.Max(0f, elapsed) / JitterStep);
            var seed = (step * 131) + (index * 977) + 17;
            var strength = elapsed < StrongSeconds ? 1f : CalmStrength;
            var gate = Hash01(seed);
            var rightSide = index % 2 == 1;
            // 조각 일부는 이 스텝에 숨겨 깜빡이는 찢김처럼 보이게 한다.
            var alpha = gate < 0.3f ? 0f : (0.55f + 0.45f * Hash01(seed + 5)) * strength;
            var centerY = 0.12f + 0.76f * Hash01(seed + 11);
            var height = 0.06f + 0.16f * Hash01(seed + 23);
            var width = 8f + 14f * Hash01(seed + 31);
            var outward = rightSide ? 1f : -1f;
            var offset = outward * (1f + MaxSliverOffset * Hash01(seed + 47)) * strength;
            if (Hash01(seed + 53) < 0.3f)
            {
                // 가끔은 안쪽으로 파고든다(가장자리 폭 안에서만).
                offset = -offset * 0.5f;
            }

            return new DroneOxygenEdgeSliver(rightSide, centerY, height, width, offset, alpha);
        }

        public static float Hash01(int n)
        {
            unchecked
            {
                var x = (uint)n;
                x ^= x >> 16;
                x *= 0x7feb352d;
                x ^= x >> 15;
                x *= 0x846ca68b;
                x ^= x >> 16;
                return (x & 0xFFFFFF) / 16777215f;
            }
        }

        private Image CreateImage(string name, Sprite sprite)
        {
            var existing = panel.Find(name);
            Image image = existing != null ? existing.GetComponent<Image>() : null;
            if (image == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(Image));
                go.layer = panel.gameObject.layer;
                go.transform.SetParent(panel, false);
                image = go.GetComponent<Image>();
            }

            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
            return image;
        }

        private static void SetActive(Image image, bool active)
        {
            if (image != null && image.gameObject.activeSelf != active)
            {
                image.gameObject.SetActive(active);
            }
        }
    }
}
