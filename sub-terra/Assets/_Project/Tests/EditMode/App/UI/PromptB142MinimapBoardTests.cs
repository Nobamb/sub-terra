using NUnit.Framework;
using SubTerra.App.Integration;
using UnityEngine;

namespace SubTerra.App.Tests.UI
{
    /// <summary>B-142 광산 관측 전광판의 순수 계산(레이아웃·창·상태·연출·시설 점유) 계약.</summary>
    public sealed class PromptB142MinimapBoardTests
    {
        // ── 레이아웃·비율 ──

        [Test]
        public void Frame_WideIsTwoToOne_SquareIsOneToOne_SameHeight()
        {
            Vector2 wide = MinimapBoardLayout.FrameSize(MinimapBoardMode.Wide);
            Vector2 square = MinimapBoardLayout.FrameSize(MinimapBoardMode.Square);
            Assert.That(wide.x / wide.y, Is.EqualTo(2f).Within(0.0001f), "가로형 외곽(헤더·푸터 포함) 가로:세로 = 2:1");
            Assert.That(square.x / square.y, Is.EqualTo(1f).Within(0.0001f), "정사각형 외곽 1:1");
            Assert.That(square.y, Is.EqualTo(wide.y), "정사각형은 같은 높이에서 폭만 절반");
            Assert.That(square.x, Is.EqualTo(wide.x * 0.5f));
            Assert.That(square.x * square.y, Is.LessThan(wide.x * wide.y), "면적도 작아진다");
            Assert.That(MinimapBoardLayout.FrameWidth(1f), Is.EqualTo(wide.x));
            Assert.That(MinimapBoardLayout.FrameWidth(0f), Is.EqualTo(square.x));
        }

        [Test]
        public void Viewport_KeepsSameHeightInBothModes_AndStaysInsideHeaderFooter()
        {
            Rect wide = MinimapBoardLayout.ViewportRect(MinimapBoardLayout.WideWidth);
            Rect square = MinimapBoardLayout.ViewportRect(MinimapBoardLayout.SquareWidth);
            Assert.That(square.height, Is.EqualTo(wide.height));
            Assert.That(square.width, Is.LessThan(wide.width));
            Assert.That(wide.yMin, Is.GreaterThanOrEqualTo(MinimapBoardLayout.FooterHeight));
            Assert.That(wide.yMax, Is.LessThanOrEqualTo(MinimapBoardLayout.FrameHeight - MinimapBoardLayout.HeaderHeight));
        }

        [Test]
        public void FitScale_IsUniformAndOnlyShrinksOnSmallScreens()
        {
            Assert.That(MinimapBoardLayout.FitScale(new Vector2(1920f, 1080f)), Is.EqualTo(1f));
            float small = MinimapBoardLayout.FitScale(new Vector2(560f, 400f));
            Assert.That(small, Is.LessThan(1f));
            float reserve = (MinimapBoardLayout.ScreenMargin + MinimapBoardLayout.GlowMargin) * 2f;
            Assert.That(MinimapBoardLayout.WideWidth * small, Is.LessThanOrEqualTo(560f - reserve + 0.01f), "가로형도 화면 안에 들어간다");
            Assert.That(MinimapBoardLayout.FitScale(Vector2.zero), Is.EqualTo(1f));
        }

        [Test]
        public void CellPixels_NeverShowsWiderOrTallerThanCamera()
        {
            Vector2 viewport = MinimapBoardLayout.WideViewportSize;
            var camera = new Vector2(17.78f, 10f);
            float px = MinimapBoardLayout.CellPixels(viewport, camera);
            Assert.That(viewport.x / px, Is.LessThanOrEqualTo(camera.x), "카메라 밖 열을 새로 공개하지 않는다");
            Assert.That(viewport.y / px, Is.LessThanOrEqualTo(camera.y), "카메라 밖 행을 새로 공개하지 않는다");
            Assert.That(px, Is.EqualTo(Mathf.Round(px)), "정수 픽셀 셀");
            Assert.That(MinimapBoardLayout.CellPixels(viewport, Vector2.zero), Is.Zero);
        }

