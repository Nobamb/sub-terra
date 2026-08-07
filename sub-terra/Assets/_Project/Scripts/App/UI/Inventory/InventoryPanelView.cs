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

        private static void SetText(TextMeshProUGUI target, string text)
        {
            if (target != null)
            {
                target.text = text ?? string.Empty;
            }
        }
    }
}
