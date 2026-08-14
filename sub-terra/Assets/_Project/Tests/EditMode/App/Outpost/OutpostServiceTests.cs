using System.Collections.Generic;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.Inventory;
using SubTerra.App.Outpost;
using SubTerra.App.State;
using SubTerra.Shared;

namespace SubTerra.App.Tests.Outpost
{
    public sealed class OutpostServiceTests
    {
        private const string Copper = DataIds.Minerals.Copper;
        private const string Iron = DataIds.Minerals.Iron;

        [Test]
        public void H_F01_ActiveCharger_ChargesToMaximum_AndRaisesEnergyImmediately()
        {
            var system = CreateSystem();
            system.State.SetEnergy(25, 100);
            system.Service.ApplyRuntimeStatus(CreateActiveStatus());
            var energyEvents = 0;
            system.State.EnergyChanged += _ => energyEvents++;

            var result = system.Service.TryCharge();

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Quantity, Is.EqualTo(75));
            Assert.That(system.State.Player.Energy, Is.EqualTo(100));
            Assert.That(energyEvents, Is.EqualTo(1));
        }

        [Test]
        public void PromptB51_ChargerNearElevator_DoesNotRequireOutpostGlobalActiveFlag()
        {
            var system = CreateSystem();
            system.State.SetCurrentEnergy(10);
            system.Service.ApplyRuntimeStatus(new OutpostStatusDto
            {
                isActive = false,
                isInInteractionRange = true,
                interactionFacilityInstanceId = "charger.elevator",
                interactionFacilityBuildingId = DataIds.Buildings.ChargerBasic,
                connectedFacilities = new List<ConnectedFacilityStatusDto>
                {
                    new ConnectedFacilityStatusDto
                    {
                        instanceId = "charger.elevator",
                        buildingId = DataIds.Buildings.ChargerBasic,
                        isActive = true
                    }
                }
            });

            var result = system.Service.TryCharge();

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(system.State.Player.Energy, Is.EqualTo(system.State.Player.MaxEnergy));
        }

        [Test]
        public void H_F02_InactiveRuntime_ShowsReason_AndDoesNotChangeEnergy()
        {
            var system = CreateSystem();
            system.State.SetEnergy(25, 100);
            system.Service.ApplyRuntimeStatus(new OutpostStatusDto
            {
                outpostInstanceId = "outpost.1",
                isActive = false,
                isInInteractionRange = true,
                inactiveReasonId = "power_disconnected",
                connectedFacilities = new List<ConnectedFacilityStatusDto>()
            });

            var before = system.State.Player.Energy;
            var result = system.Service.TryCharge();
            var snapshot = system.Service.GetSnapshot();

            Assert.That(result.Status, Is.EqualTo(OutpostOperationStatus.OutpostUnavailable));
            Assert.That(system.State.Player.Energy, Is.EqualTo(before));
            Assert.That(snapshot.InactiveReasonId, Is.EqualTo("power_disconnected"));
            Assert.That(snapshot.IsActive, Is.False);
        }

        [Test]
        public void H_F02A_Charge_UsesTheNearbyChargerInsteadOfAnotherActiveCharger()
        {
            var system = CreateSystem();
            var status = CreateActiveStatus();
            status.interactionFacilityInstanceId = "charger.nearby";
            status.interactionFacilityBuildingId = DataIds.Buildings.ChargerBasic;
            status.connectedFacilities.Add(new ConnectedFacilityStatusDto
            {
                instanceId = "charger.nearby",
                buildingId = DataIds.Buildings.ChargerBasic,
                isActive = false,
                inactiveReasonId = "power_disconnected"
            });
            system.Service.ApplyRuntimeStatus(status);

            var result = system.Service.TryCharge();

            Assert.That(result.Status, Is.EqualTo(OutpostOperationStatus.FacilityUnavailable));
            Assert.That(result.Message, Is.EqualTo("power_disconnected"));
        }