        // ── 창·좌표 변환 ──

        [Test]
        public void Window_CentersOnFocus_ClampsInsideCamera_AndSquareKeepsCellSize()
        {
            var camera = Rect.MinMaxRect(-9f, -5f, 9f, 5f);
            Vector2 wideViewport = MinimapBoardLayout.WideViewportSize;
            float px = MinimapBoardLayout.CellPixels(wideViewport, camera.size);
            var wide = MinimapBoardLayout.ComputeWindow(wideViewport, px, new Vector2(0f, 0f), camera);
            Vector2 squareViewport = MinimapBoardLayout.ViewportRect(MinimapBoardLayout.SquareWidth).size;
            var square = MinimapBoardLayout.ComputeWindow(squareViewport, px, new Vector2(0f, 0f), camera);
            Assert.That(square.CellPixels, Is.EqualTo(wide.CellPixels), "두 모드 셀 크기 동일");
            Assert.That(square.Size.y, Is.EqualTo(wide.Size.y).Within(0.001f));
            Assert.That(square.Size.x, Is.LessThan(wide.Size.x), "정사각형은 보이는 가로 범위만 줄어든다");
            Assert.That(square.Min.x + square.Size.x * 0.5f, Is.EqualTo(wide.Min.x + wide.Size.x * 0.5f).Within(1f / px),
                "같은 월드 위치를 중심으로 이어진다");

            var edge = MinimapBoardLayout.ComputeWindow(wideViewport, px, new Vector2(8.5f, 4.9f), camera);
            Assert.That(edge.Max.x, Is.LessThanOrEqualTo(camera.xMax + 0.0001f));
            Assert.That(edge.Max.y, Is.LessThanOrEqualTo(camera.yMax + 0.0001f));
            Assert.That(edge.Min.x, Is.GreaterThanOrEqualTo(camera.xMin - 0.0001f));
        }

        [Test]
        public void Window_CellToViewportAndContent_AreConsistentSquareCells()
        {
            var window = new MinimapWindow(new Vector2(2.25f, -3.5f), new Vector2(10f, 6f), 20f);
            Assert.That(window.BaseCell, Is.EqualTo(new Vector2Int(2, -4)));
            Assert.That(window.ContentOffset, Is.EqualTo(new Vector2(-5f, -10f)));
            Assert.That(window.CellCount, Is.EqualTo(new Vector2Int(11, 7)));
            Assert.That(window.CellToViewport(new Vector2(3f, -3f)), Is.EqualTo(new Vector2(15f, 10f)));
            // 콘텐츠 픽셀 + 오프셋 = 뷰포트 픽셀.
            Rect cell = window.CellRectToContent(new Rect(3f, -3f, 1f, 1f));
            Assert.That(cell.position + window.ContentOffset, Is.EqualTo(window.CellToViewport(new Vector2(3f, -3f))));
            Assert.That(cell.width, Is.EqualTo(cell.height), "셀 가로·세로 1:1");
            Assert.That(window.Overlaps(new Rect(12.5f, 0f, 1f, 1f)), Is.False);
            Assert.That(window.Overlaps(new Rect(12f, 0f, 1f, 1f)), Is.True, "경계에 걸친 시설은 그려서 마스크로 자른다");
        }

