using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.Building
{
    /// <summary>
    /// B-188: `>>` 전체 목록의 한 칸(아이콘·시설명·짧은 용도·보유/필요 비용).
    /// 설치 대상 선택은 같은 오브젝트의 BuildingMenuEntryButton이 그대로 하고, 이 칸은 상세 열기만 요청한다.
    /// </summary>
    [RequireComponent(typeof(BuildingMenuEntryButton))]
    public sealed class BuildingMenuCatalogRowView : MonoBehaviour
    {
        [SerializeField] private BuildingMenuView view;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text purposeText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private GameObject selectedHighlight;

        private BuildingMenuEntryButton entry;
        private Button button;

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
        public string CostText => costText != null ? costText.text : string.Empty;

        private void OnEnable()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (button != null)
            {
                button.onClick.RemoveListener(OpenDetail);
                button.onClick.AddListener(OpenDetail);
            }
        }

        private void OnDisable()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(OpenDetail);
            }
        }

        private void OpenDetail()
        {
            if (view != null)
            {
                view.OpenDetail(BuildingId);
            }
        }

        public void Apply(BuildingMenuItemReadModel item)
        {
            if (item == null)
            {
                return;
            }

            if (icon != null)
            {
                icon.sprite = item.Icon;
                icon.preserveAspect = true;
                icon.enabled = item.Icon != null;
            }

            if (nameText != null)
            {
                nameText.text = item.DisplayName;
            }

            if (purposeText != null)
            {
                purposeText.text = BuildingMenuDisplayFormatter.ShortPurpose(item.Description);
            }

            if (costText != null)
            {
                costText.text = BuildingMenuDisplayFormatter.CostLines(item.Costs);
            }
        }

        public void SetSelected(bool selected)
        {
            if (selectedHighlight != null && selectedHighlight.activeSelf != selected)
            {
                selectedHighlight.SetActive(selected);
            }
        }

        public void SetShown(bool shown)
        {
            if (gameObject.activeSelf != shown)
            {
                gameObject.SetActive(shown);
            }
        }

        public bool HasRequiredReferences()
        {
            return view != null && icon != null && nameText != null && purposeText != null
                && costText != null && selectedHighlight != null;
        }
    }
}
