using System.Collections.Generic;
using NUnit.Framework;
using SubTerra.Shared;
using UnityEngine;

namespace SubTerra.App.Tests
{
    public sealed class WorldSnapshotDtoTests
    {
        [Test]
        public void JsonRoundTrip_PreservesWorldRestoreConnectionData()
        {
            var source = new WorldSnapshotDto
            {
                worldSeed = 73021,
                gasChanges = new List<GasSnapshotDto>
                {
                    new GasSnapshotDto
                    {
                        gasZoneId = "gas-zone-01",
                        x = 4,
                        y = 7,
                        concentrationLevel = 0.8f,
                        remainingDuration = 12.5f
                    }
                },
                powerState = new PowerSnapshotDto
                {
                    totalStoredPower = 40f,
                    gridMaxCapacity = 100f,
                    isGridActive = true,
                    cableConnections = new List<PowerConnectionSnapshotDto>
                    {
                        new PowerConnectionSnapshotDto
                        {
                            nodeAInstanceId = "building-core-01",
                            nodeBInstanceId = "building-cable-01"
                        }
                    }
                }
            };

            var json = JsonUtility.ToJson(source);
            var restored = JsonUtility.FromJson<WorldSnapshotDto>(json);

            Assert.That(restored.version, Is.EqualTo("1.1"));
            Assert.That(restored.worldSeed, Is.EqualTo(73021));
            Assert.That(restored.gasChanges[0].gasZoneId, Is.EqualTo("gas-zone-01"));
            Assert.That(restored.gasChanges[0].remainingDuration, Is.EqualTo(12.5f));
            Assert.That(
                restored.powerState.cableConnections[0].nodeAInstanceId,
                Is.EqualTo("building-core-01"));
            Assert.That(
                restored.powerState.cableConnections[0].nodeBInstanceId,
                Is.EqualTo("building-cable-01"));
        }
    }
}
