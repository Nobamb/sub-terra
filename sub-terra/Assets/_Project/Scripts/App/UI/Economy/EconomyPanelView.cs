using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.Economy
{
    /// <summary>
    /// 판매·제작 결과 및 판매 목록 표시 View.
    /// TMP/UGUI 참조만 보유하고, 클릭은 Presenter에 위임한다. State/Inventory 쓰기 금지.
    /// </summary>
    public sealed class EconomyPanelView : MonoBehaviour, IEconomyPanelView
    {
        [SerializeField] private TMP_Text statusMessageText;
        [SerializeField] private TMP_Text statusDetailText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Selectable[] controlsToDisableWhenBusy;

        [Header("Sell modal")]
        [SerializeField] private Button openSellButton;
        [SerializeField] private Button closeSellButton;
        [SerializeField] private GameObject levelSummaryRoot;

        [Header("Sell panel")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text creditsLabelText;
        [SerializeField] private Transform sellListContent;
        [SerializeField] private EconomySellRowView sellRowPrefab;
        [SerializeField] private TMP_Text emptySellText;
        [SerializeField] private TMP_Text qtyText;
        [SerializeField] private TMP_Text previewText;
        [SerializeField] private Button qtyMinusButton;
        [SerializeField] private Button qtyPlusButton;
        [SerializeField] private Button qtyMaxButton;
        [SerializeField] private Button sellSelectedButton;
        [SerializeField] private Button sellAllButton;

        private readonly List<EconomySellRowView> activeRows = new List<EconomySellRowView>();
        private bool busy;
        private bool sellSelectedEnabled;
        private bool sellAllEnabled;
        private bool levelSummaryVisibilityCaptured;
        private bool levelSummaryWasActive;

        /// <summary>행 선택 / 수량 / 판매 버튼을 Presenter에 연결할 때 사용.</summary>
        public event Action<string> MineralRowSelected;
        public event Action QtyMinusClicked;
        public event Action QtyPlusClicked;
        public event Action QtyMaxClicked;
        public event Action SellSelectedClicked;
        public event Action SellAllClicked;

        private void Awake()
        {
            WireButtons(true);
            if (canvasGroup != null)
            {
                SetVisible(false);
            }
        }

        private void OnDestroy()
        {
            WireButtons(false);
            ClearRows();
        }

        public void SetStatusMessage(string message)
        {
            if (statusMessageText != null)
            {
                statusMessageText.text = message ?? string.Empty;
            }
        }

        public void SetStatusDetail(string detail)
        {
            if (statusDetailText != null)
            {
                statusDetailText.text = detail ?? string.Empty;
            }
        }

        public void SetBusy(bool isBusy)
        {
            busy = isBusy;
            ApplyInteractable();

            if (controlsToDisableWhenBusy == null)
            {
                return;
            }

            for (var i = 0; i < controlsToDisableWhenBusy.Length; i++)
            {
                var control = controlsToDisableWhenBusy[i];
                if (control != null)
                {
                    control.interactable = !isBusy;
                }
            }
        }

        public void SetVisible(bool visible)
        {
            if (visible)
            {
                // 레벨 요약 패널이 활성화되며 sibling 순서를 바꿔도 판매 모달이 항상 위를 덮는다.
                transform.SetAsLastSibling();
                HideLevelSummary();
            }
            else
            {
                RestoreLevelSummary();
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.interactable = visible;
                canvasGroup.blocksRaycasts = visible;
                return;
            }

            gameObject.SetActive(visible);
        }

        public void SetSellRows(IReadOnlyList<SellMineralRowReadModel> rows)
        {
            EnsureListContent();
            ClearRows();

            if (rows == null || rows.Count == 0 || sellListContent == null)
            {
                return;
            }

            for (var i = 0; i < rows.Count; i++)
            {
                var rowView = CreateRow();
                if (rowView == null)
                {
                    continue;
                }

                rowView.Bind(rows[i]);
                rowView.Selected += OnRowSelected;
                activeRows.Add(rowView);
            }
        }

        public void SetSelectedMineral(string mineralId, int sellQuantity, int owned, int unitPrice)
        {
            // 행 하이라이트는 SetSellRows의 IsSelected로 반영. 여기는 수량 텍스트만.
            if (qtyText != null && !string.IsNullOrEmpty(mineralId) && owned > 0)
            {
                qtyText.text = sellQuantity.ToString();
            }
        }

        public void SetSellQuantityControls(int sellQuantity, int min, int max)
        {
            if (qtyText != null)
            {
                qtyText.text = max > 0 ? sellQuantity.ToString() : "-";
            }

            var canAdjust = !busy && max > 0;
            if (qtyMinusButton != null)
            {
                qtyMinusButton.interactable = canAdjust && sellQuantity > min;
            }

            if (qtyPlusButton != null)
            {
                qtyPlusButton.interactable = canAdjust && sellQuantity < max;
            }

            if (qtyMaxButton != null)
            {
                qtyMaxButton.interactable = canAdjust && sellQuantity < max;
            }
        }

        public void SetPreviewCredits(int previewCredits, string previewLabel)
        {
            if (previewText != null)
            {
                previewText.text = string.IsNullOrEmpty(previewLabel)
                    ? "예상 골드 +" + previewCredits
                    : previewLabel;
            }
        }

        public void SetCreditsLabel(int credits)
        {
            if (creditsLabelText != null)
            {
                creditsLabelText.text = "골드 " + credits;
            }
        }

        public void SetSellActionsEnabled(bool sellSelected, bool sellAll)
        {
            sellSelectedEnabled = sellSelected;
            sellAllEnabled = sellAll;
            ApplyInteractable();
        }

        public void SetEmptySellState(bool isEmpty, string emptyMessage)
        {
            if (emptySellText != null)
            {
                emptySellText.gameObject.SetActive(isEmpty);
                emptySellText.text = emptyMessage ?? string.Empty;
            }

            if (sellListContent != null)
            {
                sellListContent.gameObject.SetActive(!isEmpty);
            }
        }

        private void ApplyInteractable()
        {
            if (sellSelectedButton != null)
            {
                sellSelectedButton.interactable = !busy && sellSelectedEnabled;
            }

            if (sellAllButton != null)
            {
                sellAllButton.interactable = !busy && sellAllEnabled;
            }
        }

        private void WireButtons(bool add)
        {
            Wire(openSellButton, OnOpenSell, add);
            Wire(closeSellButton, OnCloseSell, add);
            Wire(qtyMinusButton, OnQtyMinus, add);
            Wire(qtyPlusButton, OnQtyPlus, add);
            Wire(qtyMaxButton, OnQtyMax, add);
            Wire(sellSelectedButton, OnSellSelected, add);
            Wire(sellAllButton, OnSellAll, add);
        }

        private static void Wire(Button button, UnityEngine.Events.UnityAction handler, bool add)
        {
            if (button == null)
            {
                return;
            }

            if (add)
            {
                button.onClick.AddListener(handler);
            }
            else
            {
                button.onClick.RemoveListener(handler);
            }
        }

        private void OnRowSelected(string mineralId) => MineralRowSelected?.Invoke(mineralId);
        private void OnOpenSell() => SetVisible(true);
        private void OnCloseSell() => SetVisible(false);
        private void OnQtyMinus() => QtyMinusClicked?.Invoke();
        private void OnQtyPlus() => QtyPlusClicked?.Invoke();
        private void OnQtyMax() => QtyMaxClicked?.Invoke();
        private void OnSellSelected() => SellSelectedClicked?.Invoke();
        private void OnSellAll() => SellAllClicked?.Invoke();

        private void HideLevelSummary()
        {
            if (levelSummaryRoot == null || levelSummaryVisibilityCaptured)
            {
                return;
            }

            levelSummaryWasActive = levelSummaryRoot.activeSelf;
            levelSummaryVisibilityCaptured = true;
            levelSummaryRoot.SetActive(false);
        }

        private void RestoreLevelSummary()
        {
            if (levelSummaryRoot == null || !levelSummaryVisibilityCaptured)
            {
                return;
            }

            levelSummaryRoot.SetActive(levelSummaryWasActive);
            levelSummaryVisibilityCaptured = false;
        }

        private void EnsureListContent()
        {
            if (sellListContent != null)
            {
                return;
            }

            var found = transform.Find("SellListViewport/SellListContent");
            if (found != null)
            {
                sellListContent = found;
            }
        }

        private EconomySellRowView CreateRow()
        {
            if (sellRowPrefab != null && sellListContent != null)
            {
                var instance = Instantiate(sellRowPrefab, sellListContent);
                instance.gameObject.SetActive(true);
                return instance;
            }

            // Prefab 미연결 시 런타임 최소 행 생성(EditMode/헤드리스 폴백).
            if (sellListContent == null)
            {
                return null;
            }

            var go = new GameObject("SellRow", typeof(RectTransform));
            go.transform.SetParent(sellListContent, false);
            var row = go.AddComponent<EconomySellRowView>();
            var nameTmp = CreateChildTmp(go.transform, "Name", new Vector2(0f, 0f), new Vector2(200f, 36f));
            var ownedTmp = CreateChildTmp(go.transform, "Owned", new Vector2(120f, 0f), new Vector2(80f, 36f));
            var priceTmp = CreateChildTmp(go.transform, "Price", new Vector2(200f, 0f), new Vector2(80f, 36f));
            var btn = go.AddComponent<Button>();
            var image = go.AddComponent<Image>();
            image.color = new Color(0.15f, 0.18f, 0.22f, 0.9f);
            btn.targetGraphic = image;
            row.BindReferences(string.Empty, null, nameTmp, ownedTmp, priceTmp, btn, image);
            return row;
        }

        private static TMP_Text CreateChildTmp(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 16f;
            tmp.raycastTarget = false;
            return tmp;
        }

        private void ClearRows()
        {
            for (var i = 0; i < activeRows.Count; i++)
            {
                var row = activeRows[i];
                if (row == null)
                {
                    continue;
                }

                row.Selected -= OnRowSelected;
                if (Application.isPlaying)
                {
                    Destroy(row.gameObject);
                }
                else
                {
                    DestroyImmediate(row.gameObject);
                }
            }

            activeRows.Clear();
        }

#if UNITY_EDITOR
        public void EditorBind(
            TMP_Text message,
            TMP_Text detail,
            CanvasGroup group,
            Selectable[] controls)
        {
            statusMessageText = message;
            statusDetailText = detail;
            canvasGroup = group;
            controlsToDisableWhenBusy = controls;
        }

        public void EditorBindSell(
            TMP_Text title,
            TMP_Text credits,
            Transform listContent,
            EconomySellRowView rowPrefab,
            TMP_Text emptyText,
            TMP_Text qty,
            TMP_Text preview,
            Button minus,
            Button plus,
            Button max,
            Button sellSelected,
            Button sellAll)
        {
            titleText = title;
            creditsLabelText = credits;
            sellListContent = listContent;
            sellRowPrefab = rowPrefab;
            emptySellText = emptyText;
            qtyText = qty;
            previewText = preview;
            qtyMinusButton = minus;
            qtyPlusButton = plus;
            qtyMaxButton = max;
            sellSelectedButton = sellSelected;
            sellAllButton = sellAll;
        }

        public void EditorBindModal(
            Button openButton,
            Button closeButton,
            CanvasGroup group,
            GameObject levelSummary)
        {
            openSellButton = openButton;
            closeSellButton = closeButton;
            canvasGroup = group;
            levelSummaryRoot = levelSummary;
        }
#endif
    }
}