        [Test]
        public void Legend_FitsSquareFooter_AndSpreadsInWide()
        {
            var widths = new[] { 26f, 39f, 39f, 65f };
            var x = new float[4];
            float squareAvailable = MinimapBoardLayout.SquareWidth - 90f;
            var small = new[] { 22f, 33f, 33f, 55f };
            Assert.That(MinimapBoardLayout.LayoutLegend(squareAvailable, widths, 13f, 5f, 8f, 28f, x), Is.False,
                "기본 크기는 정사각형 푸터에 넘친다");
            float scale = MinimapBoardLayout.LegendScale(squareAvailable, small, 11f, 3f, 5f);
            Assert.That(scale, Is.InRange(0.6f, 1f));
            var scaled = new float[4];
            for (int i = 0; i < 4; i++) scaled[i] = small[i] * scale;
            Assert.That(MinimapBoardLayout.LayoutLegend(squareAvailable, scaled, 11f * scale, 3f * scale, 5f * scale, 8f, x), Is.True,
                "정사각형에서도 범례가 넘치지 않는다");
            Assert.That(MinimapBoardLayout.LegendScale(1000f, small, 11f, 3f, 5f), Is.EqualTo(1f), "공간이 충분하면 줄이지 않는다");
            Assert.That(MinimapBoardLayout.LayoutLegend(MinimapBoardLayout.WideWidth - 90f, widths, 13f, 5f, 8f, 28f, x), Is.True);
            for (int i = 1; i < x.Length; i++) Assert.That(x[i], Is.GreaterThan(x[i - 1]));
        }

        // ── 상태·연출 ──

        [Test]
        public void Press_CyclesClosedWideSquareClosed_OneStepEach()
        {
            var timeline = new MinimapBoardTimeline();
            Assert.That(timeline.Mode, Is.EqualTo(MinimapBoardMode.Closed), "초기 표시는 닫힘");
            Assert.That(timeline.IsVisible, Is.False);
            Assert.That(timeline.Press(), Is.EqualTo(MinimapBoardMode.Wide));
            Assert.That(timeline.Press(), Is.EqualTo(MinimapBoardMode.Square));
            Assert.That(timeline.Press(), Is.EqualTo(MinimapBoardMode.Closed));
            Assert.That(timeline.Press(), Is.EqualTo(MinimapBoardMode.Wide), "닫힌 뒤 M은 다시 가로형");
        }

        [Test]
        public void Timing_OpenShrinkClose_SettleExactlyAtDurations()
        {
            var timeline = new MinimapBoardTimeline();
            timeline.Press();
            timeline.Advance(MinimapBoardTimeline.OpenDuration - 0.01f);
            Assert.That(timeline.IsTransitioning, Is.True);
            timeline.Advance(0.02f);
            Assert.That(timeline.IsTransitioning, Is.False);
            AssertPose(timeline.Pose, 1f, 1f, 1f, 0f);

            timeline.Press();
            timeline.Advance(MinimapBoardTimeline.ShrinkDuration * 0.5f);
            float mid = timeline.Pose.Width;
            Assert.That(mid, Is.InRange(0.01f, 0.99f));
            Assert.That(mid, Is.LessThan(0.5f), "ease-out: 절반 시점에 이미 절반 이상 줄었다");
            timeline.Advance(MinimapBoardTimeline.ShrinkDuration);
            AssertPose(timeline.Pose, 1f, 0f, 1f, 0f);

            timeline.Press();
            timeline.Advance(MinimapBoardTimeline.CloseDuration * 0.5f);
            Assert.That(timeline.Pose.Line, Is.GreaterThan(0f), "닫힘 중간에는 수평선 빛이 남는다");
            Assert.That(timeline.Pose.Open, Is.LessThan(1f));
            timeline.Advance(MinimapBoardTimeline.CloseDuration);
            Assert.That(timeline.Pose.Open, Is.Zero);
            Assert.That(timeline.Pose.Line, Is.Zero, "마지막 빛까지 제거");
            Assert.That(timeline.Pose.Frame + timeline.Pose.Terrain + timeline.Pose.Markers, Is.Zero);
            Assert.That(timeline.IsVisible, Is.False);
        }

        [Test]
        public void Opening_LightsFrameThenTerrainThenMarkers_WithCentreBeam()
        {
            var from = MinimapBoardPose.Closed(1f);
            var early = MinimapBoardTimeline.Evaluate(from, MinimapBoardMode.Wide, 0.3f);
            Assert.That(early.Line, Is.GreaterThan(0.5f), "중앙의 얇은 빛");
            Assert.That(early.Frame, Is.GreaterThan(early.Terrain));
            Assert.That(early.Terrain, Is.GreaterThanOrEqualTo(early.Markers));
            var late = MinimapBoardTimeline.Evaluate(from, MinimapBoardMode.Wide, 0.7f);
            Assert.That(late.Terrain, Is.GreaterThan(late.Markers));
            Assert.That(late.Open, Is.GreaterThan(early.Open), "위아래로 펼쳐진다");
        }