        [Test]
        public void H_F02B_Charge_RejectsWhenTheNearbyFacilityIsNotACharger()
        {
            var system = CreateSystem();
            var status = CreateActiveStatus();
            status.interactionFacilityInstanceId = "storage.1";
            status.interactionFacilityBuildingId = DataIds.Buildings.StorageBasic;
            system.Service.ApplyRuntimeStatus(status);

            var result = system.Service.TryCharge();

            Assert.That(result.Status, Is.EqualTo(OutpostOperationStatus.FacilityUnavailable));
        }

        [Test]
        public void H_F03_DepositAndWithdraw_PreserveTotals_AndWeights()
        {
            var system = CreateSystem();
            system.Inventory.TryAddMineral(Copper, 5);
            system.Inventory.TryAddMineral(Iron, 3);
            system.Service.ApplyRuntimeStatus(CreateActiveStatus());

            Assert.That(system.Service.TryDeposit(Copper, 3).IsSuccess, Is.True);
            Assert.That(system.Service.TryDeposit(Iron, 1).IsSuccess, Is.True);
            Assert.That(system.Service.TryWithdraw(Copper, 1).IsSuccess, Is.True);

            Assert.That(system.Inventory.State.GetQuantity(Copper), Is.EqualTo(3));
            Assert.That(system.Service.State.GetStorageQuantity(Copper), Is.EqualTo(2));
            Assert.That(system.Inventory.State.GetQuantity(Iron), Is.EqualTo(2));
            Assert.That(system.Service.State.GetStorageQuantity(Iron), Is.EqualTo(1));

            var snapshot = system.Service.GetSnapshot();
            Assert.That(snapshot.PlayerCargo.CurrentWeight, Is.EqualTo((3 * 1.5f) + (2 * 2f)));
            Assert.That(snapshot.Storage.CurrentWeight, Is.EqualTo((2 * 1.5f) + (1 * 2f)));
        }

        [Test]
        public void H_S04_FailedWithdraw_IsAtomic()
        {
            var system = CreateSystem(maxCapacity: 8f);
            system.Inventory.TryAddMineral(Copper, 4);
            system.Service.ApplyRuntimeStatus(CreateActiveStatus());
            system.Service.TryDeposit(Copper, 2);
            system.Inventory.TryAddMineral(Iron, 2);

            var playerBefore = system.Inventory.State.CaptureFingerprint();
            var storageBefore = system.Service.State.GetStorageQuantity(Copper);
            var result = system.Service.TryWithdraw(Copper, 2);

            Assert.That(result.Status, Is.EqualTo(OutpostOperationStatus.CapacityExceeded));
            Assert.That(system.Inventory.State.CaptureFingerprint().Equals(playerBefore), Is.True);
            Assert.That(system.Service.State.GetStorageQuantity(Copper), Is.EqualTo(storageBefore));
        }

        [Test]
        public void H_F04_DuplicateSettlementId_PaysAndRemovesOnlyOnce()
        {
            var system = CreateSystem();
            system.Inventory.TryAddMineral(Copper, 4);
            system.Service.ApplyRuntimeStatus(CreateActiveStatus());
            system.Service.TryDeposit(Copper, 4);
            var operationEvents = 0;
            var saveEvents = 0;
            system.Service.OperationCompleted += result =>
            {
                if (result.IsSuccess && result.Kind == OutpostOperationKind.SettleStorage)
                {
                    operationEvents++;
                }
            };
            system.Service.AutoSaveRequested += request =>
            {
                if (request.Reason == OutpostAutoSaveReason.Settlement)
                {
                    saveEvents++;
                }
            };

            var first = system.Service.TrySettle(
                OutpostSettlementSource.Storage,
                "settlement.same");
            var duplicate = system.Service.TrySettle(
                OutpostSettlementSource.Storage,
                "settlement.same");

            Assert.That(first.IsSuccess, Is.True);
            Assert.That(first.GoldDelta, Is.EqualTo(40));
            Assert.That(duplicate.Status, Is.EqualTo(OutpostOperationStatus.AlreadyProcessed));
            Assert.That(system.State.Player.Gold, Is.EqualTo(40));
            Assert.That(system.Service.State.GetStorageQuantity(Copper), Is.Zero);
            Assert.That(operationEvents, Is.EqualTo(1));
            Assert.That(saveEvents, Is.EqualTo(1));
        }

