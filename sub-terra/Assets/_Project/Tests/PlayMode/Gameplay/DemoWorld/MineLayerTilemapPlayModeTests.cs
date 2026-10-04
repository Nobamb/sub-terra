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
        [Test]
        public void ExposedCorners_RoundOnlyConvexAirFacingEdges()
        {
            var grid = new GameObject("CornerTestGrid", typeof(Grid));
            var mapObject = new GameObject("CornerTestMap", typeof(Tilemap), typeof(TilemapRenderer));
            mapObject.transform.SetParent(grid.transform);
            var map = mapObject.GetComponent<Tilemap>();
            var tile = CreateTile("CornerRock");
            try
            {
                map.SetTile(Vector3Int.zero, tile);
                Assert.That(MineTileCornerVisual.GetExposedCorners(map, Vector3Int.zero),
                    Is.EqualTo(new Color32(255, 255, 255, 255)));
                map.SetTile(Vector3Int.right, tile);
                Assert.That(MineTileCornerVisual.GetExposedCorners(map, Vector3Int.zero),
                    Is.EqualTo(new Color32(255, 0, 255, 0)), "Connected edge must not open a seam.");
                map.SetTile(Vector3Int.up, tile);
                Assert.That(MineTileCornerVisual.GetExposedCorners(map, Vector3Int.zero),
                    Is.EqualTo(new Color32(255, 0, 0, 0)), "Only the convex outside corner may round.");
                map.SetTile(Vector3Int.right, null);
                Assert.That(MineTileCornerVisual.GetExposedCorners(map, Vector3Int.zero),
                    Is.EqualTo(new Color32(255, 255, 0, 0)), "Mining must expose the new outside corner.");
                Assert.That(map.GetTile(Vector3Int.zero), Is.SameAs(tile));
                Assert.That(tile.colliderType, Is.EqualTo(Tile.ColliderType.Grid));
            }
            finally
            {
                Object.DestroyImmediate(grid);
                Object.DestroyImmediate(tile);
            }
        }

        [UnityTest]
        public IEnumerator RoundedCorners_RuntimeMaterialUsesStrongerRadiusAndRestoresOnDisable()
        {
            var grid = new GameObject("RoundedTestGrid", typeof(Grid));
            var mapObject = new GameObject("RoundedTestMap", typeof(Tilemap), typeof(TilemapRenderer));
            mapObject.transform.SetParent(grid.transform);
            var map = mapObject.GetComponent<Tilemap>();
            var renderer = mapObject.GetComponent<TilemapRenderer>();
            var tile = CreateTile("RoundedRock");
            var sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1f);
            tile.sprite = sprite;
            var original = renderer.sharedMaterial;
            try
            {
                map.SetTile(Vector3Int.zero, tile);
                map.CompressBounds();
                var corners = mapObject.AddComponent<MineTileCornerVisual>();
                yield return null;
                Assert.That(renderer.sharedMaterial.shader.name, Is.EqualTo("SubTerra/MineRoundedCorners"));
                Assert.That(renderer.sharedMaterial.HasProperty("_CornerRadius"), Is.True);
                Assert.That(renderer.sharedMaterial.GetFloat("_CornerRadius"), Is.EqualTo(0.12f).Within(0.0001f));
                var maskField = typeof(MineTileCornerVisual).GetField("cornerTexture", BindingFlags.Instance | BindingFlags.NonPublic);
                var mask = maskField.GetValue(corners) as Texture2D;
                Assert.That(mask, Is.Not.Null);
                Assert.That((Color32)mask.GetPixel(0, 0), Is.EqualTo(new Color32(255, 255, 255, 255)));
                map.SetTile(Vector3Int.right, tile);
                yield return null;
                mask = maskField.GetValue(corners) as Texture2D;
                Assert.That((Color32)mask.GetPixel(0, 0), Is.EqualTo(new Color32(255, 0, 255, 0)));
                Assert.That(renderer.sharedMaterial.SetPass(0), Is.True, "Corner shader must compile.");
                corners.enabled = false;
                Assert.That(renderer.sharedMaterial, Is.SameAs(original));
            }
            finally
            {
                Object.DestroyImmediate(grid);
                Object.DestroyImmediate(tile);
                Object.DestroyImmediate(sprite);
            }
            yield return null;
        }

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
