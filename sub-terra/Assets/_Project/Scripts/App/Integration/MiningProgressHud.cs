using SubTerra.Gameplay.Mining;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.Integration
{
    /// <summary>채굴이 진행되는 동안 플레이어 머리 위에 진행 게이지를 표시한다.</summary>
    public sealed class MiningProgressHud : MonoBehaviour
    {
        [SerializeField] private GameObject statusRoot;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Image progressFill;
        [SerializeField] private Vector2 screenOffset = new(0f, 20f);
        [SerializeField, Min(0f)] private float failureDisplayDuration = 10f;

        private MiningSystem boundSystem;
        private Transform playerTarget;
        private Collider2D playerCollider;
        private RectTransform statusRect;
        private RectTransform progressFillRect;
        private Canvas parentCanvas;
        private bool isFailureVisible;
        private float failureHideAt;

        public void BindTo(MiningSystem system, Transform player)
        {
            if (boundSystem == system && playerTarget == player)
            {
                return;
            }

            if (boundSystem != null)
            {
                boundSystem.ProgressChanged -= OnProgressChanged;
            }

            boundSystem = system;
            playerTarget = player;
            playerCollider = playerTarget != null
                ? playerTarget.GetComponent<Collider2D>()
                : null;
            CacheUiReferences();
            if (boundSystem != null)
            {
                boundSystem.ProgressChanged += OnProgressChanged;
            }

            SetVisible(false);
        }

        public void BindTo(MiningSystem system) => BindTo(system, playerTarget);

        private void OnDisable()
        {
            if (boundSystem != null)
            {
                boundSystem.ProgressChanged -= OnProgressChanged;
                boundSystem = null;
            }
        }

        private void LateUpdate()
        {
            if (isFailureVisible && Time.unscaledTime >= failureHideAt)
            {
                isFailureVisible = false;
                SetVisible(false);
            }

            if (statusRoot != null && statusRoot.activeSelf)
            {
                UpdatePosition();
            }
        }

        private void OnProgressChanged(MiningProgressState state)
        {
            bool isMining = state.Phase == MiningPhase.Mining;
            isFailureVisible = state.Phase == MiningPhase.Failed;
            if (isFailureVisible)
            {
                // 게임이 일시 정지되어도 안내가 화면에 영구 잔류하지 않도록 실시간을 사용한다.
                failureHideAt = Time.unscaledTime + failureDisplayDuration;
            }

            if (progressFill != null)
            {
                float progress = Mathf.Clamp01(state.Progress);
                progressFillRect.localScale = new Vector3(progress, 1f, 1f);
                progressFill.gameObject.SetActive(isMining);
            }

            if (statusText != null)
            {
                statusText.text = state.Phase switch
                {
                    MiningPhase.Mining => $"채굴 {Mathf.RoundToInt(state.Progress * 100f)}%",
                    MiningPhase.Failed => FailureMessage(state.FailureReason),
                    _ => string.Empty
                };
            }

            bool isVisible = isMining || state.Phase == MiningPhase.Failed;
            SetVisible(isVisible);
            if (isVisible)
            {
                UpdatePosition();
            }
        }

        private void SetVisible(bool visible)
        {
            if (statusRoot != null)
            {
                statusRoot.SetActive(visible);
            }
        }

        private void CacheUiReferences()
        {
            statusRect = statusRoot != null
                ? statusRoot.GetComponent<RectTransform>()
                : null;
            parentCanvas = GetComponentInParent<Canvas>();
            if (progressFill != null)
            {
                progressFillRect = progressFill.rectTransform;
                progressFill.type = Image.Type.Simple;
            }
        }

        private void UpdatePosition()
        {
            Camera worldCamera = Camera.main;
            if (playerTarget == null || statusRect == null || worldCamera == null)
            {
                return;
            }

            float top = playerCollider != null
                ? playerCollider.bounds.max.y
                : playerTarget.position.y;
            Vector3 screenPoint = worldCamera.WorldToScreenPoint(
                new Vector3(playerTarget.position.x, top, playerTarget.position.z));
            screenPoint += (Vector3)screenOffset;

            Camera uiCamera = parentCanvas != null
                && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                    ? parentCanvas.worldCamera
                    : null;
            if (statusRect.parent is RectTransform parentRect
                && RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    parentRect,
                    screenPoint,
                    uiCamera,
                    out Vector3 worldPoint))
            {
                statusRect.position = worldPoint;
            }
        }

        private static string FailureMessage(MiningFailureReason reason)
        {
            return reason switch
            {
                MiningFailureReason.DrillLevelTooLow => "드릴 레벨이 부족합니다.",
                MiningFailureReason.InsufficientEnergy => "채굴 전력이 부족합니다.",
                MiningFailureReason.InventoryFull => "화물이 가득 찼습니다.",
                MiningFailureReason.OutOfRange => "채굴 범위를 벗어났습니다.",
                MiningFailureReason.NotMineable => "채굴할 수 없는 지형입니다.",
                MiningFailureReason.DeepZoneLocked => "심층 구역이 해금되어야 채굴할 수 있는 자원입니다.",
                _ => "채굴할 수 없습니다."
            };
        }
    }
}
