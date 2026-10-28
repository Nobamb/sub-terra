using SubTerra.App.Audio;
using SubTerra.App.UI.ControlTerminal;
using SubTerra.App.UI.Guide;
using SubTerra.App.UI.Sell;
using SubTerra.Shared;
using SubTerra.Shared.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.MainMenu
{
    /// <summary>
    /// 기존 설정 패널 안에 생성되는 조작 방식 선택 창('조작 단말').
    /// 선택·이전/다음 순환·즉시 저장·돌아가기 계약은 여기서 그대로 지키고, 표시 연출은 ControlTerminalView가 맡는다.
    /// </summary>
    public sealed class ControlSchemePanel : MonoBehaviour
    {
        private static readonly Color ButtonFill = new Color(0.04f, 0.12f, 0.17f, 1f);
        private static readonly Color ButtonHover = new Color(0.12f, 0.36f, 0.42f, 1f);
        private static readonly Color ButtonPressed = new Color(0.03f, 0.2f, 0.26f, 1f);
        private static readonly Color ButtonText = new Color(0.86f, 1f, 0.99f, 1f);

        private GameObject overlay;
        private RectTransform overlayRect;
        private RectTransform cardRect;
        private TMP_FontAsset font;
        private ControlTerminalView terminal;
        private bool isOpen;
        public ControlScheme Selected { get; private set; }
        public bool IsOpen => isOpen && overlay != null && overlay.activeSelf;
        public ControlTerminalView Terminal => terminal;
        /// <summary>확인용: true면 에디터 모드에서도 플레이 중처럼 등장·퇴장 연출 경로를 탄다.</summary>
        public bool ForceMotionForTests { get; set; }
        private bool CanPlayMotion => (Application.isPlaying || ForceMotionForTests) && isActiveAndEnabled;

        public static ControlSchemePanel Attach(GameObject settingsRoot)
        {
            if (settingsRoot == null) return null;
            var panel = settingsRoot.GetComponent<ControlSchemePanel>();
            if (panel != null) return panel;
            panel = settingsRoot.AddComponent<ControlSchemePanel>();
            try
            {
                panel.Build();
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception, settingsRoot);
            }
            return panel;
        }

        private void OnEnable() => ControlPreferences.IsSettingsOpen = true;
        private void OnDisable()
        {
            ControlPreferences.IsSettingsOpen = false;
            Close();
            // 비활성화되면 연출을 기다리지 않고 즉시 정리한다.
            HideNow();
        }

        private void Update()
        {
            if (terminal == null || overlay == null || !overlay.activeSelf) return;
            terminal.Tick(Time.unscaledDeltaTime);
            if (!isOpen && terminal.IsHidden) overlay.SetActive(false);
        }

        public void SetDraft(ControlScheme scheme)
        {
            Selected = ControlPreferences.FromIndex((int)scheme);
            Refresh();
        }

        public void Cycle(int delta)
        {
            var previous = Selected;
            Selected = ControlPreferences.Cycle(Selected, delta);
            Refresh();
            if (IsOpen && Selected != previous) SfxPlayer.Play(SfxId.KeysPageTurn);
            // 설정 창의 적용을 기다리지 않고 즉시 저장한다.
            SettingsRuntimeApplier.SaveControlScheme(Selected);
        }

        public void Open()
        {
            if (overlay == null) return;
            isOpen = true;
            terminal.ResetImmediate();
            overlay.SetActive(true);
            overlay.transform.SetAsLastSibling();
            FitCard();
            Refresh(false);
            if (CanPlayMotion)
            {
                // 홀로그램 펼침이 t=0 프레임 조립과 함께 나서 키캡 소리(0.42초~)보다 먼저 난다.
                SfxPlayer.Play(SfxId.KeysHoloOpen);
                terminal.PlayIntro();
            }
            else terminal.ShowSettled();
            UiKeyboardSubmitGuard.ClearSelection();
        }

        public bool Close()
        {
            if (!IsOpen) return false;
            isOpen = false;
            // 저장은 퇴장 연출과 무관하게 즉시 한다.
            SettingsRuntimeApplier.SaveControlScheme(Selected);
            UiKeyboardSubmitGuard.ClearSelection();
            if (CanPlayMotion && terminal != null)
            {
                SfxPlayer.Play(SfxId.UiPopupClose);
                terminal.PlayOutro();
            }
            else HideNow();
            return true;
        }

        public static ControlScheme PeekSelected(GameObject settingsRoot, ControlScheme fallback)
        {
            if (settingsRoot == null) return fallback;
            var panel = settingsRoot.GetComponent<ControlSchemePanel>();
            if (panel == null) panel = settingsRoot.GetComponentInChildren<ControlSchemePanel>(true);
            return panel != null ? panel.Selected : fallback;
        }

        private void HideNow()
        {
            if (terminal != null) terminal.ResetImmediate();
            if (overlay != null) overlay.SetActive(false);
        }

        private void FitCard()
        {
            if (terminal == null || overlayRect == null) return;
            terminal.FitTo(overlayRect.rect.size);
        }

        private void Build()
        {
            var sample = GetComponentInChildren<TMP_Text>(true);
            font = sample != null ? sample.font : TMP_Settings.defaultFontAsset;
            var authoredButton = transform.Find("SettingsCard/ChangeControls");
            if (authoredButton != null)
                authoredButton.GetComponent<Button>().onClick.AddListener(Open);
            else
                SimpleButton(transform, "ChangeControls", "키 조작 변경", 0, -366, 280, 40, Open);
            var backdrop = Rect(transform, "ControlSchemeOverlay", 0, 0, 0, 0);
            backdrop.anchorMin = Vector2.zero;
            backdrop.anchorMax = Vector2.one;
            backdrop.offsetMin = backdrop.offsetMax = Vector2.zero;
            backdrop.gameObject.AddComponent<Image>().color = new Color(0.005f, 0.012f, 0.02f, 0.96f);
            overlay = backdrop.gameObject;
            overlayRect = backdrop;
            Selected = ControlPreferences.FromIndex((int)ControlPreferences.Scheme);

            cardRect = Rect(backdrop, "KeyboardCard", 0, 0, ControlTerminalLayout.CardWidth, ControlTerminalLayout.CardHeight);
            var cardImage = cardRect.gameObject.AddComponent<Image>();
            cardImage.sprite = ResourceSellArt.ChamferFill();
            cardImage.type = Image.Type.Sliced;
            terminal = new ControlTerminalView(cardRect, cardImage, font, GuideSprites.Load(null));

            var chrome = terminal.Chrome;
            var arrowNormal = ControlTerminalArt.ArrowNormal();
            var arrowHover = ControlTerminalArt.ArrowHover();
            Button(chrome, "PreviousScheme", null, ControlTerminalLayout.PrevButtonX, ControlTerminalLayout.PlateCenter.y, 56, 88, () => Cycle(-1), 30f, arrowNormal, arrowHover, ControlTerminalArt.Chevron(false));
            Button(chrome, "NextScheme", null, ControlTerminalLayout.NextButtonX, ControlTerminalLayout.PlateCenter.y, 56, 88, () => Cycle(1), 30f, arrowNormal, arrowHover, ControlTerminalArt.Chevron(true));
            Button(chrome, "CloseControls", "선택 후 돌아가기", 0, -296, 330, 64, () => Close(), 22f,
                ControlTerminalArt.ButtonNormal(), ControlTerminalArt.ButtonHover());
            Button(chrome, "CloseControlsX", null, ControlTerminalLayout.CardWidth * 0.5f - 64f, ControlTerminalLayout.CardHeight * 0.5f - 64f, 56, 56, () => Close(), 20f,
                ControlTerminalArt.ButtonNormal(), ControlTerminalArt.ButtonHover(), ControlTerminalArt.CloseIcon());
            overlay.SetActive(false);
            Refresh(false);
        }

        private void Refresh(bool animate = true)
        {
            if (terminal == null) return;
            terminal.SetScheme(Selected, animate && IsOpen && !terminal.IsHidden);
        }

        private static RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        /// <summary>설정 창 안의 기존 단순 버튼(키 조작 변경 진입).</summary>
        private void SimpleButton(Transform parent, string name, string text, float x, float y, float width, float height,
            UnityEngine.Events.UnityAction action)
        {
            var rect = Rect(parent, name, x, y, width, height);
            var background = rect.gameObject.AddComponent<Image>();
            background.color = new Color(0.10f, 0.28f, 0.38f);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(action);
            UiKeyboardSubmitGuard.ConfigurePointerPreferredButton(button);
            var label = Rect(rect, "Label", 0, 0, width - 8, height - 4).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.text = text;
            label.fontSize = 22;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;
        }

        /// <summary>조작 단말과 같은 프레임 언어(각진 면·청록 테두리)의 버튼. 클릭 영역과 탐색 정책은 기존과 같다.</summary>
        private void Button(Transform parent, string name, string text, float x, float y, float width, float height,
            UnityEngine.Events.UnityAction action, float fontSize, Sprite normalArt = null, Sprite hoverArt = null, Sprite icon = null)
        {
            var rect = Rect(parent, name, x, y, width, height);
            var background = rect.gameObject.AddComponent<Image>();
            var button = rect.gameObject.AddComponent<ControlTerminalButton>();
            button.targetGraphic = background;
            button.onClick.AddListener(action);
            UiKeyboardSubmitGuard.ConfigurePointerPreferredButton(button);

            if (normalArt != null)
            {
                background.sprite = normalArt;
                background.type = Image.Type.Sliced;
                background.pixelsPerUnitMultiplier = ControlTerminalArt.SliceMultiplier(normalArt, ControlTerminalArt.ButtonCornerPixels);
                var hoverRect = Rect(rect, "Hover", 0, 0, width, height);
                var hover = hoverRect.gameObject.AddComponent<UnityEngine.UI.Image>();
                hover.sprite = hoverArt != null ? hoverArt : normalArt;
                hover.type = UnityEngine.UI.Image.Type.Sliced;
                hover.pixelsPerUnitMultiplier = background.pixelsPerUnitMultiplier;
                hover.raycastTarget = false;
                button.BindHover(hover);
            }
            else
            {
                background.sprite = ResourceSellArt.ChamferFill();
                background.type = Image.Type.Sliced;
                var colors = button.colors;
                colors.normalColor = ButtonFill;
                colors.highlightedColor = ButtonHover;
                colors.selectedColor = ButtonHover;
                colors.pressedColor = ButtonPressed;
                colors.colorMultiplier = 1f;
                colors.fadeDuration = ControlTerminalButton.HoverSeconds;
                button.colors = colors;

                var line = Rect(rect, "Outline", 0, 0, 0, 0);
                line.anchorMin = Vector2.zero;
                line.anchorMax = Vector2.one;
                line.offsetMin = line.offsetMax = Vector2.zero;
                var outline = line.gameObject.AddComponent<Image>();
                outline.sprite = ResourceSellArt.ChamferOutline();
                outline.type = Image.Type.Sliced;
                outline.color = new Color(0.25f, 0.95f, 0.92f, 0.7f);
                outline.raycastTarget = false;
            }

            if (icon != null)
            {
                var closeIcon = name == "CloseControlsX";
                var image = Rect(rect, closeIcon ? "CloseIcon" : "ArrowIcon", 0, 0, closeIcon ? 28 : 24, closeIcon ? 28 : 36).gameObject.AddComponent<UnityEngine.UI.Image>();
                image.sprite = icon;
                image.preserveAspect = true;
                image.raycastTarget = false;
            }
            else
            {
                var label = Rect(rect, "Label", 0, 0, width - 8, height - 4).gameObject.AddComponent<TextMeshProUGUI>();
                label.font = font;
                label.text = text;
                label.fontSize = fontSize;
                label.alignment = TextAlignmentOptions.Center;
                label.color = ButtonText;
                label.raycastTarget = false;
            }
            var relay = rect.gameObject.AddComponent<ControlTerminalFocusRelay>();
            relay.Bind(terminal);
        }
    }
}
