using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SubTerra.Gameplay.Structural.Tests
{
    public sealed class StructuralIntegritySystemTests
    {
        [Test]
        public void Collapse_SameSeedAndWorldState_EmitsSameCellList()
        {
            List<CollapseCellDto> first = RunCollapse(20260731L);
            List<CollapseCellDto> second = RunCollapse(20260731L);

            Assert.That(ToKeys(second), Is.EqualTo(ToKeys(first)));
        }

        [Test]
        public void Collapse_ExcludesProtectedCell_AndEmitsUnityIndependentDto()
        {
            using var fixture = new StructuralFixture(5);
            var protectedCell = new Vector3Int(0, 2, 0);
            fixture.System.RegisterProtectedCell(protectedCell);
            StructuralCollapseEventDto emitted = null;
            fixture.System.CollapseTriggered += value => emitted = value;

            fixture.Mine(1f);

            Assert.That(fixture.Foreground.HasTile(protectedCell), Is.True);
            Assert.That(emitted, Is.Not.Null);
            Assert.That(emitted.cells.Exists(cell => cell.x == 0 && cell.y == 2), Is.False);
            Assert.That(emitted.severity, Is.Not.EqualTo(0));
            foreach (FieldInfo field in typeof(StructuralCollapseEventDto).GetFields())
                Assert.That(typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType), Is.False);
        }

        [Test]
        public void RegisterSupport_ReevaluatesOnlyChangedArea_AndClearsCrackOverlay()
        {
            using var fixture = new StructuralFixture(1, true);
            fixture.Mine(0.1f);
            Assert.That(fixture.System.CurrentRisk, Is.EqualTo(StructuralRiskLevel.Caution));
            Assert.That(fixture.Overlay.HasTile(new Vector3Int(0, 2, 0)), Is.True);

            GameObject supportObject = new("Support");
            supportObject.transform.SetParent(fixture.Root.transform);
            supportObject.transform.position = fixture.Foreground.GetCellCenterWorld(Vector3Int.zero);
            StructuralSupport support = supportObject.AddComponent<StructuralSupport>();
            fixture.System.RegisterSupport(support);

            Assert.That(fixture.System.CurrentRisk, Is.EqualTo(StructuralRiskLevel.Stable));
            Assert.That(fixture.Overlay.HasTile(new Vector3Int(0, 2, 0)), Is.False);
        }

        private static List<CollapseCellDto> RunCollapse(long seed)
        {
            using var fixture = new StructuralFixture(7);
            fixture.System.ConfigureWorldSeed(seed);
            StructuralCollapseEventDto emitted = null;
            fixture.System.CollapseTriggered += value => emitted = value;

            fixture.Mine(1f);

            Assert.That(emitted, Is.Not.Null);
            Assert.That(emitted.worldSeed, Is.EqualTo(seed));
            return emitted.cells;
        }

        private static List<string> ToKeys(IEnumerable<CollapseCellDto> cells)
        {
            var keys = new List<string>();
            foreach (CollapseCellDto cell in cells) keys.Add($"{cell.x},{cell.y}");
            return keys;
        }

        private sealed class StructuralFixture : IDisposable
        {
            private readonly Tile tile;

            public StructuralFixture(int ceilingWidth, bool withOverlay = false)
            {
                Root = new GameObject("StructuralFixture");
                var grid = new GameObject("Grid");
                grid.transform.SetParent(Root.transform);
                grid.AddComponent<Grid>();
                var foregroundObject = new GameObject("Foreground");
                foregroundObject.transform.SetParent(grid.transform);
                Foreground = foregroundObject.AddComponent<Tilemap>();
                foregroundObject.AddComponent<TilemapRenderer>();
                tile = ScriptableObject.CreateInstance<Tile>();
                int half = ceilingWidth / 2;
                for (int x = -half; x <= half; x++)
                    Foreground.SetTile(new Vector3Int(x, 2, 0), tile);

                System = Root.AddComponent<StructuralIntegritySystem>();
                SetField(System, "foregroundTilemap", Foreground);
                if (!withOverlay) return;

                var overlayObject = new GameObject("Overlay");
                overlayObject.transform.SetParent(grid.transform);
                Overlay = overlayObject.AddComponent<Tilemap>();
                overlayObject.AddComponent<TilemapRenderer>();
                StructuralCrackOverlay crackOverlay = Root.AddComponent<StructuralCrackOverlay>();
                SetField(crackOverlay, "overlayTilemap", Overlay);
                SetField(System, "crackOverlay", crackOverlay);
            }

            public GameObject Root { get; }
            public Tilemap Foreground { get; }
            public Tilemap Overlay { get; }
            public StructuralIntegritySystem System { get; }

            public void Mine(float structuralImpact)
            {
                System.NotifyTileMined(
                    Vector3Int.zero,
                    new MiningTileDto(
                        "tile.test",
                        string.Empty,
                        0,
                        true,
                        1f,
                        1f,
                        structuralImpact,
                        false));
            }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(Root);
                UnityEngine.Object.DestroyImmediate(tile);
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
    }
}
