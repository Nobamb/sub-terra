using System.Collections.Generic;
using TMPro;
using UnityEngine;
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
        [SerializeField] private Color cargoNormalColor = new Color(0.45f, 0.95f, 1f, 1f);
        [SerializeField] private Color cargoNearFullColor = new Color(1f, 0.72f, 0.2f, 1f);
        [SerializeField] private Color cargoFullColor = new Color(1f, 0.45f, 0.4f, 1f);
        [SerializeField] private Color cargoEmptyColor = new Color(0.6f, 0.78f, 0.85f, 1f);

        public GameObject PanelRoot => panelRoot;
        public TextMeshProUGUI CargoSummaryText => cargoSummaryText;
        public TextMeshProUGUI UnsettledValueText => unsettledValueText;
        public TextMeshProUGUI StacksText => stacksText;
        public Button CloseButton => closeButton;

        private void Awake()
        {
            // prompt-B 36-1: 시작 시 인벤토리 창은 닫힌 상태.
            // HudPanelChromeController가 I 키/버튼으로 토글한다.
            SetVisible(false);
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
            if (stackRows == null)
            {
                return;
            }

            var owned = 0;
            var total = 0;
            for (var i = 0; i < stackRows.Length; i++)
            {
                var row = stackRows[i];
                if (row == null)
                {
                    continue;
                }

                var found = false;
                if (stacks != null)
                {
                    for (var j = 0; j < stacks.Count; j++)
                    {
                        if (stacks[j].MineralId == row.MineralId)
                        {
                            row.SetStack(stacks[j]);
                            found = true;
                            break;
                        }
                    }
                }

                if (!found)
                {
                    row.SetStack(new InventoryStackReadModel(row.MineralId, row.MineralId, null, 0));
                }
            }

            if (ownedKindsText != null)
            {
                for (var i = 0; i < stackRows.Length; i++)
                {
                    if (stackRows[i] == null)
                    {
                        continue;
                    }

                    total++;
                    if (stacks != null && HasQuantity(stacks, stackRows[i].MineralId))
                    {
                        owned++;
                    }
                }

                ownedKindsText.text = InventoryPanelDisplayFormatter.OwnedKinds(owned, total);
            }
        }

        private static bool HasQuantity(IReadOnlyList<InventoryStackReadModel> stacks, string mineralId)
        {
            for (var i = 0; i < stacks.Count; i++)
            {
                if (stacks[i].MineralId == mineralId)
                {
                    return stacks[i].Quantity > 0;
                }
            }

            return false;
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
