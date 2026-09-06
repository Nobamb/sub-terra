using System.Reflection;
using NUnit.Framework;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SubTerra.Gameplay.Building.Tests
{
    public sealed class FacilityTilemapVisualTests
    {
        [Test]
        public void ClinicPlacementAndRestore_DrawsBackWallAndSurfaceWithoutChangingTerrain()
        {
            var host = new GameObject("FacilityTilemapVisualSetup");
            host.SetActive(false);
            var grid = new GameObject("Grid");
            grid.transform.SetParent(host.transform);
            grid.AddComponent<Grid>();
            Tilemap terrain = CreateTilemap("Terrain", grid.transform);
            Tilemap backWall = CreateTilemap("FacilityBackWall", grid.transform);
            Tilemap surface = CreateTilemap("FacilitySurface", grid.transform);
            Tilemap foreground = CreateTilemap("FacilityForeground", grid.transform);
            var buildingRoot = new GameObject("RuntimeBuildings").transform;
            buildingRoot.SetParent(host.transform);

            var prefab = new GameObject("ClinicPrefab");
            prefab.AddComponent<BuildingInstance>();
            var definition = ScriptableObject.CreateInstance<BuildingPlacementDefinition>();
            definition.EditorSet("building.clinic.basic", prefab, Vector2Int.one, true);
            TileBase rock = ScriptableObject.CreateInstance<Tile>();
            var placement = host.AddComponent<BuildingPlacementSystem>();
            var visuals = host.AddComponent<FacilityTilemapVisualSystem>();
            SetField(placement, "terrainTilemap", terrain);
            SetField(placement, "buildingRoot", buildingRoot);
            SetField(placement, "restoreDefinitions", new[] { definition });
            SetField(visuals, "buildingPlacementSystem", placement);
            SetField(visuals, "sourceTerrainTilemap", terrain);
            SetField(visuals, "facilityBackWallTilemap", backWall);
            SetField(visuals, "facilitySurfaceTilemap", surface);
            SetField(visuals, "facilityForegroundTilemap", foreground);
            placement.SetResourceWallet(new AlwaysAffordableWallet());

            try
            {
                Vector3Int clinicCell = new(3, 2, 0);
                Vector3Int groundCell = clinicCell + Vector3Int.down;
                terrain.SetTile(groundCell, rock);
                host.SetActive(true);
                InvokePrivate(visuals, "OnEnable");
                placement.Select(definition);

                Assert.That(placement.TryPlaceAt(clinicCell).IsSuccess, Is.True);
                Assert.That(backWall.GetTile(clinicCell), Is.SameAs(rock));
                Assert.That(surface.GetTile(groundCell), Is.SameAs(rock));
                Assert.That(terrain.GetTile(groundCell), Is.SameAs(rock));
                Assert.That(foreground.HasTile(clinicCell), Is.False);

                placement.PrepareForWorldRestore();
                Assert.That(backWall.HasTile(clinicCell), Is.False);
                Assert.That(surface.HasTile(groundCell), Is.False);

                Assert.That(placement.TryRestoreBuilding(new BuildingSnapshotDto
                {
                    instanceId = "building.clinic.basic-0001",
                    buildingTypeId = "building.clinic.basic",
                    x = clinicCell.x,
                    y = clinicCell.y
                }), Is.True);
                Assert.That(backWall.GetTile(clinicCell), Is.SameAs(rock));
                Assert.That(surface.GetTile(groundCell), Is.SameAs(rock));
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(prefab);
                Object.DestroyImmediate(definition);
                Object.DestroyImmediate(rock);
            }
        }

        private static Tilemap CreateTilemap(string name, Transform parent)
        {
            var tilemapObject = new GameObject(name);
            tilemapObject.transform.SetParent(parent);
            Tilemap tilemap = tilemapObject.AddComponent<Tilemap>();
            tilemapObject.AddComponent<TilemapRenderer>();
            return tilemap;
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "Missing field: " + name);
            field.SetValue(target, value);
        }

        private static void InvokePrivate(object target, string name)
        {
            MethodInfo method = target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, "Missing field: " + name);
            method.Invoke(target, null);
        }

        private sealed class AlwaysAffordableWallet : IResourceWallet
        {
            public bool CanAfford(System.Collections.Generic.IReadOnlyList<ItemCostDto> costs) => true;
            public bool TrySpend(System.Collections.Generic.IReadOnlyList<ItemCostDto> costs) => true;
        }
    }
}
