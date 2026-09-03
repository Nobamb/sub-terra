using UnityEngine;

namespace SubTerra.App.UI.EmergencyRescue
{
    /// <summary>
    /// 전력 0 구출 칩을 플레이어 머리 위 Screen Space Overlay 좌표로 따라가게 한다.
    /// 팝업 루트가 꺼져 있어도 칩이 활성인 동안 LateUpdate가 동작한다.
    /// 카메라 추종 이후에 좌표를 갱신한다.
    /// </summary>
    [DefaultExecutionOrder(200)]
    public sealed class EmergencyRescueChipFollow : MonoBehaviour
    {
        public const float ScreenPadding = 12f;
        public static readonly Vector2 ScreenOffset = new(0f, 48f);

        private Transform playerTarget;
        private Collider2D playerCollider;
        private RectTransform chipRect;
        private Canvas parentCanvas;

        public void SetTarget(Transform player)
        {
            playerTarget = player;
            playerCollider = player != null
                ? player.GetComponent<Collider2D>()
                : null;
            chipRect = transform as RectTransform;
            parentCanvas = transform.parent != null
                ? transform.parent.GetComponentInParent<Canvas>()
                : null;
        }

        private void LateUpdate()
        {
            Tick();
        }

        public void Tick()
        {
            if (playerTarget == null || chipRect == null)
            {
                return;
            }

            Camera worldCamera = Camera.main;
            if (worldCamera == null)
            {
                return;
            }

            float top = playerCollider != null
                ? playerCollider.bounds.max.y
                : playerTarget.position.y;
            Vector3 screenPoint = worldCamera.WorldToScreenPoint(
                new Vector3(playerTarget.position.x, top, playerTarget.position.z));
            if (screenPoint.z < 0f)
            {
                return;
            }

            screenPoint += (Vector3)ScreenOffset;
            float pivotX = chipRect.pivot.x;
            float pivotY = chipRect.pivot.y;
            float width = chipRect.rect.width;
            float height = chipRect.rect.height;
            screenPoint.x = Mathf.Clamp(
                screenPoint.x,
                ScreenPadding + width * pivotX,
                Screen.width - ScreenPadding - width * (1f - pivotX));
            screenPoint.y = Mathf.Clamp(
                screenPoint.y,
                ScreenPadding + height * pivotY,
                Screen.height - ScreenPadding - height * (1f - pivotY));

            Camera uiCamera = parentCanvas != null
                && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                    ? parentCanvas.worldCamera
                    : null;
            if (chipRect.parent is RectTransform parentRect
                && RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    parentRect,
                    screenPoint,
                    uiCamera,
                    out Vector3 worldPoint))
            {
                chipRect.position = worldPoint;
            }
        }
    }
}
