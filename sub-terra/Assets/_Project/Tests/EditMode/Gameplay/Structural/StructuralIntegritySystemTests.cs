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
            using var fixture = new StructuralFixture(11);
            var protectedCell = new Vector3Int(-4, 2, 0);
            fixture.System.RegisterProtectedCell(protectedCell);
            StructuralCollapseEventDto emitted = null;
            fixture.System.CollapseTriggered += value => emitted = value;

            fixture.Mine(1f);
            fixture.System.AdvanceSimulation(1f);

            Assert.That(fixture.Foreground.HasTile(protectedCell), Is.True);
            Assert.That(emitted, Is.Not.Null);
            Assert.That(emitted.cells.Exists(cell => cell.x == protectedCell.x && cell.y == protectedCell.y), Is.False);
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
            using var fixture = new StructuralFixture(11, true);
            fixture.AddSideAnchors(5);
            fixture.Mine(0.1f);
            Assert.That(fixture.System.CurrentRisk, Is.EqualTo(StructuralRiskLevel.CollapseImminent));
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
            using var fixture = new StructuralFixture(9, true);
            fixture.AddSideAnchors(4);
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

        [TestCase(5, 0, false, StructuralRiskLevel.Stable)]
        [TestCase(5, -8, false, StructuralRiskLevel.Stable)]
        [TestCase(7, 0, false, StructuralRiskLevel.Caution)]
        [TestCase(9, 0, false, StructuralRiskLevel.Danger)]
        [TestCase(11, 0, false, StructuralRiskLevel.CollapseImminent)]
        [TestCase(11, 0, true, StructuralRiskLevel.Stable)]
        public void Issue281_RoofRiskDependsOnHorizontalDistance(
            int width, int floorY, bool centerPillar, StructuralRiskLevel expected)
        {
            using var fixture = new StructuralFixture(width, true);
            SetField(fixture.System, "localRiskRadius", 0);
            for (int y = floorY; y < 2; y++)
            {
                fixture.PlaceTile(new Vector3Int(-width / 2, y, 0));
                fixture.PlaceTile(new Vector3Int(width / 2, y, 0));
                if (centerPillar) fixture.PlaceTile(new Vector3Int(0, y, 0));
            }
            for (int x = -width / 2; x <= width / 2; x++)
                fixture.PlaceTile(new Vector3Int(x, floorY, 0));
            fixture.System.RegisterProtectedCell(new Vector3Int(-width / 2, floorY, 0));

            var probe = new Vector3Int(0, 1, 0);
            if (centerPillar) fixture.System.RebuildRiskFromMinedCells(new[] { probe });
            else fixture.Mine(0f);

            Assert.That(fixture.System.EvaluateAt(probe), Is.EqualTo(expected));
            Assert.That(fixture.Overlay.HasTile(probe + Vector3Int.up),
                Is.EqualTo(expected != StructuralRiskLevel.Stable));
        }

        [TestCase(7, 1, false)]
        [TestCase(9, 1, true)]
        [TestCase(7, 14, true)]
        [TestCase(9, 0, false)]
        public void Issue317_MiningRollUsesDistanceAndWorldSeed(int width, long seed, bool falls)
        {
            using var fixture = new StructuralFixture(width, true);
            fixture.AddSideAnchors(width / 2);
            var settings = fixture.EnableProbability();
            SetField(settings, "probabilisticRollRadiusCells", 0);
            fixture.System.ConfigureWorldSeed(seed);
            var roof = new Vector3Int(0, 2, 0);
            var overlay = fixture.System.GetComponent<StructuralCrackOverlay>();

            fixture.Mine(0f);

            Assert.That(overlay.IsTelegraphing(roof), Is.EqualTo(falls));
            fixture.System.AdvanceSimulation(0.9f);
            Assert.That(fixture.Foreground.HasTile(roof), Is.True);
            fixture.System.AdvanceSimulation(0.2f);
            Assert.That(fixture.Foreground.HasTile(roof), Is.EqualTo(!falls));
        }

        [Test]
        public void Issue317_ProbabilisticWarningDoesNotBlockForcedCollapse()
        {
            using var fixture = new StructuralFixture(9, true);
            fixture.AddSideAnchors(4);
            var settings = fixture.EnableProbability();
            SetField(settings, "probabilisticRollRadiusCells", 0);
            SetField(settings, "dangerCollapseChancePerMining", 1f);
            var overlay = fixture.System.GetComponent<StructuralCrackOverlay>();
            var probabilistic = new Vector3Int(0, 2, 0);
            var forced = new Vector3Int(4, 2, 0);
            fixture.Mine(0f);

            fixture.RemoveAndNotifyMined(new Vector3Int(4, 1, 0), 0f);

            Assert.That(overlay.IsTelegraphing(probabilistic), Is.True);
            Assert.That(overlay.IsTelegraphing(forced), Is.True);
            fixture.System.AdvanceSimulation(0.7f);
            Assert.That(fixture.Foreground.HasTile(forced), Is.False);
            Assert.That(fixture.Foreground.HasTile(probabilistic), Is.True);
        }

        [Test]
        public void Issue317_OffscreenWindowPausesAndExpiryDoesNotRoll()
        {
            using var fixture = new StructuralFixture(7, true);
            fixture.AddSideAnchors(3);
            var settings = fixture.EnableProbability();
            SetField(settings, "cautionCollapseChancePerMining", 1f);
            Camera camera = Camera.main;
            Vector3 onScreen = camera.transform.position;
            camera.transform.position += Vector3.right * 100f;
            var roof = new Vector3Int(0, 2, 0);
            var windows = (Dictionary<Vector3Int, float>)typeof(StructuralIntegritySystem)
                .GetField("probabilisticWindows", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(fixture.System);

            fixture.Mine(0f);
            fixture.System.AdvanceSimulation(45f);
            Assert.That(windows[roof], Is.EqualTo(30f));
            camera.transform.position = onScreen;
            fixture.System.AdvanceSimulation(31f);
            Assert.That(windows.ContainsKey(roof), Is.False);
            Assert.That(fixture.Foreground.HasTile(roof), Is.True);

            fixture.Mine(0f);
            Assert.That(windows[roof], Is.EqualTo(30f));
            camera.transform.position += Vector3.right * 100f;
            fixture.System.AdvanceSimulation(2f);
            Assert.That(fixture.Foreground.HasTile(roof), Is.True);
            camera.transform.position = onScreen;
            fixture.System.AdvanceSimulation(2f);
            Assert.That(fixture.Foreground.HasTile(roof), Is.False);
        }

        [Test]
        public void Issue317_WidenedCeilingWaitsForAnotherMiningAction()
        {
            using var fixture = new StructuralFixture(5, true);
            var anchor = new Vector3Int(-2, 1, 0);
            fixture.PlaceTile(anchor);
            fixture.System.RegisterProtectedCell(anchor);
            for (int x = 2; x <= 5; x++) fixture.PlaceTile(new Vector3Int(x, 3, 0));
            var rightAnchor = new Vector3Int(5, 2, 0);
            fixture.PlaceTile(rightAnchor);
            fixture.System.RegisterProtectedCell(rightAnchor);
            var settings = fixture.EnableProbability();
            SetField(settings, "cautionCollapseChancePerMining", 1f);
            SetField(settings, "dangerCollapseChancePerMining", 1f);

            var first = new Vector3Int(1, 2, 0);
            var widened = new Vector3Int(2, 2, 0);
            fixture.Mine(0f);
            fixture.System.AdvanceSimulation(1.1f);
            Assert.That(fixture.Foreground.HasTile(first), Is.False);
            fixture.System.AdvanceSimulation(5f);
            Assert.That(fixture.Foreground.HasTile(widened), Is.True);

            fixture.Mine(0f);
            fixture.System.AdvanceSimulation(0.7f);
            Assert.That(fixture.Foreground.HasTile(widened), Is.False);
        }

        [Test]
        public void Issue316_ObservedHangingColumnCracksMatchWarningAndClearWithSupport()
        {
            using var fixture = new StructuralFixture(1, true);
            var probe = new Vector3Int(0, 1, 0);
            Vector3Int lower = probe + Vector3Int.up;
            Vector3Int upper = lower + Vector3Int.up;
            Vector3Int roof = upper + Vector3Int.up;
            fixture.PlaceTile(upper);
            for (int x = -1; x <= 1; x++)
                fixture.PlaceTile(roof + Vector3Int.right * x);

            var overlay = fixture.System.GetComponent<StructuralCrackOverlay>();
            StructuralRiskStatus status = fixture.System.EvaluateStatusAt(probe);

            Assert.That(status.Level, Is.EqualTo(StructuralRiskLevel.Danger));
            Assert.That(overlay.TryGetCellRisk(lower, out var lowerRisk), Is.True);
            Assert.That(overlay.TryGetCellRisk(upper, out var upperRisk), Is.True);
            Assert.That(lowerRisk, Is.EqualTo(status.Level));
            Assert.That(upperRisk, Is.EqualTo(status.Level));
            Assert.That(fixture.Overlay.HasTile(roof), Is.False);
            fixture.System.AdvanceSimulation(2f);
            Assert.That(fixture.Foreground.HasTile(lower), Is.True);

            var supportObject = new GameObject("Support");
            supportObject.transform.SetParent(fixture.Root.transform);
            supportObject.transform.position = fixture.Foreground.GetCellCenterWorld(Vector3Int.zero);
            var support = supportObject.AddComponent<StructuralSupport>();
            SetField(support, "radius", 2f);
            fixture.System.RegisterSupport(support);

            Assert.That(fixture.Overlay.HasTile(lower), Is.False);
            Assert.That(fixture.Overlay.HasTile(upper), Is.False);
        }

        [Test]
        public void Issue281_RoofAnchorDoesNotReachAcrossGap()
        {
            using var fixture = new StructuralFixture(5, true);
            fixture.Foreground.SetTile(new Vector3Int(-1, 2, 0), null);
            fixture.PlaceTile(new Vector3Int(-2, 1, 0));
            SetField(fixture.System, "localRiskRadius", 0);

            fixture.Mine(0f);

            Assert.That(fixture.System.EvaluateAt(new Vector3Int(0, 1, 0)),
                Is.EqualTo(StructuralRiskLevel.CollapseImminent));
        }

        [Test]
        public void Issue281_PreviewAndPlacedSupportClearTheSameCeilingCells()
        {
            using var fixture = new StructuralFixture(11, true);
            fixture.AddSideAnchors(5);
            fixture.Mine(0f);
            var center = fixture.Foreground.GetCellCenterWorld(Vector3Int.zero);
            var supportObject = new GameObject("Support");
            supportObject.transform.SetParent(fixture.Root.transform);
            supportObject.transform.position = center;
            StructuralSupport support = supportObject.AddComponent<StructuralSupport>();
            SetField(support, "radius", 2f);
            var left = new Vector3Int(-2, 2, 0);
            Assert.That(support.Supports(fixture.Foreground.GetCellCenterWorld(left)), Is.False);

            var expectedCleared = new List<Vector3Int>();
            for (int x = -2; x <= 2; x++)
            {
                var cell = new Vector3Int(x, 2, 0);
                Assert.That(fixture.System.HasRiskAtCell(cell), Is.True);
                if (fixture.System.WouldSupportClearCeilingRisk(cell, center, support.Radius))
                    expectedCleared.Add(cell);
            }

            Assert.That(expectedCleared, Has.Count.EqualTo(5));
            fixture.System.RegisterSupport(support);
            foreach (Vector3Int cell in expectedCleared)
                Assert.That(fixture.System.HasRiskAtCell(cell), Is.False, cell.ToString());
        }

        [Test]
        public void RebuildRiskFromMinedCells_RestoresRiskWithoutCollapse()
        {
            // prompt-B 36-1: 세이브 복원처럼 런타임 상태를 비운 뒤 채굴 셀만으로 위험을 재구성한다.
            using var fixture = new StructuralFixture(1, true);
            var mineCell = new Vector3Int(0, 1, 0);
            var ceiling = new Vector3Int(0, 2, 0);

            fixture.System.RebuildRiskFromMinedCells(new[] { mineCell });
            Assert.That(fixture.System.CurrentRisk, Is.EqualTo(StructuralRiskLevel.Danger));
            Assert.That(fixture.Overlay.HasTile(ceiling), Is.True);

            fixture.System.ClearRuntimeRiskState();
            Assert.That(fixture.System.CurrentRisk, Is.EqualTo(StructuralRiskLevel.Stable));
            Assert.That(fixture.Overlay.HasTile(ceiling), Is.False);

            StructuralCollapseEventDto collapse = null;
            fixture.System.CollapseTriggered += value => collapse = value;
            fixture.System.RebuildRiskFromMinedCells(new[] { mineCell });

            Assert.That(collapse, Is.Null);
            Assert.That(fixture.System.CurrentRisk, Is.EqualTo(StructuralRiskLevel.Danger));
            Assert.That(fixture.Overlay.HasTile(ceiling), Is.True);
            Assert.That(fixture.System.EvaluateAt(mineCell), Is.EqualTo(StructuralRiskLevel.Danger));
            fixture.System.AdvanceSimulation(2f);
            Assert.That(collapse, Is.Null, "복원 재계산은 시간이 지나도 예고/붕괴를 만들면 안 됩니다.");
        }

        [Test]
        public void Collapse_TelegraphsBeforeRemovingTile_AndSupportCancelsIt()
        {
            using var fixture = new StructuralFixture(11, true);
            fixture.AddSideAnchors(5);
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
        public void RemovingSupport_RestoresSpanRiskWithSupportLossCause()
        {
            using var fixture = new StructuralFixture(11, true);
            fixture.AddSideAnchors(5);
            fixture.Mine(0.3f);

            GameObject supportObject = new("RemovableSupport");
            supportObject.transform.SetParent(fixture.Root.transform);
            supportObject.transform.position = fixture.Foreground.GetCellCenterWorld(Vector3Int.zero);
            StructuralSupport support = supportObject.AddComponent<StructuralSupport>();
            fixture.System.RegisterSupport(support);
            Assert.That(fixture.System.CurrentRisk, Is.EqualTo(StructuralRiskLevel.Stable));

            fixture.System.UnregisterSupport(support);
            StructuralRiskStatus restored = fixture.System.EvaluateStatusAt(Vector3Int.zero);

            Assert.That(restored.Level, Is.EqualTo(StructuralRiskLevel.CollapseImminent));
            Assert.That(restored.Cause, Is.EqualTo(StructuralRiskCause.SupportRemoved));
        }

        [Test]
        public void PromptB80_RemovingUpperBlock_ForcesIsolatedLowerBlockToCollapse()
        {
            using var fixture = new StructuralFixture(1, true);
            var minedCell = new Vector3Int(0, 2, 0);
            var isolatedCell = minedCell + Vector3Int.down;
            fixture.PlaceTile(isolatedCell);
            StructuralCollapseEventDto emitted = null;
            fixture.System.CollapseTriggered += value => emitted = value;

            fixture.RemoveAndNotifyMined(minedCell, 0f);

            Assert.That(fixture.Foreground.HasTile(isolatedCell), Is.True, "붕괴 예고 중에는 지형을 유지해야 합니다.");
            Assert.That(fixture.System.CurrentRisk, Is.EqualTo(StructuralRiskLevel.CollapseImminent));
            fixture.System.AdvanceSimulation(2f);

            Assert.That(fixture.Foreground.HasTile(isolatedCell), Is.False);
            Assert.That(emitted, Is.Not.Null);
            Assert.That(emitted.cells.Exists(cell => cell.x == isolatedCell.x && cell.y == isolatedCell.y), Is.True);
        }

        [Test]
        public void PromptB80_RemovingLowerBlock_ForcesIsolatedUpperBlockToCollapse()
        {
            using var fixture = new StructuralFixture(1, true);
            var minedCell = new Vector3Int(0, 2, 0);
            var isolatedCell = minedCell + Vector3Int.up;
            fixture.PlaceTile(isolatedCell);
            StructuralCollapseEventDto emitted = null;
            fixture.System.CollapseTriggered += value => emitted = value;

            fixture.RemoveAndNotifyMined(minedCell, 0f);
            fixture.System.AdvanceSimulation(2f);

            Assert.That(fixture.Foreground.HasTile(isolatedCell), Is.False);
            Assert.That(emitted, Is.Not.Null);
            Assert.That(emitted.cells.Exists(cell => cell.x == isolatedCell.x && cell.y == isolatedCell.y), Is.True);
        }

        [Test]
        public void PromptB80_ProtectedIsolatedBlock_DoesNotCollapse()
        {
            using var fixture = new StructuralFixture(1, true);
            var minedCell = new Vector3Int(0, 2, 0);
            var isolatedCell = minedCell + Vector3Int.down;
            fixture.PlaceTile(isolatedCell);
            fixture.System.RegisterProtectedCell(isolatedCell);
            StructuralCollapseEventDto emitted = null;
            fixture.System.CollapseTriggered += value => emitted = value;

            fixture.RemoveAndNotifyMined(minedCell, 0f);
            fixture.System.AdvanceSimulation(2f);

            Assert.That(fixture.Foreground.HasTile(isolatedCell), Is.True);
            Assert.That(emitted, Is.Null);
        }

        [TestCase(-1, false)]
        [TestCase(1, false)]
        [TestCase(1, true)]
        public void Issue281_RemovingLastSideNeighbor_OnlyDropsUnprotectedBlock(int side, bool isProtected)
        {
            using var fixture = new StructuralFixture(1, true);
            var isolatedCell = new Vector3Int(0, 2, 0);
            var minedCell = isolatedCell + new Vector3Int(side, 0, 0);
            fixture.PlaceTile(minedCell);
            if (isProtected) fixture.System.RegisterProtectedCell(isolatedCell);
            StructuralCollapseEventDto emitted = null;
            fixture.System.CollapseTriggered += value => emitted = value;

            fixture.RemoveAndNotifyMined(minedCell, 0f);

            Assert.That(fixture.Foreground.HasTile(isolatedCell), Is.True, "Keep the tile during the warning.");
            if (!isProtected)
                Assert.That(fixture.System.CurrentRisk, Is.EqualTo(StructuralRiskLevel.CollapseImminent));
            fixture.System.AdvanceSimulation(2f);

            Assert.That(fixture.Foreground.HasTile(isolatedCell), Is.EqualTo(isProtected));
            if (isProtected)
            {
                Assert.That(emitted, Is.Null);
            }
            else
            {
                Assert.That(emitted, Is.Not.Null);
                Assert.That(emitted.cells, Has.Count.EqualTo(1));
                Assert.That(emitted.cells[0].x, Is.EqualTo(isolatedCell.x));
                Assert.That(emitted.cells[0].y, Is.EqualTo(isolatedCell.y));
            }
        }

        [Test]
        public void Issue281_RemovingUpperNeighbor_DoesNotForceBlockAttachedToSideWallToDrop()
        {
            using var fixture = new StructuralFixture(1, true);
            var attachedCell = new Vector3Int(0, 2, 0);
            var minedCell = attachedCell + Vector3Int.up;
            fixture.PlaceTile(minedCell);
            fixture.PlaceTile(attachedCell + Vector3Int.right);
            fixture.PlaceTile(attachedCell + Vector3Int.right + Vector3Int.down);
            fixture.System.RegisterProtectedCell(attachedCell + Vector3Int.right + Vector3Int.down);

            fixture.RemoveAndNotifyMined(minedCell, 0f);
            fixture.System.AdvanceSimulation(2f);

            Assert.That(fixture.Foreground.HasTile(attachedCell), Is.True);
        }

        [TestCase(-1, 0, false)]
        [TestCase(1, 0, false)]
        [TestCase(0, 1, false)]
        [TestCase(1, 0, true)]
        public void Issue281_FirstCollapse_TelegraphsNewlyIsolatedBlockUnlessSupported(
            int offsetX, int offsetY, bool isProtected)
        {
            using var fixture = new StructuralFixture(1, true);
            var firstCell = new Vector3Int(0, 2, 0);
            var nextCell = firstCell + new Vector3Int(offsetX, offsetY, 0);
            fixture.PlaceTile(nextCell);
            SetField(fixture.System, "maximumCollapseTiles", 1);
            if (isProtected) fixture.System.RegisterProtectedCell(nextCell);
            var collapses = new List<StructuralCollapseEventDto>();
            fixture.System.CollapseTriggered += collapses.Add;

            var supportObject = new GameObject("Support");
            supportObject.transform.SetParent(fixture.Root.transform);
            supportObject.transform.position = fixture.Foreground.GetCellCenterWorld(Vector3Int.zero);
            StructuralSupport support = supportObject.AddComponent<StructuralSupport>();
            fixture.System.RegisterSupport(support);
            fixture.System.UnregisterSupport(support);
            fixture.System.AdvanceSimulation(2f);

            Assert.That(fixture.Foreground.HasTile(firstCell), Is.EqualTo(isProtected));
            Assert.That(fixture.Foreground.HasTile(nextCell), Is.True, "Keep the next tile during its warning.");
            Assert.That(collapses, Has.Count.EqualTo(isProtected ? 0 : 1));
            if (!isProtected)
            {
                Assert.That(collapses[0].cells, Has.Count.EqualTo(1));
                Assert.That(fixture.System.CurrentRisk, Is.EqualTo(StructuralRiskLevel.CollapseImminent));
            }

            fixture.System.AdvanceSimulation(2f);

            Assert.That(fixture.Foreground.HasTile(nextCell), Is.EqualTo(isProtected));
            Assert.That(collapses, Has.Count.EqualTo(isProtected ? 0 : 2));
            if (!isProtected)
            {
                Assert.That(collapses[1].cells, Has.Count.EqualTo(1));
                Assert.That(collapses[1].cells[0].x, Is.EqualTo(nextCell.x));
                Assert.That(collapses[1].cells[0].y, Is.EqualTo(nextCell.y));
            }
        }

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        public void Issue281_MiningDisconnectsVerticalGroup_OnlyDropsWhenUnsupported(
            bool protectTop, bool restoreAnchorDuringWarning)
        {
            using var fixture = new StructuralFixture(1, true);
            var bottom = new Vector3Int(0, 2, 0);
            var top = bottom + Vector3Int.up;
            var mined = bottom + Vector3Int.left;
            fixture.PlaceTile(top);
            fixture.PlaceTile(mined);
            if (protectTop) fixture.System.RegisterProtectedCell(top);
            StructuralCollapseEventDto collapse = null;
            fixture.System.CollapseTriggered += value => collapse = value;

            fixture.RemoveAndNotifyMined(mined, 0f);
            Assert.That(fixture.Foreground.HasTile(bottom), Is.True);
            Assert.That(fixture.Foreground.HasTile(top), Is.True);
            if (restoreAnchorDuringWarning)
            {
                fixture.PlaceTile(mined);
                fixture.System.RegisterProtectedCell(mined);
            }
            fixture.System.AdvanceSimulation(2f);

            bool supported = protectTop || restoreAnchorDuringWarning;
            Assert.That(fixture.Foreground.HasTile(bottom), Is.EqualTo(supported));
            Assert.That(fixture.Foreground.HasTile(top), Is.EqualTo(supported));
            if (supported) Assert.That(collapse, Is.Null);
            else Assert.That(collapse.cells, Has.Count.EqualTo(2));
        }

        private static List<CollapseCellDto> RunCollapse(long seed)
        {
            using var fixture = new StructuralFixture(13);
            fixture.AddSideAnchors(6);
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
            private StructuralRiskSettings probabilitySettings;
            private Camera previousMainCamera;

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

            public StructuralRiskSettings EnableProbability()
            {
                previousMainCamera = Camera.main;
                if (previousMainCamera != null) previousMainCamera.enabled = false;
                var cameraObject = new GameObject("MainCamera", typeof(Camera)) { tag = "MainCamera" };
                cameraObject.transform.SetParent(Root.transform);
                cameraObject.transform.position = new Vector3(0f, 2f, -10f);
                var camera = cameraObject.GetComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 8f;
                Assert.That(Camera.main, Is.SameAs(camera));
                probabilitySettings = ScriptableObject.CreateInstance<StructuralRiskSettings>();
                SetField(System, "riskSettings", probabilitySettings);
                return probabilitySettings;
            }

            public void Mine(float structuralImpact)
            {
                System.NotifyTileMined(
                    new Vector3Int(0, 1, 0),
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

            public void PlaceTile(Vector3Int cell)
            {
                Foreground.SetTile(cell, tile);
            }

            public void AddSideAnchors(int halfWidth)
            {
                var left = new Vector3Int(-halfWidth, 1, 0);
                var right = new Vector3Int(halfWidth, 1, 0);
                PlaceTile(left);
                PlaceTile(right);
                System.RegisterProtectedCell(left);
                System.RegisterProtectedCell(right);
            }

            public void RemoveAndNotifyMined(Vector3Int cell, float structuralImpact)
            {
                Foreground.SetTile(cell, null);
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
                if (probabilitySettings != null) UnityEngine.Object.DestroyImmediate(probabilitySettings);
                if (previousMainCamera != null) previousMainCamera.enabled = true;
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
