using System.Collections;
using SubTerra.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SubTerra.App.UI.Inventory
{
    /// <summary>
    /// 인벤토리 중량 물음표를 가리키는 동안 도움말을 표시한다.
    /// 아이콘 안쪽 중앙에서 글로우가 천천히 켜지고, 설명창은 아이콘 오른쪽에서 홀로그램처럼 켜진다.
    /// 연출 곡선은 InventoryHelpMotion/InventoryHelpHoverState가 계산하고 이 클래스는 값을 적용만 한다.
    /// 연출 참조(group 등)가 비어 있으면 예전처럼 즉시 켜고 끈다.
    /// </summary>
    public sealed class InventoryWeightTooltip :
        MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler
    {
        public const float ConnectorLength = 10f;
        public const float ConnectorDrop = 17f;
        public const float TextPaddingX = 12f;
        public const float TextPaddingY = 11f;
        public const float MinBodyHeight = 60f;

        [SerializeField] private GameObject tooltipRoot;
        [SerializeField] private Graphic iconGlow;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform body;
        [SerializeField] private TMP_Text description;
        [SerializeField] private RectTransform scanLine;

        private readonly InventoryHelpHoverState state = new InventoryHelpHoverState();
        private Coroutine routine;
        private int characterCount;

        public GameObject TooltipRoot => tooltipRoot;
        public InventoryHelpHoverState State => state;

        private bool HasHologram => group != null && body != null && description != null && tooltipRoot != null;

        private void Awake()
        {
            HideAll();
        }

        private void OnDisable()
        {
            HideAll();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (tooltipRoot != null && !tooltipRoot.activeSelf)
            {
                tooltipRoot.SetActive(true);
            }

            if (!HasHologram && iconGlow == null)
            {
                return;
            }

            var wasShown = state.IsShown;
            state.Enter();
            if (!wasShown && HasHologram)
            {
                Layout();
            }

            if (AccessibilityPreferences.ReduceMotion)
            {
                state.CompleteOpen();
                Apply();
                return;
            }

            Apply();
            if (routine == null && isActiveAndEnabled)
            {
                routine = StartCoroutine(Run());
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            state.Exit();
            if (!HasHologram && iconGlow == null)
            {
                SetTooltipActive(false);
                return;
            }

            if (AccessibilityPreferences.ReduceMotion || !isActiveAndEnabled)
            {
                HideAll();
            }
        }

        private IEnumerator Run()
        {
            while (!state.IsSettled)
            {
                state.Tick(Time.unscaledDeltaTime);
                Apply();
                yield return null;
            }

            routine = null;
            HideAll();
        }

        /// <summary>글자 길이에 맞춰 창 높이를 정하고, 연결선 높이에서 위아래로 펼쳐지도록 피벗을 잡는다.</summary>
        private void Layout()
        {
            var width = body.sizeDelta.x;
            description.ForceMeshUpdate();
            var preferred = description.GetPreferredValues(description.text, width - TextPaddingX * 2f, 0f);
            var height = Mathf.Max(MinBodyHeight, Mathf.Ceil(preferred.y) + TextPaddingY * 2f);
            body.sizeDelta = new Vector2(width, height);
            body.pivot = new Vector2(0f, 1f - ConnectorDrop / height);
            body.anchoredPosition = new Vector2(ConnectorLength, -ConnectorDrop);
            ((RectTransform)tooltipRoot.transform).sizeDelta = new Vector2(ConnectorLength + width, height);

            description.ForceMeshUpdate();
            characterCount = description.textInfo != null ? description.textInfo.characterCount : description.text.Length;
        }

        private void Apply()
        {
            if (iconGlow != null)
            {
                var intensity = state.GlowIntensity * InventoryHelpMotion.GlowMaxAlpha;
                var color = iconGlow.color;
                color.a = intensity;
                iconGlow.color = color;
                var scale = state.GlowScale;
                iconGlow.rectTransform.localScale = new Vector3(scale, scale, 1f);
            }

            if (!HasHologram || !state.IsShown)
            {
                return;
            }

            var reduce = AccessibilityPreferences.ReduceMotion;
            var elapsed = state.OpenElapsed;
            var alpha = reduce ? 1f : InventoryHelpMotion.Alpha(elapsed);
            var scaleX = reduce ? 1f : InventoryHelpMotion.ScaleX(elapsed);
            var scaleY = reduce ? 1f : InventoryHelpMotion.ScaleY(elapsed);
            if (state.CloseElapsed > 0f)
            {
                alpha *= InventoryHelpMotion.CloseAlpha(state.CloseElapsed);
                scaleY = Mathf.Lerp(scaleY, InventoryHelpMotion.MinScaleY, InventoryHelpMotion.CloseProgress(state.CloseElapsed));
            }

            group.alpha = alpha;
            body.localScale = new Vector3(scaleX, scaleY, 1f);

            var content = reduce ? 1f : InventoryHelpMotion.ContentAlpha(elapsed);
            description.alpha = content;
            description.maxVisibleCharacters = reduce
                ? characterCount
                : InventoryHelpMotion.VisibleCharacters(elapsed, characterCount);

            if (scanLine != null)
            {
                var scanActive = !reduce && elapsed >= InventoryHelpMotion.OpenDuration && state.CloseElapsed <= 0f;
                scanLine.localScale = scanActive ? Vector3.one : Vector3.zero;
                if (scanActive)
                {
                    var travel = Mathf.Max(0f, body.rect.height - scanLine.rect.height);
                    scanLine.anchoredPosition = new Vector2(0f, -travel * InventoryHelpMotion.ScanPosition(elapsed));
                }
            }
        }

        private void HideAll()
        {
            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }

            state.Reset();
            if (iconGlow != null)
            {
                var color = iconGlow.color;
                color.a = 0f;
                iconGlow.color = color;
            }

            if (HasHologram)
            {
                group.alpha = 0f;
                body.localScale = Vector3.one;
                description.maxVisibleCharacters = 0;
                if (scanLine != null)
                {
                    scanLine.localScale = Vector3.zero;
                }
            }

            SetTooltipActive(false);
        }

        private void SetTooltipActive(bool visible)
        {
            if (tooltipRoot != null && tooltipRoot.activeSelf != visible)
            {
                tooltipRoot.SetActive(visible);
            }
        }

#if UNITY_EDITOR
        /// <summary>편집 모드 캡처용: 호버 후 지정한 시간이 지난 모습을 즉시 그린다.</summary>
        public void EditorPreview(float seconds)
        {
            state.Reset();
            if (HasHologram)
            {
                tooltipRoot.SetActive(true);
                Layout();
            }

            state.Enter();
            state.Tick(seconds);
            Apply();
        }

        public void EditorSet(Graphic glow, CanvasGroup canvasGroup, RectTransform bodyRect, TMP_Text text, RectTransform scan)
        {
            iconGlow = glow;
            group = canvasGroup;
            body = bodyRect;
            description = text;
            scanLine = scan;
        }
#endif
    }
}
