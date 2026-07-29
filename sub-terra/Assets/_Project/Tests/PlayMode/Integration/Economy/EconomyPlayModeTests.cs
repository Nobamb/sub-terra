using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using SubTerra.App.Economy;
using SubTerra.App.Inventory;
using SubTerra.App.State;
using SubTerra.App.UI.Economy;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.TestTools;

namespace SubTerra.App.Tests.PlayMode.Integration.Economy
{
    /// <summary>
    /// Play Mode: 판매·제작 UI 메시지 구분과 중복 클릭 가드.
    /// Editor/Play 환경이 없으면 Edit Mode 게이트로 대체한다.
    /// </summary>
    public sealed class EconomyPlayModeTests
    {
        private const string Copper = "mineral.copper";

        private sealed class PlayView : IEconomyPanelView
        {
            public string Message;
            public string Detail;
            public bool LastBusy;

            public void SetStatusMessage(string message) => Message = message;
            public void SetStatusDetail(string detail) => Detail = detail;
            public void SetBusy(bool busy) => LastBusy = busy;
            public void SetVisible(bool visible) { }
        }

        private sealed class PlaceOk : IBuildingPlacementGate
        {
            public bool TryPlace(string buildingId) => true;
        }

        private sealed class PlaceFail : IBuildingPlacementGate
        {
            public bool TryPlace(string buildingId) => false;
        }

        [UnityTest]
        public IEnumerator Play_SellSuccessAndFail_MessagesDiffer()
        {
            var catalog = new InMemoryMineralCatalog();
            catalog.Register(Copper, 1f, 10, "Copper");
            var state = GameState.CreateNew();
            var inventory = new InventoryService(catalog, 50f, state);
            inventory.TryAddMineral(Copper, 3);
            var economy = new EconomyService(inventory, catalog, state);
            var crafting = new CraftingService(economy);
            var view = new PlayView();
            var presenter = new EconomyPanelPresenter(view);
            presenter.Bind(economy, crafting);

            var ok = presenter.RequestSell(Copper, 2);
            Assert.That(ok.IsSuccess, Is.True);
            Assert.That(view.Message, Does.Contain("판매"));
            Assert.That(state.Player.Gold, Is.EqualTo(20));

            var fail = presenter.RequestSell(Copper, 99);
            Assert.That(fail.IsSuccess, Is.False);
            Assert.That(view.Message, Does.Contain("부족").Or.Contain("판매"));
            Assert.That(inventory.State.GetQuantity(Copper), Is.EqualTo(1));

            yield return null;
        }

        [UnityTest]
        public IEnumerator Play_CraftPlacementFail_KeepsResources()
        {
            var catalog = new InMemoryMineralCatalog();
            catalog.Register(Copper, 1f, 10, "Copper");
            var state = GameState.CreateNew();
            var inventory = new InventoryService(catalog, 50f, state);
            inventory.TryAddMineral(Copper, 5);
            var economy = new EconomyService(inventory, catalog, state);
            var crafting = new CraftingService(economy);
            var view = new PlayView();
            var presenter = new EconomyPanelPresenter(view);
            presenter.Bind(economy, crafting);

            var costs = new List<ItemCostDto> { new ItemCostDto(Copper, 3) };
            var result = presenter.RequestCraft("building.support.basic", costs, new PlaceFail());

            Assert.That(result.Status, Is.EqualTo(EconomyTransactionStatus.PlacementFailed));
            Assert.That(inventory.State.GetQuantity(Copper), Is.EqualTo(5));
            Assert.That(view.Message, Does.Contain("설치"));

            yield return null;
        }
    }
}
