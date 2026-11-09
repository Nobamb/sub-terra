using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SubTerra.App.UI.Inventory
{
    /// <summary>
    /// 인벤토리 패널 View. TextMeshPro 참조만 보유하고 인벤토리 State를 읽거나 쓰지 않는다.
    /// </summary>
    public sealed class InventoryPanelView : MonoBehaviour, IInventoryPanelView, IInventoryPanelDetailView
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TextMeshProUGUI cargoSummaryText;
        [SerializeField] private TextMeshProUGUI unsettledValueText;
        [SerializeField] private TextMeshProUGUI stacksText;
        [SerializeField] private InventoryStackRowView[] stackRows;
        [SerializeField] private Button closeButton;

        // prompt-B 112: 적재 게이지·미정산 카드·보유 종류 표시. 비어 있으면 기존 텍스트만 쓴다.
        [SerializeField] private TMP_Text cargoAmountText;
        [SerializeField] private TMP_Text cargoPercentText;
        [SerializeField] private TMP_Text cargoStateText;
        [SerializeField] private RectTransform cargoFill;
        [SerializeField] private Image cargoFillImage;
        [SerializeField] private TMP_Text unsettledAmountText;
        [SerializeField] private TMP_Text ownedKindsText;
        [SerializeField] private TMP_Text emptyHintText;
        // prompt-B 190: 적재 내역 표 머리. 실린 자원이 없으면 빈 안내만 남긴다.
        [SerializeField] private GameObject columnHeaderRoot;
        // prompt-B 190: 자원 이름 검색·정렬 드롭다운. 표만 거르고 상단 수치는 전체 적재를 보인다.
        [SerializeField] private TMP_InputField searchField;
        [SerializeField] private TMP_Dropdown sortDropdown;
        [SerializeField] private RectTransform stackRowsContent;
        [SerializeField] private ScrollRect stackScroll;
        [SerializeField] private Color cargoNormalColor = new Color(0.45f, 0.95f, 1f, 1f);
        [SerializeField] private Color cargoNearFullColor = new Color(1f, 0.72f, 0.2f, 1f);
        [SerializeField] private Color cargoFullColor = new Color(1f, 0.45f, 0.4f, 1f);
        [SerializeField] private Color cargoEmptyColor = new Color(0.6f, 0.78f, 0.85f, 1f);

        public GameObject PanelRoot => panelRoot;
        public TextMeshProUGUI CargoSummaryText => cargoSummaryText;
        public TextMeshProUGUI UnsettledValueText => unsettledValueText;
        public TextMeshProUGUI StacksText => stacksText;
        public Button CloseButton => closeButton;
        public TMP_InputField SearchField => searchField;
        public TMP_Dropdown SortDropdown => sortDropdown;
        public string SearchQuery => searchQuery;
        public InventoryManifestSort Sort => sort;

        public const string SearchInputOwner = "inventory.search";

        private readonly List<InventoryStackReadModel> lastStacks = new List<InventoryStackReadModel>();
        private string searchQuery = string.Empty;
        private InventoryManifestSort sort = InventoryManifestQuery.DefaultSort;
        private bool searchInputHeld;

        private void Awake()
        {
            // prompt-B 36-1: 시작 시 인벤토리 창은 닫힌 상태.
            // HudPanelChromeController가 I 키/버튼으로 토글한다.
            HideImmediately();
        }

        private void OnEnable()
        {
            WireManifestControls();
        }

        private void OnDisable()
        {
            UnwireManifestControls();
            ReleaseSearchInput();
        }

        private void LateUpdate()
        {
            if (searchField != null && searchField.isFocused && searchField.textComponent != null)
            {
                // TMP의 text는 확정된 문자열만 준다. 표시 문자열에는 IME 조합 중인 글자도 포함된다.
                OnSearchChanged(searchField.textComponent.text.TrimEnd('\u200B'));
            }
        }

        public void SetCargoSummary(string cargoText)
        {
            SetText(cargoSummaryText, cargoText);
        }

        public void SetUnsettledValue(string valueText)
        {
            SetText(unsettledValueText, valueText);
        }

        public void SetCargoLoad(float currentWeight, float maxCapacity)
        {
            var level = InventoryPanelDisplayFormatter.Level(currentWeight, maxCapacity);
            var color = LevelColor(level);
            SetText(cargoAmountText, InventoryPanelDisplayFormatter.LoadAmount(currentWeight, maxCapacity));
            SetText(cargoPercentText, InventoryPanelDisplayFormatter.Percent(currentWeight, maxCapacity));
            SetText(cargoStateText, InventoryPanelDisplayFormatter.StateLine(currentWeight, maxCapacity));
            if (cargoPercentText != null)
            {
                cargoPercentText.color = color;
            }

            if (cargoStateText != null)
            {
                cargoStateText.color = color;
            }

            if (cargoFill != null)
            {
                cargoFill.anchorMax = new Vector2(
                    InventoryPanelDisplayFormatter.FillRatio(currentWeight, maxCapacity),
                    cargoFill.anchorMax.y);
            }

            if (cargoFillImage != null)
            {
                cargoFillImage.color = color;
            }
        }

        public void SetUnsettledAmount(float value)
        {
            SetText(unsettledAmountText, InventoryPanelDisplayFormatter.UnsettledAmount(value));
        }

        public void SetStacksText(string text)
        {
            SetText(stacksText, text);
        }

        public void SetStacks(IReadOnlyList<InventoryStackReadModel> stacks)
        {
            lastStacks.Clear();
            if (stacks != null)
            {
                for (var i = 0; i < stacks.Count; i++)
                {
                    lastStacks.Add(stacks[i]);
                }
            }

            // 채굴·버림으로 다시 그릴 때도 지금 검색어와 정렬을 유지한다.
            RefreshManifest();
        }

        /// <summary>검색어를 바꾼다. 정렬은 유지한다.</summary>
        public void SetSearchQuery(string query)
        {
            var next = query ?? string.Empty;
            if (searchField != null && searchField.text != next)
            {
                searchField.SetTextWithoutNotify(next);
            }

            searchQuery = next;
            RefreshManifest();
            ScrollToTop();
        }

        /// <summary>정렬을 바꾼다. 검색어는 지우지 않는다.</summary>
        public void SetSort(InventoryManifestSort value)
        {
            sort = value;
            if (sortDropdown != null && sortDropdown.value != (int)value)
            {
                sortDropdown.SetValueWithoutNotify((int)value);
            }

            RefreshManifest();
            ScrollToTop();
        }

        /// <summary>창을 새로 열 때 검색어를 비우고 정렬을 기본(희귀도순)으로 되돌린다.</summary>
        public void ResetManifestControls()
        {
            searchQuery = string.Empty;
            sort = InventoryManifestQuery.DefaultSort;
            if (searchField != null)
            {
                searchField.SetTextWithoutNotify(string.Empty);
            }

            if (sortDropdown != null)
            {
                sortDropdown.SetValueWithoutNotify((int)sort);
            }

            RefreshManifest();
            ScrollToTop();
        }

        private void RefreshManifest()
        {
            if (stackRows == null)
            {
                return;
            }

            var loaded = InventoryManifestQuery.LoadedCount(lastStacks);
            var visible = InventoryManifestQuery.Apply(lastStacks, searchQuery, sort);
            var shown = 0;
            for (var order = 0; order < visible.Count; order++)
            {
                var row = FindRow(visible[order].MineralId);
                if (row == null)
                {
                    continue;
                }

                row.SetStack(visible[order]);
                if (stackRowsContent != null && row.transform.parent == stackRowsContent)
                {
                    row.transform.SetSiblingIndex(shown);
                }

                shown++;
            }

            for (var i = 0; i < stackRows.Length; i++)
            {
                var row = stackRows[i];
                if (row != null && !Contains(visible, row.MineralId) && row.gameObject.activeSelf)
                {
                    row.gameObject.SetActive(false);
                }
            }

            if (ownedKindsText != null)
            {
                ownedKindsText.text = InventoryPanelDisplayFormatter.LoadedKinds(loaded);
            }

            if (emptyHintText != null)
            {
                // 실린 자원이 없으면 검색어와 상관없이 빈 적재 안내만 둔다.
                emptyHintText.text = loaded == 0
                    ? InventoryManifestQuery.EmptyCargoText
                    : InventoryManifestQuery.NoSearchResultText;
                emptyHintText.gameObject.SetActive(shown == 0);
            }

            if (columnHeaderRoot != null)
            {
                columnHeaderRoot.SetActive(shown > 0);
            }
        }

        private InventoryStackRowView FindRow(string mineralId)
        {
            for (var i = 0; i < stackRows.Length; i++)
            {
                if (stackRows[i] != null && stackRows[i].MineralId == mineralId)
                {
                    return stackRows[i];
                }
            }

            return null;
        }

        private static bool Contains(List<InventoryStackReadModel> stacks, string mineralId)
        {
            for (var i = 0; i < stacks.Count; i++)
            {
                if (stacks[i].MineralId == mineralId)
                {
                    return true;
                }
            }

            return false;
        }

        private void ScrollToTop()
        {
            if (stackScroll != null)
            {
                stackScroll.verticalNormalizedPosition = 1f;
            }
        }

        private void OnSearchChanged(string value)
        {
            var next = value ?? string.Empty;
            if (searchQuery == next) return;
            searchQuery = next;
            RefreshManifest();
            ScrollToTop();
        }

        private void OnSortChanged(int index)
        {
            sort = InventoryManifestQuery.FromIndex(index);
            RefreshManifest();
            ScrollToTop();
        }

        private void WireManifestControls()
        {
            if (searchField != null)
            {
                searchField.onValueChanged.RemoveListener(OnSearchChanged);
                searchField.onValueChanged.AddListener(OnSearchChanged);
                searchField.onSelect.RemoveListener(OnSearchSelected);
                searchField.onSelect.AddListener(OnSearchSelected);
                searchField.onDeselect.RemoveListener(OnSearchDeselected);
                searchField.onDeselect.AddListener(OnSearchDeselected);
                searchField.onEndEdit.RemoveListener(OnSearchEndEdit);
                searchField.onEndEdit.AddListener(OnSearchEndEdit);
            }

            if (sortDropdown != null)
            {
                sortDropdown.onValueChanged.RemoveListener(OnSortChanged);
                sortDropdown.onValueChanged.AddListener(OnSortChanged);
            }
        }

        private void UnwireManifestControls()
        {
            if (searchField != null)
            {
                searchField.onValueChanged.RemoveListener(OnSearchChanged);
                searchField.onSelect.RemoveListener(OnSearchSelected);
                searchField.onDeselect.RemoveListener(OnSearchDeselected);
                searchField.onEndEdit.RemoveListener(OnSearchEndEdit);
            }

            if (sortDropdown != null)
            {
                sortDropdown.onValueChanged.RemoveListener(OnSortChanged);
            }
        }

        private void OnSearchSelected(string _)
        {
            // I·B·이동 키는 막되 전역 일시정지로 기존 효과음을 끊지 않는다.
            var open = panelRoot == null || panelRoot.activeInHierarchy;
            if (!open || searchInputHeld || TerminalWindowOpenMotion.IsClosingWindow(gameObject))
            {
                return;
            }

            searchInputHeld = UiPauseGate.AcquireInput(SearchInputOwner);
        }

        private void OnSearchDeselected(string _)
        {
            ReleaseSearchInput();
            OnSearchChanged(searchField != null ? searchField.text : string.Empty);
        }

        private void OnSearchEndEdit(string _)
        {
            // Enter·Esc로 입력을 끝내면 선택도 풀어 다음 클릭에서 다시 포커스 이벤트가 오게 한다. 창은 닫지 않는다.
            ReleaseSearchInput();
            OnSearchChanged(searchField != null ? searchField.text : string.Empty);
            var eventSystem = EventSystem.current;
            if (eventSystem != null
                && !eventSystem.alreadySelecting
                && searchField != null
                && eventSystem.currentSelectedGameObject == searchField.gameObject)
            {
                eventSystem.SetSelectedGameObject(null);
            }
        }

        private void ReleaseSearchInput()
        {
            if (!searchInputHeld)
            {
                return;
            }

            searchInputHeld = false;
            UiPauseGate.ReleaseInput(SearchInputOwner);
        }

        private void CloseManifestControls()
        {
            if (searchField != null && searchField.isFocused)
            {
                searchField.DeactivateInputField();
            }

            if (sortDropdown != null && sortDropdown.IsExpanded)
            {
                sortDropdown.Hide();
            }

            ReleaseSearchInput();
        }

        private Color LevelColor(InventoryCargoLevel level)
        {
            switch (level)
            {
                case InventoryCargoLevel.Full:
                    return cargoFullColor;
                case InventoryCargoLevel.NearFull:
                    return cargoNearFullColor;
                case InventoryCargoLevel.Normal:
                    return cargoNormalColor;
                default:
                    return cargoEmptyColor;
            }
        }

        public void SetVisible(bool visible)
        {
            if (!visible)
            {
                CloseManifestControls();
                TerminalWindowOpenMotion.Close(gameObject, HideImmediately);
                return;
            }
            var wasVisible = gameObject.activeSelf && (panelRoot == null || panelRoot.activeSelf)
                && !TerminalWindowOpenMotion.IsClosingWindow(gameObject);
            // prompt-B 190: 닫았다가 다시 열면 검색어를 비우고 정렬을 희귀도순으로 되돌린다.
            if (!wasVisible)
            {
                ResetManifestControls();
            }
            // prompt-B 33-1: 루트 전체를 끄면 Binder 구독이 끊기고 닫기 버튼 상태가 꼬일 수 있다.
            // 루트는 유지한 채 PanelRoot·닫기 버튼만 토글한다.
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            if (panelRoot != null)
            {
                panelRoot.SetActive(visible);
            }

            if (closeButton != null)
            {
                closeButton.gameObject.SetActive(visible);
            }

            // panelRoot가 없고 본 오브젝트만 쓰는 경우 폴백.
            if (panelRoot == null && closeButton == null)
            {
                gameObject.SetActive(visible);
            }
            if (visible && !wasVisible) TerminalWindowOpenMotion.Open(gameObject);
        }

        private void HideImmediately()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            if (closeButton != null) closeButton.gameObject.SetActive(false);
            if (panelRoot == null && closeButton == null) gameObject.SetActive(false);
        }

        public bool HasRequiredReferences()
        {
            return cargoSummaryText != null
                && unsettledValueText != null
                && stacksText != null
                && stackRows != null
                && stackRows.Length > 0;
        }

        private static void SetText(TMP_Text target, string text)
        {
            if (target != null)
            {
                target.text = text ?? string.Empty;
            }
        }
    }
}
