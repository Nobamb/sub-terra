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
            fixture.System.AdvanceSimulation(1f);

            Assert.That(fixture.Foreground.HasTile(protectedCell), Is.True);
            Assert.That(emitted, Is.Not.Null);
            Assert.That(emitted.cells.Exists(cell => cell.x == 0 && cell.y == 2), Is.False);
            Assert.That(emitted.severity, Is.Not.EqualTo(0));
            foreach (FieldInfo field in typeof(StructuralCollapseEventDto).GetFields())
                Assert.That(typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType), Is.False);
        }

        [Test]
        public void Collapse_ExcludesRuntimeProtectedCell()
        {
            using var fixture = new StructuralFixture(1);
            var protectedCell = new Vector3Int(0, 2, 0);
            fixture.System.SetCellProtectionPredicate(cell => cell == protectedCell);
            StructuralCollapseEventDto emitted = null;
            fixture.System.CollapseTriggered += value => emitted = value;

            fixture.Mine(1f);
            fixture.System.AdvanceSimulation(1f);

            Assert.That(fixture.Foreground.HasTile(protectedCell), Is.True);
            Assert.That(emitted, Is.Null);
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
            bool reducedRisk = fixture.System.RegisterSupport(support);

            Assert.That(reducedRisk, Is.True);
            Assert.That(fixture.System.CurrentRisk, Is.EqualTo(StructuralRiskLevel.Stable));
            Assert.That(fixture.Overlay.HasTile(new Vector3Int(0, 2, 0)), Is.False);
        }

        [Test]
        public void RemovingUnsupportedCeiling_ClearsStuckStructuralRisk()
        {
            // prompt-B 31-3: 위험 원인 블록을 제거하면 구조 위험이 고착되지 않고 안정으로 돌아와야 한다.
            using var fixture = new StructuralFixture(3, true);
            fixture.Mine(0.2f);
            Assert.That(fixture.System.CurrentRisk, Is.EqualTo(StructuralRiskLevel.Danger));

            for (int x = -1; x <= 1; x++)
            {
                fixture.Foreground.SetTile(new Vector3Int(x, 2, 0), null);
            }

            fixture.Mine(0.01f);

            Assert.That(fixture.System.CurrentRisk, Is.EqualTo(StructuralRiskLevel.Stable));
            Assert.That(fixture.Overlay.HasTile(new Vector3Int(0, 2, 0)), Is.False);
        }

        [Test]
        public void DistantMining_DoesNotEscalateExistingCautionZone()
        {
            // prompt-B 36: 원격 채굴이 기존 주의 구역 가중치/표시를 끌어올리면 안 된다.
            using var fixture = new DualZoneStructuralFixture();
            var left = new Vector3Int(-10, 0, 0);
            var right = new Vector3Int(10, 0, 0);
            var leftCrack = new Vector3Int(-10, 2, 0);

            fixture.MineAt(left, 0.1f);
            Assert.That(fixture.System.EvaluateAt(left), Is.EqualTo(StructuralRiskLevel.Caution));
            Assert.That(fixture.CrackOverlay.TryGetCellRisk(leftCrack, out StructuralRiskLevel leftVisual), Is.True);
            Assert.That(leftVisual, Is.EqualTo(StructuralRiskLevel.Caution));

            fixture.MineAt(right, 0.3f);
            Assert.That(fixture.System.EvaluateAt(right), Is.GreaterThanOrEqualTo(StructuralRiskLevel.Danger));

            Assert.That(fixture.System.EvaluateAt(left), Is.EqualTo(StructuralRiskLevel.Caution));
            Assert.That(fixture.CrackOverlay.TryGetCellRisk(leftCrack, out StructuralRiskLevel leftVisualAfter), Is.True);
            Assert.That(leftVisualAfter, Is.EqualTo(StructuralRiskLevel.Caution));
            Assert.That(fixture.System.CurrentRisk, Is.GreaterThanOrEqualTo(StructuralRiskLevel.Danger));
        }

        [Test]
        public void OppositeWallMining_DoesNotEscalateOtherWallCaution()
        {
            // 같은 갱도 맞은편 벽(가로 거리 2, localRiskRadius=1)도 독립이어야 한다.
            using var fixture = new OppositeWallFixture();
            var left = new Vector3Int(-1, 0, 0);
            var right = new Vector3Int(1, 0, 0);
            var leftCrack = new Vector3Int(-1, 2, 0);

            fixture.MineAt(left, 0.1f);
            Assert.That(fixture.CrackOverlay.TryGetCellRisk(leftCrack, out StructuralRiskLevel leftVisual), Is.True);
            Assert.That(leftVisual, Is.EqualTo(StructuralRiskLevel.Caution));

            fixture.MineAt(right, 0.45f);
            Assert.That(fixture.System.EvaluateAt(right), Is.GreaterThanOrEqualTo(StructuralRiskLevel.Danger));

            Assert.That(fixture.CrackOverlay.TryGetCellRisk(leftCrack, out StructuralRiskLevel leftAfter), Is.True);
            Assert.That(leftAfter, Is.EqualTo(StructuralRiskLevel.Caution));
            Assert.That(fixture.System.EvaluateAt(left), Is.EqualTo(StructuralRiskLevel.Caution));
        }

        [Test]
        public void NearbyMining_IncreasesLocalZoneRiskIndependently()
        {
            using var fixture = new DualZoneStructuralFixture();
            var center = new Vector3Int(-10, 0, 0);
            var neighbor = new Vector3Int(-9, 0, 0);

            fixture.MineAt(center, 0.1f);
            Assert.That(fixture.System.EvaluateAt(center), Is.EqualTo(StructuralRiskLevel.Caution));

            fixture.MineAt(neighbor, 0.35f);
            Assert.That(fixture.System.EvaluateAt(center), Is.GreaterThanOrEqualTo(StructuralRiskLevel.Danger));

            // 원격은 왼쪽 충격이 섞이지 않아 Danger로 올라가지 않는다.
            Assert.That(
                fixture.System.EvaluateAt(new Vector3Int(10, 0, 0)),
                Is.LessThan(StructuralRiskLevel.Danger));
        }

        [Test]
        public void RebuildRiskFromMinedCells_RestoresRiskWithoutCollapse()
        {
            // prompt-B 36-1: 세이브 복원처럼 런타임 상태를 비운 뒤 채굴 셀만으로 위험을 재구성한다.
            using var fixture = new StructuralFixture(1, true);
            var mineCell = Vector3Int.zero;
            var ceiling = new Vector3Int(0, 2, 0);

            fixture.Mine(0.1f);
            Assert.That(fixture.System.CurrentRisk, Is.EqualTo(StructuralRiskLevel.Caution));
            Assert.That(fixture.Overlay.HasTile(ceiling), Is.True);

            fixture.System.ClearRuntimeRiskState();
            Assert.That(fixture.System.CurrentRisk, Is.EqualTo(StructuralRiskLevel.Stable));
            Assert.That(fixture.Overlay.HasTile(ceiling), Is.False);

            StructuralCollapseEventDto collapse = null;
            fixture.System.CollapseTriggered += value => collapse = value;
            fixture.System.RebuildRiskFromMinedCells(new[] { mineCell }, 0.1f);

            Assert.That(collapse, Is.Null);
            Assert.That(fixture.System.CurrentRisk, Is.EqualTo(StructuralRiskLevel.Caution));
            Assert.That(fixture.Overlay.HasTile(ceiling), Is.True);
            Assert.That(fixture.System.EvaluateAt(mineCell), Is.EqualTo(StructuralRiskLevel.Caution));
            fixture.System.AdvanceSimulation(2f);
            Assert.That(collapse, Is.Null, "복원 재계산은 시간이 지나도 예고/붕괴를 만들면 안 됩니다.");
        }

        [Test]
        public void Collapse_TelegraphsBeforeRemovingTile_AndSupportCancelsIt()
        {
            using var fixture = new StructuralFixture(1, true);
            var ceiling = new Vector3Int(0, 2, 0);
            StructuralCollapseEventDto emitted = null;
            fixture.System.CollapseTriggered += value => emitted = value;

            fixture.Mine(1f);

            Assert.That(fixture.Foreground.HasTile(ceiling), Is.True);
            Assert.That(fixture.System.EvaluateAt(Vector3Int.zero),
                Is.EqualTo(StructuralRiskLevel.CollapseImminent));
            Assert.That(emitted, Is.Null);

            GameObject supportObject = new("TelegraphCancelSupport");
            supportObject.transform.SetParent(fixture.Root.transform);
            supportObject.transform.position = fixture.Foreground.GetCellCenterWorld(Vector3Int.zero);
            StructuralSupport support = supportObject.AddComponent<StructuralSupport>();
            fixture.System.RegisterSupport(support);
            fixture.System.AdvanceSimulation(2f);

            Assert.That(emitted, Is.Null);
            Assert.That(fixture.Foreground.HasTile(ceiling), Is.True);
            Assert.That(fixture.System.CurrentRisk, Is.EqualTo(StructuralRiskLevel.Stable));
        }

        [Test]
        public void CrackIntensity_IncreasesWithinSameRiskBand()
        {
            using var fixture = new StructuralFixture(1, true);
            var ceiling = new Vector3Int(0, 2, 0);

            fixture.Mine(0.1f);
            Assert.That(fixture.System.GetComponent<StructuralCrackOverlay>(), Is.Not.Null);
            StructuralCrackOverlay overlay = fixture.System.GetComponent<StructuralCrackOverlay>();
            Assert.That(overlay.TryGetCellIntensity(ceiling, out float first), Is.True);

            fixture.Mine(0.05f);
            Assert.That(overlay.TryGetCellIntensity(ceiling, out float second), Is.True);
            Assert.That(second, Is.GreaterThan(first));
        }

        [Test]
        public void RemovingSupport_RestoresAccumulatedRiskWithSupportLossCause()
        {
            using var fixture = new StructuralFixture(1, true);
            fixture.Mine(0.3f);

            GameObject supportObject = new("RemovableSupport");
            supportObject.transform.SetParent(fixture.Root.transform);
            supportObject.transform.position = fixture.Foreground.GetCellCenterWorld(Vector3Int.zero);
            StructuralSupport support = supportObject.AddComponent<StructuralSupport>();
            fixture.System.RegisterSupport(support);
            Assert.That(fixture.System.CurrentRisk, Is.EqualTo(StructuralRiskLevel.Stable));

            fixture.System.UnregisterSupport(support);
            StructuralRiskStatus restored = fixture.System.EvaluateStatusAt(Vector3Int.zero);

            Assert.That(restored.Level, Is.EqualTo(StructuralRiskLevel.Caution));
            Assert.That(restored.Cause, Is.EqualTo(StructuralRiskCause.SupportRemoved));
        }

        private static List<CollapseCellDto> RunCollapse(long seed)
        {
            using var fixture = new StructuralFixture(7);
            fixture.System.ConfigureWorldSeed(seed);
            StructuralCollapseEventDto emitted = null;
            fixture.System.CollapseTriggered += value => emitted = value;

            fixture.Mine(1f);
            fixture.System.AdvanceSimulation(1f);

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
                SetField(System, "localRiskRadius", 1);
                SetField(System, "scanRadius", 3);
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

        private sealed class DualZoneStructuralFixture : IDisposable
        {
            private readonly Tile tile;

            public DualZoneStructuralFixture()
            {
                Root = new GameObject("DualZoneStructuralFixture");
                var grid = new GameObject("Grid");
                grid.transform.SetParent(Root.transform);
                grid.AddComponent<Grid>();
                var foregroundObject = new GameObject("Foreground");
                foregroundObject.transform.SetParent(grid.transform);
                Foreground = foregroundObject.AddComponent<Tilemap>();
                foregroundObject.AddComponent<TilemapRenderer>();
                tile = ScriptableObject.CreateInstance<Tile>();

                for (int x = -10; x <= -9; x++)
                    Foreground.SetTile(new Vector3Int(x, 2, 0), tile);
                for (int x = 9; x <= 10; x++)
                    Foreground.SetTile(new Vector3Int(x, 2, 0), tile);

                System = Root.AddComponent<StructuralIntegritySystem>();
                SetField(System, "foregroundTilemap", Foreground);
                SetField(System, "scanRadius", 3);
                SetField(System, "localRiskRadius", 1);

                var overlayObject = new GameObject("Overlay");
                overlayObject.transform.SetParent(grid.transform);
                Overlay = overlayObject.AddComponent<Tilemap>();
                overlayObject.AddComponent<TilemapRenderer>();
                CrackOverlay = Root.AddComponent<StructuralCrackOverlay>();
                SetField(CrackOverlay, "overlayTilemap", Overlay);
                SetField(System, "crackOverlay", CrackOverlay);
            }

            public GameObject Root { get; }
            public Tilemap Foreground { get; }
            public Tilemap Overlay { get; }
            public StructuralCrackOverlay CrackOverlay { get; }
            public StructuralIntegritySystem System { get; }

            public void MineAt(Vector3Int cell, float structuralImpact)
            {
                System.NotifyTileMined(
                    cell,
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

        /// <summary>좁은 갱도 좌/우 벽 천장 픽스처.</summary>
        private sealed class OppositeWallFixture : IDisposable
        {
            private readonly Tile tile;

            public OppositeWallFixture()
            {
                Root = new GameObject("OppositeWallFixture");
                var grid = new GameObject("Grid");
                grid.transform.SetParent(Root.transform);
                grid.AddComponent<Grid>();
                var foregroundObject = new GameObject("Foreground");
                foregroundObject.transform.SetParent(grid.transform);
                Foreground = foregroundObject.AddComponent<Tilemap>();
                tile = ScriptableObject.CreateInstance<Tile>();

                // 좌 천장 x=-1, 우 천장 x=1 (거리 2 > localRiskRadius 1)
                Foreground.SetTile(new Vector3Int(-1, 2, 0), tile);
                Foreground.SetTile(new Vector3Int(1, 2, 0), tile);

                System = Root.AddComponent<StructuralIntegritySystem>();
                SetField(System, "foregroundTilemap", Foreground);
                SetField(System, "scanRadius", 3);
                SetField(System, "localRiskRadius", 1);

                var overlayObject = new GameObject("Overlay");
                overlayObject.transform.SetParent(grid.transform);
                Overlay = overlayObject.AddComponent<Tilemap>();
                CrackOverlay = Root.AddComponent<StructuralCrackOverlay>();
                SetField(CrackOverlay, "overlayTilemap", Overlay);
                SetField(System, "crackOverlay", CrackOverlay);
            }

            public GameObject Root { get; }
            public Tilemap Foreground { get; }
            public Tilemap Overlay { get; }
            public StructuralCrackOverlay CrackOverlay { get; }
            public StructuralIntegritySystem System { get; }

            public void MineAt(Vector3Int cell, float structuralImpact)
            {
                System.NotifyTileMined(
                    cell,
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
