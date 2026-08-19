using System.Collections.Generic;
using System.Text;
using SubTerra.App.Core.Data;
using SubTerra.App.Progression;
using SubTerra.App.Tutorial;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SubTerra.App.UI.Progression
{
    /// <summary>
    /// Surface Base·Mine 업그레이드 패널 View.
    /// prompt-B 33-3: 탭별 좌측 목록 시작 Y 고정, 심층 구역 전용 탭, Surface 레벨 요약 모드.
    /// prompt-B 33-4/후속: 하위 탭 런타임 배선, 심층 안내 단일 텍스트.
    /// </summary>
    public sealed class ProgressionPanelView : MonoBehaviour, IProgressionPanelView
    {
        /// <summary>드릴 탭 기준과 동일한 좌측 목록 시작 위치(top-left anchor).</summary>
        private const float EntryListStartY = -120f;
        private const float EntryListRowHeight = 50f;
        private const float EntryListColumnX = 20f;
        private const float EntryListWidth = 340f;
        private const float EntryListHeight = 44f;

        [SerializeField] private TMP_Text upgradeListText;
        [SerializeField] private TMP_Text detailText;
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private TMP_Text deepZoneText;
        [SerializeField] private GameObject deepZoneUnlockPopupRoot;
        [SerializeField] private TMP_Text deepZoneUnlockPopupText;
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
        /// prompt-B 33-3/55: Surface Base 하단 목록에 장비 레벨 요약만 표시
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
        private UnityAction[] categoryTabRuntimeActions;

        public UpgradeCategory ActiveCategory => activeCategory;
        public bool LevelsOnlySummary => levelsOnlySummary;
        public bool HideDeepZoneTab => hideDeepZoneTab;

        private void OnEnable()
        {
            // 직렬화 리스너가 깨져 있어도 탭·하위 탭 선택이 동작하도록 런타임 배선.
            RebuildEntryButtonCacheIfNeeded();
            WireCategoryTabsRuntime();
            if (!levelsOnlySummary)
            {
                ApplyCategoryFilterToButtons();
            }
        }

        private void OnDisable()
        {
            UnwireCategoryTabsRuntime();
        }

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
            // Surface Base: 하단 상태 영역에 장비 레벨만 표시(필요 자원·변화치 없음).
            // levelsOnlySummary면 목록·상세를 레벨 요약으로 고정하고 구매 UI는 숨긴다.
            if (levelsOnlySummary)
            {
                WriteLevelsOnlySummary(upgrades);
                ApplyCategoryFilterToButtons();
                RefreshPurchaseVisibility();
                RefreshDeepZoneVisibility();
                return;
            }

            EnsureUpgradeButtons(upgrades);

            if (upgradeButtons != null && upgradeButtons.Length > 0)
            {
                for (var i = 0; i < upgradeButtons.Length; i++)
                {
                    upgradeButtons[i]?.SetSnapshot(upgrades);
                }

                ApplyCategoryFilterToButtons();
                // prompt-B 33-4: 클릭 가능한 하위 탭(엔트리 버튼)만 남긴다.
                // 텍스트 요약 목록은 클릭이 안 되어 선택이 안 되는 것처럼 보이므로 숨긴다.
                if (upgradeListText != null)
                {
                    upgradeListText.gameObject.SetActive(false);
                    upgradeListText.text = string.Empty;
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

        private void EnsureUpgradeButtons(IReadOnlyList<UpgradeSnapshot> upgrades)
        {
            if (upgrades == null
                || hideUpgradeEntryList
                || upgradeButtons == null
                || upgradeButtons.Length == 0)
            {
                return;
            }

            ProgressionUpgradeEntryButton template = null;
            var entries = new List<ProgressionUpgradeEntryButton>();
            for (var i = 0; i < upgradeButtons.Length; i++)
            {
                var entry = upgradeButtons[i];
                if (entry == null)
                {
                    continue;
                }

                if (template == null)
                {
                    template = entry;
                }
                entries.Add(entry);
            }

            if (template == null)
            {
                return;
            }

            var binder = GetComponent<ProgressionPanelBinder>();
            for (var i = 0; i < upgrades.Count; i++)
            {
                var upgradeId = upgrades[i].UpgradeId;
                if (string.IsNullOrEmpty(upgradeId) || ContainsUpgrade(entries, upgradeId))
                {
                    continue;
                }

                var clone = Instantiate(template, template.transform.parent);
                clone.name = "UpgradeEntry_" + upgradeId.Replace('.', '_');
                clone.Configure(upgradeId, binder, clone.GetComponentInChildren<TMP_Text>(true));
                entries.Add(clone);
            }

            upgradeButtons = entries.ToArray();
        }

        private static bool ContainsUpgrade(
            IReadOnlyList<ProgressionUpgradeEntryButton> entries,
            string upgradeId)
        {
            for (var i = 0; i < entries.Count; i++)
            {
                if (entries[i] != null && entries[i].UpgradeId == upgradeId)
                {
                    return true;
                }
            }

            return false;
        }

        public void SetSelectedUpgrade(UpgradeSnapshot upgrade)
        {
            // Surface Base: 하단은 장비 레벨 요약만 유지(필요 자원·변화치·구매 없음).
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

            // prompt-B 33-1/33-4 Detail Card:
            // 상단 이름·레벨 / 현재→다음 / 장비 설명 / 필요 재료 / 구매 안내.
            // 설명은 기존 필요 재료 위치에, 필요 재료는 설명 바로 아래에 둔다.
            // 선택 직후 심층 텍스트가 덮지 않도록 deepZone 영역을 끈다.
            if (deepZoneText != null)
            {
                deepZoneText.gameObject.SetActive(false);
                deepZoneText.text = string.Empty;
            }

            var name = ItemDisplayNames.PreferDisplay(upgrade.UpgradeId, upgrade.DisplayName);
            if (detailText != null)
            {
                var description = ItemDisplayNames.UpgradeDescription(upgrade.UpgradeId);
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
                        .Append(description)
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
                        .Append(description)
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

                detailText.gameObject.SetActive(true);
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
            ApplyDeepZoneDisplay();
        }

        public void ShowDeepZoneUnlockPopup()
        {
            const string message = "심층 구역 잠금이 해제되었습니다";
            CreateDeepZoneUnlockPopupIfNeeded();
            if (deepZoneUnlockPopupText != null)
            {
                deepZoneUnlockPopupText.text = message;
            }

            if (deepZoneUnlockPopupRoot != null)
            {
                deepZoneUnlockPopupRoot.SetActive(true);
                deepZoneUnlockPopupRoot.transform.SetAsLastSibling();
                return;
            }

            SetPurchaseResult(message, string.Empty);
        }

        public void HideDeepZoneUnlockPopup()
        {
            if (deepZoneUnlockPopupRoot != null)
            {
                deepZoneUnlockPopupRoot.SetActive(false);
            }
        }

        private void CreateDeepZoneUnlockPopupIfNeeded()
        {
            if (deepZoneUnlockPopupRoot != null || resultText == null)
            {
                return;
            }

            var canvases = GetComponentsInParent<Canvas>(true);
            if (canvases == null || canvases.Length == 0)
            {
                return;
            }

            var root = new GameObject(
                "DeepZoneUnlockPopup_Runtime",
                typeof(RectTransform),
                typeof(Image),
                typeof(Canvas),
                typeof(GraphicRaycaster),
                typeof(Button));
            root.transform.SetParent(canvases[canvases.Length - 1].transform, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(520f, 180f);
            root.GetComponent<Image>().color = new Color(0.035f, 0.1f, 0.16f, 0.98f);
            var popupCanvas = root.GetComponent<Canvas>();
            popupCanvas.overrideSorting = true;
            popupCanvas.sortingOrder = UiLayerPriority.ModalPanel + 10;

            var label = Instantiate(resultText, root.transform);
            label.name = "Message";
            label.gameObject.SetActive(true);
            label.raycastTarget = false;
            var labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(28f, 28f);
            labelRect.offsetMax = new Vector2(-28f, -28f);
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 28f;

            deepZoneUnlockPopupRoot = root;
            deepZoneUnlockPopupText = label;
            root.GetComponent<Button>().onClick.AddListener(HideDeepZoneUnlockPopup);
            root.SetActive(false);
        }

        public void SetBusy(bool busy)
        {
            this.busy = busy;
            RefreshPurchaseButton();
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

        /// <summary>
        /// 좌측 하위 탭(장비 행) 클릭 진입점.
        /// Entry 버튼이 binder 직렬화 없이 View→Presenter로 선택한다.
        /// </summary>
        public void SelectUpgradeEntry(string upgradeId)
        {
            if (string.IsNullOrEmpty(upgradeId) || levelsOnlySummary)
            {
                return;
            }

            if (presenter != null)
            {
                presenter.SelectUpgrade(upgradeId);
                return;
            }

            // Presenter 미연결 시 Binder 폴백.
            var binder = GetComponent<ProgressionPanelBinder>()
                ?? GetComponentInParent<ProgressionPanelBinder>(true);
            binder?.SelectUpgrade(upgradeId);
        }

        /// <summary>
        /// 업그레이드 창을 다른 HUD보다 앞에 올리고 입력 레이캐스트를 확보한다.
        /// Surface Base 레벨 요약(levelsOnlySummary)은 모달이 아니라 본문 일부이므로
        /// ModalPanel sortingOrder를 올리지 않아 설정창 등 상위 모달을 가리지 않는다.
        /// </summary>
        public void BringToFront()
        {
            var root = panelRoot != null ? panelRoot : gameObject;

            // prompt-B 44: 레벨 요약은 SurfaceBaseContent 계층에 묶인 채 표시한다.
            // 독립 Canvas/overrideSorting을 쓰면 설정 모달(SettingsModal) 위로 올라온다.
            if (levelsOnlySummary)
            {
                KeepLevelSummaryInSurfaceBaseHierarchy(root);
                RebuildEntryButtonCacheIfNeeded();
                ApplyCategoryFilterToButtons();
                return;
            }

            root.transform.SetAsLastSibling();

            var canvas = root.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = root.AddComponent<Canvas>();
            }

            canvas.overrideSorting = true;
            if (canvas.sortingOrder < UiLayerPriority.ModalPanel)
            {
                canvas.sortingOrder = UiLayerPriority.ModalPanel;
            }

            // Nested Canvas에 GraphicRaycaster가 있어야 하위 버튼 클릭이 된다.
            if (root.GetComponent<GraphicRaycaster>() == null)
            {
                root.AddComponent<GraphicRaycaster>();
            }

            // 패널 자체 배경도 레이캐스트를 받아 뒤 HUD 클릭이 새지 않게 한다.
            var bg = root.GetComponent<Image>();
            if (bg != null)
            {
                bg.raycastTarget = true;
            }

            RebuildEntryButtonCacheIfNeeded();
            ApplyCategoryFilterToButtons();
        }

        /// <summary>
        /// Surface Base 레벨 요약을 부모(SurfaceBaseContent) 하위 형제로 유지하고
        /// 모달용 Nested Canvas를 제거해 설정창이 항상 위에 오도록 한다.
        /// </summary>
        private static void KeepLevelSummaryInSurfaceBaseHierarchy(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            // SurfaceBaseContent 안에서는 형제 순서만 정리(본문 안 최후방).
            // 패널 루트 밖으로 올리지 않는다.
            root.transform.SetAsLastSibling();

            var canvas = root.GetComponent<Canvas>();
            if (canvas != null)
            {
                // 기존 BringToFront로 붙었을 수 있는 모달 Canvas를 해제한다.
                canvas.overrideSorting = false;
                if (Application.isPlaying)
                {
                    Object.Destroy(canvas);
                }
                else
                {
                    Object.DestroyImmediate(canvas);
                }
            }

            var raycaster = root.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
            {
                if (Application.isPlaying)
                {
                    Object.Destroy(raycaster);
                }
                else
                {
                    Object.DestroyImmediate(raycaster);
                }
            }
        }

        public void SetVisible(bool visible)
        {
            var root = panelRoot != null ? panelRoot : gameObject;
            root.SetActive(visible);
            if (visible)
            {
                BringToFront();
            }
        }

        private void WriteLevelsOnlySummary(IReadOnlyList<UpgradeSnapshot> upgrades)
        {
            // prompt-B 33-4: 동일 레벨 요약이 좌·중 두 곳에 겹치지 않도록
            // 중앙(UpgradeList) 하나만 남기고 상세 카드는 끈다.
            var text = BuildLevelsOnlyText(upgrades);
            if (upgradeListText != null)
            {
                upgradeListText.gameObject.SetActive(true);
                upgradeListText.text = text;
            }

            if (detailText != null)
            {
                detailText.text = string.Empty;
                detailText.gameObject.SetActive(false);
            }

            if (resultText != null)
            {
                resultText.text = string.Empty;
                resultText.gameObject.SetActive(false);
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
                builder.AppendLine("심층 구역: 최대 레벨")
                    .Append("더 이상 업그레이드할 수 없습니다.");
                return builder.ToString();
            }

            builder.AppendLine("심층 구역: 잠금");
            if (!string.IsNullOrEmpty(access.Reason))
            {
                builder.AppendLine(access.Reason);
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
            ApplyDeepZoneDisplay();
        }

        /// <summary>
        /// 심층 구역 안내는 detailText 한곳에만 표시한다.
        /// deepZoneText와 동일 내용을 동시에 켜면 겹치므로 deepZoneText는 항상 끈다.
        /// </summary>
        private void ApplyDeepZoneDisplay()
        {
            var show = !levelsOnlySummary && UpgradeCategoryRules.IsDeepZoneTab(activeCategory);
            if (!show)
            {
                if (deepZoneText != null)
                {
                    deepZoneText.gameObject.SetActive(false);
                    deepZoneText.text = string.Empty;
                }

                return;
            }

            var text = BuildDeepZoneDetail(lastDeepZoneAccess);
            if (detailText != null)
            {
                detailText.gameObject.SetActive(true);
                detailText.text = text;
            }

            // 중복 영역 제거: 별도 deepZoneText는 비활성·비움.
            if (deepZoneText != null)
            {
                deepZoneText.gameObject.SetActive(false);
                deepZoneText.text = string.Empty;
            }

            if (resultText != null)
            {
                resultText.text = string.Empty;
            }

            // 심층 탭에서는 좌측 장비 목록을 숨긴다.
            if (upgradeListText != null)
            {
                upgradeListText.gameObject.SetActive(false);
            }
        }

        private void WireCategoryTabsRuntime()
        {
            UnwireCategoryTabsRuntime();
            if (categoryTabButtons == null || categoryTabButtons.Length == 0)
            {
                return;
            }

            categoryTabRuntimeActions = new UnityAction[categoryTabButtons.Length];
            for (var i = 0; i < categoryTabButtons.Length; i++)
            {
                var button = categoryTabButtons[i];
                if (button == null)
                {
                    continue;
                }

                var index = i;
                UnityAction action = () => SelectCategoryTab(index);
                categoryTabRuntimeActions[i] = action;
                button.onClick.AddListener(action);
            }
        }

        private void UnwireCategoryTabsRuntime()
        {
            if (categoryTabButtons == null || categoryTabRuntimeActions == null)
            {
                categoryTabRuntimeActions = null;
                return;
            }

            for (var i = 0; i < categoryTabButtons.Length
                && i < categoryTabRuntimeActions.Length; i++)
            {
                if (categoryTabButtons[i] != null && categoryTabRuntimeActions[i] != null)
                {
                    categoryTabButtons[i].onClick.RemoveListener(categoryTabRuntimeActions[i]);
                }
            }

            categoryTabRuntimeActions = null;
        }

        /// <summary>직렬화 배열이 비었거나 null 항목이 있으면 자식 엔트리로 재구성한다.</summary>
        private void RebuildEntryButtonCacheIfNeeded()
        {
            var found = GetComponentsInChildren<ProgressionUpgradeEntryButton>(true);
            if (found == null || found.Length == 0)
            {
                return;
            }

            var needsRebuild = upgradeButtons == null || upgradeButtons.Length != found.Length;
            if (!needsRebuild)
            {
                for (var i = 0; i < upgradeButtons.Length; i++)
                {
                    if (upgradeButtons[i] == null)
                    {
                        needsRebuild = true;
                        break;
                    }
                }
            }

            if (needsRebuild)
            {
                upgradeButtons = found;
            }

            for (var i = 0; i < upgradeButtons.Length; i++)
            {
                upgradeButtons[i]?.EnsureInteractable();
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

                // EntryListRoot 안에서는 로컬 Y만 재배치한다(루트 자체가 좌측 열).
                var rect = entry.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchorMin = new Vector2(0f, 1f);
                    rect.anchorMax = new Vector2(0f, 1f);
                    rect.pivot = new Vector2(0f, 1f);
                    var parentIsEntryRoot = entry.transform.parent != null
                        && entry.transform.parent.name == "EntryListRoot";
                    if (parentIsEntryRoot)
                    {
                        rect.anchoredPosition = new Vector2(
                            0f,
                            -visibleIndex * EntryListRowHeight);
                    }
                    else
                    {
                        rect.anchoredPosition = new Vector2(
                            EntryListColumnX,
                            EntryListStartY - visibleIndex * EntryListRowHeight);
                    }

                    rect.sizeDelta = new Vector2(EntryListWidth, EntryListHeight);
                    rect.localScale = Vector3.one;
                    rect.SetAsLastSibling();
                }

                // 선택 가능 상태를 명시적으로 복구한다.
                entry.EnsureInteractable();
                visibleIndex++;
            }

            // 엔트리 컨테이너를 상세 텍스트 앞으로, X 버튼보다는 뒤에 둔다.
            var root = panelRoot != null ? panelRoot.transform : transform;
            var entryRoot = root.Find("EntryListRoot") ?? transform.Find("EntryListRoot");
            if (entryRoot != null)
            {
                entryRoot.SetAsLastSibling();
            }

            var close = root.Find("CloseButton") ?? transform.Find("CloseButton");
            if (close != null)
            {
                close.SetAsLastSibling();
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
