using UnityEngine;
using UnityEngine.EventSystems;

namespace SubTerra.App.UI.HUD
{
    /// <summary>제공된 두 이미지의 알파만 0.3초 동안 교차 전환한다.</summary>
    public sealed class SideMenuButtonView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public const float HoverSeconds = 0.3f;
        [SerializeField] private UnityEngine.UI.Image normal;
        [SerializeField] private UnityEngine.UI.Image hover;
        private UnityEngine.UI.Button button;
        private bool pointed;
        private float alpha;

        private void Awake() => button = GetComponent<UnityEngine.UI.Button>();
        public void OnPointerEnter(PointerEventData eventData) => pointed = true;
        public void OnPointerExit(PointerEventData eventData) => pointed = false;
        private void OnDisable()
        {
            pointed = false;
            alpha = 0f;
            Apply();
        }

        private void Update()
        {
            float target = pointed && button != null && button.IsInteractable() ? 1f : 0f;
            alpha = Mathf.MoveTowards(alpha, target, Time.unscaledDeltaTime / HoverSeconds);
            Apply();
        }

        private void Apply()
        {
            if (normal != null) normal.color = new Color(1f, 1f, 1f, 1f - alpha);
            if (hover != null) hover.color = new Color(1f, 1f, 1f, alpha);
        }
    }
}
