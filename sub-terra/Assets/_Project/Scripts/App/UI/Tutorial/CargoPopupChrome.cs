using SubTerra.App.UI.HUD;
using SubTerra.App.UI.Sell;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.Tutorial
{
    /// <summary>
    /// 화물 부족(B-152)·화물 버리기(B-153) 창이 함께 쓰는 껍데기. 판매창과 같은 철제 프레임 위에
    /// 배경 막, 내용 층, 스캔선을 만들고 열기·접기·페이지 전환 자세를 입힌다.
    /// </summary>
    internal sealed class CargoPopupFrame
    {
        private Vector2 size;
        private CargoFullPopupSkin skin;

        public Image Backdrop { get; private set; }
        public RectTransform ShellMask { get; private set; }
        public CanvasGroup ShellGroup { get; private set; }
        public Image FrameGlow { get; private set; }
        public Image ScanLine { get; private set; }
        public RectTransform ContentRoot { get; private set; }
        public CanvasGroup ContentGroup { get; private set; }
        public RectMask2D ContentMask { get; private set; }

        /// <summary>owner(패널) 크기를 맞추고 배경 막(0) · 껍데기(1) · 내용(2) · 스캔선(마지막)을 만든다.</summary>
        public static CargoPopupFrame Build(
            RectTransform owner, string prefix, Vector2 size, CargoFullPopupSkin skin, MineResetTimedPopupSkin frameSkin)
        {
            var frame = new CargoPopupFrame { size = size, skin = skin };
            owner.anchorMin = owner.anchorMax = new Vector2(0.5f, 0.5f);
            owner.pivot = new Vector2(0.5f, 0.5f);
            owner.anchoredPosition = Vector2.zero;
            owner.sizeDelta = size;
            var panelImage = owner.GetComponent<Image>();
            if (panelImage != null)
            {
                // 패널 영역 클릭이 뒤 화면으로 새지 않게 투명하게 남겨 둔다.
                panelImage.sprite = null;
                panelImage.color = Color.clear;
                panelImage.raycastTarget = true;
            }

            frame.Backdrop = ResourceSellUi.Image(owner, prefix + "Backdrop", null, Color.clear);
            frame.Backdrop.rectTransform.offsetMin = new Vector2(-4000f, -4000f);
            frame.Backdrop.rectTransform.offsetMax = new Vector2(4000f, 4000f);
            frame.Backdrop.transform.SetAsFirstSibling();

            frame.ShellMask = ResourceSellUi.Centered(owner, prefix + "Shell", Vector2.zero, size);
            frame.ShellMask.gameObject.AddComponent<RectMask2D>();
            frame.ShellGroup = frame.ShellMask.gameObject.AddComponent<CanvasGroup>();
            frame.ShellGroup.interactable = false;
            frame.ShellGroup.blocksRaycasts = false;
            frame.ShellMask.SetSiblingIndex(1);
            var inner = ResourceSellUi.Centered(frame.ShellMask, "Inner", Vector2.zero, size);
            ResourceSellPopupView.BuildPanelLayers(inner, frameSkin, size);
            var glowSprite = frameSkin != null ? CargoFullPopupArt.Sliced(frameSkin.frameGlow, 0.135f, 0.19f) : null;
            if (glowSprite != null)
            {
                frame.FrameGlow = ResourceSellUi.Image(inner, "FrameGlow", glowSprite, Color.clear);
                frame.FrameGlow.type = Image.Type.Sliced;
                frame.FrameGlow.pixelsPerUnitMultiplier = glowSprite.border.x * 100f / (glowSprite.pixelsPerUnit * 120f);
                frame.FrameGlow.enabled = false;
            }

            frame.ContentRoot = ResourceSellUi.Centered(owner, prefix + "Content", Vector2.zero, size);
            frame.ContentGroup = frame.ContentRoot.gameObject.AddComponent<CanvasGroup>();
            // 페이지 전환 때 위에서부터 드러내는 데 쓴다. 평소에는 여백 0이라 패널 안은 그대로 보인다.
            frame.ContentMask = frame.ContentRoot.gameObject.AddComponent<RectMask2D>();
            frame.ContentRoot.SetSiblingIndex(2);

            frame.ScanLine = ResourceSellUi.AddImage(
                ResourceSellUi.Centered(owner, prefix + "Scan", Vector2.zero, new Vector2(size.x, 10f)),
                frameSkin != null && frameSkin.scanLine != null ? frameSkin.scanLine : ResourceSellArt.SoftRect(),
                Color.clear);
            frame.ScanLine.enabled = false;
            return frame;
        }

        /// <summary>스캔선은 장식을 다 만든 뒤 맨 위로 올린다.</summary>
        public void BringScanToFront()
        {
            ScanLine.transform.SetAsLastSibling();
        }

        /// <summary>열기·접기(B-152 시간표): 가운데 선에서 위아래로 펼쳐지거나 접힌다.</summary>
        public void ApplyOpen(CargoFullPopupTimeline.Pose pose)
        {
            Backdrop.color = BackdropColor(pose.Backdrop);
            ShellMask.sizeDelta = new Vector2(size.x, size.y * Mathf.Clamp01(pose.Reveal));
            ShellGroup.alpha = pose.Alpha;
            if (FrameGlow != null)
            {
                FrameGlow.color = CargoPopupChrome.WithAlpha(Color.white, 0.55f * pose.FrameFlash);
                FrameGlow.enabled = pose.FrameFlash > 0.001f;
            }

            var scan = ScanLine.rectTransform;
            scan.anchoredPosition = Vector2.zero;
            scan.sizeDelta = new Vector2(size.x * Mathf.Clamp01(pose.ScanWidth), scan.sizeDelta.y);
            ScanLine.color = CargoPopupChrome.WithAlpha(CargoPopupChrome.Teal(skin), pose.ScanAlpha);
            ScanLine.enabled = pose.ScanAlpha > 0.001f;
            ContentGroup.alpha = pose.Content * pose.Alpha;
            ContentMask.padding = Vector4.zero;
        }

        /// <summary>페이지 전환: 프레임은 펼친 채로 두고 내용만 바꾼다.</summary>
        public void ApplyPage(CargoDiscardTimeline.PagePose pose)
        {
            Backdrop.color = BackdropColor(1f);
            ShellMask.sizeDelta = size;
            ShellGroup.alpha = 1f;
            if (FrameGlow != null)
            {
                FrameGlow.enabled = false;
            }

            // 위·아래 여백을 남겨 스캔선이 프레임 안쪽만 지나가게 한다.
            const float inset = 36f;
            var travel = size.y - inset * 2f;
            var scan = ScanLine.rectTransform;
            scan.sizeDelta = new Vector2(size.x - inset * 2f, scan.sizeDelta.y);
            scan.anchoredPosition = new Vector2(0f, size.y * 0.5f - inset - travel * Mathf.Clamp01(pose.ScanY));
            ScanLine.color = CargoPopupChrome.WithAlpha(CargoPopupChrome.Teal(skin), 0.9f * pose.ScanAlpha);
            ScanLine.enabled = pose.ScanAlpha > 0.001f;
            ContentGroup.alpha = pose.Content;
            var hidden = (inset + travel * (1f - Mathf.Clamp01(pose.Reveal))) * (pose.Reveal >= 1f ? 0f : 1f);
            ContentMask.padding = new Vector4(0f, hidden, 0f, 0f);
        }

        private Color BackdropColor(float amount)
        {
            var color = skin != null ? skin.backdrop : new Color(0.01f, 0.015f, 0.025f, 0.55f);
            return CargoPopupChrome.WithAlpha(color, color.a * amount);
        }
    }

    /// <summary>창 맨 아래 꼬리말: 양옆 선 + 가운데 문구.</summary>
    internal sealed class CargoPopupFooter
    {
        private TMP_Text text;
        private Image lineLeft;
        private Image lineRight;
        private float y;

        public TMP_Text Text => text;

        public static CargoPopupFooter Build(RectTransform content, float y, float innerW, TMP_FontAsset font, CargoFullPopupSkin skin)
        {
            var teal = CargoPopupChrome.Teal(skin);
            var footer = new CargoPopupFooter { y = y };
            footer.text = ResourceSellUi.Label(content, "Footer", CargoPopupChrome.Pad, y, innerW, 28f, font, 16f,
                FontStyles.Normal, CargoPopupChrome.TextMuted(skin), TextAlignmentOptions.Center);
            footer.text.text = CargoFullPopupText.Footer;
            footer.lineLeft = ResourceSellUi.AddImage(
                ResourceSellUi.Place(content, "FooterLineLeft", CargoPopupChrome.Pad, y + 13f, 10f, 2f), null,
                CargoPopupChrome.WithAlpha(teal, 0.25f));
            footer.lineRight = ResourceSellUi.AddImage(
                ResourceSellUi.Place(content, "FooterLineRight", CargoPopupChrome.Pad, y + 13f, 10f, 2f), null,
                CargoPopupChrome.WithAlpha(teal, 0.25f));
            return footer;
        }

        // 글자 폭은 글꼴이 준비된 뒤에야 정확하므로 표시할 때마다 양옆 선을 맞춘다.
        public void Layout(float panelWidth)
        {
            var pad = CargoPopupChrome.Pad;
            var innerW = panelWidth - pad * 2f;
            var textW = Mathf.Min(innerW - 120f, CargoPopupChrome.PreferredWidth(text, 16f));
            var lineW = Mathf.Max(0f, (innerW - textW) * 0.5f - 24f - 60f);
            CargoPopupChrome.SetRect(lineLeft.rectTransform, pad + 60f, y + 13f, lineW, 2f);
            CargoPopupChrome.SetRect(lineRight.rectTransform, panelWidth - pad - 60f - lineW, y + 13f, lineW, 2f);
        }
    }

    /// <summary>두 창이 같이 쓰는 배치·색·카드 버튼 도우미.</summary>
    internal static class CargoPopupChrome
    {
        public const float Pad = 72f;
        public const float TitleY = 42f;
        public const float DividerY = 102f;

        public static readonly Color TitleColor = new Color(0.84f, 0.98f, 1f, 1f);

        public static Color Teal(CargoFullPopupSkin skin) => skin != null ? skin.teal : new Color(0.42f, 0.94f, 1f, 1f);
        public static Color Amber(CargoFullPopupSkin skin) => skin != null ? skin.amber : new Color(1f, 0.74f, 0.2f, 1f);
        public static Color Danger(CargoFullPopupSkin skin) => skin != null ? skin.danger : new Color(0.95f, 0.3f, 0.3f, 1f);
        public static Color TextMain(CargoFullPopupSkin skin) => skin != null ? skin.textMain : new Color(0.9f, 0.97f, 1f, 1f);
        public static Color TextMuted(CargoFullPopupSkin skin) => skin != null ? skin.textMuted : new Color(0.62f, 0.76f, 0.82f, 1f);

        public static TMP_FontAsset ResolveFont(TMP_Text title, MineResetTimedPopupSkin frameSkin)
        {
            if (title != null && title.font != null)
            {
                return title.font;
            }

            return frameSkin != null && frameSkin.font != null ? frameSkin.font : TMP_Settings.defaultFontAsset;
        }

        /// <summary>제목 줄: 왼쪽 아이콘 + 씬 제목 글자 + 아래 구분선.</summary>
        public static void BuildHeader(
            RectTransform content,
            TMP_Text title,
            string titleLabel,
            Sprite icon,
            Color iconTint,
            float innerW,
            MineResetTimedPopupSkin frameSkin)
        {
            var image = ResourceSellUi.AddImage(ResourceSellUi.Place(content, "TitleIcon", Pad + 4f, TitleY + 9f, 38f, 38f),
                icon, iconTint);
            image.preserveAspect = true;
            image.enabled = icon != null;
            if (title != null)
            {
                title.gameObject.SetActive(true);
                title.transform.SetParent(content, false);
                SetRect(title.rectTransform, Pad + 56f, TitleY - 4f, innerW - 56f, 64f);
                title.fontSize = 40f;
                title.fontStyle = FontStyles.Bold;
                title.characterSpacing = 3f;
                title.color = TitleColor;
                title.alignment = TextAlignmentOptions.Left;
                title.textWrappingMode = TextWrappingModes.NoWrap;
                title.overflowMode = TextOverflowModes.Overflow;
                title.enableAutoSizing = false;
                title.raycastTarget = false;
                title.text = titleLabel;
                FitWidth(title, 40f, 28f, innerW - 56f);
            }

            if (frameSkin != null && frameSkin.titleDivider != null)
            {
                var divider = ResourceSellUi.AddImage(ResourceSellUi.Place(content, "TitleDivider", Pad, DividerY, innerW, 12f),
                    frameSkin.titleDivider, WithAlpha(Color.white, 0.75f));
                divider.preserveAspect = false;
            }
        }

        /// <summary>
        /// 씬 버튼을 판매창 형식의 각진 카드로 바꾼다(이름·onClick 유지). compact는 한 줄 높이가 낮은 카드다.
        /// </summary>
        public static CargoFullChoiceButton BuildChoice(
            Button target,
            RectTransform content,
            float x,
            float y,
            float width,
            float height,
            Color accent,
            Sprite iconSprite,
            float iconSize,
            string titleLabel,
            string subtitleLabel,
            bool compact,
            TMP_FontAsset font,
            CargoFullPopupSkin skin,
            out TMP_Text subtitle)
        {
            subtitle = null;
            if (target == null)
            {
                return null;
            }

            var rect = (RectTransform)target.transform;
            rect.SetParent(content, false);
            SetRect(rect, x, y, width, height);
            var glow = PrepareFace(target, rect, accent, out var face, out var border);
            var icon = ResourceSellUi.AddImage(
                ResourceSellUi.Place(rect, "Icon", 52f - iconSize * 0.5f, (height - iconSize) * 0.5f, iconSize, iconSize),
                iconSprite, accent);
            icon.preserveAspect = true;
            icon.enabled = iconSprite != null;

            var title = ResolveLabel(rect, font);
            title.transform.SetAsLastSibling();
            var titleSize = compact ? 22f : 26f;
            SetRect(title.rectTransform, 92f, compact ? 8f : 20f, width - 92f - 20f, compact ? 30f : 40f);
            title.text = titleLabel;
            title.fontSize = titleSize;
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Left;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.enableAutoSizing = true;
            title.fontSizeMin = compact ? 16f : 18f;
            title.fontSizeMax = titleSize;
            title.overflowMode = TextOverflowModes.Ellipsis;
            title.raycastTarget = false;

            var subSize = compact ? 15f : 17f;
            subtitle = ResourceSellUi.Label(rect, "Subtitle", 92f, compact ? 37f : 62f, width - 92f - 20f, compact ? 22f : 28f,
                font, subSize, FontStyles.Normal, TextMuted(skin), TextAlignmentOptions.Left);
            subtitle.text = subtitleLabel;
            subtitle.enableAutoSizing = true;
            subtitle.fontSizeMin = compact ? 12f : 13f;
            subtitle.fontSizeMax = subSize;
            subtitle.overflowMode = TextOverflowModes.Ellipsis;

            var feedback = EnsureFeedback(target);
            var idle = Color.Lerp(new Color(0.02f, 0.035f, 0.05f, 0.97f), accent, 0.06f);
            var hover = Color.Lerp(new Color(0.02f, 0.035f, 0.05f, 1f), accent, 0.2f);
            feedback.Setup(target, face, border, glow, icon, title, subtitle, accent, idle, hover, TextMain(skin), TextMuted(skin));
            return feedback;
        }

        /// <summary>글자만 있는 작은 각진 버튼(버리기 수량 버튼). 위치는 호출한 쪽이 정한다.</summary>
        public static CargoFullChoiceButton BuildCompactButton(
            Button target, string label, Color accent, float faceTint, TMP_FontAsset font, CargoFullPopupSkin skin)
        {
            var rect = (RectTransform)target.transform;
            var glow = PrepareFace(target, rect, accent, out var face, out var border);
            var title = ResolveLabel(rect, font);
            title.transform.SetAsLastSibling();
            ResourceSellUi.Stretch(title.rectTransform);
            title.text = label;
            title.fontSize = 18f;
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Center;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.enableAutoSizing = true;
            title.fontSizeMin = 13f;
            title.fontSizeMax = 18f;
            title.overflowMode = TextOverflowModes.Overflow;
            title.margin = new Vector4(4f, 0f, 4f, 0f);
            title.raycastTarget = false;

            var feedback = EnsureFeedback(target);
            var baseFace = new Color(0.02f, 0.035f, 0.05f, 0.97f);
            var idle = Color.Lerp(baseFace, accent, faceTint);
            var hover = Color.Lerp(new Color(0.02f, 0.035f, 0.05f, 1f), accent, faceTint + 0.16f);
            feedback.Setup(target, face, border, glow, null, title, null, accent, idle, hover, TextMain(skin), TextMuted(skin));
            return feedback;
        }

        public static void Plate(RectTransform parent, CargoFullPopupSkin skin, float lineAlpha)
        {
            ResourceSellUi.Image(parent, "Plate", ResourceSellArt.ChamferFill(),
                skin != null ? skin.plate : new Color(0.025f, 0.05f, 0.07f, 0.94f));
            ResourceSellUi.Image(parent, "PlateLine", ResourceSellArt.ChamferOutline(), WithAlpha(Teal(skin), lineAlpha));
        }

        public static float PreferredWidth(TMP_Text text, float fontSize)
        {
            text.enableAutoSizing = false;
            text.fontSize = fontSize;
            if (text.gameObject.activeInHierarchy)
            {
                text.ForceMeshUpdate(true, true);
            }

            return text.GetPreferredValues(text.text, 4000f, 400f).x;
        }

        /// <summary>한 줄 글자가 available을 넘으면 최소 크기까지 줄인다.</summary>
        public static void FitWidth(TMP_Text text, float maxSize, float minSize, float available)
        {
            var width = PreferredWidth(text, maxSize);
            if (width > available && width > 0f)
            {
                text.fontSize = Mathf.Max(minSize, Mathf.Floor(maxSize * available / width));
            }
        }

        /// <summary>부모 왼쪽 위 기준 (x, y, w, h). 피벗은 가운데라 눌림이 제자리에서 일어난다.</summary>
        public static void SetRect(RectTransform rect, float x, float y, float w, float h)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x + w * 0.5f, -(y + h * 0.5f));
            rect.sizeDelta = new Vector2(w, h);
        }

        public static Color WithAlpha(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }

        private static Image PrepareFace(Button target, RectTransform rect, Color accent, out Image face, out Image border)
        {
            var hitArea = target.GetComponent<Image>();
            if (hitArea != null)
            {
                hitArea.sprite = null;
                hitArea.color = Color.clear;
                hitArea.raycastTarget = true;
            }

            target.transition = Selectable.Transition.None;
            var glow = ResourceSellUi.Image(rect, "Glow", ResourceSellArt.SoftRect(), Color.clear);
            glow.rectTransform.offsetMin = new Vector2(-14f, -14f);
            glow.rectTransform.offsetMax = new Vector2(14f, 14f);
            glow.raycastTarget = false;
            face = ResourceSellUi.Image(rect, "Face", ResourceSellArt.ChamferFill(), Color.clear);
            face.raycastTarget = false;
            border = ResourceSellUi.Image(rect, "Border", ResourceSellArt.ChamferOutline(), accent);
            border.raycastTarget = false;
            return glow;
        }

        private static TMP_Text ResolveLabel(RectTransform rect, TMP_FontAsset font)
        {
            TMP_Text title = null;
            var label = rect.Find("Label");
            if (label != null)
            {
                title = label.GetComponent<TMP_Text>();
            }

            if (title == null)
            {
                title = ResourceSellUi.Text(ResourceSellUi.Place(rect, "Label", 0f, 0f, 10f, 10f), font, 26f, FontStyles.Bold,
                    Color.white, TextAlignmentOptions.Left);
            }

            if (title.font == null && font != null)
            {
                title.font = font;
            }

            return title;
        }

        private static CargoFullChoiceButton EnsureFeedback(Button target)
        {
            var feedback = target.GetComponent<CargoFullChoiceButton>();
            if (feedback == null)
            {
                feedback = target.gameObject.AddComponent<CargoFullChoiceButton>();
            }

            return feedback;
        }
    }
}
