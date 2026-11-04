using System;
using SubTerra.App.Audio;
using SubTerra.App.Drone;
using SubTerra.App.Drone.Dialogue;
using SubTerra.Shared;
using SubTerra.Shared.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.Drone
{
    /// <summary>드론을 따라가되 어둠보다 앞선 별도 Overlay Canvas에 대사를 표시한다.</summary>
    public sealed class DroneDialogueSocket : MonoBehaviour,
        IDroneDialogueView,
        IDroneOperationalStateView
    {
        public const int OverlaySortingOrder = 30_000;

        // B-181 ★ 말풍선 비프. PM이 숫자만 바꿔 끼운다.
        public const SfxId DroneBubbleSfx = SfxId.WorldFacilityTagBeep;
        public const SfxId DroneUrgentBubbleSfx = SfxId.DroneBubbleUrgent;
        public const SfxId DroneSeriousUrgentBubbleSfx = SfxId.DroneBubbleUrgentSerious;
        public const float DroneUrgentBubblePitch = 1f;
        public const float DronePriorityBubbleGap = 0.3f;
        /// <summary>시설 이름표 비프보다 높게.</summary>
        public const float DroneBubblePitch = 1.35f;
        /// <summary>드론 스캔 핑이 이 시간(실제 초) 안에 났으면 이번 비프는 뺀다.</summary>
        public const float DroneBubblePingGap = 0.3f;
        /// <summary>마지막 비프에서 이 시간(실제 초) 안이면 이번 비프는 뺀다.</summary>
        public const float DroneBubbleMinGap = 1.5f;
        /// <summary>씬 시작·바인딩(불러오기·이어하기) 직후 이 시간(실제 초) 동안은 비프를 내지 않는다.</summary>
        public const float DroneBubbleStartupQuiet = 1.0f;

        [SerializeField] private Transform anchor;
        [SerializeField] private RectTransform visualRoot;
        [SerializeField] private Canvas worldCanvas;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 0.35f, 0f);
        [SerializeField] private Vector2 viewportPadding = new Vector2(0.08f, 0.12f);
        [SerializeField, Min(0.1f)] private float regularVisibleSeconds = 4f;
        [SerializeField, Min(0.1f)] private float urgentVisibleSeconds = 6f;

        private bool boundVisible;
        private bool hasDialogue;
        private float visibleUntil;
        private RectTransform overlayRoot;
        private Canvas overlayCanvas;
        private Vector3 initialWorldScale;
        private bool bubbleThemeApplied;
        private RectTransform terminalPanel;
        private Image terminalSignalLine;
        private TextMeshProUGUI terminalTitle;
        private TextMeshProUGUI terminalStatus;
        private DroneOperationalState operationalState = DroneOperationalState.Idle;

        // B-181: 비프 생략 판정. 첫 대사(초기화)는 무음, 이후는 간격·정지·핑 겹침을 본다.
        private bool firstDialoguePending = true;
        private bool hasBeeped;
        private float lastBeepAt;
        private float quietUntil = float.NegativeInfinity;
        private Func<float> sfxClock = DefaultSfxClock;
        private Func<SfxId, float> secondsSinceSfx = DefaultSecondsSinceSfx;
        private Func<bool> isSfxPaused = DefaultSfxPaused;

        private static readonly Color TerminalBackground = new Color(0.012f, 0.035f, 0.055f, 0.975f);
        private static readonly Color TerminalHeader = new Color(0.018f, 0.12f, 0.16f, 0.99f);
        private static readonly Color AccentCyan = new Color(0.18f, 0.84f, 0.92f, 1f);
        private static readonly Color AlertAmber = new Color(1f, 0.66f, 0.16f, 1f);

        public bool IsShowing => canvasGroup != null && canvasGroup.alpha > 0f;

        private void Awake()
        {
            BeginStartupQuiet();
            EnsureOverlayCanvas();
            ApplyNonBlockingPresentation();
            SetCanvasVisible(false);
        }

        private void LateUpdate()
        {
            if (IsShowing && Time.unscaledTime >= visibleUntil)
            {
                SetCanvasVisible(false);
                return;
            }

            if (boundVisible && hasDialogue)
            {
                RefreshPosition();
            }
        }

        public void SetDialogue(DroneDialogueResult dialogue)
        {
            if (dialogue == null || dialogue.IsSuppressed || dialogueText == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(dialogue.Text))
            {
                return;
            }

            var nextText = "> " + dialogue.Text;
            var sameText = IsShowing && hasDialogue && string.Equals(dialogueText.text, nextText, StringComparison.Ordinal);
            var wasBoundVisible = boundVisible;
            dialogueText.text = nextText;
            hasDialogue = true;
            // 바인딩 전이라도 대사가 오면 표시 가능하게 둔다(Bind SetVisible 레이스 방지).
            if (!boundVisible)
            {
                boundVisible = true;
            }

            visibleUntil = Time.unscaledTime
                + (dialogue.IsUrgent ? urgentVisibleSeconds : regularVisibleSeconds);
            ApplyNonBlockingPresentation();
            if (dialogue.IsUrgent)
            {
                operationalState = DroneOperationalState.Priority;
            }

            ApplyTerminalTheme();
            ApplyTerminalState(operationalState);
            TryPlayBubbleSfx(dialogue, sameText, wasBoundVisible);
            SetCanvasVisible(true);
            RefreshPosition();
        }

        public void SetVisible(bool visible)
        {
            if (visible && !boundVisible)
            {
                // 바인딩(씬 시작·불러오기·이어하기) 직후 첫 대사와 잠깐의 시간은 초기화로 보고 무음.
                BeginStartupQuiet();
            }

            boundVisible = visible;
            SetCanvasVisible(visible && hasDialogue && Time.unscaledTime < visibleUntil);
        }

        public void SetOperationalState(DroneOperationalState state)
        {
            operationalState = state;
            if (bubbleThemeApplied)
            {
                ApplyTerminalState(state);
            }
        }

        public void RefreshPosition()
        {
            if (anchor == null || visualRoot == null)
            {
                return;
            }

            var desired = anchor.position + worldOffset;
            var camera = worldCamera != null ? worldCamera : Camera.main;
            if (camera == null)
            {
                return;
            }

            var viewport = camera.WorldToViewportPoint(desired);
            if (viewport.z > 0f)
            {
                viewport.x = Mathf.Clamp(
                    viewport.x,
                    viewportPadding.x,
                    1f - viewportPadding.x);
                viewport.y = Mathf.Clamp(
                    viewport.y,
                    viewportPadding.y,
                    1f - viewportPadding.y);
                visualRoot.position = new Vector3(
                    viewport.x * Screen.width,
                    viewport.y * Screen.height,
                    0f);
                ApplyScreenScale(camera, desired);
            }
        }

        /// <returns>비프를 냈으면 true. 생략한 회차는 늦춰 내지 않는다.</returns>
        private bool TryPlayBubbleSfx(DroneDialogueResult dialogue, bool sameText, bool wasBoundVisible)
        {
            var first = firstDialoguePending;
            firstDialoguePending = false;
            if (first || sameText || !wasBoundVisible)
            {
                return false;
            }

            var now = sfxClock();
            if (now < quietUntil || isSfxPaused())
            {
                return false;
            }

            if (secondsSinceSfx(SfxId.DroneScanPing) < DroneBubblePingGap)
            {
                return false;
            }

            if (dialogue.IsUrgent && secondsSinceSfx(SfxId.DronePriorityAlert) < DronePriorityBubbleGap)
                return false;

            if (hasBeeped && now - lastBeepAt < DroneBubbleMinGap)
            {
                return false;
            }

            hasBeeped = true;
            lastBeepAt = now;
            var id = !dialogue.IsUrgent ? DroneBubbleSfx
                : DroneUrgentBubbleRule.IsSerious(dialogue) ? DroneSeriousUrgentBubbleSfx : DroneUrgentBubbleSfx;
            SfxPlayer.Play(id, dialogue.IsUrgent ? DroneUrgentBubblePitch : DroneBubblePitch);
            return true;
        }

        private void BeginStartupQuiet()
        {
            firstDialoguePending = true;
            quietUntil = sfxClock() + DroneBubbleStartupQuiet;
        }

        /// <summary>테스트용. 시계·소리 경과·정지 판정을 주입한다. null이면 실제 값을 쓴다.</summary>
        public void ConfigureSfxForTests(Func<float> clock, Func<SfxId, float> secondsSince, Func<bool> paused)
        {
            sfxClock = clock ?? DefaultSfxClock;
            secondsSinceSfx = secondsSince ?? DefaultSecondsSinceSfx;
            isSfxPaused = paused ?? DefaultSfxPaused;
        }

        private static float DefaultSfxClock() => Time.realtimeSinceStartup;

        private static float DefaultSecondsSinceSfx(SfxId id)
        {
            var player = SfxPlayer.Instance;
            return player != null ? player.SecondsSinceLastPlayed(id) : float.PositiveInfinity;
        }

        private static bool DefaultSfxPaused() => UiPauseGate.IsHeld || ControlPreferences.IsSettingsOpen;

        public bool HasRequiredReferences()
        {
            return anchor != null
                && visualRoot != null
                && worldCanvas != null
                && canvasGroup != null
                && dialogueText != null
                && !canvasGroup.interactable
                && !canvasGroup.blocksRaycasts;
        }

        private void ApplyNonBlockingPresentation()
        {
            EnsureOverlayCanvas();
            if (worldCanvas != null)
            {
                // 별도 Overlay Canvas 안에서 어둠보다 앞, 위험 경고·모달보다 뒤에 그린다.
                worldCanvas.overrideSorting = true;
                worldCanvas.sortingOrder = OverlaySortingOrder;
            }

            if (canvasGroup != null)
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            if (dialogueText != null)
            {
                dialogueText.raycastTarget = false;
            }
        }

        /// <summary>
        /// 월드 대사를 TAB 상세창과 같은 AI 터미널 패널로 표현한다.
        /// 프리팹의 대사·추적 참조는 유지하고, 표시 요소만 런타임에 보강한다.
        /// </summary>
        private void ApplyTerminalTheme()
        {
            if (bubbleThemeApplied || visualRoot == null || dialogueText == null)
            {
                return;
            }

            terminalPanel = FindTerminalPanel();
            var background = terminalPanel.GetComponent<Image>();
            if (background == null)
            {
                background = terminalPanel.gameObject.AddComponent<Image>();
            }

            background.color = TerminalBackground;
            background.raycastTarget = false;

            var outline = terminalPanel.GetComponent<Outline>();
            if (outline == null)
            {
                outline = terminalPanel.gameObject.AddComponent<Outline>();
            }

            outline.effectColor = new Color(AccentCyan.r, AccentCyan.g, AccentCyan.b, 0.72f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            outline.useGraphicAlpha = true;

            var shadow = terminalPanel.GetComponent<Shadow>();
            if (shadow == null)
            {
                shadow = terminalPanel.gameObject.AddComponent<Shadow>();
            }

            shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
            shadow.effectDistance = new Vector2(3f, -4f);
            shadow.useGraphicAlpha = true;

            CreateTerminalFrame();
            StyleDialogueText();
            bubbleThemeApplied = true;
            ApplyTerminalState(operationalState);
        }

        private RectTransform FindTerminalPanel()
        {
            var panel = visualRoot.Find("VisualRoot") as RectTransform;
            return panel != null ? panel : visualRoot;
        }

        private void CreateTerminalFrame()
        {
            terminalSignalLine = FindOrCreateImage("SignalLine", terminalPanel);
            SetAnchors(terminalSignalLine.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(10f, -3f), new Vector2(-10f, 0f));
            terminalSignalLine.color = AccentCyan;

            var terminalHeader = FindOrCreateImage("TerminalHeader", terminalPanel);
            SetAnchors(terminalHeader.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -30f), Vector2.zero);
            terminalHeader.color = TerminalHeader;

            terminalTitle = FindOrCreateText("Title", terminalHeader.rectTransform);
            terminalTitle.text = "DIGGER-BOT  //  AI LINK";
            terminalTitle.fontSize = 13f;
            terminalTitle.fontStyle = FontStyles.Bold;
            terminalTitle.color = AccentCyan;
            terminalTitle.alignment = TextAlignmentOptions.MidlineLeft;
            SetAnchors(terminalTitle.rectTransform, Vector2.zero, new Vector2(0.66f, 1f),
                new Vector2(14f, 0f), Vector2.zero);

            terminalStatus = FindOrCreateText("Status", terminalHeader.rectTransform);
            terminalStatus.fontSize = 11f;
            terminalStatus.fontStyle = FontStyles.Bold;
            terminalStatus.alignment = TextAlignmentOptions.MidlineRight;
            SetAnchors(terminalStatus.rectTransform, new Vector2(0.68f, 0f), Vector2.one,
                Vector2.zero, new Vector2(-14f, 0f));

            var terminalFooter = FindOrCreateText("TerminalFooter", terminalPanel);
            terminalFooter.text = "ANALYSIS STREAM  //  RX-01";
            terminalFooter.fontSize = 10f;
            terminalFooter.fontStyle = FontStyles.Bold;
            terminalFooter.color = new Color(0.42f, 0.72f, 0.78f, 0.92f);
            terminalFooter.alignment = TextAlignmentOptions.MidlineLeft;
            SetAnchors(terminalFooter.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(14f, 4f), new Vector2(-14f, 20f));

            var terminalTail = FindOrCreateImage("SignalPointer", terminalPanel);
            terminalTail.rectTransform.anchorMin = new Vector2(0.2f, 0f);
            terminalTail.rectTransform.anchorMax = new Vector2(0.2f, 0f);
            terminalTail.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            terminalTail.rectTransform.anchoredPosition = new Vector2(0f, -6f);
            terminalTail.rectTransform.sizeDelta = new Vector2(12f, 12f);
            terminalTail.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            terminalTail.color = TerminalBackground;
            terminalTail.transform.SetAsFirstSibling();
        }

        private void StyleDialogueText()
        {
            dialogueText.fontSize = 20f;
            dialogueText.fontStyle = FontStyles.Normal;
            dialogueText.color = new Color(0.92f, 0.98f, 1f, 1f);
            dialogueText.alignment = TextAlignmentOptions.TopLeft;
            dialogueText.textWrappingMode = TextWrappingModes.Normal;
            dialogueText.raycastTarget = false;
            SetAnchors(dialogueText.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(16f, 23f), new Vector2(-16f, -36f));
        }

        private void ApplyTerminalState(DroneOperationalState state)
        {
            var accent = state == DroneOperationalState.Priority ? AlertAmber : AccentCyan;
            if (terminalSignalLine != null)
            {
                terminalSignalLine.color = accent;
            }

            if (terminalTitle != null)
            {
                terminalTitle.color = accent;
                terminalTitle.text = state == DroneOperationalState.Priority
                    ? "DIGGER-BOT  //  PRIORITY LINK"
                    : state == DroneOperationalState.Scanning
                        ? "DIGGER-BOT  //  SCAN LINK"
                        : "DIGGER-BOT  //  AI LINK";
            }

            if (terminalStatus != null)
            {
                terminalStatus.color = accent;
                terminalStatus.text = state == DroneOperationalState.Priority
                    ? "PRIORITY"
                    : state == DroneOperationalState.Scanning
                        ? "SCANNING"
                        : "IDLE";
            }
        }

        private Image FindOrCreateImage(string name, RectTransform parent)
        {
            var existing = parent.Find(name) as RectTransform;
            if (existing != null && existing.TryGetComponent<Image>(out var existingImage))
            {
                existingImage.raycastTarget = false;
                return existingImage;
            }

            var element = new GameObject(name, typeof(RectTransform), typeof(Image));
            element.transform.SetParent(parent, false);
            var image = element.GetComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        private TextMeshProUGUI FindOrCreateText(string name, RectTransform parent)
        {
            var existing = parent.Find(name)?.GetComponent<TextMeshProUGUI>();
            if (existing != null)
            {
                CopyTextStyle(existing);
                return existing;
            }

            var element = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            element.transform.SetParent(parent, false);
            var text = element.GetComponent<TextMeshProUGUI>();
            CopyTextStyle(text);
            return text;
        }

        private void CopyTextStyle(TextMeshProUGUI target)
        {
            target.font = dialogueText.font;
            target.fontSharedMaterial = dialogueText.fontSharedMaterial;
            target.raycastTarget = false;
        }

        private static void SetAnchors(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private void SetCanvasVisible(bool visible)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
            }
        }

        private void EnsureOverlayCanvas()
        {
            if (overlayCanvas != null || visualRoot == null)
            {
                return;
            }

            initialWorldScale = visualRoot.lossyScale;
            var overlayObject = new GameObject(
                "DroneDialogueOverlayCanvas",
                typeof(RectTransform),
                typeof(Canvas));
            overlayObject.layer = gameObject.layer;
            overlayRoot = overlayObject.GetComponent<RectTransform>();
            overlayCanvas = overlayObject.GetComponent<Canvas>();
            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            overlayCanvas.sortingOrder = OverlaySortingOrder;

            visualRoot.SetParent(overlayRoot, false);
            visualRoot.anchorMin = new Vector2(0.5f, 0.5f);
            visualRoot.anchorMax = new Vector2(0.5f, 0.5f);
            visualRoot.pivot = new Vector2(0.5f, 0.5f);
            visualRoot.localRotation = Quaternion.identity;
        }

        private void ApplyScreenScale(Camera camera, Vector3 worldPosition)
        {
            Vector3 screenOrigin = camera.WorldToScreenPoint(worldPosition);
            Vector3 screenStep = camera.WorldToScreenPoint(
                worldPosition + Vector3.up * Mathf.Max(0.0001f, initialWorldScale.y));
            float pixelScale = Mathf.Max(0.0001f, Mathf.Abs(screenStep.y - screenOrigin.y));
            visualRoot.localScale = Vector3.one * pixelScale;
        }

        private void OnDestroy()
        {
            if (overlayRoot == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(overlayRoot.gameObject);
            }
            else
            {
                DestroyImmediate(overlayRoot.gameObject);
            }
        }
    }
}
