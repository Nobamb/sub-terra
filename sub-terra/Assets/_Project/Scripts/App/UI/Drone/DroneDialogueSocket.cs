using SubTerra.App.Drone.Dialogue;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.Drone
{
    /// <summary>드론을 따라가되 어둠보다 앞선 별도 Overlay Canvas에 대사를 표시한다.</summary>
    public sealed class DroneDialogueSocket : MonoBehaviour, IDroneDialogueView
    {
        public const int OverlaySortingOrder = 30_000;

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

        private static readonly Color TerminalBackground = new Color(0.012f, 0.035f, 0.055f, 0.975f);
        private static readonly Color TerminalHeader = new Color(0.018f, 0.12f, 0.16f, 0.99f);
        private static readonly Color AccentCyan = new Color(0.18f, 0.84f, 0.92f, 1f);
        private static readonly Color AlertAmber = new Color(1f, 0.66f, 0.16f, 1f);

        public bool IsShowing => canvasGroup != null && canvasGroup.alpha > 0f;

        private void Awake()
        {
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

            dialogueText.text = "> " + dialogue.Text;
            hasDialogue = true;
            // 바인딩 전이라도 대사가 오면 표시 가능하게 둔다(Bind SetVisible 레이스 방지).
            if (!boundVisible)
            {
                boundVisible = true;
            }

            visibleUntil = Time.unscaledTime
                + (dialogue.IsUrgent ? urgentVisibleSeconds : regularVisibleSeconds);
            ApplyNonBlockingPresentation();
            ApplyTerminalTheme();
            ApplyTerminalState(dialogue.IsUrgent);
            SetCanvasVisible(true);
            RefreshPosition();
        }

        public void SetVisible(bool visible)
        {
            boundVisible = visible;
            SetCanvasVisible(visible && hasDialogue && Time.unscaledTime < visibleUntil);
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

        private void ApplyTerminalState(bool urgent)
        {
            var accent = urgent ? AlertAmber : AccentCyan;
            if (terminalSignalLine != null)
            {
                terminalSignalLine.color = accent;
            }

            if (terminalTitle != null)
            {
                terminalTitle.color = accent;
                terminalTitle.text = urgent
                    ? "DIGGER-BOT  //  PRIORITY LINK"
                    : "DIGGER-BOT  //  AI LINK";
            }

            if (terminalStatus != null)
            {
                terminalStatus.color = accent;
                terminalStatus.text = urgent ? "ALERT: HIGH" : "LINK: LIVE";
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
