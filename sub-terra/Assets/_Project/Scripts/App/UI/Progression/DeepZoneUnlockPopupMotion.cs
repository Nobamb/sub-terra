using System;
using SubTerra.App.UI.SurfaceBase;
using SubTerra.App.UI.Tutorial;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.Progression
{
    /// <summary>
    /// 심층 구역 해금 창(B-154). 시작 브리핑 글리치 연출(StartBriefingPopupMotion.ConfigureCompact)을
    /// 작은 창에 그대로 붙인다. 게임을 멈추지 않고, 닫기 연출이 시작되면 입력을 받지 않는다.
    /// </summary>
    public sealed class DeepZoneUnlockPopupMotion : MonoBehaviour
    {
        public const string RootName = "DeepZoneUnlockPopup_Runtime";
        public const string CloseButtonName = "CloseButton";
        private const string Prefix = "Unlock";
        // 작은 창에 맞춰 신호 점 시작 반경과 가장자리 간섭 막대 길이를 줄인다.
        private const float EffectScale = 0.5f;

        private static readonly Color TitleColor = new Color(0.4f, 1f, 1f, 1f);
        private static readonly Color BodyColor = new Color(0.88f, 0.96f, 0.98f, 1f);
        private static readonly Color LabelColor = new Color(0.9f, 0.99f, 1f, 1f);

        private StartBriefingPopupMotion transition;
        private CanvasGroup group;

        public StartBriefingPopupMotion Transition => transition;
        public bool IsClosing => transition != null && transition.IsClosing;

        public sealed class Parts
        {
            public GameObject Root;
            public TMP_Text Title;
            public TMP_Text Body;
            public Button CloseButton;
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
            parts.Title = NewText(rect, "Title", skin.font, skin.titleFontSize, FontStyles.Bold, TitleColor);
            Place(parts.Title.rectTransform, skin.titleY, skin.titleSize);
            parts.Title.characterSpacing = 4f * skin.windowSize.x / 1700f;
            parts.Body = NewText(rect, "Message", skin.font, skin.bodyFontSize, FontStyles.Normal, BodyColor);
            Place(parts.Body.rectTransform, skin.bodyY, skin.bodySize);
            parts.CloseButton = NewButton(rect, skin);

            var motion = root.AddComponent<DeepZoneUnlockPopupMotion>();
            motion.Configure(skin);
            root.SetActive(false);
            return parts;
        }

        private void Configure(DeepZoneUnlockPopupSkin skin)
        {
            transition = gameObject.AddComponent<StartBriefingPopupMotion>();
            transition.enabled = false;
            transition.ConfigureCompact(skin.windowSize, skin.frame, true, Prefix, null, EffectScale);
            transition.IntroSpeed = skin.introSpeed;
            group = GetComponent<CanvasGroup>();
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
