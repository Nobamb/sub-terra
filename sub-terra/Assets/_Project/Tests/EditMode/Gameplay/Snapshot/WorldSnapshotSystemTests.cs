using NUnit.Framework;
using SubTerra.Gameplay.Building;
using SubTerra.Gameplay.Structural;
using SubTerra.Shared;
using System.Reflection;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SubTerra.Gameplay.Snapshot.Tests
{
    public sealed class WorldSnapshotSystemTests
    {
        [Test]
        public void CaptureSnapshot_ReturnsInitializedChangeCollections()
        {
            GameObject host = new("Snapshot");
            WorldSnapshotSystem system = host.AddComponent<WorldSnapshotSystem>();

            WorldSnapshotDto snapshot = system.CaptureSnapshot();

            Assert.That(snapshot, Is.Not.Null);
            Assert.That(snapshot.miningChanges, Is.Not.Null);
            Assert.That(snapshot.collapseChanges, Is.Not.Null);
            Assert.That(snapshot.buildings, Is.Not.Null);
            Assert.That(snapshot.gasChanges, Is.Not.Null);
            Object.DestroyImmediate(host);
        }

        [Test]
        public void RestoreSnapshot_AllowsNullSnapshot()
        {
            GameObject host = new("Snapshot");
            WorldSnapshotSystem system = host.AddComponent<WorldSnapshotSystem>();

            Assert.That(system.RestoreSnapshot(null), Is.True);
            Assert.That(system.LastRestoreSucceeded, Is.True);
            Object.DestroyImmediate(host);
        }

        [Test]
        public void CaptureAndRestore_PreserveBaseWorldGeneratorIdentity()
        {
            GameObject host = new("Snapshot");
            WorldSnapshotSystem system = host.AddComponent<WorldSnapshotSystem>();
            BaseWorldGeneratorSpy generator = host.AddComponent<BaseWorldGeneratorSpy>();
            SetField(system, "baseWorldGeneratorBehaviour", generator);
            system.ConfigureBaseWorldIdentity(7123L, 4);

            WorldSnapshotDto captured = system.CaptureSnapshot();
            Assert.That(captured.worldSeed, Is.EqualTo(7123L));
            Assert.That(captured.generatorVersion, Is.EqualTo(4));

            Assert.That(system.RestoreSnapshot(captured), Is.True);
            Assert.That(generator.CallCount, Is.EqualTo(1));
            Assert.That(generator.LastSeed, Is.EqualTo(7123L));
            Assert.That(generator.LastVersion, Is.EqualTo(4));
            Object.DestroyImmediate(host);
        }

        [Test]
        public void RestoreSnapshot_RecreatesSupportEffectWithoutWalletSpend()
        {
            var host = new GameObject("SupportRestore");
            var gridObject = new GameObject("Grid");
            gridObject.transform.SetParent(host.transform);
            gridObject.AddComponent<Grid>();
            var tilemapObject = new GameObject("Terrain");
            tilemapObject.transform.SetParent(gridObject.transform);
            var tilemap = tilemapObject.AddComponent<Tilemap>();
            tilemapObject.AddComponent<TilemapRenderer>();
            var tile = ScriptableObject.CreateInstance<Tile>();
            var prefab = new GameObject("SupportPrefab");
            prefab.AddComponent<BuildingInstance>();
            prefab.AddComponent<StructuralSupport>();
            var definition = ScriptableObject.CreateInstance<BuildingPlacementDefinition>();
            definition.EditorSet("building.support.basic", prefab, Vector2Int.one, false);

            try
            {
                var structural = host.AddComponent<StructuralIntegritySystem>();
                SetField(structural, "foregroundTilemap", tilemap);
                var placement = host.AddComponent<BuildingPlacementSystem>();
                SetField(placement, "terrainTilemap", tilemap);
                SetField(placement, "structuralIntegritySystem", structural);
                SetField(placement, "restoreDefinitions", new[] { definition });
                var snapshotSystem = host.AddComponent<WorldSnapshotSystem>();
                SetField(snapshotSystem, "buildingPlacementSystem", placement);

                Assert.That(
                    snapshotSystem.RestoreSnapshot(new WorldSnapshotDto
                    {
                        buildings = new System.Collections.Generic.List<BuildingSnapshotDto>
                        {
                            new()
                            {
                                instanceId = "support-restore-0001",
                                buildingTypeId = "building.support.basic",
                                x = 0,
                                y = 0,
                                level = 1,
                                health = 1f
                            }
                        }
                    }),
                    Is.True);

                tilemap.SetTile(new Vector3Int(0, 1, 0), tile);
                tilemap.SetTile(new Vector3Int(6, 1, 0), tile);
                var impact = new MiningTileDto(
                    "tile.test",
                    string.Empty,
                    0,
                    true,
                    1f,
                    1f,
                    0.1f,
                    false);
                structural.NotifyTileMined(Vector3Int.zero, impact);
                structural.NotifyTileMined(new Vector3Int(6, 0, 0), impact);

                Assert.That(host.GetComponentsInChildren<BuildingInstance>().Length, Is.EqualTo(1));
                Assert.That(structural.EvaluateAt(Vector3Int.zero), Is.EqualTo(StructuralRiskLevel.Stable));
                Assert.That(
                    structural.EvaluateAt(new Vector3Int(6, 0, 0)),
                    Is.EqualTo(StructuralRiskLevel.Caution));
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(prefab);
                Object.DestroyImmediate(definition);
                Object.DestroyImmediate(tile);
            }
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }

        private sealed class BaseWorldGeneratorSpy : MonoBehaviour, IWorldBaseGenerator
        {
            public int CallCount { get; private set; }
            public long LastSeed { get; private set; }
            public int LastVersion { get; private set; }

            public bool Regenerate(long worldSeed, int generatorVersion)
            {
                CallCount++;
                LastSeed = worldSeed;
                LastVersion = generatorVersion;
                return true;
            }
        }
    }
}
