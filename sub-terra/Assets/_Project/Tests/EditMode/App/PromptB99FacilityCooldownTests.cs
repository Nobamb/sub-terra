using System.Collections.Generic;
using NUnit.Framework;
using SubTerra.App.Core;
using SubTerra.App.Core.Data;
using SubTerra.App.Inventory;
using SubTerra.App.Outpost;
using SubTerra.App.Progression;
using SubTerra.App.Save;
using SubTerra.App.State;
using SubTerra.App.UI.Outpost;
using SubTerra.Shared;

namespace SubTerra.App.Tests
{
    public sealed class PromptB99FacilityCooldownTests
    {
        [Test]
        public void PromptB99_SameCharger_BlocksReuseUntilFiveMinutesElapse()
        {
            var system = CreateChargeSystem();
            system.State.SetEnergy(10, 100);
            system.Service.ApplyRuntimeStatus(ChargerStatus("charger.a"));

            var first = system.Service.TryCharge();
            var blocked = system.Service.TryCharge();

            Assert.That(first.IsSuccess, Is.True);
            Assert.That(system.State.Player.Energy, Is.EqualTo(100));
            Assert.That(blocked.IsSuccess, Is.False);
            Assert.That(blocked.Status, Is.EqualTo(OutpostOperationStatus.FacilityUnavailable));
            Assert.That(blocked.Message, Is.EqualTo(
                OutpostService.FormatFacilityCooldownMessage(
                    DataIds.Buildings.ChargerBasic,
                    OutpostService.FacilityUseCooldownSeconds)));

            system.State.SetCurrentEnergy(10);
            system.State.AddMineResetElapsed(OutpostService.FacilityUseCooldownSeconds);
            var afterWait = system.Service.TryCharge();

            Assert.That(afterWait.IsSuccess, Is.True);
            Assert.That(system.State.Player.Energy, Is.EqualTo(100));
        }

        [Test]
        public void PromptB99_OtherCharger_CanBeUsedWhileFirstIsCoolingDown()
        {
            var system = CreateChargeSystem();
            system.State.SetEnergy(10, 100);
            system.Service.ApplyRuntimeStatus(ChargerStatus("charger.a"));
            Assert.That(system.Service.TryCharge().IsSuccess, Is.True);

            system.State.SetCurrentEnergy(20);
            system.Service.ApplyRuntimeStatus(ChargerStatus("charger.b"));
            var other = system.Service.TryCharge();

            Assert.That(other.IsSuccess, Is.True);
            Assert.That(system.State.Player.Energy, Is.EqualTo(100));
            Assert.That(
                system.Service.State.TryGetFacilityCooldownRemaining("charger.a", out var firstRemaining),
                Is.True);
            Assert.That(firstRemaining, Is.EqualTo(OutpostService.FacilityUseCooldownSeconds));
            Assert.That(
                system.Service.State.TryGetFacilityCooldownRemaining("charger.b", out var secondRemaining),
                Is.True);
            Assert.That(secondRemaining, Is.EqualTo(OutpostService.FacilityUseCooldownSeconds));
        }

        [Test]
        public void PromptB99_IndependentTimers_DoNotResetEachOther()
        {
            var system = CreateChargeSystem();
            system.State.SetEnergy(10, 100);
            system.Service.ApplyRuntimeStatus(ChargerStatus("charger.a"));
            Assert.That(system.Service.TryCharge().IsSuccess, Is.True);

            system.State.AddMineResetElapsed(120d);
            system.State.SetCurrentEnergy(20);
            system.Service.ApplyRuntimeStatus(ChargerStatus("charger.b"));
            Assert.That(system.Service.TryCharge().IsSuccess, Is.True);

            Assert.That(
                system.Service.State.TryGetFacilityCooldownRemaining("charger.a", out var firstRemaining),
                Is.True);
            Assert.That(firstRemaining, Is.EqualTo(OutpostService.FacilityUseCooldownSeconds - 120d).Within(0.0001d));
            Assert.That(
                system.Service.State.TryGetFacilityCooldownRemaining("charger.b", out var secondRemaining),
                Is.True);
            Assert.That(secondRemaining, Is.EqualTo(OutpostService.FacilityUseCooldownSeconds));
        }

        [Test]
        public void PromptB99_ClinicCooldown_DoesNotBlockCharger()
        {
            var health = new FakeHealthCommand(true);
            var system = CreateChargeSystem(health);
            system.Service.ApplyRuntimeStatus(ClinicStatus("clinic.a"));
            Assert.That(system.Service.TryHeal().IsSuccess, Is.True);

            system.State.SetCurrentEnergy(15);
            system.Service.ApplyRuntimeStatus(ChargerStatus("charger.a"));
            var charge = system.Service.TryCharge();

            Assert.That(charge.IsSuccess, Is.True);
            Assert.That(health.CallCount, Is.EqualTo(1));
        }

