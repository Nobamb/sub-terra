using System.Reflection;
using NUnit.Framework;
using SubTerra.Gameplay.Structural;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SubTerra.Gameplay.Building.Tests
{
    public sealed class BuildingPlacementTests
    {
        [Test]
        public void TestWallet_DoesNotSpendWhenEmpty()
        {
            GameObject host = new("Wallet");
            BuildingTestResourceWallet wallet = host.AddComponent<BuildingTestResourceWallet>();

            Assert.That(wallet.CanAfford("building.support"), Is.True);
            Assert.That(wallet.TrySpend("building.support"), Is.True);
            Assert.That(wallet.TrySpend("building.support"), Is.True);
            Assert.That(wallet.TrySpend("building.support"), Is.True);
            Assert.That(wallet.TrySpend("building.support"), Is.False);

            Object.DestroyImmediate(host);
        }

        [Test]
        public void PlacementResult_PreservesFailureAndCell()
        {
            var cell = new Vector3Int(4, 2, 0);
            var result = new BuildingPlacementResult(false, BuildingPlacementFailure.Occupied, string.Empty, "building.support", cell);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Failure, Is.EqualTo(BuildingPlacementFailure.Occupied));
            Assert.That(result.Cell, Is.EqualTo(cell));
        }

        [Test]
        public void CanPlaceAt_ReportsAreaDistanceGroundAndOccupiedFailures()
        {
            var setup = CreateSetup(maximumDistance: 2f, areaSize: new Vector2(12f, 8f));
            try
            {
                setup.Terrain.SetTile(new Vector3Int(0, -1, 0), setup.Tile);
                setup.Terrain.SetTile(new Vector3Int(3, -1, 0), setup.Tile);

                Assert.That(
                    setup.Placement.CanPlaceAt(new Vector3Int(0, 0, 0), out var valid),
                    Is.True,
                    valid.ToString());
                Assert.That(valid, Is.EqualTo(BuildingPlacementFailure.None));

                setup.Terrain.SetTile(new Vector3Int(0, 0, 0), setup.Tile);
                Assert.That(setup.Placement.CanPlaceAt(new Vector3Int(0, 0, 0), out var occupied), Is.False);
                Assert.That(occupied, Is.EqualTo(BuildingPlacementFailure.Occupied));
                setup.Terrain.SetTile(new Vector3Int(0, 0, 0), null);

                setup.Terrain.SetTile(new Vector3Int(0, -1, 0), null);
                Assert.That(setup.Placement.CanPlaceAt(new Vector3Int(0, 0, 0), out var missingGround), Is.False);
                Assert.That(missingGround, Is.EqualTo(BuildingPlacementFailure.MissingGround));
                setup.Terrain.SetTile(new Vector3Int(0, -1, 0), setup.Tile);

                Assert.That(setup.Placement.CanPlaceAt(new Vector3Int(3, 0, 0), out var outOfRange), Is.False);
                Assert.That(outOfRange, Is.EqualTo(BuildingPlacementFailure.OutOfRange));

                setup.Area.size = new Vector2(2f, 2f);
                Assert.That(setup.Placement.CanPlaceAt(new Vector3Int(3, 0, 0), out var outsideArea), Is.False);
                Assert.That(outsideArea, Is.EqualTo(BuildingPlacementFailure.OutsideAllowedArea));
            }
            finally
            {
                setup.Dispose();
            }
        }

        [Test]
        public void TryPlaceAt_SuccessIsSingleShotAndSpendsExactlyOnce()
        {
            var setup = CreateSetup(maximumDistance: 5f, areaSize: new Vector2(12f, 8f));
            try
            {
                setup.Terrain.SetTile(new Vector3Int(0, -1, 0), setup.Tile);
                setup.Terrain.SetTile(new Vector3Int(1, -1, 0), setup.Tile);

                var first = setup.Placement.TryPlaceAt(Vector3Int.zero);
                var duplicate = setup.Placement.TryPlaceAt(Vector3Int.right);

                Assert.That(first.IsSuccess, Is.True, first.Failure.ToString());
                Assert.That(duplicate.IsSuccess, Is.False);
                Assert.That(duplicate.Failure, Is.EqualTo(BuildingPlacementFailure.NoSelection));
                Assert.That(setup.Wallet.SpendCount, Is.EqualTo(1));
                Assert.That(setup.BuildingRoot.childCount, Is.EqualTo(1));
                Assert.That(setup.Placement.Selection, Is.Null);
            }
            finally
            {
                setup.Dispose();
            }
        }

        [Test]
        public void PlacedSupport_ReducesRiskOnlyInsideItsRadius()
        {
            var setup = CreateSetup(maximumDistance: 10f, areaSize: new Vector2(20f, 8f));
            try
            {
                var structural = setup.Host.AddComponent<StructuralIntegritySystem>();
                SetField(structural, "foregroundTilemap", setup.Terrain);
                SetField(setup.Placement, "structuralIntegritySystem", structural);

                setup.Terrain.SetTile(new Vector3Int(0, -1, 0), setup.Tile);
                setup.Terrain.SetTile(new Vector3Int(0, 1, 0), setup.Tile);
                setup.Terrain.SetTile(new Vector3Int(6, -1, 0), setup.Tile);
                setup.Terrain.SetTile(new Vector3Int(6, 1, 0), setup.Tile);
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

                Assert.That(structural.EvaluateAt(Vector3Int.zero), Is.EqualTo(StructuralRiskLevel.Caution));
                Assert.That(structural.EvaluateAt(new Vector3Int(6, 0, 0)), Is.EqualTo(StructuralRiskLevel.Caution));

                var placed = setup.Placement.TryPlaceAt(Vector3Int.zero);

                Assert.That(placed.IsSuccess, Is.True, placed.Failure.ToString());
                Assert.That(structural.EvaluateAt(Vector3Int.zero), Is.EqualTo(StructuralRiskLevel.Stable));
                Assert.That(
                    structural.EvaluateAt(new Vector3Int(6, 0, 0)),
                    Is.EqualTo(StructuralRiskLevel.Caution));
            }
            finally
            {
                setup.Dispose();
            }
        }

        private static PlacementSetup CreateSetup(float maximumDistance, Vector2 areaSize)
        {
            var host = new GameObject("PlacementSetup");
            host.SetActive(false);
            var grid = new GameObject("Grid");
            grid.transform.SetParent(host.transform);
            grid.AddComponent<Grid>();
            var terrainObject = new GameObject("Terrain");
            terrainObject.transform.SetParent(grid.transform);
            var terrain = terrainObject.AddComponent<Tilemap>();
            terrainObject.AddComponent<TilemapRenderer>();
            var buildingRoot = new GameObject("Buildings").transform;
            buildingRoot.SetParent(host.transform);
            var origin = new GameObject("PlayerOrigin").transform;
            origin.SetParent(host.transform);
            var areaObject = new GameObject("AllowedArea");
            areaObject.transform.SetParent(host.transform);
            var area = areaObject.AddComponent<BoxCollider2D>();
            area.isTrigger = true;
            area.size = areaSize;

            var prefab = new GameObject("SupportPrefab");
            prefab.AddComponent<BuildingInstance>();
            prefab.AddComponent<StructuralSupport>();
            var definition = ScriptableObject.CreateInstance<BuildingPlacementDefinition>();
            definition.EditorSet("building.support.basic", prefab, Vector2Int.one, true);
            var wallet = new RecordingWallet();
            var placement = host.AddComponent<BuildingPlacementSystem>();
            SetField(placement, "terrainTilemap", terrain);
            SetField(placement, "buildingRoot", buildingRoot);
            SetField(placement, "placementOrigin", origin);
            SetField(placement, "maximumPlacementDistance", maximumDistance);
            SetField(placement, "allowedPlacementArea", area);
            host.SetActive(true);
            placement.SetResourceWallet(wallet);
            Physics2D.SyncTransforms();
            placement.Select(definition);

            return new PlacementSetup(
                host,
                terrain,
                ScriptableObject.CreateInstance<Tile>(),
                buildingRoot,
                area,
                prefab,
                definition,
                wallet,
                placement);
        }

        private static void SetField(object target, string name, object value)
        {
            var field = target.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "Missing field: " + name);
            field.SetValue(target, value);
        }

        private sealed class RecordingWallet : IResourceWallet
        {
            public int SpendCount { get; private set; }
            public bool CanAfford(System.Collections.Generic.IReadOnlyList<ItemCostDto> costs)
                => true;

            public bool TrySpend(System.Collections.Generic.IReadOnlyList<ItemCostDto> costs)
            {
                SpendCount++;
                return true;
            }
        }

        private sealed class PlacementSetup
        {
            public GameObject Host { get; }
            public Tilemap Terrain { get; }
            public Tile Tile { get; }
            public Transform BuildingRoot { get; }
            public BoxCollider2D Area { get; }
            public RecordingWallet Wallet { get; }
            public BuildingPlacementSystem Placement { get; }
            private readonly GameObject prefab;
            private readonly BuildingPlacementDefinition definition;

            public PlacementSetup(
                GameObject host,
                Tilemap terrain,
                Tile tile,
                Transform buildingRoot,
                BoxCollider2D area,
                GameObject runtimePrefab,
                BuildingPlacementDefinition placementDefinition,
                RecordingWallet wallet,
                BuildingPlacementSystem placement)
            {
                Host = host;
                Terrain = terrain;
                Tile = tile;
                BuildingRoot = buildingRoot;
                Area = area;
                prefab = runtimePrefab;
                definition = placementDefinition;
                Wallet = wallet;
                Placement = placement;
            }

            public void Dispose()
            {
                Object.DestroyImmediate(Host);
                Object.DestroyImmediate(prefab);
                Object.DestroyImmediate(definition);
                Object.DestroyImmediate(Tile);
            }
        }
    }
}
