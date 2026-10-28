using System;
using SubTerra.App.Audio;
using SubTerra.App.UI.EmergencyRescue;
using SubTerra.App.UI.HUD;
using SubTerra.App.UI.MainMenu;
using SubTerra.Shared.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.QuitConfirm
{
    /// <summary>
    /// B-170 게임 종료 확인 창. 전력 고갈 구출 창과 같은 프레임·글리치(GlitchPopupChrome)·소리를 쓴다.
    /// 등장(0.5초 끊겨 나타남) → 표시(가장자리 간헐 글리치) → 닫기(0.3초 강한 글리치). 연출 감소면 바로 나타나고 사라진다.
    /// 버튼은 등장이 끝난 뒤에만 눌리고 키보드 Submit으로는 눌리지 않는다. 시간은 unscaledDeltaTime이다.
    /// </summary>
    public sealed class QuitConfirmPopupView : MonoBehaviour
    {
        public enum PopupState
        {
            Hidden = 0,
            Opening = 1,
            Open = 2,
            Closing = 3
        }

        public const string Title = "게임 종료";
        public const string Message = "게임을 종료할까요?";
        public const string SaveNote = "진행 상황은 자동으로 저장됩니다.";
        public const string ConfirmLabel = "종료";
        public const string CancelLabel = "취소";
        public const string IconResourcePath = "UI/QuitConfirm/quit-power-icon";
        public const int SortOrder = 32_767;
        /// <summary>프레임 그림에 9-슬라이스 경계가 없어 줄이면 모서리가 찌그러지므로 구출 창 크기를 그대로 쓴다.</summary>
        public static readonly Vector2 CardSize = EmergencyRescuePopupView.CardSize;

        private const float MaxStep = 0.05f;
        private static readonly Color DisabledTint = new Color(0.5f, 0.5f, 0.5f, 0.7f);
        private static readonly Color SecondaryTint = new Color(0.62f, 0.74f, 0.84f, 0.92f);

        private TMP_FontAsset font;
        private GlitchPopupChrome chrome;
        private RectTransform contentRect;
        private Image icon;
        private TMP_Text messageText;
        private TMP_Text noteText;
        private Button confirmButton;
        private Button cancelButton;
        private Image confirmImage;
        private Image cancelImage;
        private TMP_Text confirmText;
        private TMP_Text cancelText;

        private PopupState state;
        private float clock;
        private float idleClock;
        private bool reducedMotion;
        private Action closedCallback;

        public event Action ConfirmClicked;
        public event Action CancelClicked;

        public PopupState State => state;
        public bool IsVisible => state != PopupState.Hidden;
        public bool IsClosing => state == PopupState.Closing;
        /// <summary>등장이 끝나 버튼을 받을 수 있는 상태.</summary>
        public bool IsStable => state == PopupState.Open;
        public Button ConfirmButton => confirmButton;
        public Button CancelButton => cancelButton;
        public string NoteString => noteText != null && noteText.gameObject.activeSelf ? noteText.text : string.Empty;
        public bool HasIcon => icon != null && icon.sprite != null;
        public float ContentAlpha => chrome != null ? chrome.ContentAlpha : 0f;
        public bool AnyGlitchPieceVisible => chrome != null && chrome.AnyGlitchPieceVisible;
        /// <summary>확인용: true면 Update가 시간을 진행하지 않아 테스트가 Tick으로 진행한다.</summary>
        public bool ManualTick { get; set; }

        public static QuitConfirmPopupView Create(Transform parent, TMP_FontAsset defaultFont)
        {
            var root = new GameObject(
                "QuitConfirmPopup",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Canvas),
                typeof(GraphicRaycaster));
            root.transform.SetParent(parent, false);
            EmergencyRescueUi.Stretch(root.GetComponent<RectTransform>());
            // 설정 창(32_760)과 모든 팝업보다 위.
            var canvas = root.GetComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = SortOrder;
            var view = root.AddComponent<QuitConfirmPopupView>();
            view.Build(defaultFont);
            return view;
        }

        private void Build(TMP_FontAsset defaultFont)
        {
            var skin = Resources.Load<MineResetTimedPopupSkin>(MineResetTimedPopupSkin.ResourcePath);
            font = skin != null && skin.font != null ? skin.font : defaultFont;
            // 딤 면은 뒤쪽 클릭만 막고, 알파는 연출이 정한다.
            var blocker = GetComponent<Image>();
            blocker.raycastTarget = true;
            chrome = new GlitchPopupChrome(CardSize);
            chrome.Build(transform, blocker, skin);
            contentRect = chrome.Content;
            BuildContent(skin);
            gameObject.SetActive(false);
        }

        /// <summary>창을 연다. 게임 중이면 자동 저장 안내 줄을 보인다. 이미 보이면 무시한다.</summary>
        public bool Show(bool inGame, bool reduced)
        {
            if (state != PopupState.Hidden) return false;
            reducedMotion = reduced;
            if (noteText != null) noteText.gameObject.SetActive(inGame);
            LayoutMessage(inGame);
            state = PopupState.Opening;
            clock = 0f;
            idleClock = 0f;
            chrome.InvalidateGeometry();
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            SfxPlayer.Play(SfxId.UiPopupOpen);
            UiKeyboardSubmitGuard.ClearSelection();
            if (reduced)
            {
                EnterOpen();
            }
            else
            {
                ApplyButtons();
                chrome.Apply(EmergencyRescueTimeline.Intro(0f), 1);
            }

            return true;
        }

        /// <summary>닫힘 글리치 뒤 숨기고 onClosed를 부른다. 이미 닫는 중이거나 숨겨져 있으면 무시한다.</summary>
        public bool BeginClose(Action onClosed)
        {
            if (state == PopupState.Hidden || state == PopupState.Closing) return false;
            closedCallback = onClosed;
            state = PopupState.Closing;
            clock = 0f;
            chrome.InvalidateGeometry();
            ApplyButtons();
            SfxPlayer.Play(SfxId.UiPopupClose);
            if (reducedMotion || !isActiveAndEnabled)
            {
                FinishClose();
                return true;
            }

            chrome.Apply(EmergencyRescueTimeline.Outro(0f), 2);
            return true;
        }

        /// <summary>"종료"를 누른 뒤: 연출 없이 바로 숨긴다(소리 없음).</summary>
        public void HideImmediate()
        {
            closedCallback = null;
            state = PopupState.Hidden;
            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            state = PopupState.Hidden;
            closedCallback = null;
            clock = 0f;
            idleClock = 0f;
            if (chrome != null) chrome.ResetForHidden();
        }

        private void Update()
        {
            if (ManualTick) return;
            Tick(Mathf.Min(Time.unscaledDeltaTime, MaxStep));
        }

        public void Tick(float dt)
        {
            switch (state)
            {
                case PopupState.Opening:
                    clock += dt;
                    if (clock >= EmergencyRescueTimeline.IntroDuration) EnterOpen();
                    else chrome.Apply(EmergencyRescueTimeline.Intro(clock), 1);
                    break;
                case PopupState.Open:
                    if (reducedMotion) break;
                    idleClock += dt;
                    chrome.ApplyIdle(idleClock);
                    break;
                case PopupState.Closing:
                    clock += dt;
                    if (clock >= EmergencyRescueTimeline.OutroDuration) FinishClose();
                    else chrome.Apply(EmergencyRescueTimeline.Outro(clock), 2);
                    break;
            }
        }

        private void EnterOpen()
        {
            state = PopupState.Open;
            clock = 0f;
            idleClock = 0f;
            chrome.Settle();
            ApplyButtons();
        }

        private void FinishClose()
        {
            var callback = closedCallback;
            closedCallback = null;
            state = PopupState.Hidden;
            gameObject.SetActive(false);
            if (callback != null) callback();
        }

        // 버튼은 등장이 끝난 열린 상태에서만 눌린다.
        private void ApplyButtons()
        {
            if (confirmButton == null) return;
            var stable = state == PopupState.Open;
            confirmButton.interactable = stable;
            cancelButton.interactable = stable;
            confirmImage.color = stable ? Color.white : DisabledTint;
            cancelImage.color = stable ? SecondaryTint : EmergencyRescueUi.WithAlpha(SecondaryTint, 0.45f);
            confirmText.alpha = stable ? 1f : 0.55f;
            cancelText.alpha = stable ? 1f : 0.55f;
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

            // 구출 창과 같은 붉은 마름모. 제목이 짧아 간격만 좁힌다.
            for (var side = -1; side <= 1; side += 2)
            {
                Image diamond = EmergencyRescueUi.Image(
                    contentRect, side < 0 ? "WarnL" : "WarnR", null, new Vector2(side * 150f, 262f),
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

            // 본문이 짧아 비는 가운데를 전원 아이콘으로 채운다. 그림이 없으면 글만 가운데로 모은다.
            var iconSprite = Resources.Load<Sprite>(IconResourcePath);
            icon = EmergencyRescueUi.Image(contentRect, "PowerIcon", iconSprite, new Vector2(0f, 80f), new Vector2(190f, 190f), Color.white);
            icon.preserveAspect = true;
            icon.gameObject.SetActive(iconSprite != null);

            messageText = EmergencyRescueUi.Text(
                contentRect, "Message", font, 30f, FontStyles.Bold, EmergencyRescueUi.TextMain,
                TextAlignmentOptions.Center, Vector2.zero, new Vector2(720f, 48f));
            messageText.text = Message;

            noteText = EmergencyRescueUi.Text(
                contentRect, "SaveNote", font, 21f, FontStyles.Normal, EmergencyRescueUi.TextMuted,
                TextAlignmentOptions.Center, Vector2.zero, new Vector2(720f, 34f));
            noteText.text = SaveNote;

            confirmButton = CreateButton(
                skin, "ConfirmButton", ConfirmLabel, new Vector2(-120f, -250f), new Vector2(300f, 68f), 28f,
                Color.white, out confirmImage, out confirmText);
            cancelButton = CreateButton(
                skin, "CancelButton", CancelLabel, new Vector2(180f, -250f), new Vector2(190f, 58f), 24f,
                SecondaryTint, out cancelImage, out cancelText);
            confirmButton.onClick.AddListener(() => ConfirmClicked?.Invoke());
            cancelButton.onClick.AddListener(() => CancelClicked?.Invoke());
            // 종료는 확인음, 취소는 BeginClose가 닫힘 소리를 내므로 호버만.
            UiSfx.Attach(confirmButton, SfxId.UiConfirm, hover: true);
            UiSfx.Attach(cancelButton, SfxId.None, hover: true);
            LayoutMessage(true);
        }

        private void LayoutMessage(bool inGame)
        {
            var hasIcon = HasIcon;
            // 아이콘 아래와 버튼 위 사이 가운데. 안내 줄이 없으면(메인 화면) 본문만 그 가운데로.
            var messageY = hasIcon ? (inGame ? -72f : -100f) : inGame ? 36f : 10f;
            if (messageText != null) messageText.rectTransform.anchoredPosition = new Vector2(0f, messageY);
            if (noteText != null) noteText.rectTransform.anchoredPosition = new Vector2(0f, messageY - 50f);
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
            // Enter·Space(Submit)로 "종료"가 눌리지 않게 선택·이동을 막고 클릭 뒤 선택도 지운다.
            UiKeyboardSubmitGuard.ConfigurePointerPreferredButton(button);

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
