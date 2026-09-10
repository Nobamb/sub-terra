using System.Collections;
using System.Reflection;
using NUnit.Framework;
using SubTerra.Gameplay.Mining;
using SubTerra.Gameplay.Snapshot;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

namespace SubTerra.Gameplay.DemoWorld.Tests
{
    public sealed class MineLayerTilemapPlayModeTests
    {
        [UnityTest]
        public IEnumerator RuntimeGenerator_RendersFortyMetersAndMiningRejectsBoundary()
        {
            GameObject host = new("MineLayerRuntime");
            host.SetActive(false);
            GameObject gridObject = new("Grid");
            gridObject.transform.SetParent(host.transform);
            gridObject.AddComponent<Grid>();
            GameObject mapObject = new("ForegroundTilemap");
            mapObject.transform.SetParent(gridObject.transform);
            Tilemap tilemap = mapObject.AddComponent<Tilemap>();
            mapObject.AddComponent<TilemapRenderer>();

            MiningTileResolver resolver = host.AddComponent<MiningTileResolver>();
            MiningSystem mining = host.AddComponent<MiningSystem>();
            WorldSnapshotSystem snapshot = host.AddComponent<WorldSnapshotSystem>();
            MineLayerTilemapGenerator renderer =
                host.AddComponent<MineLayerTilemapGenerator>();
            MineLayerDistribution distribution =
                ScriptableObject.CreateInstance<MineLayerDistribution>();

            Tile rock = CreateTile("Rock");
            Tile boundary = CreateTile("Boundary");
            Tile copper = CreateTile("Copper");
            Tile iron = CreateTile("Iron");
            Tile lithium = CreateTile("Lithium");
            Tile gas = CreateTile("Gas");
            Tile signal = CreateTile("Signal");

            renderer.EditorConfigure(
                tilemap,
                distribution,
                rock,
                boundary,
                boundary,
                copper,
                iron,
                lithium,
                gas,
                signal,
                resolver,
                snapshot,
                8001L);
            SetField(mining, "foregroundTilemap", tilemap);
            SetField(mining, "tileResolver", resolver);
            SetField(snapshot, "baseWorldGeneratorBehaviour", renderer);

            host.SetActive(true);
            yield return null;

            Assert.That(renderer.CurrentLayout, Is.Not.Null);
            Assert.That(renderer.CurrentLayout.Depth, Is.EqualTo(40));
            Vector3Int boundaryCell = new(
                distribution.MinX,
                distribution.TopY,
                0);
            Assert.That(tilemap.GetTile(boundaryCell), Is.SameAs(boundary));
            Assert.That(mining.TryMineInstant(boundaryCell), Is.False);
            Assert.That(tilemap.GetTile(boundaryCell), Is.SameAs(boundary));

            Vector3Int lockedDeepCell = new(
                0,
                distribution.TopY - distribution.Bands[2].MinDepth + 1,
                0);
            TileBase lockedDeepTile = tilemap.GetTile(lockedDeepCell);
            Assert.That(lockedDeepTile, Is.Not.Null);
            Assert.That(mining.TryMineInstant(lockedDeepCell), Is.False);
            Assert.That(mining.LastFailure, Is.EqualTo(MiningFailureReason.DeepZoneLocked));
            Assert.That(tilemap.GetTile(lockedDeepCell), Is.SameAs(lockedDeepTile));

            Object.Destroy(host);
            Object.Destroy(distribution);
            Object.Destroy(rock);
            Object.Destroy(boundary);
            Object.Destroy(copper);
            Object.Destroy(iron);
            Object.Destroy(lithium);
            Object.Destroy(gas);
            Object.Destroy(signal);
            yield return null;
        }

        private static Tile CreateTile(string name)
        {
            Tile tile = ScriptableObject.CreateInstance<Tile>();
            tile.name = name;
            tile.colliderType = Tile.ColliderType.Grid;
            return tile;
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field: {name}");
            field.SetValue(target, value);
        }
    }
}
