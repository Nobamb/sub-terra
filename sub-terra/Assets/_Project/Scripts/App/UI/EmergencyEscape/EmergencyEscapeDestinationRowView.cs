using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SubTerra.App.UI.EmergencyEscape
{
    /// <summary>목적지 목록의 한 줄. 이름과 이동 불가 이유를 보여 주고 클릭을 알린다. 값 적용만 한다.</summary>
    public sealed class EmergencyEscapeDestinationRowView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private static readonly Color Cyan = new Color(0.42f, 0.94f, 1f, 1f);
        private static readonly Color Warn = new Color(1f, 0.55f, 0.4f, 1f);
        private static readonly Color Idle = new Color(0.03f, 0.07f, 0.1f, 0.85f);
        private static readonly Color Hover = new Color(0.06f, 0.2f, 0.25f, 0.95f);
        private static readonly Color Selected = new Color(0.1f, 0.3f, 0.38f, 0.95f);

        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private Image selectionBar;
        [SerializeField] private TMP_Text label;
        [SerializeField] private TMP_Text reasonLabel;

        private int index;
        private Action<int> clicked;
        private bool enabledRow = true;
        private bool isSelected;
        private bool hovered;

        public int Index => index;
        public bool IsEnabledRow => enabledRow;
        public string LabelText => label != null ? label.text : string.Empty;
        public string ReasonText => reasonLabel != null ? reasonLabel.text : string.Empty;

        public bool HasRequiredReferences()
        {
            return button != null && background != null && label != null && reasonLabel != null;
        }

        public void Bind(int rowIndex, string text, bool enabled, string reason, Action<int> onClicked)
        {
            index = rowIndex;
            clicked = onClicked;
            enabledRow = enabled;
            if (label != null)
            {
                label.text = text;
                // 이동할 수 없는 항목은 흐리게 한다. 선택(CCTV 미리보기)은 그대로 된다.
                label.alpha = enabled ? 1f : 0.45f;
            }

            if (reasonLabel != null)
            {
                reasonLabel.text = enabled ? string.Empty : reason;
                reasonLabel.color = Warn;
            }

            if (button != null)
            {
                button.onClick.RemoveListener(HandleClick);
                button.onClick.AddListener(HandleClick);
            }

            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            isSelected = selected;
            RefreshColors();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovered = true;
            RefreshColors();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovered = false;
            RefreshColors();
        }

        private void OnDisable()
        {
            hovered = false;
        }

        // 선택된 줄은 항상 밝고, 선택되지 않은 줄은 커서를 올리면 청록빛이 도는 면과 옅은 막대가 나타난다.
        private void RefreshColors()
        {
            if (background != null)
            {
                background.color = isSelected ? Selected : (hovered ? Hover : Idle);
            }

            if (selectionBar != null)
            {
                var alpha = isSelected ? 1f : (hovered ? 0.55f : 0f);
                selectionBar.color = new Color(Cyan.r, Cyan.g, Cyan.b, alpha);
            }
        }

        private void HandleClick()
        {
            clicked?.Invoke(index);
        }
    }
}
