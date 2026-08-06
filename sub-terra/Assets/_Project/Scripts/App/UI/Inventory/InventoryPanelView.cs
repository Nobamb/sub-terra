using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.Inventory
{
    /// <summary>
    /// 인벤토리 패널 View. TextMeshPro 참조만 보유하고 인벤토리 State를 읽거나 쓰지 않는다.
    /// </summary>
    public sealed class InventoryPanelView : MonoBehaviour, IInventoryPanelView
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TextMeshProUGUI cargoSummaryText;
        [SerializeField] private TextMeshProUGUI unsettledValueText;
        [SerializeField] private TextMeshProUGUI stacksText;
        [SerializeField] private InventoryStackRowView[] stackRows;
        [SerializeField] private Button closeButton;

        public GameObject PanelRoot => panelRoot;
        public TextMeshProUGUI CargoSummaryText => cargoSummaryText;
        public TextMeshProUGUI UnsettledValueText => unsettledValueText;
        public TextMeshProUGUI StacksText => stacksText;
        public Button CloseButton => closeButton;

        public void SetCargoSummary(string cargoText)
        {
            SetText(cargoSummaryText, cargoText);
        }

        public void SetUnsettledValue(string valueText)
        {
            SetText(unsettledValueText, valueText);
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
        }

        public void SetVisible(bool visible)
        {
            // 루트가 비활성이면 자식 panelRoot만 켜도 화면에 안 보이므로 루트도 함께 토글한다.
            if (!visible)
            {
                if (panelRoot != null)
                {
                    panelRoot.SetActive(false);
                }

                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
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

        private static void SetText(TextMeshProUGUI target, string text)
        {
            if (target != null)
            {
                target.text = text ?? string.Empty;
            }
        }
    }
}
