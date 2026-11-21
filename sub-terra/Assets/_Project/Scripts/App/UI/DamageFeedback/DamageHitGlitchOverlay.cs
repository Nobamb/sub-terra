using SubTerra.App.UI.RunFailure;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.DamageFeedback
{
    /// <summary>
    /// B-207: 일반 피해 순간의 전체 화면 붉은 틴트·짧은 글리치 레이어. 런타임에 만든다.
    /// GraphicRaycaster가 없고 모든 그래픽이 raycastTarget=false라 이동·점프·채굴 입력을 막지 않는다.
    /// 사망 전환 그림(RunFailureGlitchArt)을 붉게 칠해 짧게만 쓴다.
    /// </summary>
    public sealed class DamageHitGlitchOverlay
    {
        /// <summary>HUD(600) 위, 드론 말풍선(700)·모달·사망 창(31,500) 아래. 치명타면 사망 연출이 위를 덮는다.</summary>
        public const int SortingOrder = 650;
        public const int BarCount = 5;
        /// <summary>틴트 대비 가장자리 비네트 알파 비율.</summary>
        public const float VignetteRatio = 0.9f;
        /// <summary>조각이 좌우로 밀리는 최대 화면 비율.</summary>
        public const float MaxBarShift = 0.05f;

        private readonly GameObject root;
        private readonly Image tintImage;
        private readonly Image vignetteImage;
        private readonly GameObject glitchRoot;
        private readonly Image scanlineImage;
        private readonly RectTransform[] bars = new RectTransform[BarCount];
        private readonly Image[] barImages = new Image[BarCount];
        private readonly DamageHitGlitchSettings settings;

        public GameObject Root => root;
        public float TintAlpha => tintImage != null && root != null && root.activeSelf ? tintImage.color.a : 0f;
        public bool GlitchVisible => root != null && root.activeSelf && glitchRoot != null && glitchRoot.activeSelf;

        public DamageHitGlitchOverlay(DamageHitGlitchSettings settings)
        {
            this.settings = settings ?? new DamageHitGlitchSettings();
            root = new GameObject("DamageHitGlitchOverlay", typeof(RectTransform));
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = SortingOrder;

            var tint = NewStretch("Tint", root.transform);
            tintImage = tint.gameObject.AddComponent<Image>();
            tintImage.raycastTarget = false;

            var vignette = NewStretch("EdgeVignette", root.transform);
            vignetteImage = vignette.gameObject.AddComponent<Image>();
            vignetteImage.sprite = RunFailureGlitchArt.GetVignetteSprite();
            vignetteImage.raycastTarget = false;

            var glitch = NewStretch("Glitch", root.transform);
            glitchRoot = glitch.gameObject;

            var scanline = NewStretch("Scanlines", glitch);
            scanlineImage = scanline.gameObject.AddComponent<Image>();
            scanlineImage.sprite = RunFailureGlitchArt.GetScanlineSprite();
            scanlineImage.type = Image.Type.Tiled;
            scanlineImage.raycastTarget = false;

            var barSprite = RunFailureGlitchArt.GetGlitchBlockSprite();
            for (var i = 0; i < BarCount; i++)
            {
                var bar = NewStretch("Slice" + i, glitch);
                var image = bar.gameObject.AddComponent<Image>();
                image.sprite = barSprite;
                image.raycastTarget = false;
                bars[i] = bar;
                barImages[i] = image;
            }

            Apply(DamageHitGlitchFrame.Inactive);
        }

        public void Apply(DamageHitGlitchFrame frame)
        {
            if (root == null)
            {
                return;
            }

            var visible = frame.IsActive && (frame.TintAlpha > 0.001f || frame.GlitchIntensity > 0.001f);
            if (root.activeSelf != visible)
            {
                root.SetActive(visible);
            }

            if (!visible)
            {
                return;
            }

            var tintAlpha = Mathf.Clamp01(frame.TintAlpha);
            if (tintImage != null)
            {
                tintImage.color = WithAlpha(settings.tintColor, tintAlpha);
            }

            if (vignetteImage != null)
            {
                vignetteImage.color = WithAlpha(settings.tintColor, tintAlpha * VignetteRatio);
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

        private void ApplyGlitch(DamageHitGlitchFrame frame)
        {
            var intensity = Mathf.Clamp01(frame.GlitchIntensity);
            var active = intensity > 0.001f;
            if (glitchRoot != null && glitchRoot.activeSelf != active)
            {
                glitchRoot.SetActive(active);
            }

            if (!active)
            {
                return;
            }

            var seed = frame.GlitchSeed != 0 ? frame.GlitchSeed : 2207;
            var primary = settings.glitchColor;
            var secondary = settings.secondaryColor;
            if (scanlineImage != null)
            {
                scanlineImage.color = WithAlpha(primary, 0.30f * intensity);
            }

            for (var i = 0; i < BarCount; i++)
            {
                var rect = bars[i];
                var image = barImages[i];
                if (rect == null || image == null)
                {
                    continue;
                }

                var h1 = RunFailureRevealTimeline.Hash01(seed, (i * 5) + 41);
                var h2 = RunFailureRevealTimeline.Hash01(seed, (i * 5) + 43);
                var h3 = RunFailureRevealTimeline.Hash01(seed, (i * 5) + 47);
                var h4 = RunFailureRevealTimeline.Hash01(seed, (i * 5) + 53);

                var yCenter = 0.08f + (0.84f * h1);
                var halfHeight = 0.004f + (0.03f * h2 * intensity);
                var shift = (h3 - 0.5f) * 2f * MaxBarShift * intensity / 0.55f;
                rect.anchorMin = new Vector2(shift, Mathf.Clamp01(yCenter - halfHeight));
                rect.anchorMax = new Vector2(1f + shift, Mathf.Clamp01(yCenter + halfHeight));
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                var color = h4 > 0.82f
                    ? Color.Lerp(primary, Color.white, 0.4f)
                    : (h4 < 0.35f ? secondary : primary);
                color.a = (0.35f + (0.55f * h2)) * intensity;
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
