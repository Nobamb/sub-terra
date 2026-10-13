using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.EmergencyEscape
{
    /// <summary>목적지 목록의 한 줄. 이름과 이동 불가 이유를 보여 주고 클릭을 알린다. 값 적용만 한다.</summary>
    public sealed class EmergencyEscapeDestinationRowView : MonoBehaviour
    {
        private static readonly Color Cyan = new Color(0.42f, 0.94f, 1f, 1f);
        private static readonly Color Warn = new Color(1f, 0.55f, 0.4f, 1f);

        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private Image selectionBar;
        [SerializeField] private TMP_Text label;
        [SerializeField] private TMP_Text reasonLabel;

        private int index;
        private Action<int> clicked;
        private bool enabledRow = true;

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
            if (background != null)
            {
                background.color = selected
                    ? new Color(0.1f, 0.3f, 0.38f, 0.95f)
                    : new Color(0.03f, 0.07f, 0.1f, 0.85f);
            }

            if (selectionBar != null)
            {
                selectionBar.color = selected ? Cyan : new Color(Cyan.r, Cyan.g, Cyan.b, 0f);
            }
        }

        private void HandleClick()
        {
            clicked?.Invoke(index);
        }
    }
}
