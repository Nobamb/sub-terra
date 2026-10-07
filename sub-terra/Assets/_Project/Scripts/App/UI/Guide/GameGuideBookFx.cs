using SubTerra.App.UI.HUD;
using SubTerra.App.UI.Sell;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.Guide
{
    /// <summary>
    /// 청록 홀로그램 책 레이어. 닫힌 책(표지) → 표지가 넘어가 펼친 책 → 페이지 넘김 → 좌우 페이지가 넓어지며
    /// 실제 금속 프레임 패널로 이어지는 하나의 연속 동작을 GuideFrame 값으로 그린다.
    /// 책과 창은 같은 중심·같은 외곽(ExpandRect)을 공유하고, 패널 프레임은 ExpandRect의 자식이라 함께 커진다.
    /// 글자·썸네일 같은 실제 내용은 이 레이어 위에 별도로 놓이므로 늘어나거나 찌그러지지 않는다.
    /// </summary>
    internal sealed class GameGuideBookFx
    {
        public static readonly Vector2 BookSize = new Vector2(330f, 210f);

        private static readonly Color PageColor = new Color(0.03f, 0.17f, 0.22f, 1f);
        private static readonly Color CoverColor = new Color(0.02f, 0.28f, 0.36f, 1f);
        private static readonly Color LineColor = new Color(0.55f, 0.95f, 1f, 1f);

        private sealed class Page
        {
            public RectTransform Rect;
            public Image Face;
            public Image Outline;
            public CanvasGroup LineGroup;
            public CanvasGroup Emblem;
            public Image SpineBand;
        }

        private RectTransform root;
        private CanvasGroup rootGroup;
        private RectTransform expand;
        private RectTransform shell;
        private CanvasGroup shellGroup;
        private Image glow;
        private Image outlineWhole;
        private Image spine;
        private Page right;
        private Page cover;
        private Image stackA;
        private Image stackB;
        private Page[] flips;
        private Image[] comets;
        private Vector2 cardSize;

        public RectTransform Root => root;
        public RectTransform Expand => expand;
        public RectTransform Shell => shell;
        public Vector2 CurrentSize { get; private set; }
        public float CoverScaleX { get; private set; } = 1f;
        public bool CometsVisible { get; private set; }
        public int VisibleFlipCount { get; private set; }

        public static GameGuideBookFx Build(Transform parent, Vector2 cardSize, MineResetTimedPopupSkin skin)
        {
            var fx = new GameGuideBookFx { cardSize = cardSize };
            fx.root = ResourceSellUi.Centered(parent, "BookRoot", Vector2.zero, cardSize);
            fx.rootGroup = fx.root.gameObject.AddComponent<CanvasGroup>();
            fx.rootGroup.interactable = false;
            fx.rootGroup.blocksRaycasts = false;
            fx.expand = ResourceSellUi.Centered(fx.root, "ExpandRect", Vector2.zero, BookSize);

            var glowImage = ResourceSellUi.Image(fx.expand, "Glow", ResourceSellArt.SoftRect(), Color.clear);
            glowImage.rectTransform.offsetMin = new Vector2(-30f, -30f);
            glowImage.rectTransform.offsetMax = new Vector2(30f, 30f);
            fx.glow = glowImage;

            fx.shell = ResourceSellUi.Image(fx.expand, "ShellMask", null, Color.clear).rectTransform;
            fx.shell.gameObject.AddComponent<RectMask2D>();
            fx.shellGroup = fx.shell.gameObject.AddComponent<CanvasGroup>();
            var inner = ResourceSellUi.Image(fx.shell, "Inner", null, Color.clear).rectTransform;
            ResourceSellUi.Stretch(inner);
            ResourceSellPopupView.BuildPanelLayers(inner, skin, cardSize);

            fx.right = BuildPage(fx.expand, "PageRight", false, 1);
            fx.stackA = BuildStack(fx.expand, "StackA", 5f);
            fx.stackB = BuildStack(fx.expand, "StackB", 10f);
            fx.cover = BuildPage(fx.expand, "Cover", true, 2);
            fx.flips = new[]
            {
                BuildPage(fx.expand, "Flip0", false, 3),
                BuildPage(fx.expand, "Flip1", false, 4),
                BuildPage(fx.expand, "Flip2", false, 5)
            };

            var spineRect = ResourceSellUi.Centered(fx.expand, "Spine", Vector2.zero, new Vector2(3f, 0f));
            spineRect.anchorMin = new Vector2(0.5f, 0.04f);
            spineRect.anchorMax = new Vector2(0.5f, 0.96f);
            spineRect.sizeDelta = new Vector2(3f, 0f);
            fx.spine = ResourceSellUi.AddImage(spineRect, null, ResourceSellUi.WithAlpha(LineColor, 0.8f));

            var outlineRect = ResourceSellUi.Image(fx.expand, "OutlineWhole", ResourceSellArt.ChamferOutline(), Color.clear);
            outlineRect.type = Image.Type.Sliced;
            fx.outlineWhole = outlineRect;

            fx.comets = new Image[2];
            for (var i = 0; i < fx.comets.Length; i++)
            {
                var rect = ResourceSellUi.Centered(fx.expand, "Comet" + i, Vector2.zero, new Vector2(48f, 8f));
                fx.comets[i] = ResourceSellUi.AddImage(rect, ResourceSellArt.SoftRect(), Color.clear);
            }

            return fx;
        }

        private static Image BuildStack(RectTransform parent, string name, float reach)
        {
            var image = ResourceSellUi.Image(parent, name, ResourceSellArt.ChamferOutline(), Color.clear);
            var rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = new Vector2(0f, 8f + reach * 0.4f);
            rect.offsetMax = new Vector2(-4f + reach, -8f - reach * 0.4f);
            return image;
        }

        private static Page BuildPage(RectTransform parent, string name, bool isCover, int seed)
        {
            var rect = ResourceSellUi.Centered(parent, name, Vector2.zero, Vector2.zero);
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.offsetMin = new Vector2(0f, 4f);
            rect.offsetMax = new Vector2(-4f, -4f);
            var page = new Page { Rect = rect };
            page.Face = ResourceSellUi.AddImage(rect, ResourceSellArt.ChamferFill(), PageColor);
            var outline = ResourceSellUi.Image(rect, "Outline", ResourceSellArt.ChamferOutline(), ResourceSellUi.Teal);
            page.Outline = outline;

            var lines = ResourceSellUi.Image(rect, "Lines", null, Color.clear).rectTransform;
            page.LineGroup = lines.gameObject.AddComponent<CanvasGroup>();
            for (var i = 0; i < 4; i++)
            {
                var bar = ResourceSellUi.Image(lines, "Bar" + i, null, ResourceSellUi.WithAlpha(LineColor, 0.28f)).rectTransform;
                var length = 0.42f + 0.12f * ((i * 7 + seed * 3) % 4);
                bar.anchorMin = new Vector2(0.14f, 0.74f - i * 0.115f);
                bar.anchorMax = new Vector2(Mathf.Min(0.86f, 0.14f + length), 0.74f - i * 0.115f);
                bar.offsetMin = new Vector2(0f, -1.5f);
                bar.offsetMax = new Vector2(0f, 1.5f);
            }

            var mark = ResourceSellUi.Image(lines, "Mark", GameGuideArt.Diamond(), ResourceSellUi.WithAlpha(LineColor, 0.4f)).rectTransform;
            mark.anchorMin = new Vector2(0.62f, 0.12f);
            mark.anchorMax = new Vector2(0.62f, 0.12f);
            mark.sizeDelta = new Vector2(14f, 14f);
            var square = ResourceSellUi.Image(lines, "Box", null, ResourceSellUi.WithAlpha(LineColor, 0.18f)).rectTransform;
            square.anchorMin = new Vector2(0.14f, 0.1f);
            square.anchorMax = new Vector2(0.14f, 0.1f);
            square.sizeDelta = new Vector2(34f, 14f);

            if (isCover)
            {
                // 닫힌 책 표지: 왼쪽 책등 띠 + 가운데 홀로그램 문양. 글자는 없다.
                var band = ResourceSellUi.Image(rect, "SpineBand", null, ResourceSellUi.WithAlpha(ResourceSellUi.TealDeep, 0.55f));
                band.rectTransform.anchorMin = new Vector2(0f, 0.04f);
                band.rectTransform.anchorMax = new Vector2(0.09f, 0.96f);
                band.rectTransform.offsetMin = band.rectTransform.offsetMax = Vector2.zero;
                page.SpineBand = band;

                var emblem = ResourceSellUi.Image(rect, "Emblem", null, Color.clear).rectTransform;
                page.Emblem = emblem.gameObject.AddComponent<CanvasGroup>();
                var ring = ResourceSellUi.Image(emblem, "Ring", GameGuideArt.Ring(), ResourceSellUi.WithAlpha(LineColor, 0.9f)).rectTransform;
                ring.anchorMin = ring.anchorMax = new Vector2(0.55f, 0.56f);
                ring.sizeDelta = new Vector2(70f, 70f);
                var core = ResourceSellUi.Image(emblem, "Core", GameGuideArt.Diamond(), ResourceSellUi.WithAlpha(Color.white, 0.95f)).rectTransform;
                core.anchorMin = core.anchorMax = new Vector2(0.55f, 0.56f);
                core.sizeDelta = new Vector2(24f, 24f);
                for (var i = 0; i < 2; i++)
                {
                    var bar = ResourceSellUi.Image(emblem, "Bar" + i, null, ResourceSellUi.WithAlpha(LineColor, 0.55f)).rectTransform;
                    bar.anchorMin = bar.anchorMax = new Vector2(0.55f, 0.2f - i * 0.07f);
                    bar.sizeDelta = new Vector2(i == 0 ? 78f : 50f, 3f);
                }
            }

            return page;
        }

        /// <summary>한 프레임을 적용한다. clock은 가장자리 빛 이동에만 쓰인다.</summary>
        public void Apply(GuideFrame f, float clock)
        {
            root.anchoredPosition = new Vector2(0f, f.BookY);
            root.localScale = new Vector3(f.BookScale, f.BookScale, 1f);
            rootGroup.alpha = f.BookAlpha;

            var size = Vector2.Lerp(BookSize, cardSize, f.Expand);
            expand.sizeDelta = size;
            CurrentSize = size;
            // 닫힌 책은 오른쪽 절반만 있으므로 가운데로 당겨 화면 중심에 놓는다.
            expand.anchoredPosition = new Vector2(-size.x * 0.25f * (1f - f.CoverOpen), 0f);

            var teal = ResourceSellUi.Teal;
            glow.color = ResourceSellUi.WithAlpha(teal, 0.5f * f.Glow * (1f - 0.6f * f.PanelAlpha));
            shellGroup.alpha = f.PanelAlpha;

            var pageAlpha = f.PageAlpha;
            var lineFade = Mathf.Clamp01(1f - f.Expand * 4f);
            ApplyPage(right, pageAlpha, lineFade, 1f);

            // 표지: 0→0.5는 표지가 세워지며 좁아지고, 0.5→1은 뒷면(왼쪽 페이지)이 넓어진다.
            var cos = Mathf.Cos(Mathf.PI * f.CoverOpen);
            CoverScaleX = cos;
            cover.Rect.localScale = new Vector3(Mathf.Abs(cos) < 0.001f ? 0.001f : cos, 1f, 1f);
            var isCover = f.CoverOpen < 0.5f;
            cover.Face.color = WithAlpha(isCover ? CoverColor : PageColor, pageAlpha);
            cover.Outline.color = WithAlpha(teal, pageAlpha);
            cover.LineGroup.alpha = (isCover ? 0f : 1f) * lineFade * pageAlpha;
            cover.Emblem.alpha = (isCover ? 1f : 0f) * pageAlpha;
            cover.SpineBand.color = WithAlpha(ResourceSellUi.WithAlpha(ResourceSellUi.TealDeep, 0.55f), isCover ? pageAlpha : 0f);

            // 닫혀 있을 때 오른쪽에 살짝 비치는 책장 두께.
            var thickness = Mathf.Clamp01(1f - f.CoverOpen * 2.5f) * pageAlpha;
            stackA.color = WithAlpha(ResourceSellUi.WithAlpha(teal, 0.5f), thickness);
            stackB.color = WithAlpha(ResourceSellUi.WithAlpha(teal, 0.3f), thickness);

            cover.Rect.gameObject.SetActive(f.BookAlpha > 0.001f);

            // 글로우는 책이 실제로 차지한 쪽(닫힌 책은 오른쪽 절반, 표지가 넘어가면 왼쪽으로 확장)에만 둔다.
            glow.rectTransform.anchorMin = new Vector2(0.5f + 0.5f * Mathf.Min(0f, cos), 0f);

            VisibleFlipCount = 0;
            var values = new[] { f.Flip0, f.Flip1, f.Flip2 };
            for (var i = 0; i < flips.Length; i++)
            {
                var v = values[i];
                var visible = v > 0.001f && v < 0.999f;
                flips[i].Rect.gameObject.SetActive(visible);
                if (!visible)
                {
                    continue;
                }

                VisibleFlipCount++;
                var flipCos = Mathf.Cos(Mathf.PI * v);
                flips[i].Rect.localScale = new Vector3(Mathf.Abs(flipCos) < 0.001f ? 0.001f : flipCos, 1f, 1f);
                var edge = 1f - Mathf.Abs(flipCos);
                flips[i].Face.color = WithAlpha(Color.Lerp(PageColor, CoverColor, 0.5f + 0.3f * edge), pageAlpha);
                flips[i].Outline.color = WithAlpha(Color.Lerp(teal, Color.white, edge * 0.6f), pageAlpha);
                flips[i].LineGroup.alpha = 0.9f * lineFade * pageAlpha;
            }

            // 책 안쪽 접히는 선은 펼쳐진 뒤에만 보인다.
            spine.color = ResourceSellUi.WithAlpha(LineColor, 0.8f * f.CoverOpen * pageAlpha);

            var outlineAlpha = Mathf.Clamp01(f.Expand * 6f) * (1f - f.PanelAlpha) * (0.45f + 0.55f * f.Glow);
            outlineWhole.color = ResourceSellUi.WithAlpha(teal, outlineAlpha);
            ApplyComets(f, clock, size);
        }

        private void ApplyComets(GuideFrame f, float clock, Vector2 size)
        {
            var strength = f.Glow * Mathf.Clamp01(f.CoverOpen * 2f) * (1f - f.PanelAlpha);
            CometsVisible = strength > 0.02f;
            var halfW = size.x * 0.5f - 2f;
            var halfH = size.y * 0.5f - 2f;
            var perimeter = 4f * (halfW + halfH);
            for (var i = 0; i < comets.Length; i++)
            {
                comets[i].enabled = CometsVisible;
                if (!CometsVisible)
                {
                    continue;
                }

                var p = Mathf.Repeat(clock * 0.9f + i * 0.5f, 1f) * perimeter;
                Vector2 pos;
                bool horizontal;
                if (p < 2f * halfW)
                {
                    pos = new Vector2(-halfW + p, halfH);
                    horizontal = true;
                }
                else if (p < 2f * halfW + 2f * halfH)
                {
                    pos = new Vector2(halfW, halfH - (p - 2f * halfW));
                    horizontal = false;
                }
                else if (p < 4f * halfW + 2f * halfH)
                {
                    pos = new Vector2(halfW - (p - 2f * halfW - 2f * halfH), -halfH);
                    horizontal = true;
                }
                else
                {
                    pos = new Vector2(-halfW, -halfH + (p - 4f * halfW - 2f * halfH));
                    horizontal = false;
                }

                var rect = comets[i].rectTransform;
                rect.anchoredPosition = pos;
                rect.sizeDelta = horizontal ? new Vector2(54f, 9f) : new Vector2(9f, 54f);
                comets[i].color = ResourceSellUi.WithAlpha(Color.Lerp(ResourceSellUi.Teal, Color.white, 0.5f), strength);
            }
        }

        private static void ApplyPage(Page page, float alpha, float lineFade, float scaleX)
        {
            page.Rect.localScale = new Vector3(scaleX, 1f, 1f);
            page.Face.color = WithAlpha(PageColor, alpha);
            page.Outline.color = WithAlpha(ResourceSellUi.Teal, alpha);
            page.LineGroup.alpha = lineFade * alpha;
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a *= alpha;
            return color;
        }
    }
}
