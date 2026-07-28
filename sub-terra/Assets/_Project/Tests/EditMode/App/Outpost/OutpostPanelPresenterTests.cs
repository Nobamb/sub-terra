using System.Collections.Generic;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.Inventory;
using SubTerra.App.Outpost;
using SubTerra.App.State;
using SubTerra.App.UI.Outpost;
using SubTerra.Shared;

namespace SubTerra.App.Tests.Outpost
{
    public sealed class OutpostPanelPresenterTests
    {
        [Test]
        public void RuntimeRange_OpensAndClosesPanel_AndDisplaysInactiveReason()
        {
            var catalog = new InMemoryMineralCatalog();
            catalog.Register(DataIds.Minerals.Copper, 1f, 10, "구리");
            var state = GameState.CreateNew();
            var inventory = new InventoryService(catalog, 100f, state);
            var service = new OutpostService(inventory, catalog, state);
            var view = new RecordingView();
            var presenter = new OutpostPanelPresenter(view);
            presenter.Bind(service);

            service.ApplyRuntimeStatus(new OutpostStatusDto
            {
                outpostInstanceId = "outpost.1",
                isActive = false,
                isInInteractionRange = true,
                inactiveReasonId = "power_disconnected",
                totalPowerSupply = 4f,
                totalPowerConsumption = 7f,
                connectedFacilities = new List<ConnectedFacilityStatusDto>()
            });

            Assert.That(view.Visible, Is.True);
            Assert.That(view.Active, Is.False);
            Assert.That(view.Reason, Is.EqualTo("power_disconnected"));
            Assert.That(view.Supply, Is.EqualTo(4f));
            Assert.That(view.Consumption, Is.EqualTo(7f));

            service.ClearRuntimeStatus();
            Assert.That(view.Visible, Is.False);
            presenter.Unbind();
        }

        private sealed class RecordingView : IOutpostPanelView
        {
            public bool Visible;
            public bool Active;
            public string Reason;
            public float Supply;
            public float Consumption;

            public void SetVisible(bool visible) => Visible = visible;

            public void SetPower(float supply, float consumption, bool active, string inactiveReasonId)
            {
                Supply = supply;
                Consumption = consumption;
                Active = active;
                Reason = inactiveReasonId;
            }

            public void SetFacilities(IReadOnlyList<OutpostFacilityReadModel> facilities) { }
            public void SetCargo(string playerCargo, string storageCargo) { }
            public void SetCheckpoint(string checkpoint) { }
            public void SetResult(string message, bool isError) { }
            public void SetTutorialVisible(bool visible) { }
            public void SetBusy(bool busy) { }
        }
    }
}