        [Test]
        public void H_F05_DuplicateInstallation_UpdatesCheckpointAndRequestsSaveOnce()
        {
            var system = CreateSystem();
            var saveEvents = 0;
            var tutorialEvents = 0;
            system.Service.AutoSaveRequested += _ => saveEvents++;
            system.Service.TutorialRequested += () => tutorialEvents++;

            var first = system.Service.HandleOutpostInstalled(
                "outpost.1",
                "checkpoint.deep.1",
                12,
                -4);
            var duplicate = system.Service.HandleOutpostInstalled(
                "outpost.1",
                "checkpoint.deep.1",
                12,
                -4);

            Assert.That(first.IsSuccess, Is.True);
            Assert.That(duplicate.Status, Is.EqualTo(OutpostOperationStatus.AlreadyProcessed));
            Assert.That(system.Service.State.CheckpointId, Is.EqualTo("checkpoint.deep.1"));
            Assert.That(system.Service.State.CheckpointX, Is.EqualTo(12));
            Assert.That(system.Service.State.CheckpointY, Is.EqualTo(-4));
            Assert.That(system.State.Progress.HasSeenOutpostTutorial, Is.True);
            Assert.That(saveEvents, Is.EqualTo(1));
            Assert.That(tutorialEvents, Is.EqualTo(1));
        }

        [Test]
        public void H_S01_RuntimeStatus_IsConsumedWithoutDistanceOrPowerRecalculation()
        {
            var system = CreateSystem();
            var status = CreateActiveStatus();
            status.totalPowerSupply = 2f;
            status.totalPowerConsumption = 99f;
            status.isActive = true;

            system.Service.ApplyRuntimeStatus(status);
            var snapshot = system.Service.GetSnapshot();

            Assert.That(snapshot.IsActive, Is.True);
            Assert.That(snapshot.PowerSupply, Is.EqualTo(2f));
            Assert.That(snapshot.PowerConsumption, Is.EqualTo(99f));
        }

        private static TestSystem CreateSystem(float maxCapacity = 100f)
        {
            var catalog = new InMemoryMineralCatalog();
            catalog.Register(Copper, 1.5f, 10, "구리");
            catalog.Register(Iron, 2f, 15, "철");
            var state = GameState.CreateNew();
            var inventory = new InventoryService(catalog, maxCapacity, state);
            var service = new OutpostService(inventory, catalog, state);
            return new TestSystem(service, inventory, state);
        }

        private static OutpostStatusDto CreateActiveStatus()
        {
            return new OutpostStatusDto
            {
                outpostInstanceId = "outpost.1",
                isActive = true,
                isInInteractionRange = true,
                totalPowerSupply = 10f,
                totalPowerConsumption = 5f,
                connectedFacilities = new List<ConnectedFacilityStatusDto>
                {
                    new ConnectedFacilityStatusDto
                    {
                        instanceId = "charger.1",
                        buildingId = DataIds.Buildings.ChargerBasic,
                        isActive = true
                    },
                    new ConnectedFacilityStatusDto
                    {
                        instanceId = "storage.1",
                        buildingId = DataIds.Buildings.StorageBasic,
                        isActive = true
                    },
                    new ConnectedFacilityStatusDto
                    {
                        instanceId = "settlement.1",
                        buildingId = DataIds.Buildings.SettlementBasic,
                        isActive = true
                    }
                }
            };
        }

        private readonly struct TestSystem
        {
            public OutpostService Service { get; }
            public InventoryService Inventory { get; }
            public GameState State { get; }

            public TestSystem(
                OutpostService service,
                InventoryService inventory,
                GameState state)
            {
                Service = service;
                Inventory = inventory;
                State = state;
            }
        }
    }
}
