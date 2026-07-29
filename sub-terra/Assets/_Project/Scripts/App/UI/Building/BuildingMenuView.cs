using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.Building
{
    /// <summary>건설 목록·상세·A/B 복합 가능 여부를 표시하는 B 소유 View.</summary>
    public sealed class BuildingMenuView : MonoBehaviour, IBuildingMenuView
    {
        [SerializeField] private TMP_Text buildingListText;
        [SerializeField] private TMP_Text selectionText;
        [SerializeField] private TMP_Text availabilityText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Image selectedIcon;
        [SerializeField] private Button cancelButton;
        [SerializeField] private GameObject panelRoot;

        public void SetBuildingList(IReadOnlyList<BuildingMenuItemReadModel> items)
        {
            if (buildingListText == null)
            {
                return;
            }

            var builder = new StringBuilder();
            if (items != null)
            {
                for (var i = 0; i < items.Count; i++)
                {
                    if (i > 0)
                    {
                        builder.AppendLine();
                    }

                    builder.Append(items[i].DisplayName)
                        .Append("  전력 ")
                        .Append(items[i].PowerDraw);
                }
            }

            buildingListText.text = builder.ToString();
        }

        public void SetSelection(BuildingMenuItemReadModel item)
        {
            if (item == null)
            {
                ClearSelection();
                return;
            }

            if (selectedIcon != null)
            {
                selectedIcon.sprite = item.Icon;
                selectedIcon.enabled = item.Icon != null;
            }

            if (selectionText != null)
            {
                var builder = new StringBuilder()
                    .Append(item.DisplayName)
                    .AppendLine()
                    .Append(item.Description)
                    .AppendLine()
                    .Append("전력 소비: ")
                    .Append(item.PowerDraw)
                    .AppendLine()
                    .Append("비용: ");

                for (var i = 0; i < item.Costs.Count; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(", ");
                    }

                    var cost = item.Costs[i];
                    builder.Append(cost.ItemId)
                        .Append(' ')
                        .Append(cost.Owned)
                        .Append('/')
                        .Append(cost.Required);
                }

                selectionText.text = builder.ToString();
            }

            if (cancelButton != null)
            {
                cancelButton.interactable = true;
            }
        }

        public void ClearSelection()
        {
            if (selectionText != null)
            {
                selectionText.text = "시설을 선택하세요.";
            }

            if (selectedIcon != null)
            {
                selectedIcon.sprite = null;
                selectedIcon.enabled = false;
            }

            if (cancelButton != null)
            {
                cancelButton.interactable = false;
            }
        }

        public void SetAvailability(BuildingAvailabilityReadModel availability)
        {
            if (availabilityText == null)
            {
                return;
            }

            if (availability.CanPlace)
            {
                availabilityText.text = "✓ 설치 가능";
                availabilityText.color = new Color(0.35f, 0.92f, 0.5f);
                return;
            }

            if (availability.PlacementState == Shared.BuildingPlacementState.None)
            {
                availabilityText.text = string.Empty;
                return;
            }

            var prefix = availability.CanAfford ? "⚠ 위치 확인" : "✕ 자원 부족";
            availabilityText.text = string.IsNullOrEmpty(availability.Message)
                ? prefix
                : prefix + "\n" + availability.Message;
            availabilityText.color = availability.CanAfford
                ? new Color(1f, 0.78f, 0.25f)
                : new Color(1f, 0.35f, 0.3f);
        }

        public void SetStatusMessage(string message)
        {
            if (statusText != null)
            {
                statusText.text = message ?? string.Empty;
            }
        }

        public void SetVisible(bool visible)
        {
            (panelRoot != null ? panelRoot : gameObject).SetActive(visible);
        }

        public bool HasRequiredReferences()
        {
            return buildingListText != null
                && selectionText != null
                && availabilityText != null
                && statusText != null
                && cancelButton != null
                && panelRoot != null;
        }
    }
}
