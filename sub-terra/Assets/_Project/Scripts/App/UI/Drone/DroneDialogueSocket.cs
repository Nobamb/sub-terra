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

        private static readonly Color BubbleBackground = new Color(0.018f, 0.06f, 0.075f, 0.97f);
        private static readonly Color BubbleHeader = new Color(0.035f, 0.18f, 0.22f, 0.98f);
        private static readonly Color AccentCyan = new Color(0.18f, 0.84f, 0.92f, 1f);

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

            dialogueText.text = dialogue.Text;
            hasDialogue = true;
            // 바인딩 전이라도 대사가 오면 표시 가능하게 둔다(Bind SetVisible 레이스 방지).
            if (!boundVisible)
            {
                boundVisible = true;
            }

            visibleUntil = Time.unscaledTime
                + (dialogue.IsUrgent ? urgentVisibleSeconds : regularVisibleSeconds);
            ApplyNonBlockingPresentation();
            ApplyBubbleTheme();
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
        /// 월드 말풍선도 HUD와 같은 청록색 정보 패널 언어를 사용한다.
        /// 프리팹의 대사·추적 참조는 유지하고, 표시 요소만 런타임에 보강한다.
        /// </summary>
        private void ApplyBubbleTheme()
        {
            if (bubbleThemeApplied || visualRoot == null || dialogueText == null)
            {
                return;
            }

            var background = visualRoot.GetComponent<Image>();
            if (background == null)
            {
                background = visualRoot.gameObject.AddComponent<Image>();
            }

            background.color = BubbleBackground;
            background.raycastTarget = false;

            var outline = visualRoot.GetComponent<Outline>();
            if (outline == null)
            {
                outline = visualRoot.gameObject.AddComponent<Outline>();
            }

            outline.effectColor = new Color(AccentCyan.r, AccentCyan.g, AccentCyan.b, 0.72f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            outline.useGraphicAlpha = true;

            var shadow = visualRoot.GetComponent<Shadow>();
            if (shadow == null)
            {
                shadow = visualRoot.gameObject.AddComponent<Shadow>();
            }

            shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
            shadow.effectDistance = new Vector2(3f, -4f);
            shadow.useGraphicAlpha = true;

            CreateAccentLine();
            CreateHeader();
            CreateTail();
            StyleDialogueText();
            bubbleThemeApplied = true;
        }

        private void CreateAccentLine()
        {
            var line = CreateVisualElement("SignalLine", visualRoot);
            SetAnchors(line.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(10f, -4f), new Vector2(-10f, 0f));
            line.color = AccentCyan;
        }

        private void CreateHeader()
        {
            var header = CreateVisualElement("Header", visualRoot);
            SetAnchors(header.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -34f), Vector2.zero);
            header.color = BubbleHeader;

            var titleObject = new GameObject("Title", typeof(RectTransform));
            titleObject.transform.SetParent(header.transform, false);
            var title = titleObject.AddComponent<TextMeshProUGUI>();
            CopyTextStyle(title);
            title.text = "DIGGER-BOT  //  SCAN LINK";
            title.fontSize = 14f;
            title.fontStyle = FontStyles.Bold;
            title.color = AccentCyan;
            title.alignment = TextAlignmentOptions.MidlineLeft;
            title.raycastTarget = false;
            SetAnchors(title.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(16f, 0f), new Vector2(-16f, 0f));
        }

        private void CreateTail()
        {
            var tail = CreateVisualElement("Tail", visualRoot);
            tail.rectTransform.anchorMin = new Vector2(0.22f, 0f);
            tail.rectTransform.anchorMax = new Vector2(0.22f, 0f);
            tail.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            tail.rectTransform.anchoredPosition = new Vector2(0f, -7f);
            tail.rectTransform.sizeDelta = new Vector2(16f, 16f);
            tail.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            tail.color = BubbleBackground;
            tail.raycastTarget = false;
            tail.transform.SetAsFirstSibling();
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
                new Vector2(18f, 12f), new Vector2(-18f, -42f));
        }

        private Image CreateVisualElement(string name, RectTransform parent)
        {
            var element = new GameObject(name, typeof(RectTransform), typeof(Image));
            element.transform.SetParent(parent, false);
            var image = element.GetComponent<Image>();
            image.raycastTarget = false;
            return image;
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
