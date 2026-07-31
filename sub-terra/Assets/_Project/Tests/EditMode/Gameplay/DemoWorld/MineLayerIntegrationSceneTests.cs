using NUnit.Framework;
using SubTerra.Gameplay.Mining;
using SubTerra.Gameplay.Snapshot;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace SubTerra.Gameplay.DemoWorld.Tests
{
    public sealed class MineLayerIntegrationSceneTests
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/App/Mine_Demo_Integration.unity";

        [Test]
        public void IntegrationScene_HasGeneratedFortyMeterLayerAndRestoreWiring()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedForTest = !scene.IsValid() || !scene.isLoaded;
            if (openedForTest)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            }

            try
            {
                MineLayerTilemapGenerator generator =
                    FindInScene<MineLayerTilemapGenerator>(scene);
                WorldSnapshotSystem snapshot = FindInScene<WorldSnapshotSystem>(scene);
                Assert.That(generator, Is.Not.Null);
                Assert.That(snapshot, Is.Not.Null);

                var generatorData = new SerializedObject(generator);
                var snapshotData = new SerializedObject(snapshot);
                var tilemap = generatorData.FindProperty("foregroundTilemap")
                    .objectReferenceValue as Tilemap;
                var distribution = generatorData.FindProperty("distribution")
                    .objectReferenceValue as MineLayerDistribution;
                var boundary = generatorData.FindProperty("boundaryRockTile")
                    .objectReferenceValue as TileBase;

                Assert.That(tilemap, Is.Not.Null);
                Assert.That(distribution, Is.Not.Null);
                Assert.That(boundary, Is.Not.Null);
                Assert.That(distribution.Depth, Is.EqualTo(40));
                Assert.That(distribution.Bands, Has.Count.EqualTo(3));
                Assert.That(
                    (distribution.Bands[0].MinDepth, distribution.Bands[0].MaxDepth),
                    Is.EqualTo((1, 15)));
                Assert.That(
                    (distribution.Bands[1].MinDepth, distribution.Bands[1].MaxDepth),
                    Is.EqualTo((16, 35)));
                Assert.That(
                    (distribution.Bands[2].MinDepth, distribution.Bands[2].MaxDepth),
                    Is.EqualTo((36, 40)));
                Assert.That(
                    snapshotData.FindProperty("baseWorldGeneratorBehaviour")
                        .objectReferenceValue,
                    Is.SameAs(generator));

                var resolver = generatorData.FindProperty("tileResolver")
                    .objectReferenceValue as MiningTileResolver;
                Assert.That(resolver, Is.Not.Null);
                AssertResolved(
                    resolver,
                    generatorData,
                    "rockTile",
                    MineLayerTileIds.Rock);
                AssertResolved(
                    resolver,
                    generatorData,
                    "copperTile",
                    MineLayerTileIds.Copper);
                AssertResolved(
                    resolver,
                    generatorData,
                    "ironTile",
                    MineLayerTileIds.Iron);
                AssertResolved(
                    resolver,
                    generatorData,
                    "lithiumTile",
                    MineLayerTileIds.Lithium);
                AssertResolved(
                    resolver,
                    generatorData,
                    "gasPocketTile",
                    MineLayerTileIds.GasPocket);
                AssertResolved(
                    resolver,
                    generatorData,
                    "lockedSignalTile",
                    MineLayerTileIds.LockedSignal);

                for (int depth = 1; depth <= distribution.Depth; depth++)
                {
                    int y = distribution.TopY - depth + 1;
                    Assert.That(
                        tilemap.GetTile(new Vector3Int(distribution.MinX, y, 0)),
                        Is.SameAs(boundary));
                    Assert.That(
                        tilemap.GetTile(new Vector3Int(distribution.MaxX, y, 0)),
                        Is.SameAs(boundary));
                }
            }
            finally
            {
                if (openedForTest)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T component = root.GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }

        private static void AssertResolved(
            MiningTileResolver resolver,
            SerializedObject generatorData,
            string tileProperty,
            string expectedId)
        {
            TileBase tile = generatorData.FindProperty(tileProperty)
                .objectReferenceValue as TileBase;
            Assert.That(tile, Is.Not.Null, tileProperty);
            Assert.That(
                resolver.TryResolve(tile, out SubTerra.Shared.MiningTileDto definition),
                Is.True,
                tileProperty);
            Assert.That(definition.tileId, Is.EqualTo(expectedId), tileProperty);
        }
    }
}
