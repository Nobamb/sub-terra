using UnityEngine;

namespace SubTerra.App.UI
{
    /// <summary>
    /// Keeps a full-screen UI container inside the device safe area.
    /// Attach this to a child of a screen-space Canvas, not the Canvas root itself.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform target;
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;
        private bool hasApplied;

        private void Awake()
        {
            target = GetComponent<RectTransform>();
            Apply();
        }

        private void OnEnable()
        {
            Apply();
        }

        private void Update()
        {
            var safeArea = Screen.safeArea;
            var screenSize = new Vector2Int(Screen.width, Screen.height);
            if (!hasApplied || safeArea != lastSafeArea || screenSize != lastScreenSize)
            {
                Apply();
            }
        }

        public void Apply()
        {
            if (target == null)
            {
                target = GetComponent<RectTransform>();
            }

            var safeArea = Screen.safeArea;
            var screenSize = new Vector2Int(Screen.width, Screen.height);
            ApplySafeArea(target, safeArea, screenSize);
            lastSafeArea = safeArea;
            lastScreenSize = screenSize;
            hasApplied = true;
        }

        public static void ApplySafeArea(
            RectTransform target,
            Rect safeArea,
            Vector2Int screenSize)
        {
            if (target == null || screenSize.x <= 0 || screenSize.y <= 0)
            {
                return;
            }

            target.anchorMin = new Vector2(
                Mathf.Clamp01(safeArea.xMin / screenSize.x),
                Mathf.Clamp01(safeArea.yMin / screenSize.y));
            target.anchorMax = new Vector2(
                Mathf.Clamp01(safeArea.xMax / screenSize.x),
                Mathf.Clamp01(safeArea.yMax / screenSize.y));
            target.offsetMin = Vector2.zero;
            target.offsetMax = Vector2.zero;
        }
    }
}
