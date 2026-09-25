using System.Collections.Generic;
using System.Text;
using SubTerra.App.Core.Data;
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
        // prompt-B 33: X 버튼과 겹치는 민트 아이콘 제거 — 필드는 호환용으로만 남긴다.
        [SerializeField] private Image selectedIcon;
        // prompt-B 31-1/33: 건설 취소 버튼 제거. 참조가 있으면 숨긴다.
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private GameObject panelRoot;

        // prompt-B 110: 목록 행·상세 카드·비용 행·설치 상태 배너. 비어 있으면 기존 텍스트 표시로 동작한다.
        [SerializeField] private BuildingMenuEntryVisual[] entries = new BuildingMenuEntryVisual[0];
        [SerializeField] private Image detailIcon;
        [SerializeField] private TMP_Text detailNameText;
        [SerializeField] private TMP_Text detailPowerText;
        [SerializeField] private GameObject detailPowerChip;
        [SerializeField] private GameObject costSection;
        [SerializeField] private BuildingMenuCostRowView[] costRows = new BuildingMenuCostRowView[0];
        [SerializeField] private Image availabilityBanner;
        [SerializeField] private Image availabilityAccent;
        [SerializeField] private Image availabilityBadge;
        [SerializeField] private TMP_Text availabilityGlyph;
        [SerializeField] private Color idleColor = new Color(0.45f, 0.62f, 0.7f, 1f);
        [SerializeField] private Color readyColor = new Color(0.3f, 0.92f, 0.62f, 1f);
        [SerializeField] private Color warningColor = new Color(1f, 0.76f, 0.28f, 1f);
        [SerializeField] private Color blockedColor = new Color(1f, 0.38f, 0.32f, 1f);

        private string selectedBuildingId = string.Empty;
        private IReadOnlyList<BuildingCostReadModel> selectedCosts;

        public Button CloseButton => closeButton;
        public bool UsesStructuredLayout => entries != null && entries.Length > 0 && detailNameText != null;

        private void Awake()
        {
            HideLegacyChrome();
        }

        private void OnEnable()
        {
            HideLegacyChrome();
        }

        public void SetBuildingList(IReadOnlyList<BuildingMenuItemReadModel> items)
        {
            ApplyEntries(items);

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

            // 우측 상단 X와 겹치던 SelectedIcon은 더 이상 표시하지 않는다.
            if (selectedIcon != null)
            {
                selectedIcon.sprite = null;
                selectedIcon.enabled = false;
            }

            selectedBuildingId = item.BuildingId;
            RefreshEntrySelection();

            if (UsesStructuredLayout)
            {
                ShowDetail(item);
                return;
            }

            if (selectionText != null)
            {
                selectionText.text = FormatLegacySelection(item);
            }
        }

        public void ClearSelection()
        {
            selectedBuildingId = string.Empty;
            selectedCosts = null;
            RefreshEntrySelection();

            if (selectionText != null)
            {
                selectionText.text = UsesStructuredLayout
                    ? "왼쪽 목록에서 설치할 시설을 선택하세요.\n용도, 전력, 필요 자원과 설치 조건이 여기에 표시됩니다."
                    : "시설을 선택하세요.";
            }

            if (selectedIcon != null)
            {
                selectedIcon.sprite = null;
                selectedIcon.enabled = false;
            }

            if (detailIcon != null)
            {
                detailIcon.sprite = null;
                detailIcon.enabled = false;
            }

            if (detailNameText != null)
            {
                detailNameText.text = "시설 미선택";
            }

            SetActive(detailPowerChip, false);
            SetActive(costSection, false);
            HideCostRows(0);
        }

        private void HideLegacyChrome()
        {
            if (selectedIcon != null)
            {
                selectedIcon.enabled = false;
                selectedIcon.gameObject.SetActive(false);
            }

            if (cancelButton != null)
            {
                cancelButton.gameObject.SetActive(false);
            }
        }

        public void SetAvailability(BuildingAvailabilityReadModel availability)
        {
            if (availabilityText == null)
            {
                return;
            }

            if (UsesStructuredLayout)
            {
                ShowAvailabilityBanner(availability);
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

        public bool HasStructuredReferences()
        {
            if (!UsesStructuredLayout
                || detailIcon == null
                || detailPowerText == null
                || detailPowerChip == null
                || costSection == null
                || costRows == null
                || costRows.Length == 0
                || availabilityBanner == null
                || availabilityAccent == null
                || availabilityBadge == null
                || availabilityGlyph == null)
            {
                return false;
            }

            for (var i = 0; i < entries.Length; i++)
            {
                if (entries[i] == null || !entries[i].HasRequiredReferences())
                {
                    return false;
                }
            }

            for (var i = 0; i < costRows.Length; i++)
            {
                if (costRows[i] == null || !costRows[i].HasRequiredReferences())
                {
                    return false;
                }
            }

            return true;
        }

        private void ApplyEntries(IReadOnlyList<BuildingMenuItemReadModel> items)
        {
            if (entries == null || items == null)
            {
                return;
            }

            for (var i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                if (entry == null)
                {
                    continue;
                }

                for (var j = 0; j < items.Count; j++)
                {
                    if (items[j] != null && items[j].BuildingId == entry.BuildingId)
                    {
                        entry.Apply(items[j]);
                        break;
                    }
                }
            }

            RefreshEntrySelection();
        }

        private void RefreshEntrySelection()
        {
            if (entries == null)
            {
                return;
            }

            for (var i = 0; i < entries.Length; i++)
            {
                if (entries[i] != null)
                {
                    entries[i].SetSelected(
                        !string.IsNullOrEmpty(selectedBuildingId)
                        && entries[i].BuildingId == selectedBuildingId);
                }
            }
        }

        private void ShowDetail(BuildingMenuItemReadModel item)
        {
            if (detailIcon != null)
            {
                detailIcon.sprite = item.Icon;
                detailIcon.enabled = item.Icon != null;
            }

            selectedCosts = item.Costs;
            detailNameText.text = item.DisplayName;
            if (detailPowerText != null)
            {
                detailPowerText.text = BuildingMenuDisplayFormatter.PowerLabel(item.PowerDraw);
            }

            SetActive(detailPowerChip, true);

            if (selectionText != null)
            {
                selectionText.text = item.Description;
            }

            var count = item.Costs != null ? item.Costs.Count : 0;
            SetActive(costSection, count > 0);
            if (costRows == null)
            {
                return;
            }

            for (var i = 0; i < costRows.Length && i < count; i++)
            {
                if (costRows[i] != null)
                {
                    costRows[i].Show(item.Costs[i]);
                }
            }

            HideCostRows(count);
        }

        private void HideCostRows(int fromIndex)
        {
            if (costRows == null)
            {
                return;
            }

            for (var i = fromIndex < 0 ? 0 : fromIndex; i < costRows.Length; i++)
            {
                if (costRows[i] != null)
                {
                    costRows[i].Hide();
                }
            }
        }

        private void ShowAvailabilityBanner(BuildingAvailabilityReadModel availability)
        {
            var kind = BuildingMenuDisplayFormatter.Classify(availability);
            var color = ColorFor(kind);
            var message = availability.Message;
            if (kind == BuildingAvailabilityDisplayKind.NeedResources)
            {
                // 무엇이 얼마나 부족한지 읽기 모델 수량으로 구체화한다. 없으면 Presenter 사유를 그대로 쓴다.
                var shortage = BuildingMenuDisplayFormatter.ShortageDetail(selectedCosts);
                if (!string.IsNullOrEmpty(shortage))
                {
                    message = shortage;
                }
            }

            availabilityText.text = BuildingMenuDisplayFormatter.AvailabilityText(kind, message);
            availabilityText.color = kind == BuildingAvailabilityDisplayKind.Idle
                ? new Color(0.72f, 0.84f, 0.9f, 1f)
                : Color.Lerp(color, Color.white, 0.35f);

            if (availabilityBanner != null)
            {
                availabilityBanner.color = new Color(color.r * 0.22f, color.g * 0.22f, color.b * 0.22f, 0.92f);
            }

            if (availabilityAccent != null)
            {
                availabilityAccent.color = color;
            }

            if (availabilityBadge != null)
            {
                availabilityBadge.color = color;
            }

            if (availabilityGlyph != null)
            {
                availabilityGlyph.text = BuildingMenuDisplayFormatter.Glyph(kind);
            }
        }

        private Color ColorFor(BuildingAvailabilityDisplayKind kind)
        {
            switch (kind)
            {
                case BuildingAvailabilityDisplayKind.Ready:
                    return readyColor;
                case BuildingAvailabilityDisplayKind.NeedResources:
                    return blockedColor;
                case BuildingAvailabilityDisplayKind.CheckPlacement:
                    return warningColor;
                default:
                    return idleColor;
            }
        }

        private static string FormatLegacySelection(BuildingMenuItemReadModel item)
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
                builder.Append(ItemDisplayNames.Mineral(cost.ItemId))
                    .Append(' ')
                    .Append(cost.Owned)
                    .Append('/')
                    .Append(cost.Required);
            }

            return builder.ToString();
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }
    }
}
