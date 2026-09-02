using SubTerra.App.Drone.Dialogue;
using TMPro;
using UnityEngine;

namespace SubTerra.App.UI.Drone
{
    /// <summary>드론 ViewSocket을 따라가며 화면 경계 안에 대사를 표시하는 World Space View.</summary>
    public sealed class DroneDialogueSocket : MonoBehaviour, IDroneDialogueView
    {
        public const string DarknessBypassShaderProperty = "_DroneDialogueViewportRect";
        private static readonly int DarknessBypassRectId = Shader.PropertyToID(
            DarknessBypassShaderProperty);
        private static readonly Vector4 HiddenDarknessBypassRect = new(-1f, -1f, -1f, -1f);

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

        public bool IsShowing => canvasGroup != null && canvasGroup.alpha > 0f;

        private void Awake()
        {
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
                visualRoot.position = desired;
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
                var corrected = camera.ViewportToWorldPoint(viewport);
                corrected.z = desired.z;
                visualRoot.position = corrected;
            }
            else
            {
                visualRoot.position = desired;
            }

            UpdateDarknessBypass(camera);
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
            if (worldCanvas != null)
            {
                // World UI가 지형 Sprite 뒤로 숨지 않도록 UI 정렬만 최상단으로 고정한다.
                worldCanvas.overrideSorting = true;
                worldCanvas.sortingOrder = 100;
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

            if (!visible)
            {
                Shader.SetGlobalVector(DarknessBypassRectId, HiddenDarknessBypassRect);
            }
        }

        private void UpdateDarknessBypass(Camera camera)
        {
            if (!IsShowing || visualRoot == null || camera == null)
            {
                Shader.SetGlobalVector(DarknessBypassRectId, HiddenDarknessBypassRect);
                return;
            }

            var corners = new Vector3[4];
            visualRoot.GetWorldCorners(corners);
            var min = Vector2.one;
            var max = Vector2.zero;
            for (var index = 0; index < corners.Length; index++)
            {
                Vector3 viewport = camera.WorldToViewportPoint(corners[index]);
                min = Vector2.Min(min, viewport);
                max = Vector2.Max(max, viewport);
            }

            const float padding = 0.006f;
            Shader.SetGlobalVector(
                DarknessBypassRectId,
                new Vector4(
                    Mathf.Clamp01(min.x - padding),
                    Mathf.Clamp01(min.y - padding),
                    Mathf.Clamp01(max.x + padding),
                    Mathf.Clamp01(max.y + padding)));
        }

        private void OnDisable()
        {
            Shader.SetGlobalVector(DarknessBypassRectId, HiddenDarknessBypassRect);
        }
    }
}
