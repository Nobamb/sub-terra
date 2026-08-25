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
        public void RuntimeRange_OpensOnlyAfterInteraction_AndClosesWhenLeavingRange()
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
                interactionFacilityInstanceId = "outpost.1",
                interactionFacilityBuildingId = DataIds.Buildings.OutpostCoreBasic,
                inactiveReasonId = "power_disconnected",
                totalPowerSupply = 4f,
                totalPowerConsumption = 7f,
                connectedFacilities = new List<ConnectedFacilityStatusDto>()
            });

            Assert.That(view.Visible, Is.False);
            Assert.That(view.Active, Is.False);
            Assert.That(view.Reason, Is.EqualTo("power_disconnected"));
            Assert.That(view.Supply, Is.EqualTo(4f));
            Assert.That(view.Consumption, Is.EqualTo(7f));

            presenter.ToggleInteractionPanel();
            Assert.That(view.Visible, Is.True);

            service.ClearRuntimeStatus();
            Assert.That(view.Visible, Is.False);
            presenter.Unbind();
        }

        [Test]
        public void PromptB60_2_ElevatorInteractionClosesPanel_WhileOtherPositionsCanOpenIt()
        {
            var catalog = new InMemoryMineralCatalog();
            var state = GameState.CreateNew();
            var inventory = new InventoryService(catalog, 100f, state);
            var service = new OutpostService(inventory, catalog, state);
            var view = new RecordingView();
            var presenter = new OutpostPanelPresenter(view);
            presenter.Bind(service);
            service.ApplyRuntimeStatus(new OutpostStatusDto
            {
                isInInteractionRange = true,
                interactionFacilityInstanceId = "outpost.near-elevator",
                interactionFacilityBuildingId = DataIds.Buildings.OutpostCoreBasic,
                connectedFacilities = new List<ConnectedFacilityStatusDto>()
            });

            presenter.ToggleInteractionPanel(primaryInteractionClaimed: false);
            Assert.That(view.Visible, Is.True, "엘리베이터 밖에서는 전진기지 패널을 열 수 있어야 한다.");

            presenter.ToggleInteractionPanel(primaryInteractionClaimed: true);
            Assert.That(view.Visible, Is.False, "엘리베이터 이동 입력이 시설 패널보다 우선해야 한다.");
            presenter.Unbind();
        }

        [Test]
        public void RuntimeRange_Charger_OpensChargerPanel()
        {
            var catalog = new InMemoryMineralCatalog();
            var state = GameState.CreateNew();
            var inventory = new InventoryService(catalog, 100f, state);
            var service = new OutpostService(inventory, catalog, state);
            var view = new RecordingView();
            var presenter = new OutpostPanelPresenter(view);
            presenter.Bind(service);

            service.ApplyRuntimeStatus(new OutpostStatusDto
            {
                isInInteractionRange = true,
                interactionFacilityInstanceId = "charger.1",
                interactionFacilityBuildingId = DataIds.Buildings.ChargerBasic,
                connectedFacilities = new List<ConnectedFacilityStatusDto>
                {
                    new ConnectedFacilityStatusDto
                    {
                        instanceId = "charger.1",
                        buildingId = DataIds.Buildings.ChargerBasic,
                        isActive = true
                    }
                }
            });

            presenter.ToggleInteractionPanel();

            Assert.That(view.Visible, Is.True);
            Assert.That(view.Mode, Is.EqualTo(OutpostPanelMode.Charger));
            Assert.That(view.TemporaryMessage, Is.Null);
            presenter.Unbind();
        }

        [Test]
        public void RuntimeRange_LeavingOutpostForCharger_ClosesAndCannotReopenOutpostPanel()
        {
            var catalog = new InMemoryMineralCatalog();
            var state = GameState.CreateNew();
            var inventory = new InventoryService(catalog, 100f, state);
            var service = new OutpostService(inventory, catalog, state);
            var view = new RecordingView();
            var presenter = new OutpostPanelPresenter(view);
            presenter.Bind(service);

            service.ApplyRuntimeStatus(new OutpostStatusDto
            {
                isInInteractionRange = true,
                interactionFacilityInstanceId = "outpost.1",
                interactionFacilityBuildingId = DataIds.Buildings.OutpostCoreBasic,
                connectedFacilities = new List<ConnectedFacilityStatusDto>()
            });
            presenter.ToggleInteractionPanel();
            Assert.That(view.Visible, Is.True);

            service.ApplyRuntimeStatus(new OutpostStatusDto
            {
                isInInteractionRange = true,
                interactionFacilityInstanceId = "charger.1",
                interactionFacilityBuildingId = DataIds.Buildings.ChargerBasic,
                connectedFacilities = new List<ConnectedFacilityStatusDto>()
            });

            Assert.That(view.Visible, Is.False);
            presenter.ToggleInteractionPanel();
            Assert.That(view.Visible, Is.True);
            Assert.That(view.Mode, Is.EqualTo(OutpostPanelMode.Charger));
            presenter.Unbind();
        }

        [Test]
        public void ToggleInteractionPanel_OutsideRange_StaysClosed()
        {
            var catalog = new InMemoryMineralCatalog();
            var state = GameState.CreateNew();
            var inventory = new InventoryService(catalog, 100f, state);
            var service = new OutpostService(inventory, catalog, state);
            var view = new RecordingView();
            var presenter = new OutpostPanelPresenter(view);
            presenter.Bind(service);

            presenter.ToggleInteractionPanel();

            Assert.That(view.Visible, Is.False);
            presenter.Unbind();
        }

        [Test]
        public void ToggleInteractionPanel_DisconnectedCharger_ShowsCenteredMessageForThreeSeconds()
        {
            var catalog = new InMemoryMineralCatalog();
            var state = GameState.CreateNew();
            var service = new OutpostService(new InventoryService(catalog, 100f, state), catalog, state);
            var view = new RecordingView();
            var presenter = new OutpostPanelPresenter(view);
            presenter.Bind(service);
            service.ApplyRuntimeStatus(new OutpostStatusDto
            {
                isInInteractionRange = true,
                interactionFacilityInstanceId = "charger.1",
                interactionFacilityBuildingId = DataIds.Buildings.ChargerBasic,
                connectedFacilities = new List<ConnectedFacilityStatusDto>
                {
                    new ConnectedFacilityStatusDto
                    {
                        instanceId = "charger.1",
                        buildingId = DataIds.Buildings.ChargerBasic,
                        isActive = false,
                        inactiveReasonId = "power_disconnected"
                    }
                }
            });

            presenter.ToggleInteractionPanel();

            Assert.That(view.Visible, Is.False);
            Assert.That(view.TemporaryMessage, Is.EqualTo(
                "충전기 사용불가, 전력망 미연결\n"
                + " 엘레베이터 또는 전진기지 코어 근처에서 전력망 연결이 가능합니다."));
            Assert.That(view.TemporaryMessageDuration, Is.EqualTo(3f));
            presenter.Unbind();
        }

        [Test]
        public void ToggleInteractionPanel_DisconnectedSettlement_UsesSettlementName()
        {
            var catalog = new InMemoryMineralCatalog();
            var state = GameState.CreateNew();
            var service = new OutpostService(new InventoryService(catalog, 100f, state), catalog, state);
            var view = new RecordingView();
            var presenter = new OutpostPanelPresenter(view);
            presenter.Bind(service);
            service.ApplyRuntimeStatus(new OutpostStatusDto
            {
                isInInteractionRange = true,
                interactionFacilityInstanceId = "settlement.1",
                interactionFacilityBuildingId = DataIds.Buildings.SettlementBasic,
                connectedFacilities = new List<ConnectedFacilityStatusDto>
                {
                    new ConnectedFacilityStatusDto
                    {
                        instanceId = "settlement.1",
                        buildingId = DataIds.Buildings.SettlementBasic,
                        inactiveReasonId = "power_disconnected"
                    }
                }
            });

            presenter.ToggleInteractionPanel();

            Assert.That(view.TemporaryMessage, Does.StartWith("정산 콘솔 사용불가"));
            presenter.Unbind();
        }

        [TestCase(DataIds.Buildings.StorageBasic, OutpostPanelMode.Storage)]
        [TestCase(DataIds.Buildings.SettlementBasic, OutpostPanelMode.Settlement)]
        public void PromptB52_Interaction_OpensFacilitySpecificPanel(
            string buildingId,
            OutpostPanelMode expectedMode)
        {
            var catalog = new InMemoryMineralCatalog();
            var state = GameState.CreateNew();
            var service = new OutpostService(
                new InventoryService(catalog, 100f, state),
                catalog,
                state);
            var view = new RecordingView();
            var presenter = new OutpostPanelPresenter(view);
            presenter.Bind(service);
            service.ApplyRuntimeStatus(new OutpostStatusDto
            {
                isActive = true,
                isInInteractionRange = true,
                interactionFacilityInstanceId = "facility.1",
                interactionFacilityBuildingId = buildingId,
                connectedFacilities = new List<ConnectedFacilityStatusDto>
                {
                    new ConnectedFacilityStatusDto
                    {
                        instanceId = "facility.1",
                        buildingId = buildingId,
                        isActive = true
                    }
                }
            });

            presenter.ToggleInteractionPanel();

            Assert.That(view.Visible, Is.True);
            Assert.That(view.Mode, Is.EqualTo(expectedMode));
            presenter.Unbind();
        }

        [Test]
        public void PromptB69_Search_FiltersMineralDropdownByPartialName()
        {
            var catalog = new InMemoryMineralCatalog();
            catalog.Register(DataIds.Minerals.Copper, 1.5f, 10, "구리");
            catalog.Register(DataIds.Minerals.Iron, 2f, 15, "철");
            catalog.Register(DataIds.Minerals.Lithium, 0.8f, 20, "리튬");
            var state = GameState.CreateNew();
            var inventory = new InventoryService(catalog, 100f, state);
            inventory.TryAddMineral(DataIds.Minerals.Copper, 8);
            var service = new OutpostService(inventory, catalog, state);
            var view = new RecordingView();
            var presenter = new OutpostPanelPresenter(view);
            presenter.Bind(service);
            service.ApplyRuntimeStatus(new OutpostStatusDto
            {
                isInInteractionRange = true,
                interactionFacilityInstanceId = "storage.1",
                interactionFacilityBuildingId = DataIds.Buildings.StorageBasic,
                connectedFacilities = new List<ConnectedFacilityStatusDto>()
            });

            presenter.ToggleInteractionPanel();
            Assert.That(view.MineralOptions, Is.Not.Null);
            Assert.That(view.MineralOptions.Count, Is.EqualTo(3));

            presenter.SetMineralSearch("구");
            Assert.That(view.MineralOptions.Count, Is.EqualTo(1));
            Assert.That(view.MineralOptions[0].MineralId, Is.EqualTo(DataIds.Minerals.Copper));

            presenter.SelectMineral(DataIds.Minerals.Copper);
            Assert.That(view.PickerSelectedMineralId, Is.EqualTo(DataIds.Minerals.Copper));
            Assert.That(view.MineralOptions.Count, Is.EqualTo(3));
            presenter.Unbind();
        }

        [Test]
        public void PromptB70_DismissClosesPanelWhileStillInRange_AndECanReopen()
        {
            var catalog = new InMemoryMineralCatalog();
            var state = GameState.CreateNew();
            var service = new OutpostService(
                new InventoryService(catalog, 100f, state),
                catalog,
                state);
            var view = new RecordingView();
            var presenter = new OutpostPanelPresenter(view);
            presenter.Bind(service);
            service.ApplyRuntimeStatus(new OutpostStatusDto
            {
                isInInteractionRange = true,
                interactionFacilityInstanceId = "storage.1",
                interactionFacilityBuildingId = DataIds.Buildings.StorageBasic,
                connectedFacilities = new List<ConnectedFacilityStatusDto>()
            });

            presenter.ToggleInteractionPanel();
            Assert.That(view.Visible, Is.True);
            Assert.That(presenter.IsInteractionPanelOpen, Is.True);

            presenter.DismissInteractionPanel();
            Assert.That(view.Visible, Is.False);
            Assert.That(presenter.IsInteractionPanelOpen, Is.False);
            Assert.That(service.IsFacilityInteraction, Is.True, "닫아도 시설 범위에는 남아 있어야 한다.");

            presenter.ToggleInteractionPanel();
            Assert.That(view.Visible, Is.True);
            Assert.That(view.Mode, Is.EqualTo(OutpostPanelMode.Storage));
            presenter.Unbind();
        }

        private sealed class RecordingView : IOutpostPanelView
        {
            public bool Visible;
            public bool Active;
            public string Reason;
            public float Supply;
            public float Consumption;
            public string TemporaryMessage;
            public float TemporaryMessageDuration;
            public OutpostPanelMode Mode;

            public void SetVisible(bool visible) => Visible = visible;
            public void SetMode(OutpostPanelMode mode) => Mode = mode;

            public void SetPower(float supply, float consumption, bool active, string inactiveReasonId)
            {
                Supply = supply;
                Consumption = consumption;
                Active = active;
                Reason = inactiveReasonId;
            }

            public void SetFacilities(IReadOnlyList<OutpostFacilityReadModel> facilities) { }
            public void SetCargo(string playerCargo, string storageCargo) { }
            public void SetSettlementCargo(string cargo) { }
            public void SetCheckpoint(string checkpoint) { }
            public IReadOnlyList<OutpostMineralOption> MineralOptions;
            public string PickerSelectedMineralId;

            public void SetSelectedMineral(string summary) { }
            public void SetMineralOptions(
                IReadOnlyList<OutpostMineralOption> options,
                string selectedMineralId)
            {
                MineralOptions = options;
                PickerSelectedMineralId = selectedMineralId;
            }

            public void ClearMineralSearch() { }
            public void SetResult(string message, bool isError) { }
            public void ShowTemporaryMessage(string message, float durationSeconds)
            {
                TemporaryMessage = message;
                TemporaryMessageDuration = durationSeconds;
            }
            public void SetTutorialVisible(bool visible) { }
            public void SetBusy(bool busy) { }
        }
    }
}
