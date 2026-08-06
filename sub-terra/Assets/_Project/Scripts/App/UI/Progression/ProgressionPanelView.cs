using System.Collections.Generic;
using System.Text;
using SubTerra.App.Core.Data;
using SubTerra.App.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.Progression
{
    /// <summary>
    /// Surface Base·Mine 업그레이드 패널 View.
    /// prompt-B 33-3: 탭별 좌측 목록 시작 Y 고정, 심층 구역 전용 탭, Surface 레벨 요약 모드.
    /// </summary>
    public sealed class ProgressionPanelView : MonoBehaviour, IProgressionPanelView
    {
        /// <summary>드릴 탭 기준과 동일한 좌측 목록 시작 위치(top-left anchor).</summary>
        private const float EntryListStartY = -120f;
        private const float EntryListRowHeight = 46f;
        private const float EntryListColumnX = 20f;
        private const float EntryListWidth = 280f;
        private const float EntryListHeight = 40f;

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
        /// <summary>
        /// prompt-B 33-3: Surface Base 하단 목록에 7종 장비 레벨 요약만 표시
        /// (필요 자원·변화치 제외). true면 구매/상세 카드도 숨긴다.
        /// </summary>
        [SerializeField] private bool levelsOnlySummary;
        /// <summary>
        /// prompt-B 33-3: 심층 구역 탭을 숨긴다(Surface Base). 업그레이드 창에서는 false.
        /// </summary>
        [SerializeField] private bool hideDeepZoneTab;

        private bool hasSelection;
        private bool selectedAtMaximum;
        private bool selectedCanAfford;
        private bool busy;
        private UpgradeCategory activeCategory = UpgradeCategory.Drill;
        private ProgressionPanelPresenter presenter;
        private ZoneAccessResult lastDeepZoneAccess;

        public UpgradeCategory ActiveCategory => activeCategory;
        public bool LevelsOnlySummary => levelsOnlySummary;
        public bool HideDeepZoneTab => hideDeepZoneTab;

        public void BindPresenter(ProgressionPanelPresenter target)
        {
            presenter = target;
        }

        public void SetActiveCategory(UpgradeCategory category)
        {
            activeCategory = category;
            RefreshCategoryTabs();
            ApplyCategoryFilterToButtons();
            RefreshDeepZoneVisibility();
            RefreshPurchaseVisibility();
        }

        public void SetUpgradeList(IReadOnlyList<UpgradeSnapshot> upgrades)
        {
            // Surface Base: 하단 상태 영역에 7종 레벨만 표시(필요 자원·변화치 없음).
            // levelsOnlySummary면 목록·상세를 레벨 요약으로 고정하고 구매 UI는 숨긴다.
            if (levelsOnlySummary)
            {
                WriteLevelsOnlySummary(upgrades);
                ApplyCategoryFilterToButtons();
                RefreshPurchaseVisibility();
                RefreshDeepZoneVisibility();
                return;
            }

            if (upgradeButtons != null && upgradeButtons.Length > 0)
            {
                for (var i = 0; i < upgradeButtons.Length; i++)
                {
                    upgradeButtons[i]?.SetSnapshot(upgrades);
                }

                ApplyCategoryFilterToButtons();
                if (upgradeListText != null)
                {
                    // 목록 영역에도 항상 7종 레벨 요약을 보여 비용/변화치가 섞이지 않게 한다.
                    upgradeListText.text = BuildLevelsOnlyText(upgrades);
                }

                return;
            }

            if (upgradeListText == null)
            {
                return;
            }

            if (UpgradeCategoryRules.IsDeepZoneTab(activeCategory))
            {
                upgradeListText.text = "심층 구역 탭";
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
            // Surface Base: 하단은 7종 레벨 요약만 유지(필요 자원·변화치·구매 없음).
            if (levelsOnlySummary)
            {
                hasSelection = false;
                selectedAtMaximum = true;
                selectedCanAfford = false;
                RefreshPurchaseButton();
                RefreshSelectionHighlight(string.Empty);
                return;
            }

            if (UpgradeCategoryRules.IsDeepZoneTab(activeCategory))
            {
                hasSelection = false;
                selectedAtMaximum = true;
                selectedCanAfford = false;
                RefreshPurchaseButton();
                RefreshSelectionHighlight(string.Empty);
                return;
            }

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
            lastDeepZoneAccess = access;
            if (deepZoneText == null)
            {
                return;
            }

            // 심층 탭에서만 심층 안내를 보여 다른 탭·Surface Base와 섞이지 않게 한다.
            if (!UpgradeCategoryRules.IsDeepZoneTab(activeCategory) || levelsOnlySummary)
            {
                deepZoneText.gameObject.SetActive(false);
                return;
            }

            deepZoneText.gameObject.SetActive(true);
            deepZoneText.text = BuildDeepZoneDetail(access);
            if (detailText != null && UpgradeCategoryRules.IsDeepZoneTab(activeCategory))
            {
                detailText.text = BuildDeepZoneDetail(access);
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
            if (categoryIndex < 0 || categoryIndex > (int)UpgradeCategory.DeepZone)
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

        private void WriteLevelsOnlySummary(IReadOnlyList<UpgradeSnapshot> upgrades)
        {
            var text = BuildLevelsOnlyText(upgrades);
            if (upgradeListText != null)
            {
                upgradeListText.gameObject.SetActive(true);
                upgradeListText.text = text;
            }

            if (detailText != null)
            {
                detailText.gameObject.SetActive(true);
                detailText.text = text;
            }

            if (resultText != null)
            {
                resultText.text = string.Empty;
            }

            if (deepZoneText != null)
            {
                deepZoneText.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 드릴 속도 / 드릴 전력 효율 / 최대 전력 / 최대 화물 중량 /
        /// 드론 스캔 범위 / 드론 구조 보존 / 가스 저항 레벨만 표시.
        /// </summary>
        private static string BuildLevelsOnlyText(IReadOnlyList<UpgradeSnapshot> upgrades)
        {
            var builder = new StringBuilder();
            if (upgrades != null)
            {
                for (var i = 0; i < upgrades.Count; i++)
                {
                    var item = upgrades[i];
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

            return builder.Length > 0 ? builder.ToString() : "업그레이드 없음";
        }

        private static string BuildDeepZoneDetail(ZoneAccessResult access)
        {
            var builder = new StringBuilder();
            if (access.IsUnlocked)
            {
                builder.AppendLine("심층 구역: 잠금 해제");
            }
            else
            {
                builder.AppendLine("심층 구역: 잠금");
                if (!string.IsNullOrEmpty(access.Reason))
                {
                    builder.AppendLine(access.Reason);
                }
            }

            builder.AppendLine();
            builder.AppendLine("해금 조건");
            var rule = DeepZoneUnlockRule.Mvp;
            builder.Append("· 완료 목표 ").Append(rule.RequiredCompletedObjectives).AppendLine("개 이상");
            if (rule.UpgradeRequirements != null)
            {
                for (var i = 0; i < rule.UpgradeRequirements.Count; i++)
                {
                    var req = rule.UpgradeRequirements[i];
                    var display = ItemDisplayNames.PreferDisplay(req.UpgradeId, req.UpgradeId);
                    builder.Append("· ")
                        .Append(display)
                        .Append(" Lv.")
                        .Append(req.RequiredLevel)
                        .AppendLine(" 이상");
                }
            }

            builder.AppendLine();
            builder.Append("조건 충족 시 자동으로 잠금이 해제됩니다.");
            return builder.ToString();
        }

        private void RefreshPurchaseButton()
        {
            if (purchaseButton != null)
            {
                purchaseButton.interactable = hasSelection
                    && !busy
                    && !selectedAtMaximum
                    && selectedCanAfford
                    && !levelsOnlySummary
                    && !UpgradeCategoryRules.IsDeepZoneTab(activeCategory);
            }
        }

        private void RefreshPurchaseVisibility()
        {
            if (purchaseButton == null)
            {
                return;
            }

            // Surface 레벨 요약·심층 탭에서는 구매 버튼을 숨긴다.
            var show = !levelsOnlySummary && !UpgradeCategoryRules.IsDeepZoneTab(activeCategory);
            purchaseButton.gameObject.SetActive(show);
            RefreshPurchaseButton();
        }

        private void RefreshDeepZoneVisibility()
        {
            if (deepZoneText == null)
            {
                return;
            }

            var show = !levelsOnlySummary && UpgradeCategoryRules.IsDeepZoneTab(activeCategory);
            deepZoneText.gameObject.SetActive(show);
            if (show)
            {
                deepZoneText.text = BuildDeepZoneDetail(lastDeepZoneAccess);
                if (detailText != null)
                {
                    detailText.text = deepZoneText.text;
                }

                if (resultText != null)
                {
                    resultText.text = string.Empty;
                }
            }
        }

        private void RefreshCategoryTabs()
        {
            if (categoryTabButtons == null)
            {
                return;
            }

            // Surface Base 등에서 심층 탭을 숨긴 채 드릴 탭으로 되돌린다.
            if (hideDeepZoneTab && UpgradeCategoryRules.IsDeepZoneTab(activeCategory))
            {
                activeCategory = UpgradeCategory.Drill;
            }

            for (var i = 0; i < categoryTabButtons.Length; i++)
            {
                var button = categoryTabButtons[i];
                if (button == null)
                {
                    continue;
                }

                var isDeepTab = i == (int)UpgradeCategory.DeepZone;
                if (hideDeepZoneTab && isDeepTab)
                {
                    button.gameObject.SetActive(false);
                    continue;
                }

                button.gameObject.SetActive(true);
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

        /// <summary>
        /// 탭별 좌측 목록 버튼을 드릴 탭과 같은 시작 Y부터 다시 쌓아
        /// 카테고리 전환 시 목록이 아래로 밀리지 않게 한다.
        /// </summary>
        private void ApplyCategoryFilterToButtons()
        {
            if (upgradeButtons == null)
            {
                return;
            }

            var visibleIndex = 0;
            for (var i = 0; i < upgradeButtons.Length; i++)
            {
                var entry = upgradeButtons[i];
                if (entry == null)
                {
                    continue;
                }

                if (hideUpgradeEntryList
                    || levelsOnlySummary
                    || UpgradeCategoryRules.IsDeepZoneTab(activeCategory))
                {
                    entry.gameObject.SetActive(false);
                    continue;
                }

                var match = UpgradeCategoryRules.Matches(entry.UpgradeId, activeCategory);
                entry.gameObject.SetActive(match);
                if (!match)
                {
                    continue;
                }

                // 드릴 탭과 동일한 시작점에서 보이는 항목만 순서대로 배치.
                var rect = entry.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchorMin = new Vector2(0f, 1f);
                    rect.anchorMax = new Vector2(0f, 1f);
                    rect.pivot = new Vector2(0f, 1f);
                    rect.anchoredPosition = new Vector2(
                        EntryListColumnX,
                        EntryListStartY - visibleIndex * EntryListRowHeight);
                    rect.sizeDelta = new Vector2(EntryListWidth, EntryListHeight);
                }

                visibleIndex++;
            }
        }

#if UNITY_EDITOR
        public void EditorSetHideUpgradeEntryList(bool hide)
        {
            hideUpgradeEntryList = hide;
        }

        public void EditorSetLevelsOnlySummary(bool levelsOnly)
        {
            levelsOnlySummary = levelsOnly;
            if (levelsOnly)
            {
                hideUpgradeEntryList = true;
            }
        }

        public void EditorSetHideDeepZoneTab(bool hide)
        {
            hideDeepZoneTab = hide;
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
