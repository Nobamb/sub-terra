using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.Integration;
using SubTerra.App.Inventory;
using SubTerra.App.Outpost;
using SubTerra.App.State;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.TestTools;

namespace SubTerra.App.Tests.PlayMode.Outpost
{
    public sealed class OutpostPlayModeTests
    {
        [UnityTest]
        public IEnumerator H_RuntimeBridge_UsesSharedStatus_AndClosesOnDisable()
        {
            var catalog = new InMemoryMineralCatalog();
            catalog.Register(DataIds.Minerals.Copper, 1f, 10, "구리");
            var state = GameState.CreateNew();
            var inventory = new InventoryService(catalog, 100f, state);
            var service = new OutpostService(inventory, catalog, state);

            var host = new GameObject("OutpostRuntimeBridge");
            var bridge = host.AddComponent<OutpostRuntimeBridge>();
            bridge.BindTo(service);
            bridge.Publish(new GameplayEventDto
            {
                type = GameplayEventType.OutpostStatusChanged,
                outpostStatus = new OutpostStatusDto
                {
                    outpostInstanceId = "outpost.1",
                    isActive = true,
                    isInInteractionRange = true,
                    totalPowerSupply = 8f,
                    totalPowerConsumption = 3f,
                    connectedFacilities = new List<ConnectedFacilityStatusDto>()
                }
            });

            Assert.That(service.IsPanelOpen, Is.True);
            Assert.That(service.GetSnapshot().PowerSupply, Is.EqualTo(8f));

            host.SetActive(false);
            yield return null;
            Assert.That(service.IsPanelOpen, Is.False);

            Object.Destroy(host);
            yield return null;
        }

        [UnityTest]
        public IEnumerator H_RuntimeBridge_DuplicateActivation_RequestsOneAutoSave()
        {
            var catalog = new InMemoryMineralCatalog();
            var state = GameState.CreateNew();
            var inventory = new InventoryService(catalog, 100f, state);
            var service = new OutpostService(inventory, catalog, state);
            var saveRequests = 0;
            service.AutoSaveRequested += _ => saveRequests++;

            var host = new GameObject("OutpostRuntimeBridge");
            var bridge = host.AddComponent<OutpostRuntimeBridge>();
            bridge.BindTo(service);
            var activated = new GameplayEventDto
            {
                type = GameplayEventType.OutpostActivated,
                instanceId = "outpost.1",
                entityId = "checkpoint.1",
                x = 4,
                y = 7
            };

            bridge.Publish(activated);
            bridge.Publish(activated);

            Assert.That(saveRequests, Is.EqualTo(1));
            Assert.That(service.State.CheckpointId, Is.EqualTo("checkpoint.1"));
            Assert.That(service.State.CheckpointX, Is.EqualTo(4));
            Assert.That(service.State.CheckpointY, Is.EqualTo(7));

            Object.Destroy(host);
            yield return null;
        }
    }
}
