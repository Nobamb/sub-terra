using SubTerra.App.Save;
using SubTerra.App.Tutorial;
using SubTerra.Shared.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SubTerra.App.UI.HUD
{
    /// <summary>
    /// 광산 초기화 전자시계(화면 최상단 중앙)와 3시간 만료 팝업.
    /// Bootstrap 수명에 붙어 Scene이 바뀌어도 유지되며, T 키로 시계만 열고 닫는다.
    /// </summary>
    [DefaultExecutionOrder(-150)]
    public sealed class MineResetClockOverlay : MonoBehaviour
    {
        private const int ClockSortingOrder = 900;
        private const int PopupSortingOrder = 32_000;
        private static readonly Color ClockPanelColor = new Color(0.02f, 0.04f, 0.03f, 0.92f);
        private static readonly Color ClockBorderColor = new Color(0.12f, 0.28f, 0.16f, 1f);
        private static readonly Color DigitColor = new Color(0.25f, 1f, 0.42f, 1f);
        private static readonly Color LabelColor = new Color(0.45f, 0.85f, 0.55f, 1f);
        private static readonly Color OverlayColor = new Color(0.01f, 0.02f, 0.035f, 0.78f);
        private static readonly Color CardColor = new Color(0.055f, 0.075f, 0.105f, 0.98f);

        private GameObject clockRoot;
        private TMP_Text labelText;
        private TMP_Text clockText;
        private GameObject popupRoot;
        private TMP_Text popupTitle;
        private TMP_Text popupBody;
        private Button popupOkButton;
        private bool sessionVisible;

        public static MineResetClockOverlay Create(Transform parent)
        {
            var root = new GameObject(
                "MineResetClockOverlay",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            if (parent != null)
            {
                root.transform.SetParent(parent, false);
            }
            else if (Application.isPlaying)
            {
                DontDestroyOnLoad(root);
            }

            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = ClockSortingOrder;
            canvas.pixelPerfect = true;

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var overlay = root.AddComponent<MineResetClockOverlay>();
            overlay.Build();
            overlay.sessionVisible = true;
            return overlay;
        }

        public void SetSessionVisible(bool visible)
        {
            sessionVisible = visible;
            if (!visible && popupRoot != null)
            {
                popupRoot.SetActive(false);
            }

            RefreshFromState();
        }

        public void RefreshFromState()
        {
            if (clockRoot == null)
            {
                return;
            }

            var runtime = SaveRuntimeController.Instance;
            var clockOn = sessionVisible
                && runtime != null
                && runtime.ActiveSlot > 0
                && runtime.IsMineResetClockVisible;
            clockRoot.SetActive(clockOn);
            if (!clockOn)
            {
                return;
            }

            if (labelText != null)
            {
                labelText.text = LocalizationService.Get(
                    "mine_reset.clock.label",
                    "광산 초기화");
            }

            if (clockText != null)
            {
                clockText.text = MineResetService.FormatClock(
                    runtime.MineResetRemainingSeconds);
            }
        }

        public void ShowTimedResetPopup(bool fromMine)
        {
            if (popupRoot == null)
            {
                return;
            }

            if (popupTitle != null)
            {
                popupTitle.text = LocalizationService.Get(
                    "mine_reset.timed.title",
                    "광산 초기화");
            }

            if (popupBody != null)
            {
                popupBody.text = LocalizationService.Get(
                    fromMine
                        ? "mine_reset.timed.body.mine"
                        : "mine_reset.timed.body.surface");
            }

            if (popupOkButton != null)
            {
                var okLabel = popupOkButton.GetComponentInChildren<TMP_Text>(true);
                if (okLabel != null)
                {
                    okLabel.text = LocalizationService.Get("mine_reset.timed.ok", "확인");
                }
            }

            popupRoot.SetActive(true);
            popupRoot.transform.SetAsLastSibling();
        }

        public void DestroyOverlay()
        {
            if (this == null || gameObject == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                DestroyImmediate(gameObject);
            }
        }

        private void Update()
        {
            if (!sessionVisible)
            {
                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard != null
                && keyboard.tKey.wasPressedThisFrame
                && !IsTypingInInputField())
            {
                SaveRuntimeController.Instance?.ToggleMineResetClock();
            }

            if (clockRoot != null && clockRoot.activeSelf)
            {
                RefreshFromState();
            }
        }

        private void Build()
        {
            var font = TMP_Settings.defaultFontAsset;
            BuildClock(font);
            BuildPopup(font);
            RefreshFromState();
        }

        private void BuildClock(TMP_FontAsset font)
        {
            clockRoot = new GameObject("ClockRoot", typeof(RectTransform), typeof(Image));
            clockRoot.transform.SetParent(transform, false);
            var rootRect = clockRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 1f);
            rootRect.anchorMax = new Vector2(0.5f, 1f);
            rootRect.pivot = new Vector2(0.5f, 1f);
            rootRect.anchoredPosition = new Vector2(0f, -12f);
            rootRect.sizeDelta = new Vector2(280f, 78f);

            var border = clockRoot.GetComponent<Image>();
            border.color = ClockBorderColor;
            border.raycastTarget = false;

            var inner = new GameObject("Inner", typeof(RectTransform), typeof(Image));
            inner.transform.SetParent(clockRoot.transform, false);
            var innerRect = inner.GetComponent<RectTransform>();
            innerRect.anchorMin = Vector2.zero;
            innerRect.anchorMax = Vector2.one;
            innerRect.offsetMin = new Vector2(3f, 3f);
            innerRect.offsetMax = new Vector2(-3f, -3f);
            var innerImage = inner.GetComponent<Image>();
            innerImage.color = ClockPanelColor;
            innerImage.raycastTarget = false;

            labelText = CreateText(
                inner.transform,
                "Label",
                new Vector2(0f, -6f),
                new Vector2(260f, 22f),
                16f,
                font,
                LabelColor,
                FontStyles.Normal);
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.text = LocalizationService.Get("mine_reset.clock.label", "광산 초기화");

            clockText = CreateText(
                inner.transform,
                "Digits",
                new Vector2(0f, -32f),
                new Vector2(260f, 42f),
                34f,
                font,
                DigitColor,
                FontStyles.Bold);
            clockText.alignment = TextAlignmentOptions.Center;
            clockText.characterSpacing = 6f;
            clockText.fontStyle = FontStyles.Bold;
            clockText.text = "03:00:00";
        }

        private void BuildPopup(TMP_FontAsset font)
        {
            popupRoot = new GameObject(
                "TimedResetPopup",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(GraphicRaycaster),
                typeof(Image));
            popupRoot.transform.SetParent(transform, false);
            var rootRect = popupRoot.GetComponent<RectTransform>();
            Stretch(rootRect);
            var popupCanvas = popupRoot.GetComponent<Canvas>();
            popupCanvas.overrideSorting = true;
            popupCanvas.sortingOrder = Mathf.Max(PopupSortingOrder, UiLayerPriority.EmergencyRescueModal + 500);
            var blocker = popupRoot.GetComponent<Image>();
            blocker.color = OverlayColor;
            blocker.raycastTarget = true;

            var card = new GameObject("Card", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(popupRoot.transform, false);
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(640f, 320f);
            card.GetComponent<Image>().color = CardColor;

            popupTitle = CreateText(
                card.transform,
                "Title",
                new Vector2(0f, 110f),
                new Vector2(580f, 40f),
                28f,
                font,
                Color.white,
                FontStyles.Bold);
            popupTitle.alignment = TextAlignmentOptions.Center;

            popupBody = CreateText(
                card.transform,
                "Body",
                new Vector2(0f, 8f),
                new Vector2(580f, 160f),
                20f,
                font,
                new Color(0.88f, 0.92f, 0.96f, 1f),
                FontStyles.Normal);
            popupBody.alignment = TextAlignmentOptions.Center;
            popupBody.enableWordWrapping = true;

            var okObject = new GameObject("OkButton", typeof(RectTransform), typeof(Image), typeof(Button));
            okObject.transform.SetParent(card.transform, false);
            var okRect = okObject.GetComponent<RectTransform>();
            okRect.anchorMin = new Vector2(0.5f, 0f);
            okRect.anchorMax = new Vector2(0.5f, 0f);
            okRect.pivot = new Vector2(0.5f, 0f);
            okRect.anchoredPosition = new Vector2(0f, 24f);
            okRect.sizeDelta = new Vector2(180f, 48f);
            okObject.GetComponent<Image>().color = new Color(0.16f, 0.42f, 0.28f, 1f);
            popupOkButton = okObject.GetComponent<Button>();
            popupOkButton.onClick.AddListener(HidePopup);

            var okLabel = CreateText(
                okObject.transform,
                "Label",
                Vector2.zero,
                Vector2.zero,
                22f,
                font,
                Color.white,
                FontStyles.Bold);
            Stretch(okLabel.rectTransform);
            okLabel.alignment = TextAlignmentOptions.Center;
            okLabel.text = LocalizationService.Get("mine_reset.timed.ok", "확인");
            okLabel.raycastTarget = false;

            popupRoot.SetActive(false);
        }

        private void HidePopup()
        {
            if (popupRoot != null)
            {
                popupRoot.SetActive(false);
            }
        }

        private static bool IsTypingInInputField()
        {
            var selected = EventSystem.current != null
                ? EventSystem.current.currentSelectedGameObject
                : null;
            if (selected == null)
            {
                return false;
            }

            return selected.GetComponent<TMP_InputField>() != null
                || selected.GetComponent<InputField>() != null;
        }

        private static TMP_Text CreateText(
            Transform parent,
            string name,
            Vector2 anchoredPosition,
            Vector2 size,
            float fontSize,
            TMP_FontAsset font,
            Color color,
            FontStyles style)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            var text = go.GetComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = fontSize;
            text.color = color;
            text.fontStyle = style;
            text.raycastTarget = false;
            text.overflowMode = TextOverflowModes.Overflow;
            return text;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