        [Test]
        public void Interrupt_ContinuesFromCurrentPose_ToNextLogicalState()
        {
            var timeline = new MinimapBoardTimeline();
            timeline.Press();
            timeline.Advance(MinimapBoardTimeline.OpenDuration * 0.4f);
            MinimapBoardPose before = timeline.Pose;
            Assert.That(timeline.Press(), Is.EqualTo(MinimapBoardMode.Square), "전환 중 재입력은 다음 논리 상태");
            timeline.Advance(0.0001f);
            Assert.That(timeline.Pose.Open, Is.EqualTo(before.Open).Within(0.01f), "현재 개방에서 이어간다");
            Assert.That(timeline.Pose.Width, Is.EqualTo(before.Width).Within(0.01f), "현재 폭에서 이어간다");
            timeline.Advance(1f);
            AssertPose(timeline.Pose, 1f, 0f, 1f, 0f);

            // 빠른 연타: 세 번 = 닫힘. 최종값은 정확히 원복된다.
            timeline.Press();
            timeline.Press();
            timeline.Press();
            Assert.That(timeline.Mode, Is.EqualTo(MinimapBoardMode.Square));
            timeline.Press();
            timeline.Advance(1f);
            Assert.That(timeline.Mode, Is.EqualTo(MinimapBoardMode.Closed));
            Assert.That(timeline.IsVisible, Is.False);
        }

        [Test]
        public void Reopen_AfterSquareClose_StartsAtWideWidthWithoutSquash()
        {
            var timeline = new MinimapBoardTimeline();
            for (int i = 0; i < 3; i++)
            {
                timeline.Press();
                timeline.Advance(1f);
            }

            Assert.That(timeline.Mode, Is.EqualTo(MinimapBoardMode.Closed));
            Assert.That(timeline.Pose.Width, Is.EqualTo(0f), "정사각형 폭을 유지한 채 소등(가로로 다시 늘이지 않는다)");
            timeline.Press();
            timeline.Advance(0.0001f);
            Assert.That(timeline.Pose.Width, Is.EqualTo(1f).Within(0.001f), "보이지 않는 상태에서 가로형 폭으로 다시 연다");
        }

        [Test]
        public void DecorClock_UsesOnlyGivenUnscaledDelta_AndStopsWhileHidden()
        {
            // 타임라인은 Time을 읽지 않는다. View가 Time.unscaledDeltaTime만 넘기므로 timeScale 0에서도 진행한다.
            var timeline = new MinimapBoardTimeline();
            timeline.Advance(5f);
            Assert.That(timeline.DisplayClock, Is.Zero, "숨김 중 장식 애니메이션 정지");
            timeline.Press();
            timeline.Advance(MinimapBoardTimeline.ScanPeriod * 0.5f);
            Assert.That(timeline.Pose.Open, Is.EqualTo(1f));
            Assert.That(timeline.ScanPhase, Is.InRange(0.45f, 0.55f), "약 3초 주기 스캔선");
            timeline.Advance(MinimapBoardTimeline.ScanPeriod * 0.5f);
            Assert.That(timeline.ScanPhase, Is.LessThan(0.05f).Or.GreaterThan(0.95f));
            Assert.That(timeline.RingPulse, Is.InRange(0f, 1f));
            timeline.Advance(-1f);
            Assert.That(timeline.DisplayClock, Is.EqualTo(MinimapBoardTimeline.ScanPeriod).Within(0.0001f), "음수 dt는 무시");
            timeline.ForceClosed();
            Assert.That(timeline.IsVisible, Is.False);
            Assert.That(timeline.DisplayClock, Is.Zero);
        }

