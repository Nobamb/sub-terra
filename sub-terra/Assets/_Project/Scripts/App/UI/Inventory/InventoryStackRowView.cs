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

        // prompt-B 112: 보유/미보유 상태를 색과 문구로 함께 구분한다.
        [SerializeField] private Image rowBackground;
        [SerializeField] private Image accentBar;
        [SerializeField] private TMP_Text unitWeightText;
        [SerializeField] private TMP_Text stateText;
        [SerializeField] private Color ownedBackground = new Color(0.06f, 0.125f, 0.16f, 1f);
        [SerializeField] private Color emptyBackground = new Color(0.03f, 0.05f, 0.065f, 1f);
        [SerializeField] private Color ownedText = Color.white;
        [SerializeField] private Color emptyText = new Color(0.48f, 0.58f, 0.63f, 1f);
        [SerializeField] private Color ownedState = new Color(0.45f, 0.95f, 1f, 1f);

        public string MineralId => mineralId;

        public void SetStack(InventoryStackReadModel stack)
        {
            var owned = stack.Quantity > 0;
            if (iconImage != null)
            {
                iconImage.sprite = stack.Icon;
                iconImage.enabled = stack.Icon != null;
                iconImage.color = owned ? Color.white : new Color(1f, 1f, 1f, 0.4f);
            }

            if (nameText != null)
            {
                nameText.text = string.IsNullOrEmpty(stack.DisplayName)
                    ? stack.MineralId
                    : stack.DisplayName;
                nameText.color = owned ? ownedText : emptyText;
            }

            if (quantityText != null)
            {
                // 신규 행은 "N 개" 형식, 기존 단일 텍스트 행은 "xN"을 유지한다.
                quantityText.text = stateText != null
                    ? InventoryPanelDisplayFormatter.Quantity(stack.Quantity)
                    : "x" + stack.Quantity;
                quantityText.color = owned ? ownedText : emptyText;
            }

            if (unitWeightText != null && stack.UnitWeight > 0f)
            {
                unitWeightText.text = InventoryPanelDisplayFormatter.UnitWeight(stack.UnitWeight);
            }

            if (stateText != null)
            {
                stateText.text = InventoryPanelDisplayFormatter.QuantityState(stack.Quantity);
                stateText.color = owned ? ownedState : emptyText;
            }

            if (rowBackground != null)
            {
                rowBackground.color = owned ? ownedBackground : emptyBackground;
            }

            if (accentBar != null)
            {
                accentBar.enabled = owned;
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

        public void EditorSetDetailReferences(Image background, Image accent, TMP_Text unitWeight, TMP_Text state)
        {
            rowBackground = background;
            accentBar = accent;
            unitWeightText = unitWeight;
            stateText = state;
        }
#endif
    }
}
