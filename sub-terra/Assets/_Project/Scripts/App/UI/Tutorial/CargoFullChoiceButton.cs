using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SubTerra.App.UI.Tutorial
{
    /// <summary>
    /// 화물 부족 팝업 선택 칸의 상태 표현(판매창 버튼 규칙과 같다).
    /// 기본: 어두운 면 + 얇은 강조색 테두리 / 호버·키보드 포커스: 면이 밝아지고 테두리·발광이 강해짐 /
    /// 클릭: 짧게 눌림 / 비활성: 어둡게. 값은 hover·press 수준에서 매번 새로 계산해 누적되지 않는다.
    /// </summary>
    public sealed class CargoFullChoiceButton : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler,
        ISelectHandler, IDeselectHandler, ISubmitHandler
    {
        public const float HoverInSeconds = 0.06f;
        public const float HoverOutSeconds = 0.12f;
        public const float PressReleaseSeconds = 0.12f;
        public const float PressedScale = 0.97f;

        [SerializeField] private Button button;
        [SerializeField] private Image face;
        [SerializeField] private Image border;
        [SerializeField] private Image glow;
        [SerializeField] private Graphic icon;
        [SerializeField] private Graphic title;
        [SerializeField] private Graphic subtitle;
        [SerializeField] private Color accent = Color.cyan;
        [SerializeField] private Color faceIdle = new Color(0.03f, 0.08f, 0.1f, 0.96f);
        [SerializeField] private Color faceHover = new Color(0.06f, 0.2f, 0.24f, 1f);
        [SerializeField] private Color titleColor = Color.white;
        [SerializeField] private Color subtitleColor = new Color(0.62f, 0.76f, 0.82f, 1f);

        private bool hovered;
        private bool selected;
        private bool pointerDown;
        private float hover;
        private float press;

        public Button Button => button;
        public float HoverLevel => hover;
        public bool IsHighlighted => hovered || selected;
        public Color BorderColor => border != null ? border.color : Color.clear;

        internal void Setup(
            Button target,
            Image faceImage,
            Image borderImage,
            Image glowImage,
            Graphic iconGraphic,
            Graphic titleGraphic,
            Graphic subtitleGraphic,
            Color accentColor,
            Color idle,
            Color highlighted,
            Color titleTint,
            Color subtitleTint)
        {
            button = target;
            face = faceImage;
            border = borderImage;
            glow = glowImage;
            icon = iconGraphic;
            title = titleGraphic;
            subtitle = subtitleGraphic;
            accent = accentColor;
            faceIdle = idle;
            faceHover = highlighted;
            titleColor = titleTint;
            subtitleColor = subtitleTint;
            Apply();
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

        private bool IsInteractable => button != null && button.IsInteractable();

        private void OnDisable()
        {
            hovered = false;
            selected = false;
            pointerDown = false;
            hover = 0f;
            press = 0f;
            Apply();
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

            Apply();
        }

        private void Apply()
        {
            var interactable = IsInteractable;
            var dim = interactable ? 1f : 0.55f;
            if (face != null)
            {
                face.color = Color.Lerp(faceIdle, faceHover, hover);
            }

            if (border != null)
            {
                border.color = WithAlpha(accent, Mathf.Lerp(0.7f, 1f, hover) * dim);
            }

            if (glow != null)
            {
                glow.color = WithAlpha(accent, 0.3f * hover);
            }

            if (icon != null)
            {
                icon.color = WithAlpha(Color.Lerp(accent, Color.white, 0.25f * hover), dim);
            }

            if (title != null)
            {
                title.color = WithAlpha(titleColor, dim);
            }

            if (subtitle != null)
            {
                subtitle.color = WithAlpha(Color.Lerp(subtitleColor, titleColor, 0.4f * hover), dim);
            }

            var scale = Mathf.Lerp(1f, PressedScale, press);
            transform.localScale = new Vector3(scale, scale, 1f);
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }
    }
}
