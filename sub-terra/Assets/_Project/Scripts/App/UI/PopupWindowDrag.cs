using System.Collections.Generic;
using SubTerra.App.UI.MainMenu;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SubTerra.App.UI
{
    /// <summary>
    /// 팝업의 빈 프레임을 드래그해 Canvas 안에서 창을 이동한다.
    /// 버튼·슬라이더 위에서는 드래그를 시작하지 않는다.
    /// IDragHandler로 두면 입력 모듈이 조금만 움직여도 버튼 클릭을 취소한다.
    /// </summary>
    public sealed class PopupWindowDrag : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField] private RectTransform window;

        private Canvas popupCanvas;
        private RectTransform canvasRect;
        private Camera pressCamera;
        private Vector2 pressScreen;
        private Vector3 pointerStart;
        private Vector3 windowStart;
        private bool tracking;
        private bool dragging;
        private bool usesSettingsCanvas;
        private readonly Vector3[] corners = new Vector3[4];

        private void OnEnable()
        {
            if (window == null)
            {
                window = transform as RectTransform;
            }

            if (window == null)
            {
                return;
            }

            usesSettingsCanvas = window.parent != null
                && window.parent.GetComponent<SettingsMenuSkin>() != null;
            if (usesSettingsCanvas)
            {
                popupCanvas = window.parent.GetComponent<Canvas>();
            }
            else
            {
                popupCanvas = window.GetComponent<Canvas>();
                if (popupCanvas == null)
                {
                    popupCanvas = window.gameObject.AddComponent<Canvas>();
                }

                var parentCanvas = window.parent != null
                    ? window.parent.GetComponentInParent<Canvas>()
                    : null;
                if (parentCanvas != null)
                {
                    popupCanvas.additionalShaderChannels |=
                        parentCanvas.rootCanvas.additionalShaderChannels;
                }

                if (window.GetComponent<GraphicRaycaster>() == null)
                {
                    window.gameObject.AddComponent<GraphicRaycaster>();
                }

                // overrideSorting 캔버스는 부모 CanvasGroup.blocksRaycasts를 무시한다.
                // 닫힌 창이 투명한 채로 클릭을 삼키지 않도록 같은 오브젝트에서 한 번 더 거른다.
                if (window.GetComponent<PopupWindowRaycastGate>() == null)
                {
                    window.gameObject.AddComponent<PopupWindowRaycastGate>();
                }
            }

            ReleaseScrollRectFromButtons();
            if (!usesSettingsCanvas)
            {
                PopupWindowSorting.BringToFront(popupCanvas);
            }
        }

        private void OnDisable()
        {
            tracking = false;
            dragging = false;
            if (!usesSettingsCanvas)
            {
                PopupWindowSorting.Remove(popupCanvas);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            tracking = false;
            dragging = false;
            if (eventData.button != PointerEventData.InputButton.Left || window == null)
            {
                return;
            }

            if (IsInteractiveControl(eventData.pointerPressRaycast.gameObject))
            {
                return;
            }

            var canvas = window.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                return;
            }

            canvasRect = canvas.rootCanvas.transform as RectTransform;
            pressCamera = eventData.pressEventCamera;
            pressScreen = eventData.position;
            if (canvasRect == null || !TryGetPointerWorld(pressScreen, out pointerStart))
            {
                return;
            }

            windowStart = window.position;
            tracking = true;
        }

        private void Update()
        {
            if (!tracking || window == null)
            {
                return;
            }

            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.isPressed)
            {
                tracking = false;
                dragging = false;
                return;
            }

            var screen = mouse.position.ReadValue();
            if (!dragging)
            {
                var threshold = EventSystem.current != null
                    ? EventSystem.current.pixelDragThreshold
                    : 10;
                var delta = screen - pressScreen;
                if (delta.sqrMagnitude < threshold * threshold)
                {
                    return;
                }

                dragging = true;
            }

            if (canvasRect == null || !TryGetPointerWorld(screen, out var pointerNow))
            {
                return;
            }

            window.position = windowStart + pointerNow - pointerStart;
            ClampToCanvas();
        }

        private bool TryGetPointerWorld(Vector2 screen, out Vector3 world)
        {
            return RectTransformUtility.ScreenPointToWorldPointInRectangle(
                canvasRect,
                screen,
                pressCamera,
                out world);
        }

        /// <summary>
        /// 카드 전체에 붙은 ScrollRect는 형제 버튼의 클릭까지 드래그로 취소한다.
        /// 스크롤은 뷰포트 쪽으로 옮겨 목록 바깥 버튼은 클릭이 유지되게 한다.
        /// </summary>
        private void ReleaseScrollRectFromButtons()
        {
            if (!Application.isPlaying || window == null)
            {
                return;
            }

            var scroll = window.GetComponent<ScrollRect>();
            if (scroll == null || scroll.viewport == null || scroll.viewport == window)
            {
                return;
            }

            var host = scroll.viewport.gameObject;
            var moved = host.GetComponent<ScrollRect>();
            if (moved == null)
            {
                moved = host.AddComponent<ScrollRect>();
            }

            moved.content = scroll.content;
            moved.viewport = scroll.viewport;
            moved.horizontal = scroll.horizontal;
            moved.vertical = scroll.vertical;
            moved.movementType = scroll.movementType;
            moved.elasticity = scroll.elasticity;
            moved.inertia = scroll.inertia;
            moved.decelerationRate = scroll.decelerationRate;
            moved.scrollSensitivity = scroll.scrollSensitivity;
            moved.horizontalScrollbar = scroll.horizontalScrollbar;
            moved.verticalScrollbar = scroll.verticalScrollbar;
            moved.horizontalScrollbarVisibility = scroll.horizontalScrollbarVisibility;
            moved.verticalScrollbarVisibility = scroll.verticalScrollbarVisibility;
            moved.horizontalScrollbarSpacing = scroll.horizontalScrollbarSpacing;
            moved.verticalScrollbarSpacing = scroll.verticalScrollbarSpacing;
            // 부모에 남겨 두면 형제 버튼 클릭이 다시 드래그로 취소된다.
            scroll.enabled = false;
        }

        private bool IsInteractiveControl(GameObject hit)
        {
            for (var current = hit != null ? hit.transform : null; current != null; current = current.parent)
            {
                if (current.GetComponent<Selectable>() != null)
                {
                    return true;
                }

                if (current == transform)
                {
                    break;
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

    /// <summary>
    /// overrideSorting 중첩 캔버스에서도 부모 CanvasGroup의 레이캐스트 차단을 따른다.
    /// </summary>
    public sealed class PopupWindowRaycastGate : MonoBehaviour, ICanvasRaycastFilter
    {
        public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
        {
            for (var current = transform; current != null; current = current.parent)
            {
                var group = current.GetComponent<CanvasGroup>();
                if (group == null || !group.enabled)
                {
                    continue;
                }

                if (!group.blocksRaycasts)
                {
                    return false;
                }

                if (group.ignoreParentGroups)
                {
                    break;
                }
            }

            return true;
        }
    }

    public static class PopupWindowSorting
    {
        // The reset clock popup is the highest fixed UI layer (32,000).
        private const int FirstPopupSortOrder = 32_100;
        public const int SettingsSortOrder = 32_760;
        private static readonly List<Canvas> OpenWindows = new List<Canvas>();

        public static Canvas Top
        {
            get
            {
                Refresh();
                return OpenWindows.Count > 0 ? OpenWindows[OpenWindows.Count - 1] : null;
            }
        }

        public static bool Contains(Canvas canvas, GameObject root)
        {
            return canvas != null && root != null
                && (canvas.transform.IsChildOf(root.transform)
                    || root.transform.IsChildOf(canvas.transform));
        }

        public static void BringToFront(Canvas canvas)
        {
            if (canvas == null)
            {
                return;
            }

            OpenWindows.Remove(canvas);
            OpenWindows.Add(canvas);
            Refresh();
        }

        public static void Remove(Canvas canvas)
        {
            OpenWindows.Remove(canvas);
            Refresh();
        }

        private static void Refresh()
        {
            for (var i = 0; i < OpenWindows.Count; i++)
            {
                var canvas = OpenWindows[i];
                if (canvas == null || !canvas.gameObject.activeInHierarchy)
                {
                    OpenWindows.RemoveAt(i--);
                    continue;
                }

                canvas.overrideSorting = true;
                canvas.sortingOrder = FirstPopupSortOrder + i;
            }
        }
    }
}