        // ── 시설 점유·종류·중복·연출 ──

        [TestCase(1, 1)]
        [TestCase(2, 2)]
        [TestCase(1, 2)]
        [TestCase(2, 1)]
        [TestCase(3, 2)]
        public void Footprint_UsesOriginBottomLeftAndActualSize(int width, int height)
        {
            Rect cells = MinimapFacilityRegistry.FootprintCells(4, -7, width, height);
            Assert.That(cells.xMin, Is.EqualTo(4f));
            Assert.That(cells.yMin, Is.EqualTo(-7f), "시설 바닥 = 원점 셀 하단 = 아래 블록 윗면");
            Assert.That(cells.width, Is.EqualTo(width));
            Assert.That(cells.height, Is.EqualTo(height));

            // BuildingPlacementSystem의 중심 배치 규칙에서 크기를 다시 구한다.
            var center = new Vector2(4f + 0.5f + (width - 1) * 0.5f, -7f + 0.5f + (height - 1) * 0.5f);
            Assert.That(MinimapFacilityRegistry.TryDeriveFootprint(center, new Vector2Int(4, -7), out var derived), Is.True);
            Assert.That(derived, Is.EqualTo(new Vector2Int(width, height)));
        }

        [Test]
        public void Footprint_InvalidSizesFallBackToOneCell()
        {
            Assert.That(MinimapFacilityRegistry.FootprintCells(0, 0, 0, 0).size, Is.EqualTo(Vector2.one));
            Assert.That(MinimapFacilityRegistry.TryDeriveFootprint(new Vector2(-10f, 0.5f), Vector2Int.zero, out var size), Is.False);
            Assert.That(size, Is.EqualTo(Vector2Int.one));
        }

        [TestCase("building.outpost_core.basic", MinimapFacilityKind.Core)]
        [TestCase("building.charger.basic", MinimapFacilityKind.Charger)]
        [TestCase("building.clinic.basic", MinimapFacilityKind.Clinic)]
        [TestCase("elevator", MinimapFacilityKind.Elevator)]
        [TestCase("building.ladder.basic", MinimapFacilityKind.Ladder)]
        [TestCase("building.support.basic", MinimapFacilityKind.Support)]
        [TestCase("building.storage.basic", MinimapFacilityKind.Generic)]
        [TestCase(null, MinimapFacilityKind.Generic)]
        public void Kind_ClassifiesStableIds(string id, MinimapFacilityKind expected)
        {
            Assert.That(MinimapFacilityRegistry.Classify(id), Is.EqualTo(expected));
        }

        [Test]
        public void Kinds_DifferInShapeAndColour()
        {
            var kinds = new[] { MinimapFacilityKind.Core, MinimapFacilityKind.Charger, MinimapFacilityKind.Clinic, MinimapFacilityKind.Elevator };
            for (int a = 0; a < kinds.Length; a++)
            for (int b = a + 1; b < kinds.Length; b++)
            {
                Assert.That(MinimapPalette.GlyphForKind(kinds[a]), Is.Not.EqualTo(MinimapPalette.GlyphForKind(kinds[b])));
                Assert.That(MinimapPalette.ForKind(kinds[a]), Is.Not.EqualTo(MinimapPalette.ForKind(kinds[b])));
            }
        }

        [Test]
        public void Registry_UpsertsOncePerId_MovesInPlace_AndRemoves()
        {
            var registry = new MinimapFacilityRegistry();
            registry.Upsert("core-1", "building.outpost_core.basic", MinimapFacilityRegistry.FootprintCells(0, 0, 2, 2), false, 0f);
            registry.Upsert("core-1", "building.outpost_core.basic", MinimapFacilityRegistry.FootprintCells(3, 0, 2, 2), false, 0f);
            Assert.That(registry.Count, Is.EqualTo(1), "다중 셀 시설도 ID당 한 번");
            Assert.That(registry.Records[0].Cells.x, Is.EqualTo(3f), "이동은 갱신");
            int version = registry.Version;
            Assert.That(registry.SetActive("core-1", false), Is.True);
            Assert.That(registry.SetActive("core-1", false), Is.False, "같은 상태는 다시 그리지 않는다");
            Assert.That(registry.Version, Is.EqualTo(version + 1));
            Assert.That(registry.Remove("core-1"), Is.True);
            Assert.That(registry.Count, Is.Zero);
        }

