using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.Inventory
{
    /// <summary>하나의 광물 썸네일·이름·수량 행을 표시한다.</summary>
    public sealed class InventoryStackRowView : MonoBehaviour
    {
        [SerializeField] private string mineralId;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text quantityText;

        public string MineralId => mineralId;

        public void SetStack(InventoryStackReadModel stack)
        {
            if (iconImage != null)
            {
                iconImage.sprite = stack.Icon;
                iconImage.enabled = stack.Icon != null;
            }

            if (nameText != null)
            {
                nameText.text = string.IsNullOrEmpty(stack.DisplayName)
                    ? stack.MineralId
                    : stack.DisplayName;
            }

            if (quantityText != null)
            {
                quantityText.text = "x" + stack.Quantity;
            }
        }

#if UNITY_EDITOR
        public void EditorSetReferences(string id, Image icon, TMP_Text displayName, TMP_Text quantity)
        {
            mineralId = id;
            iconImage = icon;
            nameText = displayName;
            quantityText = quantity;
        }
#endif
    }
}
