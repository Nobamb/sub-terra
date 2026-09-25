using SubTerra.App.Core.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.Building
{
    /// <summary>prompt-B 110: 선택 시설의 비용 한 줄(광물 아이콘·보유/필요·충족 막대).</summary>
    public sealed class BuildingMenuCostRowView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text amountText;
        [SerializeField] private TMP_Text stateText;
        [SerializeField] private RectTransform fill;
        [SerializeField] private Image fillImage;
        [SerializeField] private Color enoughColor = new Color(0.56f, 0.96f, 0.78f, 1f);
        [SerializeField] private Color missingColor = new Color(1f, 0.48f, 0.42f, 1f);

        public void Show(BuildingCostReadModel cost)
        {
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            if (icon != null)
            {
                icon.sprite = cost.Icon;
                icon.enabled = cost.Icon != null;
            }

            if (nameText != null)
            {
                nameText.text = ItemDisplayNames.Mineral(cost.ItemId);
            }

            if (amountText != null)
            {
                amountText.text = BuildingMenuDisplayFormatter.CostAmount(cost);
            }

            var color = cost.IsEnough ? enoughColor : missingColor;
            if (stateText != null)
            {
                stateText.text = BuildingMenuDisplayFormatter.CostState(cost);
                stateText.color = color;
            }

            if (fill != null)
            {
                fill.anchorMax = new Vector2(BuildingMenuDisplayFormatter.CostFill(cost), fill.anchorMax.y);
            }

            if (fillImage != null)
            {
                fillImage.color = color;
            }
        }

        public void Hide()
        {
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }

        public bool HasRequiredReferences()
        {
            return icon != null && nameText != null && amountText != null
                && stateText != null && fill != null && fillImage != null;
        }
    }
}
