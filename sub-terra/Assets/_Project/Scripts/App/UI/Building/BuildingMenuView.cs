using System.Collections.Generic;
using System.Text;
using SubTerra.App.Audio;
using SubTerra.App.Core.Data;
using SubTerra.Shared.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.Building
{
    /// <summary>
    /// 건설 목록·상세·A/B 복합 가능 여부를 표시하는 B 소유 View.
    /// B-188: 왼쪽 아이콘 레일 + 고른 시설 카드가 기본이고, `>>`로 전체 목록·검색·상세 큰 창을 펼친다.
    /// 선택·비용·설치 판정은 Presenter 읽기 모델을 그대로 표시만 한다.
    /// </summary>
    public sealed class BuildingMenuView : MonoBehaviour, IBuildingMenuView
    {
        public const string SearchPauseOwner = "building.search";

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

        // prompt-B 110: 레일 아이콘·카드·비용 행·설치 상태. 비어 있으면 기존 텍스트 표시로 동작한다.
        [SerializeField] private BuildingMenuEntryVisual[] entries = new BuildingMenuEntryVisual[0];
        [SerializeField] private Image detailIcon;
        [SerializeField] private TMP_Text detailNameText;
        [SerializeField] private TMP_Text detailOperatingConditionText;
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

        // B-188: 아이콘 레일·작은 카드·전체 목록/검색/상세 큰 창.
        [SerializeField] private BuildingModeSlideMotion slideMotion;
        [SerializeField] private RectTransform rail;
        [SerializeField] private Button expandButton;
        [SerializeField] private TMP_Text expandLabel;
        [SerializeField] private GameObject hintChip;
        [SerializeField] private GameObject card;
        [SerializeField] private GameObject statusChip;
        [SerializeField] private GameObject catalogWindow;
        [SerializeField] private GameObject listPage;
        [SerializeField] private TMP_InputField searchField;
        [SerializeField] private BuildingMenuCatalogRowView[] catalogRows = new BuildingMenuCatalogRowView[0];
        [SerializeField] private GameObject emptyResult;
        [SerializeField] private GameObject detailPage;
        [SerializeField] private Button backButton;
        [SerializeField] private Image detailThumb;
        [SerializeField] private TMP_Text detailPageName;
        [SerializeField] private TMP_Text detailPageCondition;
        [SerializeField] private TMP_Text detailPageFunction;
        [SerializeField] private BuildingMenuCostRowView[] detailCostRows = new BuildingMenuCostRowView[0];
        [SerializeField] private TMP_Text detailPageStatus;
        [SerializeField] private Image detailPageStatusAccent;
        // 레일 아이콘 호버 시 오른쪽에 뜨는 홀로그램 이름표.
        [SerializeField] private BuildingRailHologram railHologram;

        private readonly BuildingModeNavigation navigation = new BuildingModeNavigation();
        private readonly List<BuildingMenuItemReadModel> cachedItems = new List<BuildingMenuItemReadModel>();
        private string selectedBuildingId = string.Empty;
        private IReadOnlyList<BuildingCostReadModel> selectedCosts;
        private BuildingAvailabilityReadModel latestAvailability;
        private bool hasAvailability;
        private bool controlsWired;
        private bool searchPauseHeld;
        private bool railHoverActive;

        public Button CloseButton => closeButton;
        public bool UsesStructuredLayout => entries != null && entries.Length > 0 && detailNameText != null;
        public bool UsesBuildingMode => rail != null && catalogWindow != null;
        public BuildingModeNavigation Navigation => navigation;
        public TMP_InputField SearchField => searchField;
        public Button ExpandButton => expandButton;
        public Button BackButton => backButton;
        public string SelectedBuildingId => selectedBuildingId;
        public BuildingRailHologram RailHologram => railHologram;

        private void Awake()
        {
            HideLegacyChrome();
            WireControls();
            ApplyPage();
        }

        private void OnEnable()
        {
            HideLegacyChrome();
            WireControls();
            ApplyPage();
            PlayOpenMotion();
        }

        private void OnDisable()
        {
            ReleaseSearchPause();
        }

        private void Update()
        {
            // 검색창에 글자를 치는 동안에는 B·C·이동 키가 월드로 가지 않게 기존 UI 정지 게이트를 잡는다.
            var typing = searchField != null
                && searchField.isFocused
                && navigation.Page == BuildingModePage.List
                && !IsClosing();
            if (typing && !searchPauseHeld)
            {
                UiPauseGate.Acquire(SearchPauseOwner);
                searchPauseHeld = UiPauseGate.IsHeldBy(SearchPauseOwner);
            }
            else if (!typing && searchPauseHeld)
            {
                ReleaseSearchPause();
            }
        }

        public void SetBuildingList(IReadOnlyList<BuildingMenuItemReadModel> items)
        {
            cachedItems.Clear();
            if (items != null)
            {
                for (var i = 0; i < items.Count; i++)
                {
                    if (items[i] != null)
                    {
                        cachedItems.Add(items[i]);
                    }
                }
            }

            ApplyEntries(items);
            ApplyCatalogRows();
            ApplySearchFilter();
            RenderDetail();

            if (buildingListText == null)
            {
                return;
            }

            // 목록에는 시설 이름만 표시한다.
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
            ReplaceCachedItem(item);
            RefreshEntrySelection();

            if (UsesStructuredLayout)
            {
                ShowDetail(item);
            }
            else if (selectionText != null)
            {
                selectionText.text = FormatLegacySelection(item);
            }

            // 상세를 보는 중 레일에서 다른 시설을 고르면 상세도 그 시설로 바꾼다.
            if (navigation.Page == BuildingModePage.Detail)
            {
                navigation.ShowDetail(item.BuildingId);
            }

            ApplyPage();
            RenderDetail();
        }

        public void ClearSelection()
        {
            selectedBuildingId = string.Empty;
            selectedCosts = null;
            hasAvailability = false;
            RefreshEntrySelection();

            if (selectionText != null)
            {
                selectionText.text = UsesBuildingMode
                    ? string.Empty
                    : UsesStructuredLayout
                        ? "왼쪽 목록에서 설치할 시설을 선택하세요.\n용도, 필요 자원과 설치 조건이 여기에 표시됩니다."
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

            if (detailOperatingConditionText != null)
            {
                detailOperatingConditionText.text = string.Empty;
                SetActive(detailOperatingConditionText.gameObject, false);
            }
            SetActive(costSection, false);
            HideCostRows(costRows, 0);
            ApplyPage();
            RenderDetail();
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

            // B-188: 레일에는 아이콘과 `>>`만 둔다. 닫기는 B(또는 기존 메뉴 버튼)가 맡는다.
            if (UsesBuildingMode && closeButton != null)
            {
                closeButton.gameObject.SetActive(false);
            }
        }

        public void SetAvailability(BuildingAvailabilityReadModel availability)
        {
            latestAvailability = availability;
            hasAvailability = availability.PlacementState != Shared.BuildingPlacementState.None;
            RenderDetailStatus();

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

            ApplyPage();
        }

        public void SetVisible(bool visible)
        {
            if (!visible)
            {
                ReleaseSearchPause();
                HideRailHoverNow();
                if (UsesBuildingMode)
                {
                    Motion().PlayClose(HideImmediately);
                }
                else
                {
                    TerminalWindowOpenMotion.Close(gameObject, HideImmediately);
                }

                return;
            }

            var wasVisible = gameObject.activeSelf && (panelRoot == null || panelRoot.activeSelf) && !IsClosing();
            if (!wasVisible)
            {
                // 다시 열면 언제나 레일+카드(목록 접힘)에서 시작한다.
                ResetMode();
            }

            // panelRoot와 루트 GO를 함께 맞춰 X 버튼만 남거나 내용만 사라지는 상태를 막는다.
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }

            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
            else if (!wasVisible)
            {
                PlayOpenMotion();
            }
        }

        /// <summary>기본 레일(목록 접힘)에서 아이콘에 마우스를 올리면 오른쪽에 그 시설 이름을 홀로그램으로 띄운다.</summary>
        public void ShowRailHover(string buildingId, RectTransform anchor)
        {
            if (railHologram == null)
            {
                return;
            }

            if (!UsesBuildingMode || navigation.Page != BuildingModePage.Rail || IsClosing() || anchor == null)
            {
                railHologram.HideNow();
                return;
            }

            var item = FindItem(buildingId);
            var displayName = item != null && !string.IsNullOrEmpty(item.DisplayName)
                ? item.DisplayName
                : ItemDisplayNames.Building(buildingId);
            railHologram.Show(anchor, displayName);
            SetRailHoverActive(railHologram.IsShown);
        }

        public void HideRailHover(RectTransform anchor)
        {
            if (railHologram != null)
            {
                railHologram.Hide(anchor);
                SetRailHoverActive(railHologram.IsShown && !railHologram.IsClosing);
            }
        }

        private void HideRailHoverNow()
        {
            if (railHologram != null)
            {
                railHologram.HideNow();
            }

            railHoverActive = false;
        }

        private void SetRailHoverActive(bool active)
        {
            if (railHoverActive == active)
            {
                return;
            }

            railHoverActive = active;
            ApplyPage();
        }

        /// <summary>B-188 `>>`/`<<`: 전체 목록을 펼치거나 레일+카드로 접는다.</summary>
        public void ToggleExpanded()
        {
            navigation.ToggleExpanded();
            ApplyPage();
            ApplySearchFilter();
            RenderDetail();
            UiKeyboardSubmitGuard.ClearSelection();
        }

        /// <summary>목록 칸을 고르면 같은 큰 창이 그 시설의 상세로 바뀐다. 설치 대상 선택은 행 버튼이 함께 바꾼다.</summary>
        public void OpenDetail(string buildingId)
        {
            if (string.IsNullOrEmpty(buildingId))
            {
                return;
            }

            navigation.ShowDetail(buildingId);
            ApplyPage();
            RenderDetail();
            UiKeyboardSubmitGuard.ClearSelection();
        }

        /// <summary>상세의 `시설 목록`: 방금 보던 목록(검색어 유지)으로만 돌아가고 건설 모드는 닫지 않는다.</summary>
        public void BackToList()
        {
            navigation.BackToList();
            ApplyPage();
            ApplySearchFilter();
            UiKeyboardSubmitGuard.ClearSelection();
        }

        public void SetSearchQuery(string query)
        {
            query = query ?? string.Empty;
            if (searchField != null && searchField.text != query)
            {
                searchField.SetTextWithoutNotify(query);
            }

            OnSearchChanged(query);
        }

        private void OnSearchChanged(string query)
        {
            navigation.SetQuery(query);
            ApplySearchFilter();
        }

        private void HideImmediately()
        {
            ResetMode();
            if (panelRoot != null) panelRoot.SetActive(false);
            gameObject.SetActive(false);
        }

        private void ResetMode()
        {
            ReleaseSearchPause();
            HideRailHoverNow();
            navigation.Reset();
            if (searchField != null && !string.IsNullOrEmpty(searchField.text))
            {
                searchField.SetTextWithoutNotify(string.Empty);
            }

            ApplyPage();
            ApplySearchFilter();
        }

        private void PlayOpenMotion()
        {
            if (UsesBuildingMode)
            {
                Motion().PlayOpen();
            }
            else
            {
                TerminalWindowOpenMotion.Open(gameObject);
            }
        }

        private BuildingModeSlideMotion Motion()
        {
            if (slideMotion == null)
            {
                slideMotion = GetComponent<BuildingModeSlideMotion>();
                if (slideMotion == null)
                {
                    slideMotion = gameObject.AddComponent<BuildingModeSlideMotion>();
                }
            }

            slideMotion.Configure(panelRoot != null ? panelRoot.transform as RectTransform : transform as RectTransform);
            return slideMotion;
        }

        private bool IsClosing()
        {
            return UsesBuildingMode
                ? slideMotion != null && slideMotion.IsClosing
                : TerminalWindowOpenMotion.IsClosingWindow(gameObject);
        }

        private void ReleaseSearchPause()
        {
            if (!searchPauseHeld)
            {
                return;
            }

            searchPauseHeld = false;
            UiPauseGate.Release(SearchPauseOwner);
        }

        private void WireControls()
        {
            if (controlsWired)
            {
                return;
            }

            controlsWired = true;
            if (expandButton != null)
            {
                expandButton.onClick.RemoveListener(ToggleExpanded);
                expandButton.onClick.AddListener(ToggleExpanded);
                UiKeyboardSubmitGuard.ConfigurePointerPreferredButton(expandButton);
                UiSfx.Attach(expandButton, SfxId.UiClick);
            }

            if (backButton != null)
            {
                backButton.onClick.RemoveListener(BackToList);
                backButton.onClick.AddListener(BackToList);
                UiKeyboardSubmitGuard.ConfigurePointerPreferredButton(backButton);
                UiSfx.Attach(backButton, SfxId.UiClick);
            }

            if (searchField != null)
            {
                searchField.onValueChanged.RemoveListener(OnSearchChanged);
                searchField.onValueChanged.AddListener(OnSearchChanged);
            }
        }

        private void ApplyPage()
        {
            if (!UsesBuildingMode)
            {
                return;
            }

            var page = navigation.Page;
            var hasSelection = !string.IsNullOrEmpty(selectedBuildingId);
            if (page != BuildingModePage.Rail)
            {
                // 큰 창을 펼치면 이름표는 목록이 대신하므로 바로 지운다.
                HideRailHoverNow();
            }

            SetActive(catalogWindow, page != BuildingModePage.Rail);
            SetActive(listPage, page == BuildingModePage.List);
            SetActive(detailPage, page == BuildingModePage.Detail);
            SetActive(card, page == BuildingModePage.Rail && hasSelection);
            // "시설을 고르세요"는 호버 이름표가 떠 있는 동안 비켜 준다(이름표에 반쯤 가려 글자가 잘려 보이지 않게).
            SetActive(hintChip, page == BuildingModePage.Rail && !hasSelection && !railHoverActive);
            SetActive(statusChip, page == BuildingModePage.Rail
                && statusText != null && !string.IsNullOrEmpty(statusText.text));
            if (expandLabel != null)
            {
                expandLabel.text = navigation.ExpandLabel;
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
                || detailOperatingConditionText == null
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

        /// <summary>B-188 레일·카드·목록·검색·상세 참조가 모두 연결됐는지 확인한다.</summary>
        public bool HasBuildingModeReferences()
        {
            if (!UsesBuildingMode
                || slideMotion == null
                || expandButton == null
                || expandLabel == null
                || hintChip == null
                || card == null
                || statusChip == null
                || listPage == null
                || searchField == null
                || emptyResult == null
                || detailPage == null
                || backButton == null
                || detailThumb == null
                || detailPageName == null
                || detailPageCondition == null
                || detailPageFunction == null
                || detailPageStatus == null
                || detailPageStatusAccent == null
                || catalogRows == null
                || catalogRows.Length != (entries != null ? entries.Length : 0)
                || detailCostRows == null
                || detailCostRows.Length == 0)
            {
                return false;
            }

            for (var i = 0; i < catalogRows.Length; i++)
            {
                if (catalogRows[i] == null || !catalogRows[i].HasRequiredReferences())
                {
                    return false;
                }
            }

            for (var i = 0; i < detailCostRows.Length; i++)
            {
                if (detailCostRows[i] == null || !detailCostRows[i].HasRequiredReferences())
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

                var item = FindItem(entry.BuildingId);
                if (item != null)
                {
                    entry.Apply(item);
                }
            }

            RefreshEntrySelection();
        }

        private void ApplyCatalogRows()
        {
            if (catalogRows == null)
            {
                return;
            }

            for (var i = 0; i < catalogRows.Length; i++)
            {
                var row = catalogRows[i];
                var item = row != null ? FindItem(row.BuildingId) : null;
                if (item != null)
                {
                    row.Apply(item);
                }
            }
        }

        private void ApplySearchFilter()
        {
            if (catalogRows == null || catalogRows.Length == 0)
            {
                return;
            }

            var visible = 0;
            for (var i = 0; i < catalogRows.Length; i++)
            {
                var row = catalogRows[i];
                if (row == null)
                {
                    continue;
                }

                var item = FindItem(row.BuildingId);
                // 아직 목록을 받지 못했으면 칸을 숨기지 않는다.
                var match = item == null
                    ? cachedItems.Count == 0
                    : BuildingMenuDisplayFormatter.MatchesSearch(
                        item.DisplayName,
                        BuildingMenuDisplayFormatter.ShortPurpose(item.Description),
                        navigation.Query);
                row.SetShown(match);
                if (match)
                {
                    visible++;
                }
            }

            SetActive(emptyResult, visible == 0);
        }

        private void RefreshEntrySelection()
        {
            var hasSelection = !string.IsNullOrEmpty(selectedBuildingId);
            if (entries != null)
            {
                for (var i = 0; i < entries.Length; i++)
                {
                    if (entries[i] != null)
                    {
                        entries[i].SetSelected(hasSelection && entries[i].BuildingId == selectedBuildingId);
                    }
                }
            }

            if (catalogRows != null)
            {
                for (var i = 0; i < catalogRows.Length; i++)
                {
                    if (catalogRows[i] != null)
                    {
                        catalogRows[i].SetSelected(hasSelection && catalogRows[i].BuildingId == selectedBuildingId);
                    }
                }
            }
        }

        private void ShowDetail(BuildingMenuItemReadModel item)
        {
            if (detailIcon != null)
            {
                detailIcon.sprite = item.Icon;
                detailIcon.preserveAspect = true;
                detailIcon.enabled = item.Icon != null;
            }

            selectedCosts = item.Costs;
            detailNameText.text = item.DisplayName;
            if (detailOperatingConditionText != null)
            {
                detailOperatingConditionText.text = BuildingMenuDisplayFormatter.OperatingCondition(item.BuildingId);
                SetActive(detailOperatingConditionText.gameObject, !string.IsNullOrEmpty(detailOperatingConditionText.text));
            }

            if (selectionText != null)
            {
                // 작은 카드는 용도 한두 줄만, 큰 상세는 전체 설명을 쓴다.
                selectionText.text = UsesBuildingMode
                    ? BuildingMenuDisplayFormatter.ShortPurpose(item.Description)
                    : item.Description;
            }

            var count = item.Costs != null ? item.Costs.Count : 0;
            SetActive(costSection, count > 0);
            ShowCostRows(costRows, item.Costs);
        }

        private void RenderDetail()
        {
            if (!UsesBuildingMode || navigation.Page != BuildingModePage.Detail)
            {
                return;
            }

            var item = FindItem(navigation.DetailBuildingId);
            if (item == null)
            {
                return;
            }

            if (detailThumb != null)
            {
                detailThumb.sprite = item.Icon;
                detailThumb.preserveAspect = true;
                detailThumb.enabled = item.Icon != null;
            }

            if (detailPageName != null)
            {
                detailPageName.text = item.DisplayName;
            }

            if (detailPageCondition != null)
            {
                var condition = BuildingMenuDisplayFormatter.OperatingCondition(item.BuildingId);
                detailPageCondition.text = string.IsNullOrEmpty(condition) ? string.Empty : "설치 조건 · " + condition;
                SetActive(detailPageCondition.gameObject, !string.IsNullOrEmpty(condition));
            }

            if (detailPageFunction != null)
            {
                detailPageFunction.text = item.Description;
            }

            ShowCostRows(detailCostRows, item.Costs);
            RenderDetailStatus();
        }

        private void RenderDetailStatus()
        {
            if (!UsesBuildingMode || detailPageStatus == null || navigation.Page != BuildingModePage.Detail)
            {
                return;
            }

            var item = FindItem(navigation.DetailBuildingId);
            var isTarget = item != null && item.BuildingId == selectedBuildingId && hasAvailability;
            var kind = isTarget
                ? BuildingMenuDisplayFormatter.Classify(latestAvailability)
                : BuildingAvailabilityDisplayKind.Idle;
            detailPageStatus.text = isTarget
                ? ComposeAvailabilityText(kind, latestAvailability.Message, item.Costs)
                : BuildingMenuDisplayFormatter.AvailabilityText(BuildingAvailabilityDisplayKind.Idle, string.Empty);
            var color = ColorFor(kind);
            detailPageStatus.color = kind == BuildingAvailabilityDisplayKind.Idle
                ? new Color(0.72f, 0.84f, 0.9f, 1f)
                : Color.Lerp(color, Color.white, 0.35f);
            if (detailPageStatusAccent != null)
            {
                detailPageStatusAccent.color = color;
            }
        }

        private static void ShowCostRows(BuildingMenuCostRowView[] rows, IReadOnlyList<BuildingCostReadModel> costs)
        {
            if (rows == null)
            {
                return;
            }

            var count = costs != null ? costs.Count : 0;
            for (var i = 0; i < rows.Length && i < count; i++)
            {
                if (rows[i] != null)
                {
                    rows[i].Show(costs[i]);
                }
            }

            HideCostRows(rows, count);
        }

        private static void HideCostRows(BuildingMenuCostRowView[] rows, int fromIndex)
        {
            if (rows == null)
            {
                return;
            }

            for (var i = fromIndex < 0 ? 0 : fromIndex; i < rows.Length; i++)
            {
                if (rows[i] != null)
                {
                    rows[i].Hide();
                }
            }
        }

        private void ShowAvailabilityBanner(BuildingAvailabilityReadModel availability)
        {
            var kind = BuildingMenuDisplayFormatter.Classify(availability);
            var color = ColorFor(kind);
            availabilityText.text = ComposeAvailabilityText(kind, availability.Message, selectedCosts);
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

        private static string ComposeAvailabilityText(
            BuildingAvailabilityDisplayKind kind,
            string message,
            IReadOnlyList<BuildingCostReadModel> costs)
        {
            if (kind == BuildingAvailabilityDisplayKind.NeedResources)
            {
                // 무엇이 얼마나 부족한지 읽기 모델 수량으로 구체화한다. 없으면 Presenter 사유를 그대로 쓴다.
                var shortage = BuildingMenuDisplayFormatter.ShortageDetail(costs);
                if (!string.IsNullOrEmpty(shortage))
                {
                    message = shortage;
                }
            }

            return BuildingMenuDisplayFormatter.AvailabilityText(kind, message);
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

        private BuildingMenuItemReadModel FindItem(string buildingId)
        {
            if (string.IsNullOrEmpty(buildingId))
            {
                return null;
            }

            for (var i = 0; i < cachedItems.Count; i++)
            {
                if (cachedItems[i].BuildingId == buildingId)
                {
                    return cachedItems[i];
                }
            }

            return null;
        }

        private void ReplaceCachedItem(BuildingMenuItemReadModel item)
        {
            for (var i = 0; i < cachedItems.Count; i++)
            {
                if (cachedItems[i].BuildingId == item.BuildingId)
                {
                    cachedItems[i] = item;
                    return;
                }
            }

            cachedItems.Add(item);
        }

        private static string FormatLegacySelection(BuildingMenuItemReadModel item)
        {
            var builder = new StringBuilder()
                .Append(item.DisplayName)
                .AppendLine();
            var operatingCondition = BuildingMenuDisplayFormatter.OperatingCondition(item.BuildingId);
            if (!string.IsNullOrEmpty(operatingCondition))
            {
                builder.AppendLine(operatingCondition);
            }

            builder
                .Append(item.Description)
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
