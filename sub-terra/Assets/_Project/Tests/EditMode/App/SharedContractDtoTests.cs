using System.Collections.Generic;
using NUnit.Framework;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.App.Tests
{
    public sealed class SharedContractDtoTests
    {
        [Test]
        public void RequiredCrossTeamContracts_HaveExpectedSignatures()
        {
            var publish = typeof(IGameplayEventSink).GetMethod(nameof(IGameplayEventSink.Publish));
            var createContext =
                typeof(IDroneContextProvider).GetMethod(nameof(IDroneContextProvider.CreateContext));

            Assert.That(publish, Is.Not.Null);
            Assert.That(publish.GetParameters()[0].ParameterType, Is.EqualTo(typeof(GameplayEventDto)));
            Assert.That(createContext, Is.Not.Null);
            Assert.That(createContext.ReturnType, Is.EqualTo(typeof(DroneContextDto)));
        }

        [Test]
        public void DroneContext_JsonRoundTrip_PreservesAnalysisInputs()
        {
            var source = new DroneContextDto
            {
                depth = 120,
                currentEnergy = 35,
                returnEnergyEstimate = 30,
                structuralIntegrity = 0.4f,
                gasRisk = 0.7f,
                unsettledCargoValue = 8_000L,
                cargoWeight = 24.5f,
                nearestBaseDistance = 18f,
                nearbyMineralIds = new List<string> { "mineral.lithium" },
                returnPathAvailable = true
            };

            var restored = JsonUtility.FromJson<DroneContextDto>(JsonUtility.ToJson(source));

            Assert.That(restored.depth, Is.EqualTo(120));
            Assert.That(restored.currentEnergy, Is.EqualTo(35));
            Assert.That(restored.returnEnergyEstimate, Is.EqualTo(30));
            Assert.That(restored.structuralIntegrity, Is.EqualTo(0.4f));
            Assert.That(restored.gasRisk, Is.EqualTo(0.7f));
            Assert.That(restored.unsettledCargoValue, Is.EqualTo(8_000L));
            Assert.That(restored.cargoWeight, Is.EqualTo(24.5f));
            Assert.That(restored.nearestBaseDistance, Is.EqualTo(18f));
            Assert.That(restored.nearbyMineralIds, Is.EqualTo(new[] { "mineral.lithium" }));
            Assert.That(restored.returnPathAvailable, Is.True);
        }

        [Test]
        public void GameplayEvent_JsonRoundTrip_PreservesTaggedPayload()
        {
            var source = new GameplayEventDto
            {
                type = GameplayEventType.BuildingPlaced,
                entityId = "building.outpost_core.basic",
                instanceId = "outpost-01",
                reasonId = "placement.success",
                x = 14,
                y = -3,
                depth = 90,
                structuralIntegrity = 0.75f,
                gasRisk = 0.1f,
                buildingPlacement = new BuildingPlacementResultDto
                {
                    state = BuildingPlacementState.Placed,
                    buildingId = "building.outpost_core.basic",
                    instanceId = "outpost-01",
                    reasonId = "placement.success",
                    x = 14,
                    y = -3
                }
            };

            var restored = JsonUtility.FromJson<GameplayEventDto>(JsonUtility.ToJson(source));

            Assert.That(restored.type, Is.EqualTo(GameplayEventType.BuildingPlaced));
            Assert.That(restored.entityId, Is.EqualTo("building.outpost_core.basic"));
            Assert.That(restored.instanceId, Is.EqualTo("outpost-01"));
            Assert.That(restored.reasonId, Is.EqualTo("placement.success"));
            Assert.That(restored.x, Is.EqualTo(14));
            Assert.That(restored.y, Is.EqualTo(-3));
            Assert.That(restored.depth, Is.EqualTo(90));
            Assert.That(restored.buildingPlacement.state, Is.EqualTo(BuildingPlacementState.Placed));
            Assert.That(restored.buildingPlacement.instanceId, Is.EqualTo("outpost-01"));
        }

        [Test]
        public void OutpostStatus_JsonRoundTrip_PreservesRuntimeStatus()
        {
            var source = new GameplayEventDto
            {
                type = GameplayEventType.OutpostStatusChanged,
                outpostStatus = new OutpostStatusDto
                {
                    outpostInstanceId = "outpost-01",
                    isActive = true,
                    isInInteractionRange = true,
                    isInPurificationRange = true,
                    totalPowerSupply = 100f,
                    totalPowerConsumption = 40f,
                    connectedFacilities = new List<ConnectedFacilityStatusDto>
                    {
                        new ConnectedFacilityStatusDto
                        {
                            instanceId = "charger-01",
                            buildingId = "building.charger.basic",
                            isActive = true
                        }
                    },
                    checkpointId = "checkpoint.outpost-01",
                    checkpointX = 14,
                    checkpointY = -3
                }
            };

            var restored = JsonUtility.FromJson<GameplayEventDto>(JsonUtility.ToJson(source));

            Assert.That(restored.type, Is.EqualTo(GameplayEventType.OutpostStatusChanged));
            Assert.That(restored.outpostStatus.totalPowerSupply, Is.EqualTo(100f));
            Assert.That(restored.outpostStatus.isInPurificationRange, Is.True);
            Assert.That(
                restored.outpostStatus.connectedFacilities[0].instanceId,
                Is.EqualTo("charger-01"));
            Assert.That(restored.outpostStatus.checkpointId, Is.EqualTo("checkpoint.outpost-01"));
        }
    }
}
