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
                worldSeed = 4_294_967_301L,
                changedTiles = new List<ChangedTileSnapshotDto>
                {
                    new ChangedTileSnapshotDto
                    {
                        x = 2,
                        y = 3,
                        tileId = "tile.rock.fractured",
                        remainingDurability = 0.25f
                    }
                },
                gasChanges = new List<GasSnapshotDto>
                {
                    new GasSnapshotDto
                    {
                        gasZoneId = "gas-zone-01",
                        gasTypeId = "gas.basic",
                        x = 4,
                        y = 7,
                        concentrationLevel = 0.8f,
                        remainingDuration = 12.5f,
                        isActive = true,
                        isNeutralized = false
                    }
                },
                discoveredChunkIds = new List<string> { "chunk.middle-gas.01" },
                powerState = new PowerSnapshotDto
                {
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

            Assert.That(restored.version, Is.EqualTo("1.2"));
            Assert.That(restored.worldSeed, Is.EqualTo(4_294_967_301L));
            Assert.That(restored.changedTiles[0].tileId, Is.EqualTo("tile.rock.fractured"));
            Assert.That(restored.gasChanges[0].gasZoneId, Is.EqualTo("gas-zone-01"));
            Assert.That(restored.gasChanges[0].gasTypeId, Is.EqualTo("gas.basic"));
            Assert.That(restored.gasChanges[0].remainingDuration, Is.EqualTo(12.5f));
            Assert.That(restored.gasChanges[0].isActive, Is.True);
            Assert.That(restored.gasChanges[0].isNeutralized, Is.False);
            Assert.That(restored.discoveredChunkIds, Is.EqualTo(new[] { "chunk.middle-gas.01" }));
            Assert.That(
                restored.powerState.cableConnections[0].nodeAInstanceId,
                Is.EqualTo("building-core-01"));
            Assert.That(
                restored.powerState.cableConnections[0].nodeBInstanceId,
                Is.EqualTo("building-cable-01"));
        }

        [Test]
        public void PowerSnapshot_ContainsTopologyOnly_NotDerivedGridValues()
        {
            var fields = typeof(PowerSnapshotDto).GetFields();

            Assert.That(fields, Has.Length.EqualTo(1));
            Assert.That(fields[0].Name, Is.EqualTo("cableConnections"));
        }
    }
}
