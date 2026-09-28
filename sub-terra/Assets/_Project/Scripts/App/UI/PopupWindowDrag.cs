using UnityEngine;
using UnityEngine.EventSystems;

namespace SubTerra.App.UI
{
    /// <summary>팝업의 빈 프레임을 드래그해 Canvas 안에서 창을 이동한다.</summary>
    public sealed class PopupWindowDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private RectTransform window;

        private RectTransform canvasRect;
        private Vector3 pointerStart;
        private Vector3 windowStart;
        private bool dragging;
        private readonly Vector3[] corners = new Vector3[4];

        public void OnBeginDrag(PointerEventData eventData)
        {
            dragging = false;
            if (eventData.button != PointerEventData.InputButton.Left || window == null || IsInteractiveChild(eventData.pointerPressRaycast.gameObject))
            {
                return;
            }

            var canvas = window.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                return;
            }

            canvasRect = canvas.rootCanvas.transform as RectTransform;
            if (canvasRect == null || !RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    canvasRect, eventData.position, eventData.pressEventCamera, out pointerStart))
            {
                return;
            }

            windowStart = window.position;
            dragging = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!dragging || window == null || canvasRect == null)
            {
                return;
            }

            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    canvasRect, eventData.position, eventData.pressEventCamera, out var pointerNow))
            {
                window.position = windowStart + pointerNow - pointerStart;
                ClampToCanvas();
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            dragging = false;
        }

        private bool IsInteractiveChild(GameObject hit)
        {
            for (var current = hit != null ? hit.transform : null; current != null && current != transform; current = current.parent)
            {
                if (current.GetComponent<UnityEngine.UI.Selectable>() != null || current.GetComponent<UnityEngine.UI.ScrollRect>() != null)
                {
                    return true;
                }
            }

            return false;
        }

        private void ClampToCanvas()
        {
            window.GetWorldCorners(corners);
            var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            foreach (var corner in corners)
            {
                var local = canvasRect.InverseTransformPoint(corner);
                min = Vector2.Min(min, local);
                max = Vector2.Max(max, local);
            }

            var bounds = canvasRect.rect;
            var x = max.x - min.x > bounds.width
                ? bounds.center.x - (min.x + max.x) * 0.5f
                : min.x < bounds.xMin ? bounds.xMin - min.x : max.x > bounds.xMax ? bounds.xMax - max.x : 0f;
            var y = max.y - min.y > bounds.height
                ? bounds.center.y - (min.y + max.y) * 0.5f
                : min.y < bounds.yMin ? bounds.yMin - min.y : max.y > bounds.yMax ? bounds.yMax - max.y : 0f;
            window.position += canvasRect.TransformVector(new Vector3(x, y, 0f));
        }
    }
}
