using NUnit.Framework;
using SubTerra.App.Inventory;
using SubTerra.App.State;
using SubTerra.App.Tests.UI;
using SubTerra.App.UI.HUD;
using SubTerra.App.UI.Inventory;
using SubTerra.Shared;

namespace SubTerra.App.Tests.Inventory
{
    /// <summary>
    /// D-F04 Edit Mode 동등: Shared 경계 지급 후 HUD·InventoryPanel이 동일 스냅샷을 표시.
    /// </summary>
    public sealed class InventoryUiSyncTests
    {
        private sealed class RecordingPanelView : IInventoryPanelView
        {
            public int CargoCount;
            public int ValueCount;
            public int StacksCount;
            public string Cargo;
            public string Value;
            public string Stacks;
            public bool Visible = true;

            public void ResetCounts()
            {
                CargoCount = 0;
                ValueCount = 0;
                StacksCount = 0;
            }

            public void SetCargoSummary(string cargoText)
            {
                Cargo = cargoText;
                CargoCount++;
            }

            public void SetUnsettledValue(string valueText)
            {
                Value = valueText;
                ValueCount++;
            }

            public void SetStacksText(string stacksText)
            {
                Stacks = stacksText;
                StacksCount++;
            }

            public void SetVisible(bool visible)
            {
                Visible = visible;
            }
        }

        [Test]
        public void D_F04_SharedBoundary_HudAndPanel_ShowSameTotals()
        {
            var catalog = new InMemoryMineralCatalog();
            catalog.Register("mineral.copper", 1.5f, 10, "Copper");
            catalog.Register("mineral.iron", 2f, 15, "Iron");

            var gameState = GameState.CreateNew();
            var service = new InventoryService(catalog, 50f, gameState);

            var hudView = new RecordingHudView();
            var hudPresenter = new HudPresenter(hudView);
            hudPresenter.Bind(gameState);

            var panelView = new RecordingPanelView();
            var panelPresenter = new InventoryPanelPresenter(panelView);
            panelPresenter.Bind(service);

            hudView.ResetCounts();
            panelView.ResetCounts();

            IMiningRewardReceiver receiver = service;
            receiver.AddMineral("mineral.copper", 2);
            receiver.AddMineral("mineral.iron", 1);

            // 각 성공 지급마다 HUD cargo/value 1회씩 → 2회, 패널도 동일
            Assert.That(hudView.CargoCount, Is.EqualTo(2));
            Assert.That(hudView.UnsettledValueCount, Is.EqualTo(2));
            Assert.That(panelView.CargoCount, Is.EqualTo(2));
            Assert.That(panelView.ValueCount, Is.EqualTo(2));

            var expectedWeight = 2 * 1.5f + 1 * 2f;
            var expectedValue = 2 * 10 + 1 * 15;

            Assert.That(hudView.Cargo, Is.EqualTo(HudFormatter.FormatCargo(expectedWeight)));
            Assert.That(hudView.UnsettledValue, Is.EqualTo(HudFormatter.FormatUnsettledValue(expectedValue)));

            Assert.That(panelView.Cargo, Is.EqualTo(
                HudFormatter.FormatCargo(expectedWeight) + " / " + HudFormatter.FormatCargo(50f)));
            Assert.That(panelView.Value, Is.EqualTo(HudFormatter.FormatUnsettledValue(expectedValue)));
            Assert.That(panelView.Stacks, Does.Contain("Copper x2"));
            Assert.That(panelView.Stacks, Does.Contain("Iron x1"));

            // GameState 읽기 모델과 서비스 합산 일치
            var inv = gameState.GetInventory();
            Assert.That(inv.CargoWeight, Is.EqualTo(service.CurrentWeight).Within(0.0001f));
            Assert.That(inv.UnsettledValue, Is.EqualTo(service.UnsettledValue).Within(0.0001f));
        }

        [Test]
        public void PanelUnbind_StopsUpdates_RebindRestores()
        {
            var catalog = new InMemoryMineralCatalog();
            catalog.Register("mineral.copper", 1.5f, 10, "Copper");
            var service = new InventoryService(catalog, 50f);
            var panelView = new RecordingPanelView();
            var presenter = new InventoryPanelPresenter(panelView);
            presenter.Bind(service);
            presenter.Unbind();
            panelView.ResetCounts();

            service.AddMineral("mineral.copper", 1);
            Assert.That(panelView.CargoCount, Is.Zero);
            Assert.That(presenter.IsBound, Is.False);

            presenter.Bind(service);
            Assert.That(panelView.Cargo, Does.Contain(HudFormatter.FormatCargo(1.5f)));
            panelView.ResetCounts();
            service.AddMineral("mineral.copper", 1);
            Assert.That(panelView.CargoCount, Is.EqualTo(1));
            Assert.That(panelView.Stacks, Does.Contain("x2"));
        }

        [Test]
        public void PanelViewContract_DoesNotTakeGameStateForMutation()
        {
            foreach (var method in typeof(IInventoryPanelView).GetMethods())
            {
                Assert.That(method.Name, Is.Not.EqualTo("AddMineral"));
                Assert.That(method.Name, Is.Not.EqualTo("TryAddMineral"));
                foreach (var param in method.GetParameters())
                {
                    Assert.That(param.ParameterType, Is.Not.EqualTo(typeof(GameState)));
                    Assert.That(param.ParameterType, Is.Not.EqualTo(typeof(InventoryService)));
                }
            }
        }
    }
}
