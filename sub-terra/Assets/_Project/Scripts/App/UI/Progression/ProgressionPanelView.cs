using System.Collections.Generic;
using System.Text;
using SubTerra.App.Core.Data;
using SubTerra.App.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.Progression
{
    /// <summary>Surface Base·Mine 업그레이드 패널 View. 탭 단위 필터와 한국어 비용을 표시한다.</summary>
    public sealed class ProgressionPanelView : MonoBehaviour, IProgressionPanelView
    {
        [SerializeField] private TMP_Text upgradeListText;
        [SerializeField] private TMP_Text detailText;
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private TMP_Text deepZoneText;
        [SerializeField] private Button purchaseButton;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private ProgressionUpgradeEntryButton[] upgradeButtons;
        [SerializeField] private Button[] categoryTabButtons;
        [SerializeField] private TMP_Text[] categoryTabLabels;
        /// <summary>
        /// prompt-B 33-2: Surface Base처럼 상세 카드만 쓸 때 좌측 목록 버튼을 숨긴다.
        /// </summary>
        [SerializeField] private bool hideUpgradeEntryList;

        private bool hasSelection;
        private bool selectedAtMaximum;
        private bool selectedCanAfford;
        private bool busy;
        private UpgradeCategory activeCategory = UpgradeCategory.Drill;
        private ProgressionPanelPresenter presenter;

        public UpgradeCategory ActiveCategory => activeCategory;

        public void BindPresenter(ProgressionPanelPresenter target)
        {
            presenter = target;
        }

        public void SetActiveCategory(UpgradeCategory category)
        {
            activeCategory = category;
            RefreshCategoryTabs();
            ApplyCategoryFilterToButtons();
        }

        public void SetUpgradeList(IReadOnlyList<UpgradeSnapshot> upgrades)
        {
            if (upgradeButtons != null && upgradeButtons.Length > 0)
            {
                for (var i = 0; i < upgradeButtons.Length; i++)
                {
                    upgradeButtons[i]?.SetSnapshot(upgrades);
                }

                ApplyCategoryFilterToButtons();
                if (upgradeListText != null)
                {
                    upgradeListText.text = "탭에서 업그레이드를 선택하세요.";
                }

                return;
            }

            if (upgradeListText == null)
            {
                return;
            }

            var builder = new StringBuilder();
            if (upgrades != null)
            {
                for (var i = 0; i < upgrades.Count; i++)
                {
                    var item = upgrades[i];
                    if (!UpgradeCategoryRules.Matches(item.UpgradeId, activeCategory))
                    {
                        continue;
                    }

                    if (builder.Length > 0)
                    {
                        builder.AppendLine();
                    }

                    builder.Append(ItemDisplayNames.PreferDisplay(item.UpgradeId, item.DisplayName))
                        .Append("  Lv.")
                        .Append(item.CurrentLevel)
                        .Append('/')
                        .Append(item.MaximumLevel);
                }
            }

            upgradeListText.text = builder.Length > 0
                ? builder.ToString()
                : "이 탭에 업그레이드가 없습니다.";
        }

        public void SetSelectedUpgrade(UpgradeSnapshot upgrade)
        {
            // prompt-B 33-1 Detail Card:
            // 상단 이름·레벨 / 중단 현재→다음·재료 / 하단 구매 안내.
            var name = ItemDisplayNames.PreferDisplay(upgrade.UpgradeId, upgrade.DisplayName);
            if (detailText != null)
            {
                var builder = new StringBuilder()
                    .Append(name)
                    .Append("  Lv.")
                    .Append(upgrade.CurrentLevel)
                    .Append('/')
                    .Append(upgrade.MaximumLevel);

                if (upgrade.IsMaximumLevel)
                {
                    builder.AppendLine()
                        .Append("현재 수치 ")
                        .Append(upgrade.CurrentEffectValue.ToString("0.##"))
                        .AppendLine()
                        .Append("최대 레벨입니다.");
                }
                else
                {
                    var delta = upgrade.NextEffectValue - upgrade.CurrentEffectValue;
                    var deltaSign = delta >= 0f ? "+" : string.Empty;
                    builder.AppendLine()
                        .Append("현재 ")
                        .Append(upgrade.CurrentEffectValue.ToString("0.##"))
                        .Append("  →  다음 ")
                        .Append(upgrade.NextEffectValue.ToString("0.##"))
                        .Append(" (")
                        .Append(deltaSign)
                        .Append(delta.ToString("0.##"))
                        .Append(')')
                        .AppendLine()
                        .Append("필요 재료: ");

                    if (upgrade.NextCosts == null || upgrade.NextCosts.Count == 0)
                    {
                        builder.Append("없음");
                    }
                    else
                    {
                        for (var i = 0; i < upgrade.NextCosts.Count; i++)
                        {
                            if (i > 0)
                            {
                                builder.Append(", ");
                            }

                            var cost = upgrade.NextCosts[i];
                            builder.Append(ItemDisplayNames.Mineral(cost.ItemId))
                                .Append(" x")
                                .Append(cost.Quantity);
                        }
                    }

                    builder.AppendLine()
                        .Append(upgrade.CanAffordNextLevel
                            ? "구매 가능"
                            : "자원 부족 (인벤토리 보유량 기준)");
                }

                detailText.text = builder.ToString();
            }

            hasSelection = !string.IsNullOrEmpty(upgrade.UpgradeId);
            selectedAtMaximum = upgrade.IsMaximumLevel;
            selectedCanAfford = upgrade.CanAffordNextLevel;
            RefreshPurchaseButton();
            RefreshSelectionHighlight(upgrade.UpgradeId);
        }

        public void SetPurchaseResult(string message, string detail)
        {
            if (resultText != null)
            {
                resultText.text = string.IsNullOrEmpty(detail)
                    ? message ?? string.Empty
                    : (message ?? string.Empty) + "\n" + detail;
            }
        }

        public void SetDeepZoneAccess(ZoneAccessResult access)
        {
            if (deepZoneText != null)
            {
                deepZoneText.text = access.IsUnlocked
                    ? "심층 구역: 잠금 해제"
                    : "심층 구역: " + access.Reason;
            }
        }

        public void SetBusy(bool busy)
        {
            this.busy = busy;
            RefreshPurchaseButton();
        }

        public void SetVisible(bool visible)
        {
            (panelRoot != null ? panelRoot : gameObject).SetActive(visible);
        }

        /// <summary>탭 버튼 클릭 (Inspector persistent 리스너용 인덱스 오버로드).</summary>
        public void SelectCategoryTab(int categoryIndex)
        {
            if (categoryIndex < 0 || categoryIndex > (int)UpgradeCategory.Hazard)
            {
                return;
            }

            var category = (UpgradeCategory)categoryIndex;
            if (presenter != null)
            {
                presenter.SelectCategory(category);
            }
            else
            {
                SetActiveCategory(category);
            }
        }

        private void RefreshPurchaseButton()
        {
            if (purchaseButton != null)
            {
                purchaseButton.interactable = hasSelection
                    && !busy
                    && !selectedAtMaximum
                    && selectedCanAfford;
            }
        }

        private void RefreshCategoryTabs()
        {
            if (categoryTabButtons == null)
            {
                return;
            }

            for (var i = 0; i < categoryTabButtons.Length; i++)
            {
                var button = categoryTabButtons[i];
                if (button == null)
                {
                    continue;
                }

                var image = button.GetComponent<Image>();
                if (image != null)
                {
                    image.color = i == (int)activeCategory
                        ? new Color(0.22f, 0.42f, 0.55f, 1f)
                        : new Color(0.12f, 0.18f, 0.24f, 0.95f);
                }
            }

            if (categoryTabLabels != null)
            {
                for (var i = 0; i < categoryTabLabels.Length
                    && i < UpgradeCategoryRules.TabLabels.Length; i++)
                {
                    if (categoryTabLabels[i] != null)
                    {
                        categoryTabLabels[i].text = UpgradeCategoryRules.TabLabels[i];
                    }
                }
            }
        }

        private void ApplyCategoryFilterToButtons()
        {
            if (upgradeButtons == null)
            {
                return;
            }

            for (var i = 0; i < upgradeButtons.Length; i++)
            {
                var entry = upgradeButtons[i];
                if (entry == null)
                {
                    continue;
                }

                if (hideUpgradeEntryList)
                {
                    entry.gameObject.SetActive(false);
                    continue;
                }

                var match = UpgradeCategoryRules.Matches(entry.UpgradeId, activeCategory);
                entry.gameObject.SetActive(match);
            }
        }

#if UNITY_EDITOR
        public void EditorSetHideUpgradeEntryList(bool hide)
        {
            hideUpgradeEntryList = hide;
        }
#endif

        private void RefreshSelectionHighlight(string selectedUpgradeId)
        {
            if (upgradeButtons == null)
            {
                return;
            }

            for (var i = 0; i < upgradeButtons.Length; i++)
            {
                var entry = upgradeButtons[i];
                if (entry == null)
                {
                    continue;
                }

                entry.SetSelected(
                    !string.IsNullOrEmpty(selectedUpgradeId)
                    && entry.UpgradeId == selectedUpgradeId);
            }
        }
    }
}
