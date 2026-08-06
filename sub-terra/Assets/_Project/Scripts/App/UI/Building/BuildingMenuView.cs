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
        [SerializeField] private Button closeButton;
        [SerializeField] private GameObject panelRoot;

        public Button CloseButton => closeButton;

        public void SetBuildingList(IReadOnlyList<BuildingMenuItemReadModel> items)
        {
            if (buildingListText == null)
            {
                return;
            }

            // 필요 전력은 우측 상세(selection)에 이미 표시하므로 목록에는 이름만.
            var builder = new StringBuilder();
            if (items != null)
            {
                for (var i = 0; i < items.Count; i++)
                {
                    if (i > 0)
                    {
                        builder.AppendLine();
                    }

                    builder.Append(items[i].DisplayName);
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

            var prefix = availability.CanAfford ? "⚠ 위치 확인" : "X 자원 부족";
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
            // panelRoot와 루트 GO를 함께 맞춰 X 버튼만 남거나 내용만 사라지는 상태를 막는다.
            if (panelRoot != null)
            {
                panelRoot.SetActive(visible);
            }

            if (gameObject.activeSelf != visible)
            {
                gameObject.SetActive(visible);
            }
        }

        public bool HasRequiredReferences()
        {
            // closeButton·cancelButton은 레이아웃 정책에 따라 선택 필드다.
            // prompt-B 31-1: 건설 취소 버튼 제거.
            return buildingListText != null
                && selectionText != null
                && availabilityText != null
                && statusText != null
                && panelRoot != null;
        }
    }
}
