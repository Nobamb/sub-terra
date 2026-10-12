using NUnit.Framework;
using SubTerra.Gameplay.Building;
using SubTerra.Gameplay.Power;
using SubTerra.Gameplay.Structural;
using SubTerra.Shared;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SubTerra.Gameplay.Snapshot.Tests
{
    public sealed class WorldSnapshotSystemTests
    {
        [TestCase("light_basic")]
        [TestCase("settlement_basic")]
        public void LegacyUtility_RestoreAndRecaptureSaveCanonicalSizeAndKeepIdentity(string assetName)
        {
            var definition = AssetDatabase.LoadAssetAtPath<BuildingPlacementDefinition>(
                "Assets/_Project/Data/Buildings/Placement/" + assetName + "Placement.asset");
            var host = new GameObject("LegacyUtilitySave");
            try
            {
                var placement = host.AddComponent<BuildingPlacementSystem>();
                SetField(placement, "restoreDefinitions", new[] { definition });
                var system = host.AddComponent<WorldSnapshotSystem>();
                SetField(system, "buildingPlacementSystem", placement);
                var original = new BuildingSnapshotDto { instanceId = "saved-0099",
                    buildingTypeId = definition.BuildingId, x = 3, y = -2, rotation = 1,
                    level = 4, health = 0.65f, footprintWidth = 1, footprintHeight = 1 };
                var save = new WorldSnapshotDto { buildings = new System.Collections.Generic.List<BuildingSnapshotDto> { original } };
                Assert.That(system.RestoreSnapshot(save), Is.True);
                var captured = system.CaptureSnapshot();
                Assert.That(captured.buildings, Has.Count.EqualTo(1));
                var restored = captured.buildings[0];
                Assert.That(restored.footprintWidth, Is.EqualTo(1));
                Assert.That(restored.footprintHeight, Is.EqualTo(2));
                Assert.That(restored.instanceId, Is.EqualTo(original.instanceId));
                Assert.That(restored.x, Is.EqualTo(original.x));
                Assert.That(restored.y, Is.EqualTo(original.y));
                Assert.That(restored.level, Is.EqualTo(original.level));
                Assert.That(restored.health, Is.EqualTo(original.health));
                Assert.That(restored.rotation, Is.EqualTo(original.rotation));
                Assert.That(save.buildings[0].footprintHeight, Is.EqualTo(1));
                Assert.That(system.RestoreSnapshot(JsonUtility.FromJson<WorldSnapshotDto>(JsonUtility.ToJson(captured))), Is.True);
                Assert.That(host.GetComponentsInChildren<BuildingInstance>(), Has.Length.EqualTo(1));
                Assert.That(system.CaptureSnapshot().buildings[0].footprintHeight, Is.EqualTo(2));
            }
            finally { Object.DestroyImmediate(host); }
        }

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

        [Test]
        public void RestoreSnapshot_InstantiatesConfiguredCablePrefab()
        {
            var host = new GameObject("CableRestore");
            var prefab = new GameObject("PowerCablePrefab");
            prefab.AddComponent<SpriteRenderer>();
            var prefabCable = prefab.AddComponent<PowerCable>();

            try
            {
                var network = host.AddComponent<PowerNetworkSystem>();
                var source = new GameObject("Source").AddComponent<PowerNode>();
                var facility = new GameObject("Facility").AddComponent<PowerNode>();
                source.transform.SetParent(host.transform);
                facility.transform.SetParent(host.transform);
                source.Configure(network, true, 5, 0, PowerPriority.Critical);
                facility.Configure(network, false, 0, 1, PowerPriority.Normal);
                source.SetEntityId("source-0001");
                facility.SetEntityId("facility-0001");

                var snapshotSystem = host.AddComponent<WorldSnapshotSystem>();
                SetField(snapshotSystem, "powerNetworkSystem", network);
                SetField(snapshotSystem, "powerCablePrefab", prefabCable);

                Assert.That(
                    snapshotSystem.RestoreSnapshot(new WorldSnapshotDto
                    {
                        powerState = new PowerSnapshotDto
                        {
                            cableConnections = new System.Collections.Generic.List<PowerConnectionSnapshotDto>
                            {
                                new()
                                {
                                    nodeAInstanceId = "source-0001",
                                    nodeBInstanceId = "facility-0001"
                                }
                            }
                        }
                    }),
                    Is.True);

                PowerCable[] restored = host.GetComponentsInChildren<PowerCable>();
                Assert.That(restored, Has.Length.EqualTo(1));
                Assert.That(restored[0].GetComponent<SpriteRenderer>(), Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(prefab);
            }
        }

        [TestCase("charger_basic", 2, 2)]
        [TestCase("storage_basic", 1, 1)]
        [TestCase("outpost_core_basic", 2, 2)]
        public void PlacedFacility_JsonRoundTripPreservesIdentityFootprintAndOccupancy(
            string assetName, int width, int height)
        {
            var definition = AssetDatabase.LoadAssetAtPath<BuildingPlacementDefinition>(
                "Assets/_Project/Data/Buildings/Placement/" + assetName + "Placement.asset");
            Assert.That(definition, Is.Not.Null);
            Assert.That(definition.RuntimePrefab, Is.Not.Null);
            var footprint = new Vector2Int(width, height);
            Assert.That(definition.Footprint, Is.EqualTo(footprint));

            var host = new GameObject("FacilitySnapshotRoundTrip");
            host.SetActive(false);
            var gridObject = new GameObject("Grid", typeof(Grid));
            gridObject.transform.SetParent(host.transform);
            var terrainObject = new GameObject("Terrain", typeof(Tilemap), typeof(TilemapRenderer));
            terrainObject.transform.SetParent(gridObject.transform);
            var terrain = terrainObject.GetComponent<Tilemap>();
            var buildingRoot = new GameObject("Buildings").transform;
            buildingRoot.SetParent(host.transform);
            var area = host.AddComponent<BoxCollider2D>();
            area.isTrigger = true;
            area.size = Vector2.one * 30f;
            var ground = ScriptableObject.CreateInstance<Tile>();
            var wallet = new RecordingWallet();
            var placement = host.AddComponent<BuildingPlacementSystem>();
            SetField(placement, "terrainTilemap", terrain);
            SetField(placement, "buildingRoot", buildingRoot);
            SetField(placement, "placementOrigin", host.transform);
            SetField(placement, "allowedPlacementArea", area);
            SetField(placement, "restoreDefinitions", new[] { definition });
            var snapshots = host.AddComponent<WorldSnapshotSystem>();
            SetField(snapshots, "buildingPlacementSystem", placement);

            try
            {
                host.SetActive(true);
                // Direct EditMode calls do not rely on a running scene lifecycle.
                InvokeLifecycle(snapshots, "OnDisable");
                InvokeLifecycle(snapshots, "OnEnable");
                placement.SetResourceWallet(wallet);
                for (int x = 0; x < width; x++)
                    terrain.SetTile(new Vector3Int(x, -1, 0), ground);
                Physics2D.SyncTransforms();
                placement.Select(definition);
                var result = placement.TryPlaceAt(Vector3Int.zero);
                Assert.That(result.IsSuccess, Is.True, result.Failure.ToString());
                Assert.That(wallet.SpendCount, Is.EqualTo(1));
                var original = buildingRoot.GetComponentInChildren<BuildingInstance>();
                Assert.That(original, Is.Not.Null);
                Vector3 position = original.transform.position;

                var encoded = JsonUtility.ToJson(snapshots.CaptureSnapshot());
                var saved = JsonUtility.FromJson<WorldSnapshotDto>(encoded);
                Assert.That(saved.buildings, Has.Count.EqualTo(1));
                var record = saved.buildings[0];
                Assert.That(record.instanceId, Is.EqualTo(result.InstanceId));
                Assert.That(record.buildingTypeId, Is.EqualTo(definition.BuildingId));
                Assert.That(record.footprintWidth, Is.EqualTo(width));
                Assert.That(record.footprintHeight, Is.EqualTo(height));
                Assert.That(record.x, Is.EqualTo(0));
                Assert.That(record.y, Is.EqualTo(0));

                // Includes clearing the original instance; restoration must never spend again.
                Assert.That(snapshots.RestoreSnapshot(saved), Is.True);
                var restored = buildingRoot.GetComponentsInChildren<BuildingInstance>();
                Assert.That(restored, Has.Length.EqualTo(1));
                Assert.That(restored[0].InstanceId, Is.EqualTo(result.InstanceId));
                Assert.That(restored[0].BuildingId, Is.EqualTo(definition.BuildingId));
                Assert.That(restored[0].transform.position, Is.EqualTo(position));
                Assert.That(wallet.SpendCount, Is.EqualTo(1));
                placement.Select(definition);
                for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    Assert.That(placement.CanPlaceAt(new Vector3Int(x, y, 0), out var failure), Is.False);
                    Assert.That(failure, Is.EqualTo(BuildingPlacementFailure.Occupied));
                }
                var recaptured = snapshots.CaptureSnapshot().buildings;
                Assert.That(recaptured, Has.Count.EqualTo(1));
                Assert.That(recaptured[0].footprintWidth, Is.EqualTo(width));
                Assert.That(recaptured[0].footprintHeight, Is.EqualTo(height));
            }
            finally
            {
                InvokeLifecycle(snapshots, "OnDisable");
                placement.ClearSelection();
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(ground);
            }
        }

        private sealed class RecordingWallet : IResourceWallet
        {
            public int SpendCount { get; private set; }
            public bool CanAfford(System.Collections.Generic.IReadOnlyList<ItemCostDto> costs) => true;
            public bool TrySpend(System.Collections.Generic.IReadOnlyList<ItemCostDto> costs)
            {
                SpendCount++;
                return true;
            }
        }

        private static void InvokeLifecycle(object target, string name)
        {
            MethodInfo method = target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(target, null);
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