        [Test]
        public void Registry_ClearBuildings_KeepsElevators()
        {
            var registry = new MinimapFacilityRegistry();
            registry.Upsert("elevator:0", "elevator", new Rect(0f, 0f, 2f, 3f), false, 0f);
            registry.Upsert("c-1", "building.charger.basic", MinimapFacilityRegistry.FootprintCells(5, 0, 1, 1), false, 0f);
            registry.ClearBuildings();
            Assert.That(registry.Count, Is.EqualTo(1));
            Assert.That(registry.Records[0].Kind, Is.EqualTo(MinimapFacilityKind.Elevator));
        }

        [Test]
        public void BuildFlash_RunsOnlyOncePerNewlyBuiltFacility()
        {
            var registry = new MinimapFacilityRegistry();
            var record = registry.Upsert("c-1", "building.charger.basic", MinimapFacilityRegistry.FootprintCells(0, 0, 1, 1), true, 10f);
            Assert.That(MinimapFacilityRegistry.FlashProgress(record, 10.1f), Is.InRange(0f, 1f));
            Assert.That(MinimapFacilityRegistry.FlashProgress(record, 10f + MinimapFacilityRegistry.BuildFlashDuration), Is.EqualTo(-1f));
            registry.Upsert("c-1", "building.charger.basic", MinimapFacilityRegistry.FootprintCells(0, 0, 1, 1), true, 20f);
            Assert.That(MinimapFacilityRegistry.FlashProgress(record, 20.1f), Is.EqualTo(-1f), "재등록·재열기에서 반복하지 않는다");
            var restored = registry.Upsert("c-2", "building.charger.basic", MinimapFacilityRegistry.FootprintCells(2, 0, 1, 1), false, 30f);
            Assert.That(MinimapFacilityRegistry.FlashProgress(restored, 30.1f), Is.EqualTo(-1f), "복원 시설은 건설 연출 없음");
        }

        [Test]
        public void MinedFlashes_AreShortAndBounded()
        {
            var flashes = new MinimapMinedFlashes();
            for (int i = 0; i < MinimapMinedFlashes.Capacity + 5; i++) flashes.Add(new Vector2Int(i, 0), 1f);
            Assert.That(flashes.Count, Is.EqualTo(MinimapMinedFlashes.Capacity));
            Assert.That(flashes.AnyActive(1.1f), Is.True);
            Assert.That(flashes.AnyActive(1f + MinimapMinedFlashes.Duration), Is.False);
            flashes.Clear();
            Assert.That(flashes.Count, Is.Zero);
        }

        [Test]
        public void TerrainClassifier_SeparatesBlockEmptyAndUnobserved()
        {
            Assert.That(MinimapTerrainClassifier.Classify(true, true), Is.EqualTo(MinimapCellKind.Block));
            Assert.That(MinimapTerrainClassifier.Classify(true, false), Is.EqualTo(MinimapCellKind.Empty));
            Assert.That(MinimapTerrainClassifier.Classify(false, false), Is.EqualTo(MinimapCellKind.Void));
        }

        private static void AssertPose(MinimapBoardPose pose, float open, float width, float alpha, float line)
        {
            Assert.That(pose.Open, Is.EqualTo(open));
            Assert.That(pose.Width, Is.EqualTo(width));
            Assert.That(pose.Frame, Is.EqualTo(alpha));
            Assert.That(pose.Terrain, Is.EqualTo(alpha));
            Assert.That(pose.Markers, Is.EqualTo(alpha));
            Assert.That(pose.Line, Is.EqualTo(line));
        }
    }
}
