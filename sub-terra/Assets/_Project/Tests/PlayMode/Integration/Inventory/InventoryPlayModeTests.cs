using System.Collections;
using NUnit.Framework;
using SubTerra.App.Core;
using SubTerra.App.Inventory;
using SubTerra.App.State;
using SubTerra.App.UI.HUD;
using SubTerra.App.UI.Inventory;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.TestTools;

namespace SubTerra.App.Tests
{
    /// <summary>
    /// D-F04 Play Mode: Shared 경계 지급 후 HUD·패널 동기화, 재바인드 시 핸들러 잔존 없음.
    /// </summary>
    public sealed class InventoryPlayModeTests
    {
        [UnityTest]
        public IEnumerator SharedReceiver_UpdatesHudAndPanel_ThenUnbindLeavesNoHandlers()
        {
            GameBootstrapper.ResetInstanceForTests();
            var bootGo = new GameObject("InvPlayBoot");
            var boot = bootGo.AddComponent<GameBootstrapper>();
            Assert.That(boot.Initialize(new NullCatalog(), new EmptySave(), new NoOpSceneLoader()), Is.True);

            var catalog = new InMemoryMineralCatalog();
            catalog.Register("mineral.copper", 1.5f, 10, "Copper");
            catalog.Register("mineral.lithium", 0.8f, 40, "Lithium");

            var state = boot.State;
            var service = new InventoryService(catalog, 50f, state);

            var hudView = new CountingHud();
            var hudPresenter = new HudPresenter(hudView);
            hudPresenter.Bind(state);

            var panelView = new CountingPanel();
            var panelPresenter = new InventoryPanelPresenter(panelView);
            panelPresenter.Bind(service);

            hudView.ResetCounts();
            panelView.ResetCounts();

            IMiningRewardReceiver receiver = service;
            receiver.AddMineral("mineral.copper", 3);
            receiver.AddMineral("mineral.lithium", 2);

            var expectedWeight = 3 * 1.5f + 2 * 0.8f;
            var expectedValue = 3 * 10 + 2 * 40;

            Assert.That(hudView.Cargo, Is.EqualTo(HudFormatter.FormatCargo(expectedWeight)));
            Assert.That(hudView.Unsettled, Is.EqualTo(HudFormatter.FormatUnsettledValue(expectedValue)));
            Assert.That(panelView.Value, Is.EqualTo(HudFormatter.FormatUnsettledValue(expectedValue)));
            Assert.That(panelView.Stacks, Does.Contain("Copper x3"));
            Assert.That(panelView.Stacks, Does.Contain("Lithium x2"));
            Assert.That(state.GetInventory().CargoWeight, Is.EqualTo(service.CurrentWeight).Within(0.0001f));

            // Unbind 후 추가 지급은 UI에 반영되지 않음
            hudPresenter.Unbind();
            panelPresenter.Unbind();
            hudView.ResetCounts();
            panelView.ResetCounts();
            receiver.AddMineral("mineral.copper", 1);
            Assert.That(hudView.CargoCount, Is.Zero);
            Assert.That(panelView.CargoCount, Is.Zero);

            // 재바인드: 현재 스냅샷 표시 후 추가 갱신 1회
            hudPresenter.Bind(state);
            panelPresenter.Bind(service);
            Assert.That(service.State.GetQuantity("mineral.copper"), Is.EqualTo(4));
            hudView.ResetCounts();
            panelView.ResetCounts();
            receiver.AddMineral("mineral.copper", 1);
            Assert.That(hudView.CargoCount, Is.EqualTo(1));
            Assert.That(panelView.CargoCount, Is.EqualTo(1));
            Assert.That(hudView.UnsettledCount, Is.EqualTo(1));

            hudPresenter.Unbind();
            panelPresenter.Unbind();
            Object.Destroy(bootGo);
            GameBootstrapper.ResetInstanceForTests();
            yield return null;
        }

        private sealed class NoOpSceneLoader : ISceneLoader
        {
            public bool Load(string sceneName) => true;
        }

        private sealed class CountingHud : IHudView
        {
            public int CargoCount;
            public int UnsettledCount;
            public string Cargo;
            public string Unsettled;

            public void ResetCounts()
            {
                CargoCount = 0;
                UnsettledCount = 0;
            }

            public void SetEnergy(string text) { }
            public void SetDepth(string text) { }
            public void SetGold(string text) { }

            public void SetCargo(string text)
            {
                Cargo = text;
                CargoCount++;
            }

            public void SetUnsettledValue(string text)
            {
                Unsettled = text;
                UnsettledCount++;
            }

            public void SetStructuralRisk(string text) { }
            public void SetGasRisk(string text) { }
            public void SetGasWarningVisible(bool visible) { }
            public void SetBuildingSelection(string text) { }
            public void SetInteractionPrompt(string text) { }
        }

        private sealed class CountingPanel : IInventoryPanelView
        {
            public int CargoCount;
            public string Cargo;
            public string Value;
            public string Stacks;

            public void ResetCounts()
            {
                CargoCount = 0;
            }

            public void SetCargoSummary(string cargoText)
            {
                Cargo = cargoText;
                CargoCount++;
            }

            public void SetUnsettledValue(string valueText)
            {
                Value = valueText;
            }

            public void SetStacksText(string stacksText)
            {
                Stacks = stacksText;
            }

            public void SetStacks(System.Collections.Generic.IReadOnlyList<InventoryStackReadModel> stacks) { }

            public void SetVisible(bool visible) { }
        }
    }
}
