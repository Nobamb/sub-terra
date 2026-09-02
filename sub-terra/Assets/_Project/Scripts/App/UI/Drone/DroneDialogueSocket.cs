using SubTerra.App.Drone.Dialogue;
using TMPro;
using UnityEngine;

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