        [Test]
        public void PromptB99_SameClinic_BlocksReuseUntilCooldownExpires()
        {
            var health = new FakeHealthCommand(true);
            var system = CreateChargeSystem(health);
            system.Service.ApplyRuntimeStatus(ClinicStatus("clinic.a"));

            Assert.That(system.Service.TryHeal().IsSuccess, Is.True);
            var blocked = system.Service.TryHeal();

            Assert.That(blocked.IsSuccess, Is.False);
            Assert.That(blocked.Message, Is.EqualTo(
                OutpostService.FormatFacilityCooldownMessage(
                    DataIds.Buildings.ClinicBasic,
                    OutpostService.FacilityUseCooldownSeconds)));
            Assert.That(health.CallCount, Is.EqualTo(1));

            system.State.AddMineResetElapsed(OutpostService.FacilityUseCooldownSeconds);
            health.Changed = false;
            var afterWait = system.Service.TryHeal();

            Assert.That(afterWait.IsSuccess, Is.True);
            Assert.That(afterWait.Message, Is.EqualTo("이미 체력이 최대입니다."));
            Assert.That(health.CallCount, Is.EqualTo(2));
        }

        [Test]
        public void PromptB99_DisconnectedCharger_DoesNotStartCooldown()
        {
            var system = CreateChargeSystem();
            system.State.SetEnergy(10, 100);
            system.Service.ApplyRuntimeStatus(ChargerStatus("charger.a", active: false));

            var failed = system.Service.TryCharge();

            Assert.That(failed.Status, Is.EqualTo(OutpostOperationStatus.FacilityUnavailable));
            Assert.That(
                system.Service.State.TryGetFacilityCooldownRemaining("charger.a", out _),
                Is.False);
            Assert.That(system.State.Player.Energy, Is.EqualTo(10));
        }

        [Test]
        public void PromptB99_Presenter_ShowsCooldownMessageWithoutOpeningPanel()
        {
            var system = CreateChargeSystem();
            system.State.SetEnergy(10, 100);
            var view = new RecordingView();
            var presenter = new OutpostPanelPresenter(view);
            presenter.Bind(system.Service);
            system.Service.ApplyRuntimeStatus(ChargerStatus("charger.a"));

            presenter.ToggleInteractionPanel();
            Assert.That(view.Visible, Is.True);
            Assert.That(system.State.Player.Energy, Is.EqualTo(100));

            presenter.DismissInteractionPanel();
            presenter.ToggleInteractionPanel();

            Assert.That(view.Visible, Is.False);
            Assert.That(view.TemporaryMessage, Is.EqualTo(
                OutpostService.FormatFacilityCooldownMessage(
                    DataIds.Buildings.ChargerBasic,
                    OutpostService.FacilityUseCooldownSeconds)));
            Assert.That(view.TemporaryMessageDuration, Is.EqualTo(3f));
            presenter.Unbind();
        }

        [Test]
        public void PromptB99_SaveRestore_KeepsPerInstanceRemaining()
        {
            var system = CreateChargeSystem();
            system.State.SetEnergy(10, 100);
            system.Service.ApplyRuntimeStatus(ChargerStatus("charger.a"));
            Assert.That(system.Service.TryCharge().IsSuccess, Is.True);
            system.State.AddMineResetElapsed(45d);

            var mapper = new SaveDataMapper(new SystemSaveClock());
            var data = mapper.Capture(new SaveCaptureContext(
                system.State,
                system.Inventory.State,
                new UpgradeState(),
                null,
                null,
                SceneNames.SurfaceBase,
                "test"));

            Assert.That(data, Is.Not.Null);
            Assert.That(data.outpost.facilityCooldowns.Count, Is.EqualTo(1));
            Assert.That(data.outpost.facilityCooldowns[0].instanceId, Is.EqualTo("charger.a"));
            Assert.That(
                data.outpost.facilityCooldowns[0].remainingSeconds,
                Is.EqualTo(OutpostService.FacilityUseCooldownSeconds - 45d).Within(0.0001d));

            Assert.That(mapper.TryRestore(data, out var restored), Is.True);
            Assert.That(
                restored.GameState.Outpost.TryGetFacilityCooldownRemaining("charger.a", out var remaining),
                Is.True);
            Assert.That(remaining, Is.EqualTo(OutpostService.FacilityUseCooldownSeconds - 45d).Within(0.0001d));
        }

