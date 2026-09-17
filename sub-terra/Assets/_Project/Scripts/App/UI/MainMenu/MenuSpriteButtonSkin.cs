using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SubTerra.App.UI.MainMenu
{
    /// <summary>
    /// 104-4번: 메인 메뉴 액션 버튼의 off/on 스프라이트.
    /// 마우스 호버와 좌우 키 포커스를 같은 on 이미지로 표시한다.
    /// </summary>
    public sealed class MenuSpriteButtonSkin : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image overlay;
        private bool hovered;
        private bool keyboardActive;

        public bool IsHighlighted => hovered || keyboardActive;

        public void SetKeyboardActive(bool active)
        {
            keyboardActive = active;
            Refresh();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovered = true;
            Refresh();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovered = false;
            Refresh();
        }

        private void OnEnable() => Refresh();

        private void OnDisable()
        {
            hovered = false;
            Refresh();
        }

        private void Refresh()
        {
            if (overlay == null) return;
            overlay.color = IsHighlighted ? Color.white : new Color(1f, 1f, 1f, 0f);
        }
    }
}
