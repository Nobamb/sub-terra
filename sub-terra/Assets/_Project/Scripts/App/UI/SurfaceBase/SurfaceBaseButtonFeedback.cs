using UnityEngine;
using UnityEngine.EventSystems;

namespace SubTerra.App.UI.SurfaceBase
{
    /// <summary>기지 버튼의 밝기 전환과 내부 빛 이동. 게임 상태는 변경하지 않는다.</summary>
    [RequireComponent(typeof(UnityEngine.UI.Button))]
    public sealed class SurfaceBaseButtonFeedback : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        [SerializeField] private UnityEngine.UI.Image plate;
        [SerializeField] private UnityEngine.UI.Image highlight;
        [SerializeField] private UnityEngine.UI.Image sweep;
        private UnityEngine.UI.Button button;
        private bool hovered;
        private bool selected;
        private float blend;
        private float sweepTime;

        public float HighlightAmount => blend;

        private void Awake() => button = GetComponent<UnityEngine.UI.Button>();
        public void OnPointerEnter(PointerEventData eventData) => hovered = true;
        public void OnPointerExit(PointerEventData eventData) => hovered = false;
        public void OnSelect(BaseEventData eventData) => selected = true;
        public void OnDeselect(BaseEventData eventData) => selected = false;

        private void Update()
        {
            if (button == null) button = GetComponent<UnityEngine.UI.Button>();
            bool enabled = button.IsInteractable();
            float target = enabled && (hovered || selected) ? 1f : 0f;
            blend = Mathf.MoveTowards(blend, target, Time.unscaledDeltaTime / 0.2f);
            Apply(enabled);
            if (target > 0f) sweepTime += Time.unscaledDeltaTime;
            else sweepTime = 0f;
        }

        private void OnDisable()
        {
            hovered = selected = false;
            blend = sweepTime = 0f;
            Apply(true);
        }

        private void Apply(bool enabled)
        {
            if (plate != null)
            {
                float brightness = enabled ? 0.82f : 0.4f;
                plate.color = new Color(brightness, brightness, brightness, 1f);
            }
            if (highlight != null) highlight.color = new Color(1f, 1f, 1f, blend);
            if (sweep == null) return;
            var parent = sweep.rectTransform.parent as RectTransform;
            float phase = Mathf.Repeat(sweepTime / 1.8f, 1f);
            sweep.rectTransform.anchoredPosition = new Vector2(
                Mathf.Lerp(-parent.rect.width * 0.6f, parent.rect.width * 0.6f, phase), 0f);
            var color = sweep.color;
            color.a = blend * 0.13f * Mathf.Sin(phase * Mathf.PI);
            sweep.color = color;
        }
    }
}
