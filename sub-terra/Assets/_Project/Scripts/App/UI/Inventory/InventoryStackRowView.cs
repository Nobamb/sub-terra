using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.Inventory
{
    /// <summary>적재 내역 한 줄(아이콘·이름·개당 무게·수량·이 줄 무게)을 표시한다.</summary>
    public sealed class InventoryStackRowView : MonoBehaviour
    {
        [SerializeField] private string mineralId;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text quantityText;

        // prompt-B 190: 명세표 열. 수량 0인 행은 표에 올리지 않는다.
        [SerializeField] private Image rowBackground;
        [SerializeField] private TMP_Text unitWeightText;
        [SerializeField] private TMP_Text lineWeightText;
        [SerializeField] private Color ownedBackground = new Color(0.04f, 0.09f, 0.12f, 0.92f);
        [SerializeField] private Color ownedText = Color.white;

        public string MineralId => mineralId;

        public void SetStack(InventoryStackReadModel stack)
        {
            var owned = stack.Quantity > 0;
            if (gameObject.activeSelf != owned)
            {
                gameObject.SetActive(owned);
            }

            if (!owned)
            {
                return;
            }

            if (iconImage != null)
            {
                iconImage.sprite = stack.Icon;
                iconImage.enabled = stack.Icon != null;
                iconImage.color = Color.white;
            }

            if (nameText != null)
            {
                // TMP maxVisibleLines is runtime state, not a serialized prefab property.
                if (unitWeightText != null) nameText.maxVisibleLines = 2;
                nameText.text = string.IsNullOrEmpty(stack.DisplayName)
                    ? stack.MineralId
                    : stack.DisplayName;
                nameText.color = ownedText;
            }

            if (quantityText != null)
            {
                // 명세표 행은 "N 개" 형식, 기존 단일 텍스트 행은 "xN"을 유지한다.
                quantityText.text = unitWeightText != null
                    ? InventoryPanelDisplayFormatter.Quantity(stack.Quantity)
                    : "x" + stack.Quantity;
                quantityText.color = ownedText;
            }

            if (unitWeightText != null)
            {
                unitWeightText.text = InventoryPanelDisplayFormatter.UnitWeight(stack.UnitWeight);
            }

            if (lineWeightText != null)
            {
                lineWeightText.text = InventoryPanelDisplayFormatter.LineWeight(stack.Quantity, stack.UnitWeight);
            }

            if (rowBackground != null)
            {
                rowBackground.color = ownedBackground;
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

        public void EditorSetDetailReferences(Image background, TMP_Text unitWeight, TMP_Text lineWeight)
        {
            rowBackground = background;
            unitWeightText = unitWeight;
            lineWeightText = lineWeight;
        }
#endif
    }
}
