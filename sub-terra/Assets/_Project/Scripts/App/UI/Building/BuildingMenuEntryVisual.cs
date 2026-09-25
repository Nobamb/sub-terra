using SubTerra.App.Core.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.Building
{
    /// <summary>
    /// prompt-B 110: 시설 목록 한 행의 아이콘·이름·비용 요약·선택 강조 표시.
    /// 선택 요청은 같은 오브젝트의 BuildingMenuEntryButton이 그대로 담당한다.
    /// </summary>
    [RequireComponent(typeof(BuildingMenuEntryButton))]
    public sealed class BuildingMenuEntryVisual : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private TMP_Text stateText;
        [SerializeField] private GameObject selectedHighlight;
        [SerializeField] private Color nameColor = Color.white;
        [SerializeField] private Color selectedNameColor = new Color(0.55f, 0.97f, 1f, 1f);
        [SerializeField] private Color readyStateColor = new Color(0.56f, 0.96f, 0.78f, 1f);
        [SerializeField] private Color missingStateColor = new Color(1f, 0.48f, 0.42f, 1f);

        private BuildingMenuEntryButton entry;

        public string BuildingId
        {
            get
            {
                if (entry == null)
                {
                    entry = GetComponent<BuildingMenuEntryButton>();
                }

                return entry != null ? entry.BuildingId : string.Empty;
            }
        }

        public bool IsSelected => selectedHighlight != null && selectedHighlight.activeSelf;

        public void Apply(BuildingMenuItemReadModel item)
        {
            if (item == null)
            {
                return;
            }

            if (icon != null)
            {
                icon.sprite = item.Icon;
                icon.enabled = item.Icon != null;
            }

            if (nameText != null)
            {
                nameText.text = ItemDisplayNames.Building(item.BuildingId);
            }

            if (costText != null)
            {
                costText.text = BuildingMenuDisplayFormatter.CostSummary(item.Costs);
            }

            if (stateText != null)
            {
                var enough = BuildingMenuDisplayFormatter.HasAllCosts(item.Costs);
                stateText.text = BuildingMenuDisplayFormatter.EntryStateLabel(item.Costs);
                stateText.color = enough ? readyStateColor : missingStateColor;
            }
        }

        public void SetSelected(bool selected)
        {
            if (selectedHighlight != null && selectedHighlight.activeSelf != selected)
            {
                selectedHighlight.SetActive(selected);
            }

            if (nameText != null)
            {
                nameText.color = selected ? selectedNameColor : nameColor;
            }
        }

        public bool HasRequiredReferences()
        {
            return icon != null && nameText != null && costText != null
                && stateText != null && selectedHighlight != null;
        }
    }
}
