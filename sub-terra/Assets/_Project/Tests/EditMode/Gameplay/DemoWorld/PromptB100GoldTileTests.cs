using System.Collections.Generic;
using NUnit.Framework;
using SubTerra.Shared;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using SubTerra.Gameplay.Mining;
using SubTerra.Gameplay.Snapshot;
using UnityEngine.Tilemaps;
using System.Linq;

namespace SubTerra.Gameplay.DemoWorld.Tests
{
    public sealed class PromptB100GoldTileTests
    {
        [Test]
        public void IntegrationRegenerationAndSnapshotKeepMinedGoldEmpty()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/App/Mine_Demo_Integration.unity", OpenSceneMode.Additive);
            try
            {
                var generator = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MineLayerTilemapGenerator>(true)).Single();
                var generatorData = new SerializedObject(generator);
                var map = generatorData.FindProperty("foregroundTilemap").objectReferenceValue as Tilemap;
                var resolver = generatorData.FindProperty("tileResolver").objectReferenceValue as MiningTileResolver;
                var snapshot = generatorData.FindProperty("snapshotSystem").objectReferenceValue as WorldSnapshotSystem;
                Assert.That(generator.Regenerate(generator.WorldSeed, generator.GeneratorVersion), Is.True);
                var goldCells = new List<Vector3Int>();
                foreach (var cell in map.cellBounds.allPositionsWithin)
                    if (resolver.TryResolve(map.GetTile(cell), out var d) && d.goldDrop > 0) goldCells.Add(cell);
                Assert.That(goldCells.Count, Is.GreaterThan(1));
                var mined = goldCells[0];
                var remaining = goldCells[1];
                Assert.That(resolver.TryResolve(map.GetTile(remaining), out var original), Is.True);
                var dto = new WorldSnapshotDto
                {
                    worldSeed = generator.WorldSeed,
                    generatorVersion = generator.GeneratorVersion,
                    miningChanges = new List<MiningSnapshotDto> { new MiningSnapshotDto { x = mined.x, y = mined.y, isDestroyed = true } }
                };
                Assert.That(snapshot.RestoreSnapshot(dto), Is.True, snapshot.LastRestoreFailureReason);
                Assert.That(map.GetTile(mined), Is.Null);
                Assert.That(resolver.TryResolve(map.GetTile(remaining), out var restored), Is.True);
                Assert.That(restored.tileId, Is.EqualTo(original.tileId));
                Assert.That(restored.goldDrop, Is.EqualTo(original.goldDrop));
                foreach (var cell in new[] { new Vector3Int(-8,-2,0), new Vector3Int(-7,-2,0), new Vector3Int(-6,-2,0), new Vector3Int(-7,-3,0) })
                {
                    Assert.That(resolver.TryResolve(map.GetTile(cell), out var definition), Is.True);
                    Assert.That(definition.goldDrop, Is.Zero);
                }
                foreach (var name in new[] { "Rock", "GasPocket", "Copper", "Iron", "Lithium" })
                {
                    var tile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/_Project/Tilemaps/DemoWorld/" + name + "Gold.asset");
                    Assert.That(tile.sprite, Is.Not.Null);
                    Assert.That(tile.sprite.pixelsPerUnit, Is.EqualTo(256));
                    Assert.That(tile.sprite.bounds.size.x, Is.EqualTo(1).Within(0.001f));
                }
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        [Test]
        public void OverlayIsDeterministicAndLeavesBaseLayoutHashUnchanged()
        {
            var settings = ScriptableObject.CreateInstance<GoldDropSettings>();
            var distribution = ScriptableObject.CreateInstance<MineLayerDistribution>();
            try
            {
                var layout = new MineLayerGenerator().Generate(20260731, distribution);
                var hash = layout.ComputeStableHash();
                var first = new List<string>();
                var second = new List<string>();
                foreach (var cell in layout.EnumerateCells())
                {
                    if (settings.IsGold(20260731, cell.X, cell.Y, cell.Kind)) first.Add(cell.X + "," + cell.Y);
                    if (settings.IsGold(20260731, cell.X, cell.Y, cell.Kind)) second.Add(cell.X + "," + cell.Y);
                }
                Assert.That(first, Is.Not.Empty);
                Assert.That(second, Is.EqualTo(first));
                Assert.That(layout.ComputeStableHash(), Is.EqualTo(hash));
                Assert.That(new MineLayerGenerator().Generate(20260731, distribution).ComputeStableHash(), Is.EqualTo(hash));
            }
            finally { Object.DestroyImmediate(settings); Object.DestroyImmediate(distribution); }
        }

        [Test]
        public void FullChanceExcludesUnmineableKindsAndProtectedDefinitions()
        {
            var settings = ScriptableObject.CreateInstance<GoldDropSettings>();
            try
            {
                var serialized = new SerializedObject(settings);
                foreach (string field in new[] { "rockChancePercent", "gasChancePercent", "oreChancePercent" })
                    serialized.FindProperty(field).intValue = 100;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                for (int x = -50; x < 50; x++)
                {
                    Assert.That(settings.IsGold(1, x, -1, MineLayerCellKind.Rock), Is.True);
                    Assert.That(settings.IsGold(1, x, -1, MineLayerCellKind.BoundaryRock), Is.False);
                    Assert.That(settings.IsGold(1, x, -1, MineLayerCellKind.LockedSignal), Is.False);
                }
                var protectedTile = new MiningTileDto("tile.elevator.protected", "", 0, false, 1, 0, 0, false);
                Assert.That(settings.CreateVariant(protectedTile).goldDrop, Is.Zero);
            }
            finally { Object.DestroyImmediate(settings); }
        }

        [TestCase("tile.copper", "mineral.copper")]
        [TestCase("tile.iron", "mineral.iron")]
        [TestCase("tile.lithium", "mineral.lithium")]
        public void OreGrantReadsCatalogAndPreservesMiningRules(string tileId, string mineralId)
        {
            var settings = AssetDatabase.LoadAssetAtPath<GoldDropSettings>("Assets/_Project/Data/World/GoldDropSettings.asset");
            Assert.That(settings, Is.Not.Null);
            var prices = AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/_Project/Data/Catalog/GameDataCatalog.asset") as IMineralPriceProvider;
            Assert.That(prices.TryGetMineralUnitPrice(mineralId, out int price), Is.True);
            var original = new MiningTileDto(tileId, mineralId, 1, true, 7, 3, 4, false, 2, 3);
            var gold = settings.CreateVariant(original);
            Assert.That(gold.goldDrop, Is.EqualTo(price * 5));
            Assert.That(gold.tileId, Is.EqualTo(tileId + ".gold"));
            gold.tileId = original.tileId;
            gold.goldDrop = 0;
            Assert.That(JsonUtility.ToJson(gold), Is.EqualTo(JsonUtility.ToJson(original)));
        }

        [Test]
        public void GasRetainsHazardAndBaseId()
        {
            var settings = ScriptableObject.CreateInstance<GoldDropSettings>();
            try
            {
                var gas = settings.CreateVariant(new MiningTileDto("tile.gas-pocket", "", 0, true, 1, 1, 1, true, 1, 2));
                Assert.That(gas.goldDrop, Is.EqualTo(50));
                Assert.That(gas.containsGas, Is.True);
                Assert.That(MineLayerTileIds.BaseTileId(gas.tileId), Is.EqualTo("tile.gas-pocket"));
            }
            finally { Object.DestroyImmediate(settings); }
        }
    }
}
