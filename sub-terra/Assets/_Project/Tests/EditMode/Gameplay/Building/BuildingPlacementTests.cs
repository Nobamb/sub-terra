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
        public void PromptB64_CannotPlaceDirectlyAboveAnotherBuilding()
        {
            var setup = CreateSetup(
                maximumDistance: 5f,
                areaSize: new Vector2(12f, 8f),
                needsGround: false);
            try
            {
                Assert.That(setup.Placement.TryPlaceAt(Vector3Int.zero).IsSuccess, Is.True);
                setup.Placement.Select(setup.Definition);

                var cellAboveBuilding = Vector3Int.up;
                Assert.That(
                    setup.Placement.CanPlaceAt(cellAboveBuilding, out var failure),
                    Is.False);
                Assert.That(failure, Is.EqualTo(BuildingPlacementFailure.Occupied));

                var rejected = setup.Placement.TryPlaceAt(cellAboveBuilding);
                Assert.That(rejected.IsSuccess, Is.False);
                Assert.That(setup.Wallet.SpendCount, Is.EqualTo(1));
                Assert.That(setup.BuildingRoot.childCount, Is.EqualTo(1));
            }
            finally
            {
                setup.Dispose();
            }
        }

        [Test]
        public void PromptB78_CanPlaceLadderDirectlyAboveLadder()
        {
            var setup = CreateSetup(
                maximumDistance: 10f,
                areaSize: new Vector2(12f, 16f),
                footprint: new Vector2Int(1, 5),
                needsGround: false,
                buildingId: "building.ladder.basic");
            try
            {
                Assert.That(setup.Placement.TryPlaceAt(Vector3Int.zero).IsSuccess, Is.True);
                setup.Placement.Select(setup.Definition);

                var nextLadderOrigin = new Vector3Int(0, 5, 0);
                Assert.That(
                    setup.Placement.CanPlaceAt(nextLadderOrigin, out var failure),
                    Is.True,
                    failure.ToString());

                var placed = setup.Placement.TryPlaceAt(nextLadderOrigin);
                Assert.That(placed.IsSuccess, Is.True, placed.Failure.ToString());
                Assert.That(setup.Wallet.SpendCount, Is.EqualTo(2));
                Assert.That(setup.BuildingRoot.childCount, Is.EqualTo(2));
            }
            finally
            {
                setup.Dispose();
            }
        }

        [Test]
        public void PromptB78_LadderCannotStackOnNonLadderBuilding()
        {
            var setup = CreateSetup(
                maximumDistance: 5f,
                areaSize: new Vector2(12f, 8f),
                needsGround: false);
            try
            {
                Assert.That(setup.Placement.TryPlaceAt(Vector3Int.zero).IsSuccess, Is.True);
                setup.Definition.EditorSet(
                    "building.ladder.basic",
                    setup.RuntimePrefab,
                    Vector2Int.one,
                    false);
                setup.Placement.Select(setup.Definition);

                Assert.That(
                    setup.Placement.CanPlaceAt(Vector3Int.up, out var failure),
                    Is.False);
                Assert.That(failure, Is.EqualTo(BuildingPlacementFailure.Occupied));
            }
            finally
            {
                setup.Dispose();
            }
        }

        [Test]
        public void PromptB64_CannotPlaceOnElevatorProtectedGround()
        {
            var setup = CreateSetup(
                maximumDistance: 5f,
                areaSize: new Vector2(12f, 8f),
                needsGround: false);
            try
            {
                setup.Tile.name = "ElevatorProtectedBlock";
                setup.Terrain.SetTile(Vector3Int.down, setup.Tile);

                Assert.That(
                    setup.Placement.CanPlaceAt(Vector3Int.zero, out var failure),
                    Is.False);
                Assert.That(failure, Is.EqualTo(BuildingPlacementFailure.Occupied));

                var rejected = setup.Placement.TryPlaceAt(Vector3Int.zero);
                Assert.That(rejected.IsSuccess, Is.False);
                Assert.That(setup.Wallet.SpendCount, Is.Zero);
                Assert.That(setup.BuildingRoot.childCount, Is.Zero);
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
        public void RankPlacementPreference_UnderFeetThenFacingThenDownSide()
        {
            var player = Vector3Int.zero;
            Assert.That(
                BuildingPlacementSystem.RankPlacementPreference(Vector3Int.zero, player, 1),
                Is.EqualTo(0));
            Assert.That(
                BuildingPlacementSystem.RankPlacementPreference(new Vector3Int(0, -1, 0), player, 1),
                Is.EqualTo(1));
            Assert.That(
                BuildingPlacementSystem.RankPlacementPreference(new Vector3Int(1, 0, 0), player, 1),
                Is.EqualTo(2));
            Assert.That(
                BuildingPlacementSystem.RankPlacementPreference(new Vector3Int(1, -1, 0), player, 1),
                Is.EqualTo(3));
            Assert.That(
                BuildingPlacementSystem.RankPlacementPreference(new Vector3Int(-1, -1, 0), player, 1),
                Is.EqualTo(4));
            Assert.That(
                BuildingPlacementSystem.RankPlacementPreference(new Vector3Int(-1, 0, 0), player, 1),
                Is.EqualTo(5));
        }

        [Test]
        public void ResolveFootprintOrigin_ExpandsRightOrLeftFromCursor()
        {
            var cursor = new Vector3Int(5, 2, 0);
            var footprint = new Vector2Int(2, 2);

            // 캐릭터보다 오른쪽 → 커서가 왼쪽 열, 오른쪽으로 확장.
            Assert.That(
                BuildingPlacementSystem.ResolveFootprintOrigin(cursor, footprint, playerWorldX: 0f, cursorWorldX: 5f),
                Is.EqualTo(new Vector3Int(5, 2, 0)));

            // 캐릭터보다 왼쪽 → 커서가 오른쪽 열, 왼쪽으로 확장(origin.x = cursor.x - 1).
            Assert.That(
                BuildingPlacementSystem.ResolveFootprintOrigin(cursor, footprint, playerWorldX: 10f, cursorWorldX: 5f),
                Is.EqualTo(new Vector3Int(4, 2, 0)));
        }

        [Test]
        public void CanPlaceAt_TwoByTwo_RequiresGroundOnlyOnBottomRow()
        {
            // 긴급 탈출 포탈 2x2: 하단 2칸 지면 + 4칸 공중이면 설치 가능해야 한다.
            var setup = CreateSetup(
                maximumDistance: 10f,
                areaSize: new Vector2(20f, 12f),
                footprint: new Vector2Int(2, 2));
            try
            {
                setup.Terrain.SetTile(new Vector3Int(0, -1, 0), setup.Tile);
                setup.Terrain.SetTile(new Vector3Int(1, -1, 0), setup.Tile);

                Assert.That(
                    setup.Placement.CanPlaceAt(new Vector3Int(0, 0, 0), out var valid),
                    Is.True,
                    valid.ToString());
                Assert.That(valid, Is.EqualTo(BuildingPlacementFailure.None));

                // 하단 한 칸만 지면이면 MissingGround.
                setup.Terrain.SetTile(new Vector3Int(1, -1, 0), null);
                Assert.That(
                    setup.Placement.CanPlaceAt(new Vector3Int(0, 0, 0), out var missing),
                    Is.False);
                Assert.That(missing, Is.EqualTo(BuildingPlacementFailure.MissingGround));
            }
            finally
            {
                setup.Dispose();
            }
        }

        [Test]
        public void TryPlaceAt_TwoByTwo_OccupiesFourCellsAndSpendsOnce()
        {
            var setup = CreateSetup(
                maximumDistance: 10f,
                areaSize: new Vector2(20f, 12f),
                footprint: new Vector2Int(2, 2));
            try
            {
                setup.Terrain.SetTile(new Vector3Int(0, -1, 0), setup.Tile);
                setup.Terrain.SetTile(new Vector3Int(1, -1, 0), setup.Tile);

                var placed = setup.Placement.TryPlaceAt(new Vector3Int(0, 0, 0));
                Assert.That(placed.IsSuccess, Is.True, placed.Failure.ToString());
                Assert.That(setup.Wallet.SpendCount, Is.EqualTo(1));
                Assert.That(setup.BuildingRoot.childCount, Is.EqualTo(1));

                // 동일 footprint 겹치면 Occupied. 선택은 성공 후 해제되므로 다시 Select.
                setup.Placement.Select(setup.Definition);
                Assert.That(
                    setup.Placement.CanPlaceAt(new Vector3Int(0, 0, 0), out var occupied),
                    Is.False);
                Assert.That(occupied, Is.EqualTo(BuildingPlacementFailure.Occupied));
                Assert.That(
                    setup.Placement.CanPlaceAt(new Vector3Int(1, 0, 0), out var occupiedNeighbor),
                    Is.False);
                Assert.That(occupiedNeighbor, Is.EqualTo(BuildingPlacementFailure.Occupied));
            }
            finally
            {
                setup.Dispose();
            }
        }

        [Test]
        public void PlacedBuilding_ProtectsOnlyItsSupportingGroundCells()
        {
            var setup = CreateSetup(
                maximumDistance: 10f,
                areaSize: new Vector2(20f, 12f),
                footprint: new Vector2Int(2, 2));
            try
            {
                var leftGround = new Vector3Int(0, -1, 0);
                var rightGround = new Vector3Int(1, -1, 0);
                setup.Terrain.SetTile(leftGround, setup.Tile);
                setup.Terrain.SetTile(rightGround, setup.Tile);

                Assert.That(setup.Placement.TryPlaceAt(Vector3Int.zero).IsSuccess, Is.True);
                Assert.That(setup.Placement.IsGroundSupportingBuilding(leftGround), Is.True);
                Assert.That(setup.Placement.IsGroundSupportingBuilding(rightGround), Is.True);
                Assert.That(setup.Placement.IsGroundSupportingBuilding(Vector3Int.zero), Is.False);

                setup.Placement.PrepareForWorldRestore();
                Assert.That(setup.Placement.IsGroundSupportingBuilding(leftGround), Is.False);
                Assert.That(setup.Placement.IsGroundSupportingBuilding(rightGround), Is.False);
            }
            finally
            {
                setup.Dispose();
            }
        }

        [Test]
        public void PlacedLadder_PreventsExistingSupportingGroundCollapse()
        {
            var setup = CreateSetup(
                maximumDistance: 10f,
                areaSize: new Vector2(20f, 12f),
                footprint: new Vector2Int(1, 5),
                needsGround: false,
                buildingId: "building.ladder.basic");
            try
            {
                var structural = setup.Host.AddComponent<StructuralIntegritySystem>();
                SetField(structural, "foregroundTilemap", setup.Terrain);
                SetField(setup.Placement, "structuralIntegritySystem", structural);
                Object.DestroyImmediate(setup.Definition.RuntimePrefab.GetComponent<StructuralSupport>());

                var supportingGround = new Vector3Int(0, -1, 0);
                setup.Terrain.SetTile(supportingGround, setup.Tile);
                structural.NotifyTileMined(
                    new Vector3Int(0, -3, 0),
                    new MiningTileDto(
                        "tile.test",
                        string.Empty,
                        0,
                        true,
                        1f,
                        1f,
                        1f,
                        false));

                Assert.That(setup.Placement.TryPlaceAt(Vector3Int.zero).IsSuccess, Is.True);
                Assert.That(setup.Definition.BuildingId, Is.EqualTo("building.ladder.basic"));
                Assert.That(setup.Definition.RequiresGround, Is.False);
                Assert.That(setup.Placement.IsGroundSupportingBuilding(supportingGround), Is.True);
                structural.AdvanceSimulation(1f);

                Assert.That(setup.Terrain.HasTile(supportingGround), Is.True);
            }
            finally
            {
                setup.Dispose();
            }
        }

        [Test]
        public void TryFindBestPlacementCell_PrefersNearestWithinRange()
        {
            BuildingPlacementActivity.ResetForTests();
            var setup = CreateSetup(maximumDistance: 6f, areaSize: new Vector2(20f, 12f));
            try
            {
                // 플레이어 원점 기준: 전방 2칸만 지면·빈 칸 확보, 발밑(0,0)은 지면 없음.
                setup.Terrain.SetTile(new Vector3Int(2, -1, 0), setup.Tile);
                setup.Terrain.SetTile(new Vector3Int(3, -1, 0), setup.Tile);

                Assert.That(
                    setup.Placement.TryFindBestPlacementCell(1f, out var best, out var failure),
                    Is.True,
                    failure.ToString());
                Assert.That(failure, Is.EqualTo(BuildingPlacementFailure.None));
                Assert.That(best, Is.EqualTo(new Vector3Int(2, 0, 0)));
            }
            finally
            {
                setup.Dispose();
                BuildingPlacementActivity.ResetForTests();
            }
        }

        [Test]
        public void TryPlaceNearest_SpendsOnceAndClearsSelection()
        {
            BuildingPlacementActivity.ResetForTests();
            var setup = CreateSetup(maximumDistance: 6f, areaSize: new Vector2(20f, 12f));
            try
            {
                setup.Terrain.SetTile(new Vector3Int(0, -1, 0), setup.Tile);
                setup.Terrain.SetTile(new Vector3Int(1, -1, 0), setup.Tile);

                var result = setup.Placement.TryPlaceNearest(1f);
                Assert.That(result.IsSuccess, Is.True, result.Failure.ToString());
                Assert.That(setup.Wallet.SpendCount, Is.EqualTo(1));
                Assert.That(setup.Placement.Selection, Is.Null);
                Assert.That(BuildingPlacementActivity.IsActive, Is.False);

                // 두 번째 Enter는 선택 없음.
                var second = setup.Placement.TryPlaceNearest(1f);
                Assert.That(second.IsSuccess, Is.False);
                Assert.That(second.Failure, Is.EqualTo(BuildingPlacementFailure.NoSelection));
                Assert.That(setup.Wallet.SpendCount, Is.EqualTo(1));
            }
            finally
            {
                setup.Dispose();
                BuildingPlacementActivity.ResetForTests();
            }
        }

        [Test]
        public void TryFindBestPlacementCell_NoCandidate_ReportsReasonWithoutPlacing()
        {
            BuildingPlacementActivity.ResetForTests();
            var setup = CreateSetup(maximumDistance: 6f, areaSize: new Vector2(20f, 12f));
            try
            {
                // 지면 타일 없음 → MissingGround 계열 실패.
                Assert.That(
                    setup.Placement.TryFindBestPlacementCell(1f, out _, out var failure),
                    Is.False);
                Assert.That(failure, Is.EqualTo(BuildingPlacementFailure.MissingGround));
                Assert.That(setup.Wallet.SpendCount, Is.EqualTo(0));
                Assert.That(setup.Placement.Selection, Is.Not.Null);
            }
            finally
            {
                setup.Dispose();
                BuildingPlacementActivity.ResetForTests();
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

        private static PlacementSetup CreateSetup(
            float maximumDistance,
            Vector2 areaSize,
            Vector2Int? footprint = null,
            bool needsGround = true,
            string buildingId = null)
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
            var size = footprint ?? Vector2Int.one;
            var resolvedBuildingId = !string.IsNullOrWhiteSpace(buildingId)
                ? buildingId
                : size.x > 1 || size.y > 1
                    ? "building.escape_portal.emergency"
                    : "building.support.basic";
            definition.EditorSet(resolvedBuildingId, prefab, size, needsGround);
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
            public BuildingPlacementDefinition Definition { get; }
            public GameObject RuntimePrefab => prefab;
            private readonly GameObject prefab;

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
                Definition = placementDefinition;
                Wallet = wallet;
                Placement = placement;
            }

            public void Dispose()
            {
                Placement?.ClearSelection();
                BuildingPlacementActivity.ResetForTests();
                Object.DestroyImmediate(Host);
                Object.DestroyImmediate(prefab);
                Object.DestroyImmediate(Definition);
                Object.DestroyImmediate(Tile);
            }
        }
    }
}
