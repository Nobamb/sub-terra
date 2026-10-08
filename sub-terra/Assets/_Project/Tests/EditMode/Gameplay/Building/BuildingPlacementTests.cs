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
        public void Charger_SuccessRecordsFourCellsAndSpendsOnce()
        {
            var setup = CreateSetup(10f, new Vector2(20f, 20f), buildingId: "building.charger.basic");
            try
            {
                setup.Terrain.SetTile(new Vector3Int(0, -1, 0), setup.Tile);
                setup.Terrain.SetTile(new Vector3Int(1, -1, 0), setup.Tile);
                var result = setup.Placement.TryPlaceAt(Vector3Int.zero);
                Assert.That(result.IsSuccess, Is.True);
                Assert.That(result.Footprint, Is.EqualTo(new Vector2Int(2, 2)));
                Assert.That(setup.Wallet.SpendCount, Is.EqualTo(1));
                setup.Placement.Select(setup.Definition);
                Assert.That(setup.Placement.CanPlaceAt(new Vector3Int(1, 1, 0), out var failure), Is.False);
                Assert.That(failure, Is.EqualTo(BuildingPlacementFailure.Occupied));
            }
            finally { setup.Dispose(); }
        }

        [Test]
        public void Elevator_ShaftSpaceRejectsWithoutSpending()
        {
            var setup = CreateSetup(10f, new Vector2(20f, 20f), needsGround: false);
            var elevatorRoot = new GameObject("Elevator");
            try
            {
                elevatorRoot.SetActive(false);
                elevatorRoot.transform.position = new Vector3(0.5f, 0.5f, 0f);
                elevatorRoot.AddComponent<BoxCollider2D>().size = Vector2.one;
                var elevatorType = System.Type.GetType("SubTerra.Gameplay.Player.ElevatorController, SubTerra.Gameplay.Player", true);
                var elevator = elevatorRoot.AddComponent(elevatorType);
                elevatorRoot.SetActive(true);
                Physics2D.SyncTransforms();
                var elevators = System.Array.CreateInstance(elevatorType, 1);
                elevators.SetValue(elevator, 0);
                SetField(setup.Placement, "placementElevators", elevators);
                Assert.That(setup.Placement.TryPlaceAt(Vector3Int.zero).Failure,
                    Is.EqualTo(BuildingPlacementFailure.ElevatorSpace));
                Assert.That(setup.Wallet.SpendCount, Is.Zero);
            }
            finally { Object.DestroyImmediate(elevatorRoot); setup.Dispose(); }
        }

        [TestCase(0, 0)]
        [TestCase(1, 0)]
        [TestCase(0, 1)]
        [TestCase(1, 1)]
        public void Charger_BlockedFootprintCellRejectsWithoutSpending(int x, int y)
        {
            var setup = CreateSetup(10f, new Vector2(20f, 20f),
                buildingId: "building.charger.basic");
            try
            {
                Assert.That(setup.Definition.Footprint, Is.EqualTo(new Vector2Int(2, 2)));
                setup.Terrain.SetTile(new Vector3Int(0, -1, 0), setup.Tile);
                setup.Terrain.SetTile(new Vector3Int(1, -1, 0), setup.Tile);
                setup.Terrain.SetTile(new Vector3Int(x, y, 0), setup.Tile);
                var result = setup.Placement.TryPlaceAt(Vector3Int.zero);
                Assert.That(result.Failure, Is.EqualTo(BuildingPlacementFailure.Occupied));
                Assert.That(setup.Wallet.SpendCount, Is.Zero);
                Assert.That(setup.BuildingRoot.childCount, Is.Zero);
            }
            finally { setup.Dispose(); }
        }

        [TestCase(0, 0, 1)]
        [TestCase(2, 2, 2)]
        public void Charger_RestoreKeepsLegacyOrRecordedFootprint(int width, int height, int expectedSize)
        {
            var setup = CreateSetup(10f, new Vector2(20f, 20f),
                buildingId: "building.charger.basic");
            try
            {
                // Only existing ground is protected; legacy width must not protect the next column.
                setup.Terrain.SetTile(new Vector3Int(0, -1, 0), setup.Tile);
                setup.Terrain.SetTile(new Vector3Int(1, -1, 0), setup.Tile);
                Assert.That(setup.Placement.TryRestoreBuilding(new BuildingSnapshotDto
                {
                    instanceId = "charger-saved", buildingTypeId = "building.charger.basic",
                    footprintWidth = width, footprintHeight = height
                }), Is.True);
                Assert.That(setup.Wallet.SpendCount, Is.Zero);
                Assert.That(setup.BuildingRoot.GetChild(0).position,
                    Is.EqualTo(new Vector3(expectedSize * 0.5f, expectedSize * 0.5f, 0f)));
                Assert.That(setup.Placement.IsGroundSupportingBuilding(new Vector3Int(0, -1, 0)), Is.True);
                Assert.That(setup.Placement.IsGroundSupportingBuilding(new Vector3Int(1, -1, 0)),
                    Is.EqualTo(expectedSize == 2));
                Assert.That(setup.Placement.TryRestoreBuilding(new BuildingSnapshotDto
                { instanceId = "charger-saved", buildingTypeId = "building.charger.basic" }), Is.False);
            }
            finally { setup.Dispose(); }
        }

        [TestCase(0, 0)]
        [TestCase(1, 0)]
        [TestCase(0, 1)]
        [TestCase(1, 1)]
        public void Outpost_BlockedFootprintCellRejectsWithoutSpending(int x, int y)
        {
            var setup = CreateSetup(10f, new Vector2(20f, 20f), new Vector2Int(2, 2),
                buildingId: "building.outpost_core.basic");
            try
            {
                setup.Terrain.SetTile(new Vector3Int(0, -1, 0), setup.Tile);
                setup.Terrain.SetTile(new Vector3Int(1, -1, 0), setup.Tile);
                setup.Terrain.SetTile(new Vector3Int(x, y, 0), setup.Tile);
                Assert.That(setup.Placement.TryPlaceAt(Vector3Int.zero).Failure, Is.EqualTo(BuildingPlacementFailure.Occupied));
                Assert.That(setup.Wallet.SpendCount, Is.Zero);
                Assert.That(setup.BuildingRoot.childCount, Is.Zero);
            }
            finally { setup.Dispose(); }
        }

        [TestCase(0, 0, 2)]
        [TestCase(1, 2, 1)]
        [TestCase(2, 2, 2)]
        public void Outpost_RestoreKeepsLegacyNarrowOrOriginalFootprint(int width, int height, int expectedWidth)
        {
            var setup = CreateSetup(10f, new Vector2(20f, 20f), new Vector2Int(2, 2),
                buildingId: "building.outpost_core.basic");
            try
            {
                setup.Terrain.SetTile(new Vector3Int(0, -1, 0), setup.Tile);
                setup.Terrain.SetTile(new Vector3Int(1, -1, 0), setup.Tile);
                Assert.That(setup.Placement.TryRestoreBuilding(new BuildingSnapshotDto
                {
                    instanceId = "outpost-saved", buildingTypeId = "building.outpost_core.basic",
                    footprintWidth = width, footprintHeight = height
                }), Is.True);
                Assert.That(setup.Wallet.SpendCount, Is.Zero);
                Assert.That(setup.BuildingRoot.GetChild(0).position, Is.EqualTo(new Vector3(expectedWidth * 0.5f, 1f, 0f)));
                Assert.That(setup.Placement.IsGroundSupportingBuilding(new Vector3Int(1, -1, 0)), Is.EqualTo(expectedWidth == 2));
                Assert.That(setup.Placement.CanPlaceAt(new Vector3Int(0, 1, 0), out var failure), Is.False);
                Assert.That(failure, Is.EqualTo(BuildingPlacementFailure.Occupied));
            }
            finally { setup.Dispose(); }
        }

        [TestCase("building.light.basic", 1, 2, -1.040f)]
        [TestCase("building.settlement.basic", 1, 2, -1.040f)]
        [TestCase("building.light.basic", 1, 1, -0.540f)]
        [TestCase("building.settlement.basic", 1, 1, -0.540f)]
        [TestCase("building.charger.basic", 2, 2, -1.040f)]
        [TestCase("building.storage.basic", 1, 1, -0.540f)]
        [TestCase("building.outpost_core.basic", 1, 2, -1.08f)]
        [TestCase("building.outpost_core.basic", 2, 2, -1.08f)]
        public void Facility_GroundContactIgnoresSpritePivot(string id, int width, int height, float bottom)
        {
            var texture = new Texture2D(32, 32);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.2f, 0.8f),
                32f, 0, SpriteMeshType.FullRect);
            try
            {
                Assert.That(FacilityGroundedVisual.TryGetGeometry(sprite, id, new Vector2Int(width, height),
                    out var position, out var scale), Is.True);
                Assert.That(position.y + sprite.bounds.min.y * scale.y, Is.EqualTo(bottom).Within(0.001f));
                Assert.That(position.x + sprite.bounds.center.x * scale.x, Is.EqualTo(0f).Within(0.001f));
                Assert.That(scale.x, Is.EqualTo(scale.y));
            }
            finally { Object.DestroyImmediate(sprite); Object.DestroyImmediate(texture); }
        }

        [TestCase("building.light.basic")]
        [TestCase("building.settlement.basic")]
        public void TallUtility_SuccessOccupiesBothCellsAndSpendsOnce(string id)
        {
            var setup = CreateSetup(10f, new Vector2(20f, 20f), buildingId: id);
            try
            {
                setup.Terrain.SetTile(new Vector3Int(0, -1, 0), setup.Tile);
                var result = setup.Placement.TryPlaceAt(Vector3Int.zero);
                Assert.That(result.IsSuccess, Is.True);
                Assert.That(result.Footprint, Is.EqualTo(new Vector2Int(1, 2)));
                Assert.That(setup.Wallet.SpendCount, Is.EqualTo(1));
                Assert.That(setup.BuildingRoot.GetChild(0).position, Is.EqualTo(new Vector3(0.5f, 1f, 0f)));
                setup.Placement.Select(setup.Definition);
                Assert.That(setup.Placement.CanPlaceAt(new Vector3Int(0, 1, 0), out var failure), Is.False);
                Assert.That(failure, Is.EqualTo(BuildingPlacementFailure.Occupied));
            }
            finally { setup.Dispose(); }
        }

        [TestCase("building.light.basic", 0)]
        [TestCase("building.light.basic", 1)]
        [TestCase("building.settlement.basic", 0)]
        [TestCase("building.settlement.basic", 1)]
        public void TallUtility_BlockedEitherCellRejectsWithoutSpending(string id, int row)
        {
            var setup = CreateSetup(10f, new Vector2(20f, 20f), buildingId: id);
            try
            {
                setup.Terrain.SetTile(new Vector3Int(0, -1, 0), setup.Tile);
                setup.Terrain.SetTile(new Vector3Int(0, row, 0), setup.Tile);
                Assert.That(setup.Placement.TryPlaceAt(Vector3Int.zero).Failure, Is.EqualTo(BuildingPlacementFailure.Occupied));
                Assert.That(setup.Wallet.SpendCount, Is.Zero);
                Assert.That(setup.BuildingRoot.childCount, Is.Zero);
            }
            finally { setup.Dispose(); }
        }

        [TestCase("building.light.basic", 0, 0, 2)]
        [TestCase("building.light.basic", 1, 1, 2)]
        [TestCase("building.light.basic", 1, 2, 2)]
        [TestCase("building.settlement.basic", 0, 0, 2)]
        [TestCase("building.settlement.basic", 1, 1, 2)]
        [TestCase("building.settlement.basic", 1, 2, 2)]
        public void TallUtility_RestoreUpgradesLegacyAreaWithoutSpending(string id, int width, int height, int rows)
        {
            var setup = CreateSetup(10f, new Vector2(20f, 20f), buildingId: id);
            try
            {
                Assert.That(setup.Placement.TryRestoreBuilding(new BuildingSnapshotDto
                {
                    instanceId = "utility-saved", buildingTypeId = id,
                    footprintWidth = width, footprintHeight = height, rotation = 1, level = 3, health = 0.7f
                }, out var restored), Is.True);
                Assert.That(restored.footprintHeight, Is.EqualTo(2));
                Assert.That(restored.level, Is.EqualTo(3));
                Assert.That(restored.rotation, Is.EqualTo(1));
                Assert.That(restored.health, Is.EqualTo(0.7f));
                Assert.That(setup.Wallet.SpendCount, Is.Zero);
                Assert.That(setup.BuildingRoot.GetChild(0).position, Is.EqualTo(new Vector3(0.5f, rows * 0.5f, 0f)));
                // 두 칸 위에서 검사해야 구형의 빈 상단과 신형의 점유 상단을 구분할 수 있다.
                Assert.That(setup.Placement.CanPlaceAt(new Vector3Int(0, 2, 0), out var failure), Is.False);
                Assert.That(failure, Is.EqualTo(rows == 2 ? BuildingPlacementFailure.Occupied : BuildingPlacementFailure.MissingGround));
            }
            finally { setup.Dispose(); }
        }

        [TestCase("building.light.basic", false)]
        [TestCase("building.settlement.basic", false)]
        [TestCase("building.light.basic", true)]
        [TestCase("building.settlement.basic", true)]
        public void TallUtility_RestoreDoesNotOverwriteTerrainOrLaterSavedFacility(string id, bool savedNeighbour)
        {
            var setup = CreateSetup(10f, new Vector2(20f, 20f), buildingId: id);
            try
            {
                var original = new BuildingSnapshotDto { instanceId = "old", buildingTypeId = id,
                    footprintWidth = 1, footprintHeight = 1 };
                if (savedNeighbour)
                    setup.Placement.ReserveSavedBuildingAreas(new[] { original,
                        new BuildingSnapshotDto { instanceId = "later", buildingTypeId = id, y = 1,
                            footprintWidth = 1, footprintHeight = 1 } });
                else
                    setup.Terrain.SetTile(Vector3Int.up, setup.Tile);
                UnityEngine.TestTools.LogAssert.Expect(LogType.Warning,
                    "[SubTerra] Saved facility old could not expand because its new area is blocked; its original position and size were preserved.");
                Assert.That(setup.Placement.TryRestoreBuilding(original, out var restored), Is.True);
                Assert.That(restored.footprintHeight, Is.EqualTo(1));
                Assert.That(setup.Wallet.SpendCount, Is.Zero);
                if (!savedNeighbour) Assert.That(setup.Terrain.GetTile(Vector3Int.up), Is.SameAs(setup.Tile));
                Assert.That(original.footprintHeight, Is.EqualTo(1));
            }
            finally { setup.Dispose(); }
        }

        [Test]
        public void Foundation_ReconfigureKeepsOneBaseAndDoesNotAddColliders()
        {
            var root = new GameObject("Storage");
            var texture = new Texture2D(32, 32);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
            try
            {
                var visualRoot = new GameObject("VisualRoot").transform;
                visualRoot.SetParent(root.transform, false);
                var art = new GameObject("Artwork").AddComponent<SpriteRenderer>();
                art.transform.SetParent(visualRoot, false);
                art.sprite = sprite;
                var grounding = new GameObject("MVP_Grounding").transform;
                grounding.SetParent(root.transform, false);
                var oldBase = new GameObject("FoundationTile").AddComponent<SpriteRenderer>();
                oldBase.transform.SetParent(grounding, false);
                var port = new GameObject("PowerPortAnchor").transform;
                port.SetParent(grounding, false);
                FacilityGroundedVisual.Apply(root.transform, "building.storage.basic", Vector2Int.one);
                var foundation = root.transform.Find("FacilityFoundation").GetComponent<FacilityFoundationVisual>();
                foundation.BuildVisuals();
                FacilityGroundedVisual.Apply(root.transform, "building.storage.basic", Vector2Int.one);
                foundation.BuildVisuals();
                Assert.That(foundation.transform.childCount, Is.EqualTo(6));
                Assert.That(root.GetComponentsInChildren<Collider2D>(), Is.Empty);
                Assert.That(oldBase.enabled, Is.False);
                Assert.That(port.gameObject.activeInHierarchy, Is.True);
                var plate = foundation.transform.Find("PlateOutline").GetComponent<SpriteRenderer>();
                Assert.That(plate.bounds.min.y, Is.EqualTo(-0.530f).Within(0.001f));
                Assert.That(plate.bounds.size.x, Is.LessThanOrEqualTo(0.96f));
                Bounds visible = FacilityGroundedVisual.GetVisibleBounds(art.sprite);
                Assert.That(art.transform.TransformPoint(new Vector3(visible.center.x, visible.min.y, 0f)).y,
                    Is.EqualTo(-0.540f).Within(0.001f));
                Assert.That(FacilityFoundationVisual.Supports("building.clinic.basic"), Is.False);
                Assert.That(FacilityFoundationVisual.Supports("building.outpost_core.basic"), Is.False);
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(sprite); Object.DestroyImmediate(texture); }
        }

        [Test]
        public void Foundation_PreviewPlateStaysAtFootprintCenterDespiteArtworkScaleAndOffset()
        {
            var root = new GameObject("Preview");
            var prefab = new GameObject("StoragePrefab");
            var texture = new Texture2D(32, 32);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.2f, 0.8f), 32f);
            try
            {
                var visualRoot = new GameObject("VisualRoot").transform;
                visualRoot.SetParent(prefab.transform, false);
                var art = new GameObject("Artwork").AddComponent<SpriteRenderer>();
                art.transform.SetParent(visualRoot, false);
                art.sprite = sprite;
                root.AddComponent<SpriteRenderer>();
                var preview = root.AddComponent<BuildingPlacementPreview>();
                preview.ConfigureFromPrefab(prefab, "building.storage.basic", Vector2Int.one);
                preview.SetCell(null, new Vector3Int(4, 2, 0), true);
                var foundation = root.transform.Find("FacilityFoundation").GetComponent<FacilityFoundationVisual>();
                foundation.BuildVisuals();
                Assert.That(foundation.transform.position, Is.EqualTo(new Vector3(4, 2, 0)));
                var plate = foundation.transform.Find("PlateOutline").GetComponent<SpriteRenderer>();
                Assert.That(plate.bounds.min.y, Is.EqualTo(1.470f).Within(0.001f));
                preview.SetCell(null, new Vector3Int(5, 3, 0), false);
                Assert.That(plate.bounds.min.y, Is.EqualTo(2.470f).Within(0.001f));
                Assert.That(plate.color.r, Is.GreaterThan(plate.color.g));
                preview.Configure((Sprite)null);
                Assert.That(foundation.gameObject.activeSelf, Is.False);
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(prefab); Object.DestroyImmediate(sprite); Object.DestroyImmediate(texture); }
        }

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
                Transform lower = setup.BuildingRoot.GetChild(0);
                Transform upper = setup.BuildingRoot.GetChild(1);
                Assert.That(upper.position.x, Is.EqualTo(lower.position.x));
                Assert.That(upper.position.y - lower.position.y, Is.EqualTo(5f).Within(0.0001f),
                    "Stacked 1x5 ladders must share a continuous five-cell visual pitch.");
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
        public void CanPlaceAt_OneByTwo_RejectsBlockedUpperCell()
        {
            var setup = CreateSetup(
                maximumDistance: 10f,
                areaSize: new Vector2(20f, 12f),
                footprint: new Vector2Int(1, 2));
            try
            {
                setup.Terrain.SetTile(new Vector3Int(0, -1, 0), setup.Tile);
                Assert.That(
                    setup.Placement.CanPlaceAt(Vector3Int.zero, out var clear),
                    Is.True,
                    clear.ToString());

                setup.Terrain.SetTile(new Vector3Int(0, 1, 0), setup.Tile);
                Assert.That(
                    setup.Placement.CanPlaceAt(Vector3Int.zero, out var blocked),
                    Is.False);
                Assert.That(blocked, Is.EqualTo(BuildingPlacementFailure.Occupied));
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
