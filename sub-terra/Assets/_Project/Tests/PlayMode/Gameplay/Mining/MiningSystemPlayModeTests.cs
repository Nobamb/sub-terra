using NUnit.Framework;
using SubTerra.Gameplay.Mining;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SubTerra.Gameplay.Mining.Tests
{
    public sealed class MiningSystemPlayModeTests
    {
        private sealed class RewardReceiver : MonoBehaviour, IMiningRewardReceiver
        {
            public int Calls; public string MineralId; public int Quantity;
            public void AddMineral(string mineralId, int quantity) { Calls++; MineralId = mineralId; Quantity = quantity; }
        }

        [Test]
        public void CompletionRemovesTileAndPaysRewardOnlyOnce()
        {
            GameObject root = new("MiningTest");
            GameObject gridObject = new("Grid"); gridObject.transform.SetParent(root.transform); gridObject.AddComponent<Grid>();
            GameObject tilemapObject = new("Tilemap"); tilemapObject.transform.SetParent(gridObject.transform);
            Tilemap tilemap = tilemapObject.AddComponent<Tilemap>();
            MiningTileResolver resolver = root.AddComponent<MiningTileResolver>();
            RewardReceiver receiver = root.AddComponent<RewardReceiver>();
            MiningSystem system = root.AddComponent<MiningSystem>();

            SetPrivate(system, "foregroundTilemap", tilemap);
            SetPrivate(system, "tileResolver", resolver);
            SetPrivate(system, "rewardReceiverBehaviour", receiver);
            Tile tile = ScriptableObject.CreateInstance<Tile>();
            resolver.RegisterRuntime(tile, new MiningTileDto("tile.copper", "mineral.copper", 2, true, 1f, 0.2f, 0f, false));
            Vector3Int cell = new(1, 2, 0); tilemap.SetTile(cell, tile);

            Assert.IsTrue(system.TryStartMining(cell));
            system.TickMining(0.2f);
            system.TickMining(1f);

            Assert.IsNull(tilemap.GetTile(cell));
            Assert.AreEqual(1, receiver.Calls);
            Assert.AreEqual("mineral.copper", receiver.MineralId);
            Assert.AreEqual(2, receiver.Quantity);
            Object.DestroyImmediate(root); Object.DestroyImmediate(tile);
        }

        [Test]
        public void MiningDoesNotStartWithoutPower()
        {
            GameObject root = new("MiningTest");
            MiningSystem system = root.AddComponent<MiningSystem>();
            system.SetMiningPowerAvailable(false);
            Assert.IsFalse(system.TryStartMining(Vector3Int.zero));
            Object.DestroyImmediate(root);
        }

        [Test]
        public void InstantMiningRemovesTerrainAndSpawnsMineralFromTheCell()
        {
            GameObject root = new("MiningTest");
            GameObject gridObject = new("Grid"); gridObject.transform.SetParent(root.transform); gridObject.AddComponent<Grid>();
            GameObject tilemapObject = new("Tilemap"); tilemapObject.transform.SetParent(gridObject.transform);
            Tilemap tilemap = tilemapObject.AddComponent<Tilemap>();
            MiningTileResolver resolver = root.AddComponent<MiningTileResolver>();
            MiningSystem system = root.AddComponent<MiningSystem>();

            SetPrivate(system, "foregroundTilemap", tilemap);
            SetPrivate(system, "tileResolver", resolver);
            Tile tile = ScriptableObject.CreateInstance<Tile>();
            resolver.RegisterRuntime(tile, new MiningTileDto(
                "tile.iron",
                "mineral.iron",
                1,
                true,
                1f,
                0.2f,
                0f,
                false));
            Vector3Int cell = new(2, -3, 0);
            tilemap.SetTile(cell, tile);

            Assert.IsTrue(system.TryMineInstant(cell));

            Assert.IsNull(tilemap.GetTile(cell));
            Assert.AreEqual(1, system.SpawnedResourceDropCount);
            Transform drop = system.transform.Find("MinedResourceDrops/MinedResource_mineral_iron");
            Assert.IsNotNull(drop);
            Assert.AreEqual(
                tilemap.GetCellCenterWorld(cell) + Vector3.up * 0.2f,
                drop.position);
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(tile);
        }

        [Test]
        public void InstantMiningAlsoRemovesOrdinaryRockWithoutCreatingAResource()
        {
            GameObject root = new("MiningTest");
            GameObject gridObject = new("Grid"); gridObject.transform.SetParent(root.transform); gridObject.AddComponent<Grid>();
            GameObject tilemapObject = new("Tilemap"); tilemapObject.transform.SetParent(gridObject.transform);
            Tilemap tilemap = tilemapObject.AddComponent<Tilemap>();
            MiningTileResolver resolver = root.AddComponent<MiningTileResolver>();
            MiningSystem system = root.AddComponent<MiningSystem>();

            SetPrivate(system, "foregroundTilemap", tilemap);
            SetPrivate(system, "tileResolver", resolver);
            Tile tile = ScriptableObject.CreateInstance<Tile>();
            resolver.RegisterRuntime(tile, new MiningTileDto(
                "tile.rock.normal",
                string.Empty,
                0,
                true,
                1f,
                0.2f,
                0f,
                false));
            Vector3Int cell = new(0, -2, 0);
            tilemap.SetTile(cell, tile);

            Assert.IsTrue(system.TryMineInstant(cell));

            Assert.IsNull(tilemap.GetTile(cell));
            Assert.AreEqual(0, system.SpawnedResourceDropCount);
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(tile);
        }

        private static void SetPrivate(object target, string field, object value)
        {
            target.GetType().GetField(field, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(target, value);
        }
    }
}