        [Test]
        public void PromptB99_LegacySave_NormalizesEmptyFacilityCooldowns()
        {
            var data = new GameSaveData
            {
                saveVersion = SaveVersions.Current,
                targetSceneName = SceneNames.SurfaceBase,
                outpost = new OutpostSaveData
                {
                    facilityCooldowns = null
                }
            };

            SaveDataValidator.NormalizeMissingCollections(data);

            Assert.That(data.outpost.facilityCooldowns, Is.Not.Null);
            Assert.That(data.outpost.facilityCooldowns, Is.Empty);
            Assert.That(SaveDataValidator.TryValidate(data, out _), Is.True);
        }

        [Test]
        public void PromptB99_MineReset_ClearsFacilityCooldowns()
        {
            var system = CreateChargeSystem();
            system.State.SetGold(800);
            system.State.SetEnergy(10, 100);
            system.Service.ApplyRuntimeStatus(ChargerStatus("charger.a"));
            Assert.That(system.Service.TryCharge().IsSuccess, Is.True);

            var cache = new MineWorldCache();
            cache.ReplaceFromProvider(new WorldSnapshotDto
            {
                worldSeed = 41,
                generatorVersion = 7,
                buildings = new List<BuildingSnapshotDto>
                {
                    new BuildingSnapshotDto { instanceId = "charger.a" }
                }
            });

            Assert.That(
                MineResetService.TryReset(
                    system.State,
                    cache,
                    new FixedSeedSource(99),
                    out _),
                Is.True);
            Assert.That(
                system.State.Outpost.TryGetFacilityCooldownRemaining("charger.a", out _),
                Is.False);
        }

        private static ChargeSystem CreateChargeSystem(IPlayerHealthCommand health = null)
        {
            var catalog = new InMemoryMineralCatalog();
            catalog.Register(DataIds.Minerals.Copper, 1.5f, 10, "구리");
            var state = GameState.CreateNew();
            var inventory = new InventoryService(catalog, 100f, state);
            var service = new OutpostService(inventory, catalog, state, healthCommand: health);
            return new ChargeSystem(service, inventory, state);
        }

        private static OutpostStatusDto ChargerStatus(string instanceId, bool active = true)
        {
            return new OutpostStatusDto
            {
                isActive = false,
                isInInteractionRange = true,
                interactionFacilityInstanceId = instanceId,
                interactionFacilityBuildingId = DataIds.Buildings.ChargerBasic,
                connectedFacilities = new List<ConnectedFacilityStatusDto>
                {
                    new ConnectedFacilityStatusDto
                    {
                        instanceId = instanceId,
                        buildingId = DataIds.Buildings.ChargerBasic,
                        isActive = active,
                        inactiveReasonId = active ? string.Empty : "power_disconnected"
                    }
                }
            };
        }

        private static OutpostStatusDto ClinicStatus(string instanceId)
        {
            return new OutpostStatusDto
            {
                isActive = false,
                isInInteractionRange = true,
                interactionFacilityInstanceId = instanceId,
                interactionFacilityBuildingId = DataIds.Buildings.ClinicBasic,
                connectedFacilities = new List<ConnectedFacilityStatusDto>
                {
                    new ConnectedFacilityStatusDto
                    {
                        instanceId = instanceId,
                        buildingId = DataIds.Buildings.ClinicBasic,
                        isActive = true
                    }
                }
            };
        }

        private readonly struct ChargeSystem
        {
            public OutpostService Service { get; }
            public InventoryService Inventory { get; }
            public GameState State { get; }

            public ChargeSystem(
                OutpostService service,
                InventoryService inventory,
                GameState state)
            {
                Service = service;
                Inventory = inventory;
                State = state;
            }
        }

        private sealed class FakeHealthCommand : IPlayerHealthCommand
        {
            public bool Changed;
            public int CallCount;

            public FakeHealthCommand(bool changed)
            {
                Changed = changed;
            }

            public bool RestoreFull()
            {
                CallCount++;
                return Changed;
            }
        }

        private sealed class FixedSeedSource : IMineResetSeedSource
        {
            private readonly long seed;

            public FixedSeedSource(long seed)
            {
                this.seed = seed;
            }

            public long NextSeed()
            {
                return seed;
            }
        }

        private sealed class RecordingView : IOutpostPanelView
        {
            public bool Visible;
            public string TemporaryMessage;
            public float TemporaryMessageDuration;

            public void SetVisible(bool visible) => Visible = visible;
            public void SetMode(OutpostPanelMode mode) { }
            public void SetPower(float supply, float consumption, bool active, string inactiveReasonId) { }
            public void SetFacilities(IReadOnlyList<OutpostFacilityReadModel> facilities) { }
            public void SetCargo(string playerCargo, string storageCargo) { }
            public void SetSettlementCargo(string cargo) { }
            public void SetCheckpoint(string checkpoint) { }
            public void SetSelectedMineral(string summary) { }
            public void SetMineralOptions(
                IReadOnlyList<OutpostMineralOption> options,
                string selectedMineralId)
            {
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
