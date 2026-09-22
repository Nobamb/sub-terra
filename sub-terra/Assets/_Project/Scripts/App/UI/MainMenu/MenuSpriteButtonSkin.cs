using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SubTerra.App.UI.MainMenu
{
    /// <summary>
    /// off/on 스프라이트 오버레이. 마우스 호버와 좌우 키 포커스를 같은 on 이미지로 표시하고,
    /// 세이브 슬롯 선택과 같은 0.2초 동안 알파를 보간한다.
    /// </summary>
    public sealed class MenuSpriteButtonSkin : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public const float HighlightDurationSeconds = 0.2f;

        [SerializeField] private Image overlay;
        private bool hovered;
        private bool keyboardActive;
        private float currentAlpha;

        public bool IsHighlighted => hovered || keyboardActive;
        public float OverlayAlpha => overlay != null ? overlay.color.a : 0f;

        public void SetKeyboardActive(bool active)
        {
            keyboardActive = active;
            if (!isActiveAndEnabled || !Application.isPlaying)
                SnapToTarget();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovered = true;
            if (!Application.isPlaying)
                SnapToTarget();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovered = false;
            if (!Application.isPlaying)
                SnapToTarget();
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
                SnapToTarget();
            else
                ApplyAlpha(currentAlpha);
        }

        private void OnDisable()
        {
            hovered = false;
            keyboardActive = false;
            SnapToTarget();
        }

        private void Update()
        {
            if (overlay == null) return;
            float target = IsHighlighted ? 1f : 0f;
            currentAlpha = Mathf.MoveTowards(
                currentAlpha,
                target,
                Time.unscaledDeltaTime / HighlightDurationSeconds);
            ApplyAlpha(currentAlpha);
        }

        private void SnapToTarget()
        {
            currentAlpha = IsHighlighted ? 1f : 0f;
            ApplyAlpha(currentAlpha);
        }

        private void ApplyAlpha(float alpha)
        {
            if (overlay == null) return;
            var color = overlay.color;
            color.a = alpha;
            overlay.color = color;
        }
    }
}
