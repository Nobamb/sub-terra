using TMPro;
using UnityEngine;

namespace SubTerra.App.UI.Tutorial
{
    /// <summary>클리어 보상 1칸. 수량이 0이면 숨긴다.</summary>
    public sealed class QuestRewardSlotView : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text amountText;
        [SerializeField] private string mineralLabel;

        public void SetAmount(int amount)
        {
            var show = amount > 0;
            gameObject.SetActive(show);
            if (!show)
            {
                return;
            }

            if (nameText != null && !string.IsNullOrEmpty(mineralLabel))
            {
                nameText.text = mineralLabel;
            }

            if (amountText != null)
            {
                amountText.text = amount.ToString();
            }
        }

        /// <summary>보상 개수에 맞춘 칸 너비로 행 안에 놓고, 글자가 칸 밖으로 나가지 않게 한다.</summary>
        public void PlaceInRow(float x, float width)
        {
            var rect = (RectTransform)transform;
            rect.anchoredPosition = new Vector2(x, rect.anchoredPosition.y);
            rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);

            var textLeft = 16f;
            var icon = transform.Find("Icon") as RectTransform;
            if (icon != null)
            {
                var iconSize = Mathf.Min(52f, rect.sizeDelta.y - 16f);
                icon.sizeDelta = new Vector2(iconSize, iconSize);
                icon.anchoredPosition = new Vector2(14f, -(rect.sizeDelta.y - iconSize) * 0.5f);
                textLeft = 14f + iconSize + 10f;
            }

            var textWidth = Mathf.Max(24f, width - textLeft - 12f);
            FitLabel("Name", textLeft, textWidth);
            FitLabel("Amount", textLeft, textWidth);
        }

        private void FitLabel(string childName, float x, float width)
        {
            var child = transform.Find(childName) as RectTransform;
            if (child == null)
            {
                return;
            }

            child.anchoredPosition = new Vector2(x, child.anchoredPosition.y);
            child.sizeDelta = new Vector2(width, child.sizeDelta.y);
        }
    }
}
