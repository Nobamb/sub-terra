using SubTerra.Shared;
using TMPro;
using UnityEngine;

namespace SubTerra.App.UI.Building
{
    /// <summary>
    /// B-188 레일에서 시설 아이콘에 마우스를 올리면 아이콘 오른쪽에 뜨는 홀로그램 이름표.
    /// 클릭을 받지 않고(raycast 없음) 이름만 보여 준다. 모양 계산은 BuildingRailHologramMotion이 한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BuildingRailHologram : MonoBehaviour
    {
        public const float Gap = 6f;
        public const float ConnectorLength = 10f;
        public const float BodyHeight = 34f;
        public const float TextPadding = 14f;
        public const float GlowSpread = 4f;
        public const float MinBodyWidth = 64f;

        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform body;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private RectTransform scanLine;
        [SerializeField] private RectTransform glow;

        private RectTransform anchor;
        private float elapsed;
        private float closeElapsed;
        private int visibleCharacters = -1;

        public bool IsShown { get; private set; }
        public bool ScanVisible => scanLine != null && scanLine.localScale.y > 0f;
        public bool IsClosing { get; private set; }
        public string CurrentName => nameText != null ? nameText.text : string.Empty;
        public RectTransform CurrentAnchor => anchor;

        public bool HasRequiredReferences()
        {
            return group != null && body != null && nameText != null && scanLine != null;
        }

        private void Awake()
        {
            if (!IsShown)
            {
                SetHidden();
            }
        }

        private void OnDisable()
        {
            HideNow();
        }

        /// <summary>호버한 아이콘 오른쪽 가운데에 이름표를 띄운다. 다른 아이콘으로 옮기면 그 자리에서 다시 켜진다.</summary>
        public void Show(RectTransform target, string displayName)
        {
            if (target == null || !HasRequiredReferences() || string.IsNullOrEmpty(displayName))
            {
                HideNow();
                return;
            }

            var sameTarget = IsShown && !IsClosing && anchor == target && nameText.text == displayName;
            anchor = target;
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            PlaceBeside(target);
            if (sameTarget)
            {
                return;
            }

            nameText.text = displayName;
            var width = Mathf.Max(MinBodyWidth, nameText.GetPreferredValues(displayName, 9999f, BodyHeight).x + TextPadding * 2f);
            body.sizeDelta = new Vector2(width, BodyHeight);
            if (glow != null)
            {
                glow.anchoredPosition = new Vector2(ConnectorLength - GlowSpread, 0f);
                glow.sizeDelta = new Vector2(width + GlowSpread * 2f, BodyHeight + GlowSpread * 2f);
            }
            var rect = (RectTransform)transform;
            rect.sizeDelta = new Vector2(ConnectorLength + width, BodyHeight);

            IsShown = true;
            IsClosing = false;
            closeElapsed = 0f;
            elapsed = AccessibilityPreferences.ReduceMotion ? BuildingRailHologramMotion.OpenDuration + BuildingRailHologramMotion.TypeDuration : 0f;
            visibleCharacters = -1;
            ApplyFrame();
        }

        /// <summary>그 아이콘에서 마우스가 나갔을 때만 끈다. 이미 다른 아이콘으로 옮겨 갔으면 그대로 둔다.</summary>
        public void Hide(RectTransform target)
        {
            if (!IsShown || IsClosing || (target != null && target != anchor))
            {
                return;
            }

            if (AccessibilityPreferences.ReduceMotion || !isActiveAndEnabled)
            {
                HideNow();
                return;
            }

            IsClosing = true;
            closeElapsed = 0f;
        }

        public void HideNow()
        {
            IsShown = false;
            IsClosing = false;
            anchor = null;
            elapsed = 0f;
            closeElapsed = 0f;
            SetHidden();
        }

        private void Update() => Advance(Time.unscaledDeltaTime);

        public void Advance(float deltaTime)
        {
            if (!IsShown)
            {
                return;
            }

            deltaTime = Mathf.Max(0f, deltaTime);
            if (IsClosing)
            {
                closeElapsed += deltaTime;
                if (closeElapsed >= BuildingRailHologramMotion.CloseDuration)
                {
                    HideNow();
                    return;
                }
            }
            else
            {
                elapsed += deltaTime;
            }

            ApplyFrame();
        }

        private void PlaceBeside(RectTransform target)
        {
            var rect = (RectTransform)transform;
            var parent = rect.parent as RectTransform;
            if (parent == null)
            {
                return;
            }

            var bounds = target.rect;
            var rightMiddle = target.TransformPoint(new Vector3(bounds.xMax, bounds.center.y, 0f));
            var local = parent.InverseTransformPoint(rightMiddle);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.localPosition = new Vector3(local.x + Gap, local.y, 0f);
        }

        private void ApplyFrame()
        {
            var reduce = AccessibilityPreferences.ReduceMotion;
            var alpha = reduce ? 1f : BuildingRailHologramMotion.Alpha(elapsed);
            if (IsClosing)
            {
                alpha *= BuildingRailHologramMotion.CloseAlpha(closeElapsed);
            }

            group.alpha = alpha;
            transform.localScale = new Vector3(reduce ? 1f : BuildingRailHologramMotion.ScaleX(elapsed), 1f, 1f);

            var length = nameText.text != null ? nameText.text.Length : 0;
            var visible = reduce ? length : BuildingRailHologramMotion.VisibleCharacters(elapsed, length);
            if (visible != visibleCharacters)
            {
                visibleCharacters = visible;
                nameText.maxVisibleCharacters = visible;
            }

            var scanActive = !reduce && elapsed >= BuildingRailHologramMotion.OpenDuration;
            scanLine.localScale = scanActive ? Vector3.one : Vector3.zero;

            if (scanActive)
            {
                var travel = Mathf.Max(0f, body.rect.height - scanLine.rect.height);
                var position = BuildingRailHologramMotion.ScanPosition(elapsed - BuildingRailHologramMotion.OpenDuration);
                scanLine.anchoredPosition = new Vector2(0f, -travel * position);
            }
        }

        private void SetHidden()
        {
            if (group != null)
            {
                group.alpha = 0f;
                group.interactable = false;
                group.blocksRaycasts = false;
            }

            if (nameText != null)
            {
                visibleCharacters = 0;
                nameText.maxVisibleCharacters = 0;
            }

            // 오브젝트는 켜 둔 채 투명도로만 숨긴다(OnDisable 중 SetActive 충돌 방지).
            transform.localScale = Vector3.one;
            if (scanLine != null)
            {
                scanLine.localScale = Vector3.zero;
            }
        }

#if UNITY_EDITOR
        public void EditorSet(CanvasGroup canvasGroup, RectTransform bodyRect, TMP_Text text, RectTransform scan, RectTransform glowRect)
        {
            glow = glowRect;
            group = canvasGroup;
            body = bodyRect;
            nameText = text;
            scanLine = scan;
        }
#endif
    }
}
