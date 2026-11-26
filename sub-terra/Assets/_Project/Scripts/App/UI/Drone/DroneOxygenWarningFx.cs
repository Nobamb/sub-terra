using SubTerra.App.Core.Data;
using SubTerra.App.UI.RunFailure;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.Drone
{
    /// <summary>가장자리 글리치 조각 한 개의 한 시점 모양. 크기·오프셋은 px, 세로 위치는 쓸 수 있는 높이 기준 0~1.</summary>
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
    /// B-213 산소 부족 경고 연출. 말풍선·터미널 패널 좌우 가장자리 밖으로 가로로 긴 빨간 글리치 조각을 흔들고,
    /// 패널 배경(바깥 틀)만 가로 띠로 찢어 살짝 왜곡한다(글자는 그대로). 터미널에는 둘레 빨간 후광을 깜빡인다.
    /// topExclusion(px) 위쪽(터미널 X 버튼 헤더)에는 글리치·왜곡을 넣지 않는다. 화면 전체·카메라는 건드리지 않는다.
    /// 시간 → 모양 계산은 정적 함수라 EditMode에서 그대로 검사한다.
    /// </summary>
    public sealed class DroneOxygenWarningFx
    {
        public const string TemplateId = DataIds.Dialogue.DroneOxygenWarning;
        public const string DefaultText = "산소가 부족합니다. 위로 올라가거나 긴급 탈출(구출 요청)을 쓰세요.";
        public const int SliverCount = 8;
        public const float JitterStep = 0.07f;
        public const float HaloBlinkPeriod = 0.55f;
        public const float StrongSeconds = 1.2f;
        public const float CalmStrength = 0.7f;
        public const float HaloPadding = 16f;
        public const float SliverMinWidth = 34f;
        public const float SliverMaxWidth = 90f;
        public const float SliverMinHeight = 4f;
        public const float SliverMaxHeight = 12f;
        /// <summary>터미널 헤더(높이 36px, X 버튼) 위로는 글리치·왜곡을 넣지 않는다. 여유 4px 포함.</summary>
        public const float TerminalHeaderExclusion = 40f;

        public static readonly Color AlertRed = new Color(1f, 0.2f, 0.18f, 1f);
        public static readonly Color AlertText = new Color(1f, 0.66f, 0.62f, 1f);
        public static readonly Color AlertHeader = new Color(0.24f, 0.03f, 0.04f, 0.99f);

        // 원본 후광 스프라이트 9-slice 테두리 픽셀(RunFailureGlitchArt.HaloBorder).
        private const float HaloSpriteBorder = 24f;

        private readonly RectTransform panel;
        private readonly float topExclusion;
        private readonly Image[] slivers = new Image[SliverCount];
        private readonly Image halo;
        private readonly DroneOxygenEdgeWarp warp;
        private readonly DroneOxygenEdgeWarp haloWarp;

        public DroneOxygenWarningFx(RectTransform panel, bool withHalo, float topExclusion = 0f)
        {
            this.panel = panel;
            this.topExclusion = Mathf.Max(0f, topExclusion);
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
                // 후광의 안쪽 빨간 선이 곧은 채로 찢긴 틀을 덮지 않게, 후광 좌우 기둥도 패널과 같은 띠로 민다.
                haloWarp = halo.GetComponent<DroneOxygenEdgeWarp>();
                if (haloWarp == null)
                {
                    haloWarp = halo.gameObject.AddComponent<DroneOxygenEdgeWarp>();
                }

                haloWarp.Configure(this.topExclusion, panel);
            }

            for (var i = 0; i < SliverCount; i++)
            {
                slivers[i] = CreateImage("OxygenWarningEdgeGlitch" + i, RunFailureGlitchArt.GetGlitchBlockSprite());
            }

            // 패널 배경(틀)에만 왜곡을 건다. 외곽선·그림자 뒤에 붙어 그 복사본까지 같이 찢긴다.
            if (panel.TryGetComponent<Image>(out var background))
            {
                warp = panel.GetComponent<DroneOxygenEdgeWarp>();
                if (warp == null)
                {
                    warp = panel.gameObject.AddComponent<DroneOxygenEdgeWarp>();
                }

                warp.Configure(this.topExclusion);
            }

            Hide();
        }

        public bool HasHalo => halo != null;
        public bool HasWarp => warp != null;

        /// <summary>경고 시작 후 elapsed(실제 초) 시점의 가장자리 글리치·왜곡·후광을 적용한다.</summary>
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

            if (warp != null)
            {
                warp.SetFrame(elapsed);
            }

            if (haloWarp != null)
            {
                haloWarp.SetFrame(elapsed);
            }

            var usable = Mathf.Max(0f, size.y - topExclusion);
            for (var i = 0; i < SliverCount; i++)
            {
                var image = slivers[i];
                if (image == null)
                {
                    continue;
                }

                var frame = Sliver(elapsed, i);
                var visible = frame.Alpha > 0.001f && usable > SliverMaxHeight;
                SetActive(image, visible);
                if (!visible)
                {
                    continue;
                }

                image.transform.SetAsLastSibling();
                var rect = image.rectTransform;
                var side = frame.RightSide ? 1f : 0f;
                rect.anchorMin = new Vector2(side, 0f);
                rect.anchorMax = new Vector2(side, 0f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(frame.Width, frame.Height);
                rect.anchoredPosition = new Vector2(frame.OffsetX, frame.CenterY * usable);
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

            if (warp != null)
            {
                warp.Clear();
            }

            if (haloWarp != null)
            {
                haloWarp.Clear();
            }
        }

        /// <summary>후광 깜빡임 알파. 0.25~1 사이를 HaloBlinkPeriod 주기로 오간다.</summary>
        public static float HaloAlpha(float elapsed)
        {
            var phase = Mathf.Repeat(Mathf.Max(0f, elapsed), HaloBlinkPeriod) / HaloBlinkPeriod;
            var pulse = 0.5f + 0.5f * Mathf.Cos(phase * Mathf.PI * 2f);
            return Mathf.Lerp(0.25f, 1f, pulse * pulse);
        }

        public static float Strength(float elapsed) => elapsed < StrongSeconds ? 1f : CalmStrength;

        /// <summary>
        /// index번 조각의 모양. 짝수는 왼쪽, 홀수는 오른쪽 가장자리. 가로로 길고 세로로 얇으며,
        /// 대부분(65~95%)이 패널 바깥으로 빠져나온다. JitterStep마다 바뀌고 같은 시간이면 항상 같은 결과다.
        /// </summary>
        public static DroneOxygenEdgeSliver Sliver(float elapsed, int index)
        {
            var step = StepOf(elapsed);
            var seed = (step * 131) + (index * 977) + 17;
            var strength = Strength(elapsed);
            var gate = Hash01(seed);
            var rightSide = index % 2 == 1;
            // 조각 일부는 이 스텝에 숨겨 깜빡이는 찢김처럼 보이게 한다.
            var alpha = gate < 0.25f ? 0f : (0.7f + 0.3f * Hash01(seed + 5)) * strength;
            var centerY = 0.06f + 0.88f * Hash01(seed + 11);
            var height = Mathf.Lerp(SliverMinHeight, SliverMaxHeight, Hash01(seed + 23));
            var width = Mathf.Lerp(SliverMinWidth, SliverMaxWidth, Hash01(seed + 31)) * Mathf.Lerp(0.75f, 1f, strength);
            var outward = rightSide ? 1f : -1f;
            // 중심을 가장자리 바깥으로 폭의 15~45%만큼 민다 → 조각의 65~95%가 패널 밖.
            var offset = outward * width * (0.15f + 0.3f * Hash01(seed + 47));
            return new DroneOxygenEdgeSliver(rightSide, centerY, height, width, offset, alpha);
        }

        /// <summary>
        /// 패널 배경을 stripCount개 가로 띠로 나눴을 때 strip번 띠(아래부터 0)의 가로 밀림(px).
        /// 스텝마다 2~4개 띠 묶음(각 1~3띠)만 밀리고 나머지는 0. excluded(헤더 영역)면 항상 0.
        /// </summary>
        public static float StripOffset(float elapsed, int strip, int stripCount, bool excluded)
        {
            if (excluded || stripCount <= 0 || strip < 0 || strip >= stripCount)
            {
                return 0f;
            }

            var step = StepOf(elapsed);
            var strength = Strength(elapsed);
            var bands = 2 + Mathf.FloorToInt(Hash01(step * 59 + 3) * 2.999f);
            for (var b = 0; b < bands; b++)
            {
                var seed = (step * 211) + (b * 389) + 29;
                if (Hash01(seed) < 0.1f)
                {
                    continue;
                }

                var start = Mathf.FloorToInt(Hash01(seed + 7) * stripCount);
                var length = 1 + Mathf.FloorToInt(Hash01(seed + 13) * 3f);
                if (strip < start || strip >= start + length)
                {
                    continue;
                }

                var sign = Hash01(seed + 19) < 0.5f ? -1f : 1f;
                return sign * Mathf.Lerp(DroneOxygenEdgeWarp.MinShift, DroneOxygenEdgeWarp.MaxShift, Hash01(seed + 23)) * strength;
            }

            return 0f;
        }

        public static int StepOf(float elapsed) => Mathf.FloorToInt(Mathf.Max(0f, elapsed) / JitterStep);

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
