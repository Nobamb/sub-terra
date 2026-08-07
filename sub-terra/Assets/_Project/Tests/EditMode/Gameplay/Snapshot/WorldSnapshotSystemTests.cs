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
        public void RestoreSnapshot_RebuildsStructuralRiskFromMinedCells()
        {
            // prompt-B 36-1: 월드 복원 후 구조 위험이 Stable로 남지 않고 맵 기준으로 재계산된다.
            var host = new GameObject("StructuralRestore");
            var gridObject = new GameObject("Grid");
            gridObject.transform.SetParent(host.transform);
            gridObject.AddComponent<Grid>();
            var tilemapObject = new GameObject("Terrain");
            tilemapObject.transform.SetParent(gridObject.transform);
            var tilemap = tilemapObject.AddComponent<Tilemap>();
            tilemapObject.AddComponent<TilemapRenderer>();
            var tile = ScriptableObject.CreateInstance<Tile>();
            var overlayObject = new GameObject("CrackOverlay");
            overlayObject.transform.SetParent(gridObject.transform);
            var overlayMap = overlayObject.AddComponent<Tilemap>();
            overlayObject.AddComponent<TilemapRenderer>();

            try
            {
                // y=1 바닥, y=2 비지지 천장. 채굴 셀 (0,0) 위 천장이 위험 후보.
                tilemap.SetTile(new Vector3Int(0, 1, 0), tile);
                tilemap.SetTile(new Vector3Int(0, 2, 0), tile);

                var structural = host.AddComponent<StructuralIntegritySystem>();
                var overlay = overlayObject.AddComponent<StructuralCrackOverlay>();
                SetField(overlay, "overlayTilemap", overlayMap);
                SetField(structural, "foregroundTilemap", tilemap);
                SetField(structural, "crackOverlay", overlay);
                SetField(structural, "localRiskRadius", 1);
                SetField(structural, "scanRadius", 3);

                var snapshotSystem = host.AddComponent<WorldSnapshotSystem>();
                SetField(snapshotSystem, "foregroundTilemap", tilemap);
                SetField(snapshotSystem, "structuralSystem", structural);

                Assert.That(
                    snapshotSystem.RestoreSnapshot(new WorldSnapshotDto
                    {
                        miningChanges = new System.Collections.Generic.List<MiningSnapshotDto>
                        {
                            new()
                            {
                                x = 0,
                                y = 0,
                                isDestroyed = true,
                                remainingDurability = 0f
                            }
                        }
                    }),
                    Is.True);

                Assert.That(structural.CurrentRisk, Is.GreaterThan(StructuralRiskLevel.Stable));
                Assert.That(
                    structural.EvaluateAt(Vector3Int.zero),
                    Is.GreaterThan(StructuralRiskLevel.Stable));
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(tile);
            }
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
