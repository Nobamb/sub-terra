using System.Collections.Generic;
using SubTerra.App.UI.Sell;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SubTerra.App.UI.Guide
{
    /// <summary>
    /// 가이드 버튼 공통 상태 규칙. 기본: 진한 남색 면·흰 글자·얇은 청록 테두리 / 호버·키보드 포커스: 밝은 청록 면·어두운 글자.
    /// 탭·필터는 선택 상태를 따로 가진다. 값은 hover·press·selected에서 매번 새로 계산하므로 빠른 입력에도 누적되지 않는다.
    /// 시간은 unscaledDeltaTime이다.
    /// </summary>
    public sealed class GameGuideButton : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler,
        ISelectHandler, IDeselectHandler
    {
        public enum Kind
        {
            Normal = 0,
            Tab = 1,
            Chip = 2,
            Close = 3
        }

        public const float HoverInSeconds = 0.06f;
        public const float HoverOutSeconds = 0.12f;
        public const float PressSeconds = 0.12f;

        private static readonly Color FaceNormal = new Color(0.035f, 0.075f, 0.11f, 0.97f);
        private static readonly Color FaceHover = new Color(0.47f, 0.93f, 1f, 1f);
        private static readonly Color FaceTabSelected = new Color(0.04f, 0.25f, 0.31f, 1f);
        private static readonly Color FaceChipSelected = new Color(0.30f, 0.84f, 0.93f, 1f);

        [SerializeField] private Button button;
        [SerializeField] private Image face;
        [SerializeField] private Image border;
        [SerializeField] private Image glow;
        [SerializeField] private Image underline;
        [SerializeField] private Graphic[] contents;
        [SerializeField] private Kind kind;

        private bool hovered;
        private bool focused;
        private bool pointerDown;
        private bool selected;
        private float hover;
        private float press;
        private float lastHover = -1f;
        private float lastPress = -1f;
        private bool lastSelected;
        private bool lastInteractable = true;
        private bool dirty = true;

        public Button Button => button;
        public Kind ButtonKind => kind;
        public bool Selected => selected;
        public float HoverLevel => hover;
        public bool IsHighlighted => hovered || focused;
        public Color FaceColor => face != null ? face.color : Color.clear;
        public Color BorderColor => border != null ? border.color : Color.clear;
        public Color ContentColor => contents != null && contents.Length > 0 && contents[0] != null ? contents[0].color : Color.clear;
        public bool UnderlineVisible => underline != null && underline.enabled && underline.color.a > 0.01f;

        internal void Setup(Button target, Image faceImage, Image borderImage, Image glowImage, Image underlineImage,
            Graphic[] contentGraphics, Kind buttonKind)
        {
            button = target;
            face = faceImage;
            border = borderImage;
            glow = glowImage;
            underline = underlineImage;
            contents = contentGraphics;
            kind = buttonKind;
            dirty = true;
            Apply(true);
        }

        public void SetSelected(bool value)
        {
            if (selected == value)
            {
                return;
            }

            selected = value;
            dirty = true;
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

        public void OnSelect(BaseEventData eventData) => focused = true;

        public void OnDeselect(BaseEventData eventData) => focused = false;

        private bool IsInteractable => button == null || button.IsInteractable();

        private void OnEnable()
        {
            dirty = true;
        }

        private void OnDisable()
        {
            hovered = false;
            focused = false;
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
            var target = interactable && (hovered || focused) ? 1f : 0f;
            if (!interactable)
            {
                hover = 0f;
            }
            else
            {
                hover = Mathf.MoveTowards(hover, target, dt / (target > hover ? HoverInSeconds : HoverOutSeconds));
            }

            if (!pointerDown || !interactable)
            {
                press = Mathf.MoveTowards(press, 0f, dt / PressSeconds);
            }

            Apply(dirty || interactable != lastInteractable);
        }

        private void Apply(bool force)
        {
            var interactable = IsInteractable;
            if (!force && Mathf.Approximately(hover, lastHover) && Mathf.Approximately(press, lastPress)
                && selected == lastSelected && interactable == lastInteractable)
            {
                return;
            }

            dirty = false;
            lastHover = hover;
            lastPress = press;
            lastSelected = selected;
            lastInteractable = interactable;

            Color baseFace;
            Color baseBorder;
            Color baseContent;
            switch (kind)
            {
                case Kind.Tab:
                    baseFace = selected ? FaceTabSelected : FaceNormal;
                    baseBorder = ResourceSellUi.WithAlpha(ResourceSellUi.Teal, selected ? 1f : 0.28f);
                    baseContent = selected ? Color.white : new Color(0.78f, 0.88f, 0.92f, 1f);
                    break;
                case Kind.Chip:
                    baseFace = selected ? FaceChipSelected : FaceNormal;
                    baseBorder = ResourceSellUi.WithAlpha(ResourceSellUi.Teal, selected ? 1f : 0.45f);
                    baseContent = selected ? ResourceSellUi.Navy : Color.white;
                    break;
                default:
                    baseFace = FaceNormal;
                    baseBorder = ResourceSellUi.WithAlpha(ResourceSellUi.Teal, 0.55f);
                    baseContent = Color.white;
                    break;
            }

            var faceColor = Color.Lerp(baseFace, FaceHover, hover);
            var borderColor = Color.Lerp(baseBorder, ResourceSellUi.Teal, hover);
            var content = Color.Lerp(baseContent, ResourceSellUi.Navy, hover);
            if (!interactable)
            {
                faceColor = new Color(0.05f, 0.062f, 0.072f, 0.9f);
                borderColor = new Color(0.36f, 0.42f, 0.46f, 0.35f);
                content = new Color(0.40f, 0.45f, 0.48f, 1f);
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
                var glowAlpha = interactable ? Mathf.Max(hover * 0.38f, selected && kind == Kind.Tab ? 0.3f : 0f) : 0f;
                glow.color = ResourceSellUi.WithAlpha(ResourceSellUi.Teal, glowAlpha);
            }

            if (underline != null)
            {
                underline.enabled = kind == Kind.Tab && selected;
                underline.color = ResourceSellUi.WithAlpha(ResourceSellUi.Teal, 1f);
            }

            for (var i = 0; contents != null && i < contents.Length; i++)
            {
                if (contents[i] != null)
                {
                    contents[i].color = content;
                }
            }

            var scale = Mathf.Lerp(1f, 0.96f, press);
            transform.localScale = new Vector3(scale, scale, 1f);
        }

        /// <summary>각진 면·테두리·발광이 있는 버튼 하나. 안쪽 글자·아이콘은 호출자가 content에 넣어 돌려받는다.</summary>
        internal static GameGuideButton Create(
            Transform parent,
            string name,
            float x,
            float y,
            float w,
            float h,
            TMP_FontAsset font,
            string label,
            float fontSize,
            Kind kind,
            Sprite icon = null,
            float iconSize = 20f,
            float iconOffset = 0f)
        {
            var rect = ResourceSellUi.Place(parent, name, x, y, w, h);
            var glowImage = ResourceSellUi.Image(rect, "Glow", ResourceSellArt.SoftRect(), Color.clear);
            glowImage.rectTransform.offsetMin = new Vector2(-12f, -12f);
            glowImage.rectTransform.offsetMax = new Vector2(12f, 12f);
            var faceImage = ResourceSellUi.Image(rect, "Face", ResourceSellArt.ChamferFill(), FaceNormal);
            faceImage.raycastTarget = true;
            var borderImage = ResourceSellUi.Image(rect, "Border", ResourceSellArt.ChamferOutline(), ResourceSellUi.Teal);

            Image underline = null;
            if (kind == Kind.Tab)
            {
                var line = ResourceSellUi.Place(rect, "Underline", 0f, h - 4f, w, 4f);
                underline = ResourceSellUi.AddImage(line, null, ResourceSellUi.Teal);
                underline.enabled = false;
            }

            var target = rect.gameObject.AddComponent<Button>();
            target.targetGraphic = faceImage;
            target.transition = Selectable.Transition.None;
            var navigation = target.navigation;
            navigation.mode = Navigation.Mode.None;
            target.navigation = navigation;

            var graphics = new List<Graphic>(2);
            var hasLabel = !string.IsNullOrEmpty(label);
            if (icon != null)
            {
                var iconX = hasLabel ? -w * 0.5f + iconOffset + iconSize * 0.5f + 12f : 0f;
                var iconRect = ResourceSellUi.Centered(rect, "Icon", new Vector2(iconX, 0f), new Vector2(iconSize, iconSize));
                var iconImage = ResourceSellUi.AddImage(iconRect, icon, Color.white);
                iconImage.preserveAspect = true;
                graphics.Add(iconImage);
            }

            if (hasLabel)
            {
                var shift = icon != null ? (iconSize + 12f) * 0.5f : 0f;
                var labelRect = ResourceSellUi.Centered(rect, "Label", new Vector2(shift, 0f), new Vector2(w - 12f - shift * 2f, h));
                var text = ResourceSellUi.Text(labelRect, font, fontSize, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
                text.text = label;
                graphics.Add(text);
            }

            var view = rect.gameObject.AddComponent<GameGuideButton>();
            view.Setup(target, faceImage, borderImage, glowImage, underline, graphics.ToArray(), kind);
            return view;
        }
    }
}
