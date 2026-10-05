using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SubTerra.App.UI.Sell
{
    /// <summary>
    /// 판매 창 버튼 공통 상태 규칙.
    /// 기본: 진한 남색 면·흰 글자·얇은 청록 테두리 / 호버·키보드 포커스: 밝은 청록 면·짙은 남색 글자·테두리 발광 강화 /
    /// 호버 해제: 약 0.12초 동안 기본색 복귀 / 클릭: 즉시 짧게 눌림(기능은 Button.onClick이 바로 실행) /
    /// 비활성: 채도·밝기를 낮추고 호버해도 밝아지지 않음.
    /// 모든 값은 hover·press 수준(0~1) 두 개에서 매번 새로 계산하므로 빠른 입력에도 색·크기가 누적되지 않는다.
    /// </summary>
    public sealed class ResourceSellButton : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler,
        ISelectHandler, IDeselectHandler, ISubmitHandler
    {
        public enum Variant
        {
            Normal = 0,
            Primary = 1,
            Close = 2
        }

        public const float HoverInSeconds = 0.06f;
        public const float HoverOutSeconds = 0.12f;
        public const float PressReleaseSeconds = 0.12f;
        public const float PressedScale = 0.95f;

        private static readonly Color FaceNormal = new Color(0.035f, 0.075f, 0.11f, 0.96f);
        private static readonly Color FacePrimary = new Color(0.04f, 0.29f, 0.36f, 1f);
        private static readonly Color FaceHover = new Color(0.47f, 0.93f, 1f, 1f);
        private static readonly Color FaceDisabled = new Color(0.05f, 0.062f, 0.072f, 0.9f);
        private static readonly Color BorderDisabled = new Color(0.36f, 0.42f, 0.46f, 0.35f);
        private static readonly Color ContentDisabled = new Color(0.40f, 0.45f, 0.48f, 1f);

        [SerializeField] private Button button;
        [SerializeField] private Image face;
        [SerializeField] private Image border;
        [SerializeField] private Image glow;
        [SerializeField] private Graphic[] contents;
        [SerializeField] private Variant variant;

        private bool hovered;
        private bool selected;
        private bool pointerDown;
        private float hover;
        private float press;
        private bool lastInteractable = true;
        private float lastHover = -1f;
        private float lastPress = -1f;

        public Button Button => button;
        public Variant Kind => variant;
        public float HoverLevel => hover;
        public float PressLevel => press;
        public bool IsHighlighted => hovered || selected;
        public Color FaceColor => face != null ? face.color : Color.clear;
        public Color ContentColor => contents != null && contents.Length > 0 && contents[0] != null ? contents[0].color : Color.clear;

        internal void Setup(Button target, Image faceImage, Image borderImage, Image glowImage, Graphic[] contentGraphics, Variant kind)
        {
            button = target;
            face = faceImage;
            border = borderImage;
            glow = glowImage;
            contents = contentGraphics;
            variant = kind;
            Apply(true);
        }

        public void OnPointerEnter(PointerEventData eventData) => hovered = true;

        public void OnPointerExit(PointerEventData eventData)
        {
            hovered = false;
            pointerDown = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            if (IsInteractable)
            {
                pointerDown = true;
                press = 1f;
            }
        }

        public void OnPointerUp(PointerEventData eventData) => pointerDown = false;

        public void OnSelect(BaseEventData eventData) => selected = true;

        public void OnDeselect(BaseEventData eventData) => selected = false;

        public void OnSubmit(BaseEventData eventData)
        {
            if (IsInteractable)
            {
                press = 1f;
            }
        }

        /// <summary>코드 경로(테스트·단축키)에서 같은 눌림 피드백을 준다.</summary>
        public void Kick()
        {
            if (IsInteractable)
            {
                press = 1f;
            }
        }

        private bool IsInteractable => button != null && button.IsInteractable();

        private void OnDisable()
        {
            hovered = false;
            selected = false;
            pointerDown = false;
            hover = 0f;
            press = 0f;
            Apply(true);
        }

        private void Update()
        {
            Step(Time.unscaledDeltaTime);
        }

        /// <summary>테스트와 Update가 같은 경로로 시간을 진행한다.</summary>
        public void Step(float dt)
        {
            var interactable = IsInteractable;
            var target = interactable && (hovered || selected) ? 1f : 0f;
            if (!interactable)
            {
                // 비활성은 즉시 어둡게. 다시 켜질 때 남은 호버 값이 튀지 않는다.
                hover = 0f;
            }
            else
            {
                var duration = target > hover ? HoverInSeconds : HoverOutSeconds;
                hover = Mathf.MoveTowards(hover, target, dt / duration);
            }

            if (!pointerDown || !interactable)
            {
                press = Mathf.MoveTowards(press, 0f, dt / PressReleaseSeconds);
            }

            Apply(interactable != lastInteractable);
        }

        private void Apply(bool force)
        {
            var interactable = IsInteractable;
            if (!force && Mathf.Approximately(hover, lastHover) && Mathf.Approximately(press, lastPress)
                && interactable == lastInteractable)
            {
                return;
            }

            lastHover = hover;
            lastPress = press;
            lastInteractable = interactable;

            var primary = variant == Variant.Primary;
            Color faceColor;
            Color borderColor;
            float glowAlpha;
            Color content;
            if (!interactable)
            {
                faceColor = FaceDisabled;
                borderColor = BorderDisabled;
                glowAlpha = 0f;
                content = ContentDisabled;
            }
            else
            {
                faceColor = Color.Lerp(primary ? FacePrimary : FaceNormal, FaceHover, hover);
                borderColor = ResourceSellUi.WithAlpha(ResourceSellUi.Teal, Mathf.Lerp(primary ? 0.95f : 0.55f, 1f, hover));
                glowAlpha = Mathf.Lerp(primary ? 0.42f : 0f, primary ? 0.75f : 0.38f, hover);
                content = Color.Lerp(Color.white, ResourceSellUi.Navy, hover);
            }

            if (face != null)
            {
                face.color = faceColor;
            }

            if (border != null)
            {
                border.color = borderColor;
            }

            if (glow != null)
            {
                glow.color = ResourceSellUi.WithAlpha(ResourceSellUi.Teal, glowAlpha);
            }

            for (var i = 0; contents != null && i < contents.Length; i++)
            {
                if (contents[i] != null)
                {
                    contents[i].color = content;
                }
            }

            var scale = Mathf.Lerp(1f, PressedScale, press);
            transform.localScale = new Vector3(scale, scale, 1f);
        }

        /// <summary>각진 면·테두리·발광·(선택) 기호·글자로 버튼 하나를 만든다.</summary>
        internal static ResourceSellButton Create(
            Transform parent,
            string name,
            float x,
            float y,
            float w,
            float h,
            TMP_FontAsset font,
            string label,
            float fontSize,
            Variant kind,
            Sprite icon = null,
            float iconSize = 20f,
            bool tintIcon = true)
        {
            var rect = ResourceSellUi.Place(parent, name, x, y, w, h);
            var glowImage = ResourceSellUi.Image(rect, "Glow", ResourceSellArt.SoftRect(), Color.clear);
            var glowRect = glowImage.rectTransform;
            glowRect.offsetMin = new Vector2(-14f, -14f);
            glowRect.offsetMax = new Vector2(14f, 14f);
            var faceImage = ResourceSellUi.Image(rect, "Face", ResourceSellArt.ChamferFill(), FaceNormal);
            faceImage.raycastTarget = true;
            var borderImage = ResourceSellUi.Image(rect, "Border", ResourceSellArt.ChamferOutline(), ResourceSellUi.Teal);

            var target = rect.gameObject.AddComponent<Button>();
            target.targetGraphic = faceImage;
            target.transition = Selectable.Transition.None;
            var navigation = target.navigation;
            navigation.mode = Navigation.Mode.None;
            target.navigation = navigation;

            var graphics = new System.Collections.Generic.List<Graphic>(2);
            var hasLabel = !string.IsNullOrEmpty(label);
            if (icon != null)
            {
                var iconX = hasLabel ? -w * 0.5f + 26f : 0f;
                var iconRect = ResourceSellUi.Centered(rect, "Icon", new Vector2(iconX, 0f), new Vector2(iconSize, iconSize));
                var iconImage = ResourceSellUi.AddImage(iconRect, icon, Color.white);
                iconImage.preserveAspect = true;
                if (tintIcon)
                {
                    graphics.Add(iconImage);
                }
            }

            if (hasLabel)
            {
                var labelRect = ResourceSellUi.Centered(rect, "Label", new Vector2(icon != null ? 14f : 0f, 0f),
                    new Vector2(w - (icon != null ? 44f : 10f), h));
                var text = ResourceSellUi.Text(labelRect, font, fontSize, FontStyles.Bold, Color.white,
                    TextAlignmentOptions.Center);
                text.text = label;
                text.overflowMode = TextOverflowModes.Overflow;
                graphics.Add(text);
            }

            var view = rect.gameObject.AddComponent<ResourceSellButton>();
            view.Setup(target, faceImage, borderImage, glowImage, graphics.ToArray(), kind);
            return view;
        }
    }
}
