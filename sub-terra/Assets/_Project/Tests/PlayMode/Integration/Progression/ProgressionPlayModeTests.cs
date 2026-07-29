using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.Progression;
using SubTerra.App.UI.Progression;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.TestTools;

namespace SubTerra.App.Tests.PlayMode.Integration.Progression
{
    /// <summary>구매 성공 프레임에 UI와 Gameplay Provider가 같은 레벨을 읽는지 검증.</summary>
    public sealed class ProgressionPlayModeTests
    {
        private sealed class Catalog : IUpgradeCatalog
        {
            private readonly UpgradeData data;

            public Catalog(UpgradeData data)
            {
                this.data = data;
            }

            public IReadOnlyList<UpgradeData> Upgrades => new[] { data };

            public bool TryGetUpgrade(string upgradeId, out UpgradeData found)
            {
                found = data != null && data.Id == upgradeId ? data : null;
                return found != null;
            }
        }

        private sealed class Wallet : IResourceWallet
        {
            public int Copper = 3;

            public bool CanAfford(IReadOnlyList<ItemCostDto> costs)
            {
                return costs != null && costs.Count == 1 && Copper >= costs[0].Quantity;
            }

            public bool TrySpend(IReadOnlyList<ItemCostDto> costs)
            {
                if (!CanAfford(costs))
                {
                    return false;
                }

                Copper -= costs[0].Quantity;
                return true;
            }
        }

        private sealed class View : IProgressionPanelView
        {
            public UpgradeSnapshot Selected;
            public string Message;
            public bool Busy;

            public void SetUpgradeList(IReadOnlyList<UpgradeSnapshot> upgrades) { }
            public void SetSelectedUpgrade(UpgradeSnapshot upgrade) => Selected = upgrade;
            public void SetPurchaseResult(string message, string detail) => Message = message;
            public void SetDeepZoneAccess(ZoneAccessResult access) { }
            public void SetBusy(bool busy) => Busy = busy;
            public void SetVisible(bool visible) { }
        }

        [UnityTest]
        public IEnumerator Purchase_UpdatesUiAndProviderInSameFrame()
        {
            var data = ScriptableObject.CreateInstance<UpgradeData>();
            data.EditorSet(
                DataIds.Upgrades.DrillSpeed,
                "드릴 속도",
                1,
                new List<UpgradeLevelDefinition>
                {
                    new UpgradeLevelDefinition(
                        1,
                        0.25f,
                        new List<ItemCostEntry>
                        {
                            new ItemCostEntry(DataIds.Minerals.Copper, 2)
                        })
                });
            var state = new UpgradeState();
            var wallet = new Wallet();
            var service = new ProgressionService(state, new Catalog(data), wallet);
            var view = new View();
            var presenter = new ProgressionPanelPresenter(view);
            presenter.Bind(service);
            Assert.That(presenter.SelectUpgrade(DataIds.Upgrades.DrillSpeed), Is.True);

            var result = presenter.RequestPurchase();

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(view.Selected.CurrentLevel, Is.EqualTo(1));
            Assert.That(view.Selected.CurrentEffectValue, Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(service.Effects.GetDrillSpeedMultiplier(), Is.EqualTo(1.25f).Within(0.0001f));
            Assert.That(wallet.Copper, Is.EqualTo(1));
            Assert.That(view.Message, Does.Contain("완료"));
            Assert.That(view.Busy, Is.False);

            Object.Destroy(data);
            yield return null;
        }
    }
}
