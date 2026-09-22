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
    }
}
