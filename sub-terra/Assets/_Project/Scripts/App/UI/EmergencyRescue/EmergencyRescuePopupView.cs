using System;
using SubTerra.App.Audio;
using SubTerra.App.Run;
using SubTerra.App.UI.HUD;
using SubTerra.App.UI.MainMenu;
using SubTerra.Shared.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.EmergencyRescue
{
    /// <summary>
    /// 전력 고갈 구출 팝업 본체. 등장(끊겨 나타나는 글리치) → 표시(가장자리 간헐 글리치) → 종료(강한 글리치) 상태기계와
    /// 비용 표·버튼을 소유한다. 표시·비용 계산은 하지 않고 EmergencyRescueCost를 받아 보여 주기만 한다.
    /// 모든 효과는 장식 레이어에만 적용되고 버튼·본문은 제자리에서 읽힌다. 시간은 unscaledDeltaTime이다.
    /// </summary>
    public sealed class EmergencyRescuePopupView : MonoBehaviour
    {
        public enum PopupState
        {
            Hidden = 0,
            Opening = 1,
            Open = 2,
            Closing = 3
        }

        public const string Title = "전력이 바닥났습니다";
        public const string DefaultMessage =
            "전력이 모두 소진되었습니다.\n구출을 요청하면 엘리베이터로 이동하며 아래 자원이 차감됩니다.";
        public const string RescueLabel = "구출 요청";
        public const string CloseLabel = "닫기";
        public static readonly Vector2 CardSize = new Vector2(880f, 675f);

        private const float MaxStep = 0.05f;

        private static readonly Color DisabledTint = new Color(0.5f, 0.5f, 0.5f, 0.7f);
        private static readonly Color SecondaryTint = new Color(0.62f, 0.74f, 0.84f, 0.92f);

        private TMP_FontAsset font;
        private Canvas canvas;
        private GlitchPopupChrome chrome;
        private RectTransform contentRect;
        private TMP_Text messageText;
        private TMP_Text footerText;
        private EmergencyRescueCostTable table;
        private Button rescueButton;
        private Button closeButton;
        private Image rescueImage;
        private Image closeImage;
        private TMP_Text rescueLabel;
        private TMP_Text closeLabel;
        private Func<string, Sprite> iconResolver;

        private PopupState state;
        private float clock;
        private float idleClock;
        private bool externalInteractable = true;
        private EmergencyRescueCost displayedCost;
        private Action closedCallback;

        public PopupState State => state;
        /// <summary>등장 중이거나 열려 있음. 닫는 중은 포함하지 않는다.</summary>
        public bool IsOpen => state == PopupState.Opening || state == PopupState.Open;
        public bool IsClosing => state == PopupState.Closing;
        public bool IsVisible => state != PopupState.Hidden;
        /// <summary>등장이 끝나 입력을 받을 수 있는 상태.</summary>
        public bool IsStable => state == PopupState.Open;
        public EmergencyRescueCost DisplayedCost => displayedCost;
        public Button RescueButton => rescueButton;
        public Button CloseButton => closeButton;
        public float Clock => clock;
        public int VisibleRowCount => table != null ? table.VisibleRowCount : 0;
        public string MessageString => messageText != null ? messageText.text : string.Empty;
        public string FooterString => footerText != null ? footerText.text : string.Empty;
        public float ContentAlpha => chrome != null ? chrome.ContentAlpha : 0f;
        public float VisualAlpha => chrome != null ? chrome.VisualAlpha : 0f;
        public Vector2 VisualOffset => chrome != null ? chrome.VisualOffset : Vector2.zero;
        public RectTransform CardRect => chrome != null ? chrome.Card : null;
        public bool AnyGlitchPieceVisible => chrome != null && chrome.AnyGlitchPieceVisible;

        /// <summary>Create가 만든 루트(Image·Canvas)에 팝업 내용을 짓는다.</summary>
        internal void Build(TMP_FontAsset defaultFont)
        {
            var skin = Resources.Load<MineResetTimedPopupSkin>(MineResetTimedPopupSkin.ResourcePath);
            font = skin != null && skin.font != null ? skin.font : defaultFont;
            canvas = GetComponent<Canvas>();
            // B-170: 프레임·글리치 장식은 종료 확인 창과 공용(GlitchPopupChrome).
            chrome = new GlitchPopupChrome(CardSize);
            chrome.Build(transform, GetComponent<Image>(), skin);
            contentRect = chrome.Content;

            BuildContent(skin);
            gameObject.SetActive(false);
        }

        public void SetIconResolver(Func<string, Sprite> resolver)
        {
            iconResolver = resolver;
        }

        /// <summary>
        /// 팝업을 연다. 숨겨져 있으면 등장 연출을 처음부터 재생하고, 이미 열려 있으면 내용만 갱신한다.
        /// 닫는 중이면 아무것도 하지 않는다(재열기는 컨트롤러가 닫기 완료 뒤에 처리한다).
        /// </summary>
        public bool Show(EmergencyRescueCost cost, string message)
        {
            if (state == PopupState.Closing)
            {
                return false;
            }

            Refresh(cost, message);
            if (state == PopupState.Hidden)
            {
                StartOpening();
            }
            else
            {
                BringToFront();
            }

            return true;
        }

        public void Refresh(EmergencyRescueCost cost, string message)
        {
            displayedCost = cost;
            SetMessage(string.IsNullOrWhiteSpace(message) ? DefaultMessage : message);
            table.Apply(
                EmergencyRescueCostRows.Build(cost),
                cost == null ? EmergencyRescueCostRows.BuildFooter(null) : EmergencyRescueCostRows.FreeMessage,
                iconResolver);
            footerText.text = cost == null || cost.IsFree ? string.Empty : EmergencyRescueCostRows.BuildFooter(cost);
        }

        public void SetMessage(string message)
        {
            if (messageText != null)
            {
                messageText.text = message ?? string.Empty;
            }
        }

        /// <summary>강한 글리치 종료 연출 뒤 숨기고 onClosed를 부른다. 이미 닫는 중이거나 숨겨져 있으면 무시한다.</summary>
        public bool BeginClose(Action onClosed)
        {
            if (state == PopupState.Hidden || state == PopupState.Closing)
            {
                return false;
            }

            closedCallback = onClosed;
            state = PopupState.Closing;
            clock = 0f;
            chrome.InvalidateGeometry();
            ApplyButtons();
            if (!isActiveAndEnabled)
            {
                FinishClose();
                return true;
            }

            SfxPlayer.Play(SfxId.UiPopupClose);
            chrome.Apply(EmergencyRescueTimeline.Outro(0f), 2);
            return true;
        }

        /// <summary>연출 없이 즉시 숨기고 대기 중인 닫기 콜백을 버린다. 구출 완료·전력 회복 때 쓴다.</summary>
        public void HideImmediate()
        {
            closedCallback = null;
            if (state == PopupState.Hidden && !gameObject.activeSelf)
            {
                return;
            }

            state = PopupState.Hidden;
            PopupWindowSorting.Remove(canvas);
            gameObject.SetActive(false);
        }

        public void SetInteractable(bool interactable)
        {
            externalInteractable = interactable;
            ApplyButtons();
        }

        private void OnDisable()
        {
            // 씬 전환·비활성화로 꺼져도 다음 표시가 깨끗하게 시작되도록 모두 되돌린다.
            state = PopupState.Hidden;
            closedCallback = null;
            clock = 0f;
            idleClock = 0f;
            PopupWindowSorting.Remove(canvas);
            if (chrome != null)
            {
                chrome.ResetForHidden();
            }
        }

        /// <summary>확인용: true면 Update가 시간을 진행하지 않아 테스트가 Tick으로 프레임을 직접 진행한다.</summary>
        public bool ManualTick { get; set; }

        private void Update()
        {
            if (ManualTick)
            {
                return;
            }

            Tick(Mathf.Min(Time.unscaledDeltaTime, MaxStep));
        }

        /// <summary>테스트와 Update가 같은 경로로 시간을 진행한다.</summary>
        public void Tick(float dt)
        {
            switch (state)
            {
                case PopupState.Opening:
                    clock += dt;
                    if (clock >= EmergencyRescueTimeline.IntroDuration)
                    {
                        state = PopupState.Open;
                        clock = 0f;
                        idleClock = 0f;
                        chrome.Settle();
                        ApplyButtons();
                    }
                    else
                    {
                        chrome.Apply(EmergencyRescueTimeline.Intro(clock), 1);
                    }

                    break;
                case PopupState.Open:
                    idleClock += dt;
                    chrome.ApplyIdle(idleClock);
                    break;
                case PopupState.Closing:
                    clock += dt;
                    if (clock >= EmergencyRescueTimeline.OutroDuration)
                    {
                        FinishClose();
                    }
                    else
                    {
                        chrome.Apply(EmergencyRescueTimeline.Outro(clock), 2);
                    }

                    break;
            }
        }

        private void StartOpening()
        {
            state = PopupState.Opening;
            SfxPlayer.Play(SfxId.UiPopupOpen);
            clock = 0f;
            idleClock = 0f;
            chrome.InvalidateGeometry();
            gameObject.SetActive(true);
            BringToFront();
            ApplyButtons();
            chrome.Apply(EmergencyRescueTimeline.Intro(0f), 1);
        }

        private void BringToFront()
        {
            transform.SetAsLastSibling();
            PopupWindowSorting.BringToFront(canvas);
        }

        private void FinishClose()
        {
            Action callback = closedCallback;
            closedCallback = null;
            state = PopupState.Hidden;
            PopupWindowSorting.Remove(canvas);
            gameObject.SetActive(false);
            if (callback != null)
            {
                callback();
            }
        }

        // 버튼은 등장이 끝난 열린 상태에서만 눌린다. 등장·종료 도중의 클릭은 받지 않는다.
        private void ApplyButtons()
        {
            if (rescueButton == null)
            {
                return;
            }

            var stable = state == PopupState.Open;
            rescueButton.interactable = stable && externalInteractable;
            closeButton.interactable = stable;
            rescueImage.color = rescueButton.interactable ? Color.white : DisabledTint;
            closeImage.color = closeButton.interactable ? SecondaryTint : EmergencyRescueUi.WithAlpha(SecondaryTint, 0.45f);
            rescueLabel.alpha = rescueButton.interactable ? 1f : 0.55f;
            closeLabel.alpha = closeButton.interactable ? 1f : 0.55f;
        }

        private void BuildContent(MineResetTimedPopupSkin skin)
        {
            TMP_Text title = EmergencyRescueUi.Text(
                contentRect, "Title", font, 40f, FontStyles.Bold, EmergencyRescueUi.TitleRed,
                TextAlignmentOptions.Center, new Vector2(0f, 262f), new Vector2(600f, 56f));
            title.text = Title;
            title.characterSpacing = 3f;
            title.enableAutoSizing = true;
            title.fontSizeMin = 28f;
            title.fontSizeMax = 40f;

            // 제목 양옆 붉은 마름모. 경고색은 제목과 이 장식에만 쓴다.
            for (var side = -1; side <= 1; side += 2)
            {
                Image diamond = EmergencyRescueUi.Image(
                    contentRect, side < 0 ? "WarnL" : "WarnR", null, new Vector2(side * 232f, 262f),
                    new Vector2(14f, 14f), EmergencyRescueUi.Red);
                diamond.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            }

            Image divider = EmergencyRescueUi.Image(
                contentRect, "TitleDivider", skin != null ? skin.titleDivider : null,
                new Vector2(0f, 226f), new Vector2(600f, 16.4f),
                skin != null && skin.titleDivider != null
                    ? EmergencyRescueUi.WithAlpha(new Color(1f, 0.55f, 0.5f, 1f), 0.9f)
                    : EmergencyRescueUi.WithAlpha(EmergencyRescueUi.Red, 0.6f));
            if (skin == null || skin.titleDivider == null)
            {
                divider.rectTransform.sizeDelta = new Vector2(600f, 2f);
            }

            messageText = EmergencyRescueUi.Text(
                contentRect, "Message", font, 22f, FontStyles.Normal, EmergencyRescueUi.TextMain,
                TextAlignmentOptions.Center, new Vector2(0f, 176f), new Vector2(720f, 64f));
            messageText.textWrappingMode = TextWrappingModes.Normal;
            messageText.enableAutoSizing = true;
            messageText.fontSizeMin = 16f;
            messageText.fontSizeMax = 22f;
            messageText.lineSpacing = 8f;
            messageText.overflowMode = TextOverflowModes.Truncate;

            table = new EmergencyRescueCostTable(contentRect, font);

            footerText = EmergencyRescueUi.Text(
                contentRect, "Footer", font, 19f, FontStyles.Normal, EmergencyRescueUi.TextMuted,
                TextAlignmentOptions.Center, new Vector2(0f, -176f), new Vector2(720f, 30f));

            rescueButton = CreateButton(
                skin, "RescueButton", RescueLabel, new Vector2(-120f, -250f), new Vector2(300f, 68f), 28f,
                Color.white, out rescueImage, out rescueLabel);
            closeButton = CreateButton(
                skin, "CloseButton", CloseLabel, new Vector2(180f, -250f), new Vector2(190f, 58f), 24f,
                SecondaryTint, out closeImage, out closeLabel);
            AttachButtonSfx();
        }

        /// <summary>구출은 확인음, 닫기는 BeginClose가 닫힘 소리를 내므로 호버만. 리스너를 갈아 끼운 뒤 다시 불러도 한 번만 난다.</summary>
        internal void AttachButtonSfx()
        {
            UiSfx.Attach(rescueButton, SfxId.UiConfirm, hover: true);
            UiSfx.Attach(closeButton, SfxId.None, hover: true);
        }

        private Button CreateButton(
            MineResetTimedPopupSkin skin,
            string name,
            string label,
            Vector2 position,
            Vector2 size,
            float fontSize,
            Color tint,
            out Image image,
            out TMP_Text text)
        {
            RectTransform rect = EmergencyRescueUi.Rect(contentRect, name, position, size);
            image = rect.gameObject.AddComponent<Image>();
            image.sprite = skin != null ? skin.buttonConfirm : null;
            image.color = image.sprite != null ? tint : new Color(0.12f, 0.36f, 0.34f, 1f);
            image.raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            var navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;

            Image overlay = EmergencyRescueUi.Image(
                rect, "HoverOverlay", skin != null ? skin.buttonConfirmHover : null, Vector2.zero, size,
                new Color(1f, 1f, 1f, 0f));
            EmergencyRescueUi.Stretch(overlay.rectTransform);
            rect.gameObject.AddComponent<MenuSpriteButtonSkin>().SetOverlay(overlay);

            text = EmergencyRescueUi.Text(
                rect, "Label", font, fontSize, FontStyles.Bold, Color.white,
                TextAlignmentOptions.Center, Vector2.zero, size);
            EmergencyRescueUi.Stretch(text.rectTransform);
            text.text = label;
            text.enableAutoSizing = true;
            text.fontSizeMin = 18f;
            text.fontSizeMax = fontSize;
            return button;
        }
    }
}
