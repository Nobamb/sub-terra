using System.Collections.Generic;
using System.Text;
using SubTerra.App.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SubTerra.App.UI.Progression
{
    /// <summary>Surface Base 업그레이드 패널의 최소 TextMeshPro View.</summary>
    public sealed class ProgressionPanelView : MonoBehaviour, IProgressionPanelView
    {
        [SerializeField] private TMP_Text upgradeListText;
        [SerializeField] private TMP_Text detailText;
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private TMP_Text deepZoneText;
        [SerializeField] private Button purchaseButton;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private ProgressionUpgradeEntryButton[] upgradeButtons;

        private bool hasSelection;
        private bool selectedAtMaximum;
        private bool selectedCanAfford;
        private bool busy;

        public void SetUpgradeList(IReadOnlyList<UpgradeSnapshot> upgrades)
        {
            if (upgradeListText == null)
            {
                return;
            }

            if (upgradeButtons != null && upgradeButtons.Length > 0)
            {
                for (var i = 0; i < upgradeButtons.Length; i++)
                {
                    upgradeButtons[i]?.SetSnapshot(upgrades);
                }

                upgradeListText.text = "업그레이드를 선택하세요.";
                return;
            }

            var builder = new StringBuilder();
            if (upgrades != null)
            {
                for (var i = 0; i < upgrades.Count; i++)
                {
                    var item = upgrades[i];
                    if (i > 0)
                    {
                        builder.AppendLine();
                    }

                    builder.Append(item.DisplayName)
                        .Append("  Lv.")
                        .Append(item.CurrentLevel)
                        .Append('/')
                        .Append(item.MaximumLevel);
                }
            }

            upgradeListText.text = builder.ToString();
        }

        public void SetSelectedUpgrade(UpgradeSnapshot upgrade)
        {
            if (detailText == null)
            {
                return;
            }

            var builder = new StringBuilder()
                .Append(upgrade.DisplayName)
                .Append("  Lv.")
                .Append(upgrade.CurrentLevel)
                .Append('/')
                .Append(upgrade.MaximumLevel)
                .AppendLine()
                .Append("현재 효과: ")
                .Append(upgrade.CurrentEffectValue.ToString("0.##"));

            if (!upgrade.IsMaximumLevel)
            {
                builder.AppendLine()
                    .Append("다음 효과: ")
                    .Append(upgrade.NextEffectValue.ToString("0.##"))
                    .AppendLine()
                    .Append("비용: ");

                for (var i = 0; i < upgrade.NextCosts.Count; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(", ");
                    }

                    builder.Append(upgrade.NextCosts[i].ItemId)
                        .Append(" x")
                        .Append(upgrade.NextCosts[i].Quantity);
                }

                builder.AppendLine()
                    .Append(upgrade.CanAffordNextLevel
                        ? "구매 가능"
                        : "자원 부족");
            }

            detailText.text = builder.ToString();
            hasSelection = !string.IsNullOrEmpty(upgrade.UpgradeId);
            selectedAtMaximum = upgrade.IsMaximumLevel;
            selectedCanAfford = upgrade.CanAffordNextLevel;
            RefreshPurchaseButton();
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
    }
}
