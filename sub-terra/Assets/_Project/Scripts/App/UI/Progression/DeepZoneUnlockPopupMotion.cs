using System;
using SubTerra.App.UI.HUD;
using SubTerra.App.UI.SurfaceBase;
using SubTerra.App.UI.Tutorial;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.Progression
{
    /// <summary>
    /// 심층 구역 해금 창(B-154, B-154-1). 시작 브리핑 글리치 연출(StartBriefingPopupMotion.ConfigureCompact)로
    /// 광산 초기화 카드 프레임을 열고, 가운데 심층 발견 그림은 DeepZoneUnlockArtMotion이 연출한다.
    /// 게임을 멈추지 않고, 닫기 연출이 시작되면 입력을 받지 않는다.
    /// </summary>
    public sealed class DeepZoneUnlockPopupMotion : MonoBehaviour
    {
        public const string RootName = "DeepZoneUnlockPopup_Runtime";
        public const string CloseButtonName = "CloseButton";
        public const string HeroName = "Hero";
        private const string Prefix = "Unlock";
        private const int MoteCount = 10;

        private static readonly Color TitleColor = new Color(0.4f, 1f, 1f, 1f);
        private static readonly Color BodyColor = new Color(0.45f, 0.96f, 1f, 1f);
        private static readonly Color DetailColor = new Color(0.8f, 0.88f, 0.92f, 1f);
        private static readonly Color LabelColor = new Color(0.9f, 0.99f, 1f, 1f);
        private static readonly Color HaloColor = new Color(0.12f, 0.75f, 0.9f, 1f);
        private static readonly Color MoteColor = new Color(0.55f, 0.95f, 1f, 0f);
        private static readonly Color GhostColor = new Color(0.25f, 0.85f, 0.95f, 0f);

        private StartBriefingPopupMotion transition;
        private DeepZoneUnlockArtMotion art;
        private CanvasGroup group;

        public StartBriefingPopupMotion Transition => transition;
        public DeepZoneUnlockArtMotion Art => art;
        public bool IsClosing => transition != null && transition.IsClosing;

        public sealed class Parts
        {
            public GameObject Root;
            public TMP_Text Title;
            public TMP_Text Body;
            public TMP_Text Detail;
            public Button CloseButton;
            public DeepZoneUnlockArtMotion.Layers Art;
            public Image FrameFlash;
        }

        /// <summary>스킨으로 창을 만든다. 만들어진 창은 비활성 상태다.</summary>
        public static Parts Create(Transform canvas, DeepZoneUnlockPopupSkin skin, int sortingOrder)
        {
            var root = new GameObject(
                RootName,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(GraphicRaycaster),
                typeof(CanvasGroup));
            root.transform.SetParent(canvas, false);
            var rect = (RectTransform)root.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            // StartBriefingPopupMotion.FitWindow의 여백(0.94/0.92)만큼 키워 기준 해상도에서 창이 1배로 보이게 한다.
            rect.sizeDelta = new Vector2(
                Mathf.Ceil(skin.windowSize.x / 0.94f),
                Mathf.Ceil(skin.windowSize.y / 0.92f));
            var popupCanvas = root.GetComponent<Canvas>();
            popupCanvas.overrideSorting = true;
            popupCanvas.sortingOrder = sortingOrder;

            var parts = new Parts { Root = root };
            // 아래 자식들은 ConfigureCompact가 본문 레이어로 옮긴다(본문 알파만 바뀌고 글리치는 받지 않는다).
            // 발견 순간 프레임 전체가 번쩍이는 빛. 본문 맨 아래, 글자·그림보다 뒤에 둔다.
            parts.FrameFlash = NewImage(rect, "FrameFlash", skin.frameGlow);
            parts.FrameFlash.rectTransform.sizeDelta = skin.windowSize;
            parts.FrameFlash.color = new Color(1f, 1f, 1f, 0f);

            parts.Title = NewText(rect, "Title", skin.font, skin.titleFontSize, FontStyles.Bold, TitleColor);
            Place(parts.Title.rectTransform, skin.titleY, skin.titleSize);
            parts.Title.characterSpacing = 4f;
            if (skin.titleDivider != null)
            {
                var divider = NewImage(rect, "TitleDivider", skin.titleDivider);
                Place(divider.rectTransform, skin.dividerY, skin.dividerSize);
            }

            if (skin.HasArt)
            {
                parts.Art = NewHero(rect, skin);
            }

            parts.Body = NewText(rect, "Message", skin.font, skin.bodyFontSize, FontStyles.Bold, BodyColor);
            Place(parts.Body.rectTransform, skin.bodyY, skin.bodySize);
            parts.Detail = NewText(rect, "Detail", skin.font, skin.detailFontSize, FontStyles.Normal, DetailColor);
            Place(parts.Detail.rectTransform, skin.detailY, skin.detailSize);
            parts.Detail.text = DeepZoneUnlockPopupText.Detail;
            parts.CloseButton = NewButton(rect, skin);

            var motion = root.AddComponent<DeepZoneUnlockPopupMotion>();
            motion.Configure(skin, parts);
            root.SetActive(false);
            return parts;
        }

        private void Configure(DeepZoneUnlockPopupSkin skin, Parts parts)
        {
            transition = gameObject.AddComponent<StartBriefingPopupMotion>();
            transition.enabled = false;
            transition.ConfigureCompact(
                skin.windowSize,
                Vector2.Scale(skin.windowSize, skin.frameExtentRatio),
                skin.frame,
                true,
                Prefix,
                null,
                1f);
            transition.IntroSpeed = skin.introSpeed;
            // 카드 프레임 그림은 안쪽이 비어 있으므로 프레임과 함께 열리는 바탕을 깐다(광산 초기화 카드와 같은 두 겹).
            var backingSize = skin.windowSize - new Vector2(24f, 24f);
            transition.AddFrameBacking("Panel", skin.panel, skin.panelColor, backingSize);
            transition.AddFrameBacking("PanelBase", null, skin.panelColor, backingSize);
            group = GetComponent<CanvasGroup>();

            var layers = parts.Art;
            var contentRect = transform.Find(Prefix + "Content");
            layers.Content = contentRect != null ? contentRect.GetComponent<CanvasGroup>() : null;
            layers.FrameFlash = parts.FrameFlash;
            layers.Counter = parts.Body;
            art = gameObject.AddComponent<DeepZoneUnlockArtMotion>();
            art.SetDepth(skin.minDepth, skin.maxDepth);
            art.Bind(layers);
        }

        /// <summary>처음부터 연다. 닫는 중이면 닫기를 끊고 연출을 다시 시작한다.</summary>
        public void Open()
        {
            if (group != null)
            {
                group.blocksRaycasts = true;
            }

            if (transition == null)
            {
                gameObject.SetActive(true);
                return;
            }

            // 그림은 본문이 다시 보이기 시작할 때 처음부터 드러난다.
            if (art != null)
            {
                art.Restart();
            }

            // 비활성화가 이전 연출의 잔상·노이즈 막대·닫기 콜백을 모두 정리한다.
            transition.enabled = false;
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            transition.enabled = true;
        }

        /// <summary>닫기 연출을 시작한다. 연출을 시작하지 못하면 false이고 호출 측이 바로 닫는다.</summary>
        public bool RequestClose(Action closed)
        {
            if (group != null)
            {
                group.blocksRaycasts = false;
            }

            return transition != null
                && transition.isActiveAndEnabled
                && transition.RequestCompactClose(closed);
        }

        private void OnDisable()
        {
            PopupWindowSorting.Remove(GetComponent<Canvas>());
        }

        // 심층 발견 그림. 모든 그림 레이어는 같은 크기라 겹쳐 놓고 채움 비율·알파만 바꾼다.
        private static DeepZoneUnlockArtMotion.Layers NewHero(RectTransform parent, DeepZoneUnlockPopupSkin skin)
        {
            var hero = NewRect(parent, HeroName);
            Place(hero, skin.emblemY, skin.emblemSize);
            var size = skin.emblemSize;
            var layers = new DeepZoneUnlockArtMotion.Layers { Hero = hero };

            layers.Halo = NewImage(hero, "Halo", MineResetPopupArt.Soft("glow", 0.15f));
            layers.Halo.rectTransform.sizeDelta = new Vector2(size.x * 1.3f, size.y * 1.45f);
            layers.Halo.color = HaloColor;

            layers.Rings = new Image[2];
            for (var i = 0; i < layers.Rings.Length; i++)
            {
                layers.Rings[i] = NewImage(hero, "PingRing" + i, skin.emblemRing);
                layers.Rings[i].rectTransform.sizeDelta = size;
            }

            // 굴착 빛이 지나가기 전에도 심층의 윤곽이 탐지기 화면처럼 희미하게 보이게 한다.
            layers.Ghost = NewImage(hero, "EmblemGhost", skin.emblem);
            layers.Ghost.rectTransform.sizeDelta = size;
            layers.Ghost.color = GhostColor;
            layers.Emblem = NewFilled(hero, "Emblem", skin.emblem, size);
            var glows = skin.emblemCrystalGlows ?? new Sprite[0];
            layers.CrystalGlows = new Image[glows.Length];
            for (var i = 0; i < glows.Length; i++)
            {
                layers.CrystalGlows[i] = NewFilled(hero, "CrystalGlow" + i, glows[i], size);
            }

            layers.CoreGlow = NewFilled(hero, "CoreGlow", skin.emblemCoreGlow, size);

            // 굴착 빛·훑기 빛은 엠블럼 안쪽 사각형 밖으로 나가지 않게 자른다.
            var clip = NewRect(hero, "ScanClip");
            clip.sizeDelta = new Vector2(size.x * 0.93f, size.y * 0.89f);
            clip.gameObject.AddComponent<RectMask2D>();
            layers.ScanLine = NewImage(clip, "ScanLine", skin.scanLine);
            layers.ScanLine.rectTransform.sizeDelta = new Vector2(size.x * 0.95f, 26f);
            layers.SweepLine = NewImage(clip, "SweepLine", skin.scanLine);
            layers.SweepLine.rectTransform.sizeDelta = new Vector2(size.x * 0.95f, 44f);
            layers.SweepLine.color = new Color(0.55f, 0.95f, 1f, 0f);

            layers.BorderGlow = NewImage(hero, "BorderGlow", skin.emblemBorderGlow);
            layers.BorderGlow.rectTransform.sizeDelta = size;
            layers.Border = NewImage(hero, "Border", skin.emblemBorder);
            layers.Border.rectTransform.sizeDelta = size;

            layers.Motes = new Image[MoteCount];
            for (var i = 0; i < layers.Motes.Length; i++)
            {
                var dot = 6f + (i % 3) * 2.5f;
                layers.Motes[i] = NewImage(hero, "Mote" + i, skin.mote);
                layers.Motes[i].rectTransform.sizeDelta = new Vector2(dot, dot);
                layers.Motes[i].color = MoteColor;
            }

            return layers;
        }

        private static Image NewFilled(Transform parent, string name, Sprite sprite, Vector2 size)
        {
            var image = NewImage(parent, name, sprite);
            image.rectTransform.sizeDelta = size;
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Vertical;
            image.fillOrigin = (int)Image.OriginVertical.Top;
            image.fillAmount = 0f;
            return image;
        }

        private static Button NewButton(RectTransform parent, DeepZoneUnlockPopupSkin skin)
        {
            var go = new GameObject(CloseButtonName, typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            Place(rect, skin.buttonY, skin.buttonSize);
            var hit = go.GetComponent<Image>();
            hit.color = Color.clear;
            hit.raycastTarget = true;
            var button = go.GetComponent<Button>();
            button.targetGraphic = hit;
            button.transition = Selectable.Transition.None;

            // PromptB117SurfaceBaseBuilder.StyleButton과 같은 레이어 구성(380x78 기준 여백을 버튼 크기에 맞춘다).
            var inset = new Vector2(35f * skin.buttonSize.x / 380f, 15f * skin.buttonSize.y / 78f);
            var plate = NewImage(rect, "Frame", skin.buttonOff);
            Fill(plate.rectTransform, Vector2.zero);
            var highlight = NewImage(rect, "HoverFrame", skin.buttonOn != null ? skin.buttonOn : skin.buttonOff);
            Fill(highlight.rectTransform, Vector2.zero);
            highlight.color = Color.clear;
            var mask = NewRect(rect, "InnerLight");
            Fill(mask, inset);
            mask.gameObject.AddComponent<RectMask2D>();
            var sweep = NewImage(mask, "Sweep", skin.buttonSweep);
            sweep.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            sweep.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            sweep.rectTransform.sizeDelta = new Vector2(90f * skin.buttonSize.x / 380f, 0f);
            sweep.color = Color.clear;
            var particles = NewRect(rect, "HoverParticles");
            Fill(particles, inset);

            var label = NewText(rect, "Label", skin.font, skin.buttonFontSize, FontStyles.Bold, LabelColor);
            Fill(label.rectTransform, Vector2.zero);
            label.text = DeepZoneUnlockPopupText.CloseLabel;

            go.AddComponent<SurfaceBaseButtonFeedback>().Configure(plate, highlight, sweep, particles, skin.buttonParticle);
            return button;
        }

        private static TextMeshProUGUI NewText(
            Transform parent,
            string name,
            TMP_FontAsset font,
            float size,
            FontStyles style,
            Color color)
        {
            var text = NewRect(parent, name).gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null)
            {
                text.font = font;
            }

            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            // 긴 레벨 문구도 한 줄에 들어가도록 하한까지 줄인다.
            text.enableAutoSizing = true;
            text.fontSizeMax = size;
            text.fontSizeMin = Mathf.Round(size * 0.7f);
            text.fontSize = size;
            text.raycastTarget = false;
            return text;
        }

        private static Image NewImage(Transform parent, string name, Sprite sprite)
        {
            var image = NewRect(parent, name).gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        private static RectTransform NewRect(Transform parent, string name)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            return rect;
        }

        private static void Place(RectTransform rect, float y, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = size;
        }

        private static void Fill(RectTransform rect, Vector2 inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = inset;
            rect.offsetMax = -inset;
        }
    }
}
