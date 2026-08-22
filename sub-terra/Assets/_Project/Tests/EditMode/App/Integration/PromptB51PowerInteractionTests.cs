using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SubTerra.App.Core.Data;
using SubTerra.App.Integration;
using SubTerra.App.State;
using SubTerra.Gameplay.Building;
using SubTerra.Gameplay.Integration;
using SubTerra.Gameplay.Power;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.App.Tests.Integration
{
    public sealed class PromptB51PowerInteractionTests
    {
        [Test]
        public void FacilityStatus_ChargerUsesTenBlockPowerSourceRange()
        {
            var root = new GameObject("PromptB51_Root");
            try
            {
                var network = root.AddComponent<PowerNetworkSystem>();
                var bridge = root.AddComponent<GameplayEventBridge>();
                SetField(bridge, "powerNetworkSystem", network);
                CreateNode(root.transform, network, "source.1", string.Empty, Vector3.zero, true);
                var charger = CreateNode(
                    root.transform,
                    network,
                    "charger.1",
                    DataIds.Buildings.ChargerBasic,
                    new Vector3(10f, 0f, 0f),
                    false);

                var withinRange = BuildFacilityStatuses(bridge);
                Assert.That(withinRange, Has.Count.EqualTo(1));
                Assert.That(withinRange[0].isActive, Is.True);

                charger.transform.position = new Vector3(10.01f, 0f, 0f);
                var outsideRange = BuildFacilityStatuses(bridge);
                Assert.That(outsideRange[0].isActive, Is.False);
                Assert.That(outsideRange[0].inactiveReasonId, Is.EqualTo("power_disconnected"));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void FacilityStatus_SettlementAcceptsElevatorAsPowerOrigin()
        {
            var root = new GameObject("PromptB51_Root");
            var elevator = new GameObject("PromptB51_Elevator");
            try
            {
                var network = root.AddComponent<PowerNetworkSystem>();
                var bridge = root.AddComponent<GameplayEventBridge>();
                SetField(bridge, "powerNetworkSystem", network);
                bridge.SetElevatorPowerOrigin(elevator.transform);
                CreateNode(
                    root.transform,
                    network,
                    "settlement.1",
                    DataIds.Buildings.SettlementBasic,
                    new Vector3(0f, -10f, 0f),
                    false);

                var statuses = BuildFacilityStatuses(bridge);

                Assert.That(statuses, Has.Count.EqualTo(1));
                Assert.That(statuses[0].isActive, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(elevator);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void PromptB62_FacilityStatusContainsOnlyChargerAndSettlement()
        {
            var root = new GameObject("PromptB62_Root");
            try
            {
                var network = root.AddComponent<PowerNetworkSystem>();
                var bridge = root.AddComponent<GameplayEventBridge>();
                SetField(bridge, "powerNetworkSystem", network);
                CreateNode(root.transform, network, "storage.1", DataIds.Buildings.StorageBasic, Vector3.zero, false);
                CreateNode(root.transform, network, "light.1", DataIds.Buildings.LightBasic, Vector3.right, false);
                CreateNode(root.transform, network, "charger.1", DataIds.Buildings.ChargerBasic, Vector3.right * 2f, false);
                CreateNode(root.transform, network, "settlement.1", DataIds.Buildings.SettlementBasic, Vector3.right * 3f, false);

                var statuses = BuildFacilityStatuses(bridge);

                Assert.That(statuses, Has.Count.EqualTo(2));
                Assert.That(statuses[0].buildingId, Is.EqualTo(DataIds.Buildings.ChargerBasic));
                Assert.That(statuses[1].buildingId, Is.EqualTo(DataIds.Buildings.SettlementBasic));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void InteractionPrompt_UsesOnlyCurrentFacility_AndHidesPassiveFailure()
        {
            var host = new GameObject("PromptB51_HazardBridge");
            try
            {
                var state = GameState.CreateNew();
                var bridge = host.AddComponent<GameplayHazardStatusBridge>();
                bridge.BindGameState(state);
                var status = new OutpostStatusDto
                {
                    isInInteractionRange = true,
                    interactionFacilityInstanceId = "storage.1",
                    interactionFacilityBuildingId = DataIds.Buildings.StorageBasic,
                    connectedFacilities = new List<ConnectedFacilityStatusDto>
                    {
                        new ConnectedFacilityStatusDto
                        {
                            instanceId = "charger.1",
                            buildingId = DataIds.Buildings.ChargerBasic,
                            inactiveReasonId = "power_disconnected"
                        },
                        new ConnectedFacilityStatusDto
                        {
                            instanceId = "storage.1",
                            buildingId = DataIds.Buildings.StorageBasic,
                            isActive = true
                        }
                    }
                };

                bridge.ApplyOutpostStatus(status);
                Assert.That(state.InteractionPrompt, Is.Empty);

                status.interactionFacilityInstanceId = "charger.1";
                status.interactionFacilityBuildingId = DataIds.Buildings.ChargerBasic;
                bridge.ApplyOutpostStatus(status);
                Assert.That(state.InteractionPrompt, Is.Empty);

                status.connectedFacilities[0].isActive = true;
                status.connectedFacilities[0].inactiveReasonId = string.Empty;
                bridge.ApplyOutpostStatus(status);
                Assert.That(state.InteractionPrompt, Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        private static BuildingInstance CreateNode(
            Transform parent,
            PowerNetworkSystem network,
            string instanceId,
            string buildingId,
            Vector3 position,
            bool isPowerSource)
        {
            var nodeObject = new GameObject(instanceId);
            nodeObject.transform.SetParent(parent);
            nodeObject.transform.position = position;
            var instance = nodeObject.AddComponent<BuildingInstance>();
            instance.Initialize(instanceId, buildingId);
            nodeObject.AddComponent<PowerNode>().Configure(
                network,
                isPowerSource,
                isPowerSource ? 100 : 0,
                isPowerSource ? 0 : 1,
                PowerPriority.Normal);
            return instance;
        }

        private static List<ConnectedFacilityStatusDto> BuildFacilityStatuses(
            GameplayEventBridge bridge)
        {
            var method = typeof(GameplayEventBridge).GetMethod(
                "BuildFacilityStatuses",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            return (List<ConnectedFacilityStatusDto>)method.Invoke(bridge, null);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }
    }
}
