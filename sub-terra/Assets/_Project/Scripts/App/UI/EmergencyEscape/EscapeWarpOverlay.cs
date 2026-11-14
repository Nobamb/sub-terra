using SubTerra.App.UI.RunFailure;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.EmergencyEscape
{
    /// <summary>
    /// B-195: 긴급 탈출 워프의 전체 화면 암전·간단 글리치 레이어. 런타임에 만들고 끝나면 지운다.
    /// 암전 이미지는 클릭을 막아 연출 중 월드 입력이 새지 않게 한다. 사망 전환(RunFailurePanelView)의 그림만 재사용한다.
    /// </summary>
    public sealed class EscapeWarpOverlay
    {
        /// <summary>다른 팝업보다 앞, 설정 창(32,760)보다는 뒤.</summary>
        public const int SortingOrder = 32_650;
        public const int BarCount = 6;

        private readonly GameObject root;
        private readonly Image blackImage;
        private readonly GameObject glitchRoot;
        private readonly Image scanlineImage;
        private readonly Image flashImage;
        private readonly RectTransform[] bars = new RectTransform[BarCount];
        private readonly Image[] barImages = new Image[BarCount];
        private readonly EscapeWarpSettings settings;

        public GameObject Root => root;
        public float BlackoutAlpha => blackImage != null ? blackImage.color.a : 0f;
        public bool GlitchVisible => glitchRoot != null && glitchRoot.activeSelf;

        public EscapeWarpOverlay(EscapeWarpSettings settings)
        {
            this.settings = settings ?? new EscapeWarpSettings();
            root = new GameObject("EscapeWarpOverlay", typeof(RectTransform));
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = SortingOrder;
            root.AddComponent<GraphicRaycaster>();

            var black = NewStretch("Black", root.transform);
            blackImage = black.gameObject.AddComponent<Image>();
            blackImage.color = new Color(0f, 0f, 0f, 0f);
            blackImage.raycastTarget = true;

            var glitch = NewStretch("Glitch", root.transform);
            glitchRoot = glitch.gameObject;

            var scanline = NewStretch("Scanlines", glitch);
            scanlineImage = scanline.gameObject.AddComponent<Image>();
            scanlineImage.sprite = RunFailureGlitchArt.GetScanlineSprite();
            scanlineImage.type = Image.Type.Tiled;
            scanlineImage.raycastTarget = false;

            var flash = NewStretch("Flash", glitch);
            flash.offsetMin = new Vector2(-80f, -80f);
            flash.offsetMax = new Vector2(80f, 80f);
            flashImage = flash.gameObject.AddComponent<Image>();
            flashImage.sprite = RunFailureGlitchArt.GetHaloSprite();
            flashImage.raycastTarget = false;

            var barSprite = RunFailureGlitchArt.GetGlitchBlockSprite();
            for (var i = 0; i < BarCount; i++)
            {
                var bar = NewStretch("Bar" + i, glitch);
                var image = bar.gameObject.AddComponent<Image>();
                image.sprite = barSprite;
                image.raycastTarget = false;
                bars[i] = bar;
                barImages[i] = image;
            }

            glitchRoot.SetActive(false);
        }

        public void Apply(EscapeWarpFrame frame)
        {
            if (blackImage != null)
            {
                blackImage.color = new Color(0f, 0f, 0f, Mathf.Clamp01(frame.BlackoutAlpha));
            }

            ApplyGlitch(frame);
        }

        public void Destroy()
        {
            if (root == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(root);
            }
            else
            {
                Object.DestroyImmediate(root);
            }
        }

        private void ApplyGlitch(EscapeWarpFrame frame)
        {
            var intensity = Mathf.Clamp01(frame.GlitchIntensity);
            var active = intensity > 0.001f;
            if (glitchRoot != null)
            {
                glitchRoot.SetActive(active);
            }

            if (!active)
            {
                return;
            }

            var seed = frame.GlitchSeed != 0 ? frame.GlitchSeed : 1301;
            var primary = settings.glitchColor;
            var secondary = settings.secondaryColor;
            if (scanlineImage != null)
            {
                scanlineImage.color = WithAlpha(primary, 0.22f * intensity);
            }

            if (flashImage != null)
            {
                var roll = RunFailureRevealTimeline.Hash01(seed, 7);
                flashImage.color = WithAlpha(primary, (roll > 0.52f ? 0.30f : 0.10f) * intensity);
            }

            for (var i = 0; i < BarCount; i++)
            {
                var rect = bars[i];
                var image = barImages[i];
                if (rect == null || image == null)
                {
                    continue;
                }

                var h1 = RunFailureRevealTimeline.Hash01(seed, (i * 6) + 19);
                var h2 = RunFailureRevealTimeline.Hash01(seed, (i * 6) + 23);
                var h3 = RunFailureRevealTimeline.Hash01(seed, (i * 6) + 29);
                var h4 = RunFailureRevealTimeline.Hash01(seed, (i * 6) + 31);
                var h5 = RunFailureRevealTimeline.Hash01(seed, (i * 6) + 37);

                var yCenter = 0.06f + (0.88f * h1);
                var halfHeight = 0.003f + (0.022f * h2 * intensity);
                rect.anchorMin = new Vector2(0f, Mathf.Clamp01(yCenter - halfHeight));
                rect.anchorMax = new Vector2(1f, Mathf.Clamp01(yCenter + halfHeight));
                rect.offsetMin = new Vector2((h3 - 0.5f) * 120f, 0f);
                rect.offsetMax = new Vector2((h4 - 0.5f) * 120f, 0f);

                var color = h5 > 0.8f
                    ? Color.Lerp(primary, Color.white, 0.55f)
                    : (h5 < 0.3f ? secondary : primary);
                color.a = (0.28f + (0.5f * h2)) * intensity;
                image.color = color;
            }
        }

        private static RectTransform NewStretch(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
