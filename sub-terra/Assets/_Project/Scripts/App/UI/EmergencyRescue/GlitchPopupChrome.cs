using SubTerra.App.UI.HUD;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.EmergencyRescue
{
    /// <summary>
    /// 전력 고갈 구출 창의 프레임·글리치 장식(B-170에서 공용으로 분리). 딤 배경, MineResetTimedPopupSkin 프레임(윗부분 붉은 경고색),
    /// 청록·붉은 잔상, 어긋나는 띠, 번쩍임, 가로 조각을 짓고 글리치 프레임 값을 적용한다.
    /// 본문은 Content 아래에 각 창이 직접 짓는다. 구출 창과 종료 확인 창이 같이 쓴다.
    /// </summary>
    public sealed class GlitchPopupChrome
    {
        public const float BackdropAlpha = 0.78f;
        private const int BarCount = 12;
        private const int BandCount = 4;
        private const float EdgeSpill = 34f;

        public static readonly Color CardColor = new Color(0.02f, 0.035f, 0.05f, 0.98f);
        public static readonly Color BackdropColor = new Color(0.01f, 0.015f, 0.025f, 1f);

        private sealed class Band
        {
            public RectTransform Mask;
            public RectTransform Inner;
        }

        private readonly Vector2 size;
        private Image backdrop;
        private RectTransform card;
        private RectTransform visualRect;
        private CanvasGroup visualGroup;
        private RectTransform contentRect;
        private CanvasGroup contentGroup;
        private Image ghostTeal;
        private Image ghostRed;
        private Image flash;
        private Image[] bars;
        private Band[] bands;
        private int lastGeometryKey = int.MinValue;

        public GlitchPopupChrome(Vector2 cardSize)
        {
            size = cardSize;
        }

        public Vector2 Size => size;
        public RectTransform Card => card;
        public RectTransform Content => contentRect;
        public float ContentAlpha => contentGroup != null ? contentGroup.alpha : 0f;
        public float VisualAlpha => visualGroup != null ? visualGroup.alpha : 0f;
        public Vector2 VisualOffset => visualRect != null ? visualRect.anchoredPosition : Vector2.zero;

        public bool AnyGlitchPieceVisible
        {
            get
            {
                for (var i = 0; bars != null && i < bars.Length; i++)
                {
                    if (bars[i].enabled)
                    {
                        return true;
                    }
                }

                for (var i = 0; bands != null && i < bands.Length; i++)
                {
                    if (bands[i].Mask.gameObject.activeSelf)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>root(딤 Image가 붙은 오브젝트) 아래에 카드·장식·Content를 짓는다.</summary>
        public void Build(Transform root, Image backdropImage, MineResetTimedPopupSkin skin)
        {
            backdrop = backdropImage;
            backdrop.color = EmergencyRescueUi.WithAlpha(BackdropColor, 0f);
            backdrop.canvasRenderer.cullTransparentMesh = false;

            card = EmergencyRescueUi.Rect(root, "Card", Vector2.zero, size);

            visualRect = EmergencyRescueUi.Rect(card, "Visual", Vector2.zero, size);
            visualGroup = visualRect.gameObject.AddComponent<CanvasGroup>();
            visualGroup.interactable = false;
            visualGroup.blocksRaycasts = false;
            BuildLayers(visualRect, skin, true);

            // 프레임 윗부분만 붉은 보조색으로 칠한다(경고). 프레임 그림이 있을 때만.
            if (skin != null && skin.frame != null)
            {
                RectTransform redMask = EmergencyRescueUi.Rect(
                    visualRect, "FrameRedMask", new Vector2(0f, size.y * 0.5f - 55f), new Vector2(size.x, 110f));
                redMask.gameObject.AddComponent<RectMask2D>();
                EmergencyRescueUi.Image(
                    redMask, "FrameRed", skin.frame, new Vector2(0f, -(size.y * 0.5f - 55f)), size,
                    EmergencyRescueUi.WithAlpha(new Color(1f, 0.28f, 0.24f, 1f), 0.85f));
            }

            Sprite frameSprite = skin != null ? skin.frame : null;
            ghostTeal = EmergencyRescueUi.Image(
                card, "GhostTeal", frameSprite, Vector2.zero, size, EmergencyRescueUi.WithAlpha(EmergencyRescueUi.Teal, 0f));
            ghostRed = EmergencyRescueUi.Image(
                card, "GhostRed", frameSprite, Vector2.zero, size, EmergencyRescueUi.WithAlpha(EmergencyRescueUi.Red, 0f));

            ghostTeal.enabled = frameSprite != null;
            ghostRed.enabled = frameSprite != null;

            bands = new Band[BandCount];
            for (var i = 0; i < BandCount; i++)
            {
                RectTransform mask = EmergencyRescueUi.Rect(
                    card, "Band" + i, Vector2.zero, new Vector2(size.x + EdgeSpill * 2f, 30f));
                mask.gameObject.AddComponent<RectMask2D>();
                RectTransform inner = EmergencyRescueUi.Rect(mask, "Inner", Vector2.zero, size);
                BuildLayers(inner, skin, false);
                mask.gameObject.SetActive(false);
                bands[i] = new Band { Mask = mask, Inner = inner };
            }

            flash = EmergencyRescueUi.Image(
                card, "Flash", skin != null ? skin.frameGlow : null, Vector2.zero, size,
                EmergencyRescueUi.WithAlpha(EmergencyRescueUi.Teal, 0f));
            flash.enabled = skin != null && skin.frameGlow != null;

            bars = new Image[BarCount];
            for (var i = 0; i < BarCount; i++)
            {
                bars[i] = EmergencyRescueUi.Image(card, "Bar" + i, null, Vector2.zero, new Vector2(40f, 3f), EmergencyRescueUi.Teal);
                bars[i].enabled = false;
            }

            contentRect = EmergencyRescueUi.Rect(card, "Content", Vector2.zero, size);
            contentGroup = contentRect.gameObject.AddComponent<CanvasGroup>();
        }

        // 등장이 끝나면 모든 글리치 조각을 걷어 내고 본문·프레임을 선명한 정상 상태로 둔다.
        public void Settle()
        {
            Clear();
            visualGroup.alpha = 1f;
            contentGroup.alpha = 1f;
            visualRect.anchoredPosition = Vector2.zero;
            contentRect.anchoredPosition = Vector2.zero;
            backdrop.color = EmergencyRescueUi.WithAlpha(BackdropColor, BackdropAlpha);
        }

        /// <summary>창이 꺼질 때 다음 표시가 깨끗하게 시작되도록 되돌린다.</summary>
        public void ResetForHidden()
        {
            Clear();
            if (visualGroup != null)
            {
                visualGroup.alpha = 1f;
            }

            if (contentGroup != null)
            {
                contentGroup.alpha = 1f;
            }
        }

        public void Clear()
        {
            if (visualRect != null)
            {
                visualRect.anchoredPosition = Vector2.zero;
            }

            if (contentRect != null)
            {
                contentRect.anchoredPosition = Vector2.zero;
            }

            if (ghostTeal != null)
            {
                ghostTeal.color = EmergencyRescueUi.WithAlpha(ghostTeal.color, 0f);
                ghostRed.color = EmergencyRescueUi.WithAlpha(ghostRed.color, 0f);
                flash.color = EmergencyRescueUi.WithAlpha(flash.color, 0f);
            }

            for (var i = 0; bars != null && i < bars.Length; i++)
            {
                bars[i].enabled = false;
            }

            for (var i = 0; bands != null && i < bands.Length; i++)
            {
                if (bands[i].Mask.gameObject.activeSelf)
                {
                    bands[i].Mask.gameObject.SetActive(false);
                }
            }

            lastGeometryKey = int.MinValue;
        }

        /// <summary>다음 Apply가 같은 시드여도 배치를 다시 계산하게 한다.</summary>
        public void InvalidateGeometry()
        {
            lastGeometryKey = int.MinValue;
        }

        public void ApplyIdle(float time)
        {
            var intensity = EmergencyRescueTimeline.Idle(time, out var seed);
            Apply(new EmergencyRescueGlitchFrame(intensity, seed, seed % 3 == 0 ? 1 : 0, 1f, 1f, 1f), 0);
        }

        // mode 0 = 표시 중(가장자리 조각만), 1 = 등장, 2 = 종료.
        public void Apply(EmergencyRescueGlitchFrame f, int mode)
        {
            var intensity = f.Intensity;
            visualGroup.alpha = f.VisualAlpha;
            contentGroup.alpha = f.ContentAlpha;
            if (mode != 0)
            {
                backdrop.color = EmergencyRescueUi.WithAlpha(BackdropColor, BackdropAlpha * f.BackdropAlpha);
            }

            var key = mode * 1_000_000 + f.Seed * 64 + Mathf.RoundToInt(intensity * 20f);
            if (key == lastGeometryKey)
            {
                return;
            }

            lastGeometryKey = key;
            Color primary = f.Tint == 1 ? EmergencyRescueUi.Red : EmergencyRescueUi.Teal;
            Color other = f.Tint == 1 ? EmergencyRescueUi.Teal : EmergencyRescueUi.Red;
            var seed = f.Seed;

            // 프레임 어긋남: 장식 레이어만 가로로 밀리고 본문·버튼은 (종료 때만 약간) 흔들린다.
            if (mode == 0)
            {
                visualRect.anchoredPosition = Vector2.zero;
                contentRect.anchoredPosition = Vector2.zero;
            }
            else
            {
                var visualJitter = mode == 2 ? 16f : 11f;
                var contentJitter = mode == 2 ? 8f : 3f;
                visualRect.anchoredPosition = new Vector2((Hash(seed, 1) - 0.5f) * 2f * visualJitter * intensity, 0f);
                contentRect.anchoredPosition = new Vector2(
                    (Hash(seed, 2) - 0.5f) * 2f * contentJitter * intensity,
                    mode == 2 ? (Hash(seed, 3) - 0.5f) * 2f * 3f * intensity : 0f);
            }

            var ghostScale = mode == 0 ? 0.3f : 0.6f;
            var ghostShift = mode == 0 ? 3f + 4f * intensity : 5f + 10f * intensity;
            ghostTeal.rectTransform.anchoredPosition = new Vector2(-ghostShift, 0f);
            ghostRed.rectTransform.anchoredPosition = new Vector2(ghostShift, 0f);
            ghostTeal.color = EmergencyRescueUi.WithAlpha(EmergencyRescueUi.Teal, ghostScale * intensity);
            ghostRed.color = EmergencyRescueUi.WithAlpha(EmergencyRescueUi.Red, ghostScale * intensity);
            flash.color = EmergencyRescueUi.WithAlpha(primary, (mode == 0 ? 0.25f : 0.8f) * intensity);

            var maxBars = mode == 2 ? BarCount : mode == 1 ? 8 : 4;
            var barCount = intensity < 0.02f ? 0 : Mathf.Clamp(Mathf.CeilToInt(intensity * maxBars), 1, maxBars);
            for (var i = 0; i < bars.Length; i++)
            {
                if (i >= barCount)
                {
                    bars[i].enabled = false;
                    continue;
                }

                var width = mode == 0 ? 14f + Hash(seed, i * 5 + 11) * 56f : 50f + Hash(seed, i * 5 + 11) * 250f;
                var height = 2f + Hash(seed, i * 5 + 12) * 4f;
                var side = Hash(seed, i * 5 + 13) < 0.5f ? -1f : 1f;
                var yRange = size.y * 0.5f - 40f;
                var y = (Hash(seed, i * 5 + 14) - 0.5f) * 2f * yRange;
                float x;
                if (mode == 0)
                {
                    // 표시 중: 창 가장자리에 걸쳐 바깥으로 살짝 번지는 조각만.
                    x = side * (size.x * 0.5f + EdgeSpill * 0.8f - width * 0.5f);
                }
                else
                {
                    var xRange = size.x * 0.5f - width * 0.5f + EdgeSpill;
                    x = (Hash(seed, i * 5 + 15) - 0.5f) * 2f * xRange;
                }

                bars[i].rectTransform.anchoredPosition = new Vector2(x, y);
                bars[i].rectTransform.sizeDelta = new Vector2(width, height);
                Color color = Hash(seed, i * 5 + 16) < 0.65f ? primary : other;
                bars[i].color = EmergencyRescueUi.WithAlpha(color, (0.4f + 0.5f * Hash(seed, i * 5 + 17)) * Mathf.Min(1f, intensity * 1.3f));
                bars[i].enabled = true;
            }

            // 패널 일부가 가로로 어긋나 분절되는 띠. 표시 중에는 쓰지 않는다.
            var bandCount = mode == 0 || intensity < 0.15f
                ? 0
                : Mathf.Clamp(Mathf.RoundToInt(intensity * (mode == 2 ? BandCount : 2)), mode == 2 ? 2 : 1, mode == 2 ? BandCount : 2);
            for (var i = 0; i < bands.Length; i++)
            {
                Band band = bands[i];
                if (i >= bandCount)
                {
                    if (band.Mask.gameObject.activeSelf)
                    {
                        band.Mask.gameObject.SetActive(false);
                    }

                    continue;
                }

                var height = 16f + Hash(seed, i * 7 + 31) * (mode == 2 ? 56f : 34f);
                var y = (Hash(seed, i * 7 + 32) - 0.5f) * 2f * (size.y * 0.5f - 70f);
                var dx = (Hash(seed, i * 7 + 33) < 0.5f ? -1f : 1f)
                    * (8f + Hash(seed, i * 7 + 34) * (mode == 2 ? 34f : 22f)) * intensity;
                band.Mask.anchoredPosition = new Vector2(0f, y);
                band.Mask.sizeDelta = new Vector2(size.x + EdgeSpill * 2f, height);
                band.Inner.anchoredPosition = new Vector2(dx, -y);
                if (!band.Mask.gameObject.activeSelf)
                {
                    band.Mask.gameObject.SetActive(true);
                }
            }
        }

        private static float Hash(int seed, int salt)
        {
            return EmergencyRescueTimeline.Hash01(seed * 131 + salt * 17 + 5);
        }

        // 패널·프레임 레이어. 본체와 어긋나는 띠 복사본이 같은 구성을 쓴다.
        private void BuildLayers(RectTransform parent, MineResetTimedPopupSkin skin, bool decorations)
        {
            EmergencyRescueUi.Image(parent, "PanelBase", null, Vector2.zero, size, CardColor);
            if (skin != null && skin.panel != null)
            {
                EmergencyRescueUi.Image(parent, "Panel", skin.panel, Vector2.zero, size, CardColor);
            }

            if (decorations)
            {
                // 위쪽에 아주 옅은 붉은 번짐 하나. 경고 분위기용 보조색.
                EmergencyRescueUi.Image(
                    parent, "RedWash", EmergencyRescueArt.Soft(), new Vector2(0f, size.y * 0.5f - 112f),
                    new Vector2(size.x * 0.85f, 220f), EmergencyRescueUi.WithAlpha(EmergencyRescueUi.Red, 0.14f));
            }

            if (skin != null && skin.frame != null)
            {
                EmergencyRescueUi.Image(parent, "Frame", skin.frame, Vector2.zero, size, Color.white);
            }
            else if (decorations)
            {
                BuildFallbackFrame(parent);
            }
        }

        private void BuildFallbackFrame(RectTransform parent)
        {
            var w = size.x;
            var h = size.y;
            Color edge = EmergencyRescueUi.WithAlpha(EmergencyRescueUi.Teal, 0.8f);
            EmergencyRescueUi.Image(parent, "EdgeTop", null, new Vector2(0f, h * 0.5f - 2f), new Vector2(w, 4f), edge);
            EmergencyRescueUi.Image(parent, "EdgeBottom", null, new Vector2(0f, -h * 0.5f + 2f), new Vector2(w, 4f), edge);
            EmergencyRescueUi.Image(parent, "EdgeLeft", null, new Vector2(-w * 0.5f + 2f, 0f), new Vector2(4f, h), edge);
            EmergencyRescueUi.Image(parent, "EdgeRight", null, new Vector2(w * 0.5f - 2f, 0f), new Vector2(4f, h), edge);
        }
    }
}
