using System.Collections;
using System.Reflection;
using NUnit.Framework;
using SubTerra.Gameplay.Snapshot;
using SubTerra.Shared;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

namespace SubTerra.Gameplay.Structural.Tests
{
    public sealed class StructuralCollapsePlayModeTests
    {
        [UnityTest]
        public IEnumerator WarningOverlay_AppearsOnSeparateNonCollidingTilemap()
        {
            using var fixture = new RuntimeFixture(1, true, false);
            yield return null;

            fixture.Mine(0.1f);

            var warningCell = new Vector3Int(0, 2, 0);
            Assert.That(fixture.System.CurrentRisk, Is.EqualTo(StructuralRiskLevel.Caution));
            Assert.That(fixture.Foreground.HasTile(warningCell), Is.True);
            Assert.That(fixture.Overlay.HasTile(warningCell), Is.True);
            Assert.That(fixture.Overlay.GetComponent<Collider2D>(), Is.Null);
        }

        [UnityTest]
        public IEnumerator Collapse_RefreshesCollider_EmitsDto_AndEntersSnapshot()
        {
            using var fixture = new RuntimeFixture(5, false, true);
            yield return null;
            fixture.ForegroundCollider.ProcessTilemapChanges();
            StructuralCollapseEventDto received = null;
            fixture.System.CollapseTriggered += collapse => received = collapse;

            fixture.Mine(1f);
            yield return new WaitForSecondsRealtime(1f);

            Assert.That(received, Is.Not.Null);
            Assert.That(received.cells, Has.Count.EqualTo(3));
            Assert.That(fixture.ForegroundCollider.hasTilemapChanges, Is.False);
            WorldSnapshotDto snapshot = fixture.Snapshot.CaptureSnapshot();
            Assert.That(snapshot.collapseChanges, Has.Count.EqualTo(received.cells.Count));
            foreach (CollapseCellDto cell in received.cells)
                Assert.That(fixture.Foreground.HasTile(new Vector3Int(cell.x, cell.y, 0)), Is.False);

            fixture.RestoreCollapsedTiles(received);
            fixture.Snapshot.RestoreSnapshot(snapshot);

            Assert.That(fixture.ForegroundCollider.hasTilemapChanges, Is.False);
            foreach (CollapseCellDto cell in received.cells)
                Assert.That(fixture.Foreground.HasTile(new Vector3Int(cell.x, cell.y, 0)), Is.False);
        }

        [UnityTest]
        public IEnumerator PromptB55_1_EachFallingRockConsumesOnePlayerContact()
        {
            using var fixture = new RuntimeFixture(1, true, false);
            var receiver = new CountingCollapseReceiver();
            fixture.System.BindCollapseDamageReceiver(receiver);

            fixture.CrackOverlay.PlayCollapse(
                new Vector3Int(0, 2, 0), fixture.Foreground, 0.1f);
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.That(receiver.ImpactCount, Is.EqualTo(1));

            fixture.CrackOverlay.PlayCollapse(
                new Vector3Int(0, 2, 0), fixture.Foreground, 0.1f);
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.That(receiver.ImpactCount, Is.EqualTo(2));
        }

        private sealed class CountingCollapseReceiver : ICollapseDamageReceiver
        {
            public int ImpactCount { get; private set; }

            public bool IsCollapseContact(float fromX, float fromY, float toX, float toY)
            {
                return true;
            }

            public bool ApplyCollapseImpact()
            {
                ImpactCount++;
                return true;
            }
        }

        private sealed class RuntimeFixture : System.IDisposable
        {
            private readonly Tile tile;

            public RuntimeFixture(int ceilingWidth, bool withOverlay, bool withSnapshot)
            {
                Root = new GameObject("StructuralRuntimeFixture");
                Root.SetActive(false);
                var grid = new GameObject("Grid");
                grid.transform.SetParent(Root.transform);
                grid.AddComponent<Grid>();
                var foregroundObject = new GameObject("Foreground");
                foregroundObject.transform.SetParent(grid.transform);
                Foreground = foregroundObject.AddComponent<Tilemap>();
                foregroundObject.AddComponent<TilemapRenderer>();
                ForegroundCollider = foregroundObject.AddComponent<TilemapCollider2D>();
                foregroundObject.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
                tile = ScriptableObject.CreateInstance<Tile>();
                int half = ceilingWidth / 2;
                for (int x = -half; x <= half; x++)
                    Foreground.SetTile(new Vector3Int(x, 2, 0), tile);

                System = Root.AddComponent<StructuralIntegritySystem>();
                SetField(System, "foregroundTilemap", Foreground);
                if (withOverlay)
                {
                    var overlayObject = new GameObject("StructuralCrackOverlay");
                    overlayObject.transform.SetParent(grid.transform);
                    Overlay = overlayObject.AddComponent<Tilemap>();
                    overlayObject.AddComponent<TilemapRenderer>();
                    StructuralCrackOverlay crackOverlay = Root.AddComponent<StructuralCrackOverlay>();
                    SetField(crackOverlay, "overlayTilemap", Overlay);
                    SetField(System, "crackOverlay", crackOverlay);
                    CrackOverlay = crackOverlay;
                }

                if (withSnapshot)
                {
                    Snapshot = Root.AddComponent<WorldSnapshotSystem>();
                    SetField(Snapshot, "foregroundTilemap", Foreground);
                    SetField(Snapshot, "structuralSystem", System);
                }

                Root.SetActive(true);
            }

            public GameObject Root { get; }
            public Tilemap Foreground { get; }
            public Tilemap Overlay { get; }
            public StructuralCrackOverlay CrackOverlay { get; }
            public TilemapCollider2D ForegroundCollider { get; }
            public StructuralIntegritySystem System { get; }
            public WorldSnapshotSystem Snapshot { get; }

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

            public void RestoreCollapsedTiles(StructuralCollapseEventDto collapse)
            {
                foreach (CollapseCellDto cell in collapse.cells)
                    Foreground.SetTile(new Vector3Int(cell.x, cell.y, 0), tile);
                ForegroundCollider.ProcessTilemapChanges();
            }

            public void Dispose()
            {
                Object.DestroyImmediate(Root);
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
    }
}
