#if UNITY_EDITOR || SUBTERRA_BUILD_DEVELOPMENT
using System;
using SubTerra.App.Core;
using SubTerra.App.Inventory;
using SubTerra.App.Save;
using SubTerra.App.State;
using SubTerra.App.UI;
using SubTerra.App.UI.Tutorial;
using SubTerra.Gameplay.Player;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SubTerra.App.Integration
{
    /// <summary>
    /// 개발자 디버그 터미널. Bootstrap DontDestroyOnLoad 루트 아래에 붙고 오버레이는 코드로 만든다.
    /// Ctrl+` 로 열고 닫는다. 열린 동안 debug-terminal 정지가 이동·채굴을 막는다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DeveloperDebugTerminal : MonoBehaviour
    {
        // Canvas.sortingOrder는 16비트라 32767을 넘으면 음수로 감긴다. 설정창보다 위에 둔다.
        public const int SortingOrder = PopupWindowSorting.SettingsSortOrder + 7;

        private const float PanelWidth = 980f;
        private const float PanelHeight = 640f;

        private DeveloperDebugCommandSession session;
        private DeveloperDebugCommandContext boundContext;
        private GameObject canvasRoot;
        private TMP_Text outputText;
        private TMP_Text candidateText;
        private TMP_InputField inputField;
        private ScrollRect outputScroll;
        private RectTransform outputContent;
        private RectTransform candidateRect;
        private bool isOpen;
        private bool handlingSubmit;
        private bool changingInput;
        private bool stripGrave;
        private bool hasShownGuide;

        public bool IsOpen => isOpen;
        public string Transcript => outputText != null ? outputText.text : string.Empty;
        public string CandidateText => candidateText != null ? candidateText.text : string.Empty;
        public string CurrentInput => inputField != null ? inputField.text : string.Empty;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (FindAnyObjectByType<DeveloperDebugTerminal>(FindObjectsInactive.Include) != null)
            {
                return;
            }

            var host = new GameObject("DeveloperDebugTerminal");
            host.AddComponent<DeveloperDebugTerminal>();
        }

        public void BindContext(DeveloperDebugCommandContext context)
        {
            boundContext = context;
        }

        public void Initialize()
        {
            if (canvasRoot != null)
            {
                AttachToRuntimeRoot();
                return;
            }

            if (session == null)
            {
                session = new DeveloperDebugCommandSession(DeveloperDebugCommandRegistry.Shared);
            }

            Build();
            AttachToRuntimeRoot();
        }

        public void AttachToRuntimeRoot()
        {
            var bootstrap = GameBootstrapper.Instance;
            if (bootstrap == null || bootstrap.IsDuplicateDiscarded)
            {
                if (Application.isPlaying && transform.parent == null)
                {
                    DontDestroyOnLoad(gameObject);
                }

                return;
            }

            if (transform.parent == bootstrap.transform)
            {
                return;
            }

            transform.SetParent(bootstrap.transform, false);
        }

        public void HandleShortcutChord(bool ctrlHeld, bool backquotePressed)
        {
            if (!ctrlHeld || !backquotePressed || !isActiveAndEnabled)
            {
                return;
            }

            Toggle();
        }

        public void Toggle()
        {
            if (isOpen)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        public void SetInput(string value)
        {
            if (inputField == null)
            {
                return;
            }

            inputField.text = value ?? string.Empty;
        }

        public void Submit()
        {
            if (handlingSubmit || !isOpen || inputField == null || session == null)
            {
                return;
            }

            handlingSubmit = true;
            try
            {
                if (CompleteCandidate())
                {
                    return;
                }

                ExecuteCurrent();
                FocusInput(0);
            }
            finally
            {
                handlingSubmit = false;
            }
        }

        public bool CompleteCandidate()
        {
            if (!isOpen || inputField == null || session == null || !session.TryConfirm(out var confirmed))
            {
                return false;
            }

            SetInputWithoutNotify(confirmed);
            RefreshCandidates();
            FocusInput(confirmed.Length);
            return true;
        }

        public void RecallHistory(int delta)
        {
            if (!isOpen || inputField == null || session == null
                || !session.TryRecallHistory(delta, inputField.text, out var recalled))
            {
                return;
            }

            SetInputWithoutNotify(recalled);
            RefreshCandidates();
            FocusInput(recalled.Length);
        }

        public void HandleArrowKey(int delta, bool shiftHeld)
        {
            if (!shiftHeld)
            {
                RecallHistory(delta);
                return;
            }

            if (!isOpen || inputField == null || session == null || session.CandidateCount == 0)
            {
                return;
            }

            session.Move(delta);
            RefreshCandidates();
            FocusInput(inputField.text.Length);
        }

        private void Awake()
        {
            try
            {
                Initialize();
            }
            catch (Exception exception)
            {
                Debug.LogError("[SubTerra] DeveloperDebugTerminal init failed: " + exception.GetType().Name);
                enabled = false;
            }
        }

        private void OnDestroy()
        {
            if (!isOpen)
            {
                return;
            }

            isOpen = false;
            UiPauseGate.Release(SaveRuntimeController.DebugTerminalPauseOwner);
        }

        private void Update()
        {
            AttachToRuntimeRoot();
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            var ctrl = keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed;
            if (ctrl && keyboard.backquoteKey.wasPressedThisFrame)
            {
                HandleShortcutChord(true, true);
                stripGrave = true;
                return;
            }
        }

        private void LateUpdate()
        {
            if (stripGrave)
            {
                stripGrave = false;
                StripGrave();
            }

            var keyboard = Keyboard.current;
            if (!isOpen || inputField == null || !inputField.isFocused || keyboard == null)
            {
                return;
            }

            if (keyboard.tabKey.wasPressedThisFrame)
            {
                CompleteCandidate();
            }
            else if (keyboard.upArrowKey.wasPressedThisFrame)
            {
                HandleArrowKey(-1, keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
            }
            else if (keyboard.downArrowKey.wasPressedThisFrame)
            {
                HandleArrowKey(1, keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
            }
        }

        private void Open()
        {
            if (canvasRoot == null)
            {
                return;
            }

            isOpen = true;
            UiPauseGate.Acquire(SaveRuntimeController.DebugTerminalPauseOwner);
            UiKeyboardSubmitGuard.ClearSelection();
            canvasRoot.SetActive(true);
            ApplyCanvasSort();
            if (!hasShownGuide)
            {
                hasShownGuide = true;
                Append("help 또는 도움말을 입력하면 명령어 설명을 볼 수 있습니다.\n"
                    + "↑ / ↓: 이전 / 이후 명령어 히스토리 탐색\n"
                    + "Shift + ↑ / ↓: 추천 명령어 선택\n"
                    + "Tab: 선택한 명령어 자동완성\n"
                    + "Enter: 추천 명령어 확정, 완성된 명령어 실행\n"
                    + "Ctrl + `: 터미널 열기 / 닫기");
            }

            var caret = inputField != null && inputField.text != null ? inputField.text.Length : 0;
            FocusInput(caret);
        }

        private void Close()
        {
            isOpen = false;
            if (inputField != null)
            {
                inputField.DeactivateInputField();
            }

            UiKeyboardSubmitGuard.ClearSelection();
            if (canvasRoot != null)
            {
                canvasRoot.SetActive(false);
            }

            UiPauseGate.Release(SaveRuntimeController.DebugTerminalPauseOwner);
        }

        private void ExecuteCurrent()
        {
            var line = inputField.text;
            var trimmed = string.IsNullOrWhiteSpace(line) ? string.Empty : line.Trim();
            SetInputWithoutNotify(string.Empty);
            session.NotifyTextChanged(string.Empty);
            RefreshCandidates();
            if (trimmed.Length == 0)
            {
                return;
            }

            Append("> " + trimmed);
            var result = session.Execute(trimmed, ResolveContext());
            if (!string.IsNullOrEmpty(result))
            {
                Append(result);
            }
        }

        private DeveloperDebugCommandContext ResolveContext()
        {
            if (boundContext != null)
            {
                return boundContext;
            }

            GameState state = null;
            var bootstrap = GameBootstrapper.Instance;
            if (bootstrap != null)
            {
                state = bootstrap.State;
            }

            InventoryService inventory = null;
            var runtime = SaveRuntimeController.Instance;
            if (runtime != null)
            {
                inventory = runtime.InventoryService;
            }

            return new DeveloperDebugCommandContext(
                state,
                inventory,
                () => FindAnyObjectByType<PlayerSurvivalController>(),
                () => FindAnyObjectByType<TutorialDirectorBinder>());
        }

        private void RefreshCandidates()
        {
            if (candidateText == null || session == null)
            {
                return;
            }

            candidateText.text = session.FormatCandidates();
            var height = string.IsNullOrEmpty(candidateText.text) ? 0f : candidateText.preferredHeight;
            candidateRect.sizeDelta = new Vector2(candidateRect.sizeDelta.x, height);
            candidateRect.gameObject.SetActive(height > 0f);
            var scrollRect = outputScroll.GetComponent<RectTransform>();
            scrollRect.offsetMin = new Vector2(16f, 66f + (height > 0f ? height + 10f : 0f));
            RebuildOutput();
        }

        private void Append(string line)
        {
            if (outputText == null || line == null)
            {
                return;
            }

            var current = outputText.text;
            outputText.text = string.IsNullOrEmpty(current) ? line : current + "\n" + line;
            RebuildOutput();
        }

        private void RebuildOutput()
        {
            if (outputContent == null)
            {
                return;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(outputContent);
            Canvas.ForceUpdateCanvases();
            if (outputScroll != null)
            {
                outputScroll.verticalNormalizedPosition = 0f;
            }
        }

        private void FocusInput(int caret)
        {
            if (inputField == null || EventSystem.current == null)
            {
                return;
            }

            EventSystem.current.SetSelectedGameObject(inputField.gameObject);
            inputField.ActivateInputField();
            var length = inputField.text != null ? inputField.text.Length : 0;
            if (caret < 0 || caret > length)
            {
                caret = length;
            }

            inputField.caretPosition = caret;
            inputField.stringPosition = caret;
        }

        private void StripGrave()
        {
            if (inputField == null || string.IsNullOrEmpty(inputField.text))
            {
                return;
            }

            var text = inputField.text;
            if (text[text.Length - 1] != '`')
            {
                return;
            }

            SetInputWithoutNotify(text.Substring(0, text.Length - 1));
            if (session != null)
            {
                session.NotifyTextChanged(inputField.text);
                RefreshCandidates();
            }
        }

        private void ApplyCanvasSort()
        {
            if (canvasRoot == null)
            {
                return;
            }

            var canvas = canvasRoot.GetComponent<Canvas>();
            if (canvas == null)
            {
                return;
            }

            canvas.sortingOrder = SortingOrder;
        }

        private void Build()
        {
            var font = TMP_Settings.defaultFontAsset;
            canvasRoot = new GameObject(
                "DeveloperDebugTerminalCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasRoot.transform.SetParent(transform, false);

            var canvas = canvasRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            ApplyCanvasSort();

            var scaler = canvasRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvasRoot.transform, false);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
            var panelImage = panel.GetComponent<Image>();
            panelImage.color = Color.black;
            panelImage.raycastTarget = true;

            BuildOutput(panel.transform, font);
            BuildCandidates(panel.transform, font);
            BuildInput(panel.transform, font);
            canvasRoot.SetActive(false);
        }

        private void BuildOutput(Transform panel, TMP_FontAsset font)
        {
            var scrollGo = new GameObject("OutputScroll", typeof(RectTransform), typeof(ScrollRect));
            scrollGo.transform.SetParent(panel, false);
            var scrollRect = scrollGo.GetComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = new Vector2(16f, 66f);
            scrollRect.offsetMax = new Vector2(-16f, -16f);

            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportGo.transform.SetParent(scrollGo.transform, false);
            var viewportRect = viewportGo.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            var viewportImage = viewportGo.GetComponent<Image>();
            viewportImage.color = Color.white;
            viewportImage.raycastTarget = true;
            var mask = viewportGo.GetComponent<Mask>();
            mask.showMaskGraphic = false;

            var contentGo = new GameObject(
                "Content",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            contentGo.transform.SetParent(viewportGo.transform, false);
            outputContent = contentGo.GetComponent<RectTransform>();
            outputContent.anchorMin = new Vector2(0f, 1f);
            outputContent.anchorMax = new Vector2(1f, 1f);
            outputContent.pivot = new Vector2(0.5f, 1f);
            outputContent.anchoredPosition = Vector2.zero;
            outputContent.sizeDelta = Vector2.zero;

            var layout = contentGo.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.spacing = 0f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = contentGo.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            outputText = CreateText(contentGo.transform, "OutputText", 20f, font, new Color(0.86f, 0.9f, 0.86f, 1f));
            outputText.alignment = TextAlignmentOptions.TopLeft;
            outputText.textWrappingMode = TextWrappingModes.Normal;
            outputText.overflowMode = TextOverflowModes.Overflow;
            outputText.raycastTarget = false;
            outputText.characterSpacing = 1.5f;

            outputScroll = scrollGo.GetComponent<ScrollRect>();
            outputScroll.content = outputContent;
            outputScroll.viewport = viewportRect;
            outputScroll.horizontal = false;
            outputScroll.vertical = true;
            outputScroll.movementType = ScrollRect.MovementType.Clamped;
            outputScroll.scrollSensitivity = 40f;
            outputScroll.inertia = true;
        }

        private void BuildCandidates(Transform panel, TMP_FontAsset font)
        {
            var candidateGo = new GameObject("Candidates", typeof(RectTransform));
            candidateGo.transform.SetParent(panel, false);
            var rect = candidateGo.GetComponent<RectTransform>();
            candidateRect = rect;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(16f, 66f);
            rect.offsetMax = new Vector2(-16f, 66f);

            candidateText = CreateText(candidateGo.transform, "CandidateText", 18f, font, new Color(0.75f, 0.86f, 0.72f, 1f));
            var textRect = candidateText.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8f, 0f);
            textRect.offsetMax = new Vector2(-8f, 0f);
            candidateText.alignment = TextAlignmentOptions.BottomLeft;
            candidateText.textWrappingMode = TextWrappingModes.NoWrap;
            candidateText.overflowMode = TextOverflowModes.Overflow;
            candidateText.raycastTarget = false;
            candidateText.characterSpacing = 1.5f;
        }

        private void BuildInput(Transform panel, TMP_FontAsset font)
        {
            var inputGo = new GameObject("InputLine", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            inputGo.transform.SetParent(panel, false);
            var rect = inputGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(16f, 16f);
            rect.offsetMax = new Vector2(-16f, 56f);
            var image = inputGo.GetComponent<Image>();
            image.color = new Color(0.12f, 0.12f, 0.12f, 1f);
            image.raycastTarget = true;

            var viewport = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(inputGo.transform, false);
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(8f, 4f);
            viewportRect.offsetMax = new Vector2(-8f, -4f);

            var placeholder = CreateText(
                viewport.transform,
                "Placeholder",
                20f,
                font,
                new Color(1f, 1f, 1f, 0.35f));
            Stretch(placeholder.rectTransform);
            placeholder.text = "명령";
            placeholder.fontStyle = FontStyles.Italic;
            placeholder.raycastTarget = false;

            var text = CreateText(viewport.transform, "Text", 20f, font, Color.white);
            Stretch(text.rectTransform);
            text.characterSpacing = 1.5f;

            inputField = inputGo.GetComponent<TMP_InputField>();
            inputField.textViewport = viewportRect;
            inputField.textComponent = text;
            inputField.placeholder = placeholder;
            inputField.lineType = TMP_InputField.LineType.SingleLine;
            inputField.contentType = TMP_InputField.ContentType.Standard;
            inputField.richText = false;
            inputField.onFocusSelectAll = false;
            inputField.resetOnDeActivation = false;
            inputField.onSubmit.AddListener(_ => Submit());
            inputField.onValueChanged.AddListener(OnInputChanged);
        }

        private void SetInputWithoutNotify(string value)
        {
            changingInput = true;
            try
            {
                inputField.SetTextWithoutNotify(value);
            }
            finally
            {
                changingInput = false;
            }
        }

        private void OnInputChanged(string value)
        {
            if (handlingSubmit || changingInput || session == null)
            {
                return;
            }

            session.NotifyTextChanged(value);
            RefreshCandidates();
        }

        private static TMP_Text CreateText(
            Transform parent,
            string name,
            float size,
            TMP_FontAsset font,
            Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<TextMeshProUGUI>();
            if (font != null)
            {
                text.font = font;
            }

            text.fontSize = size;
            text.color = color;
            text.richText = false;
            text.raycastTarget = false;
            text.text = string.Empty;
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
#endif
